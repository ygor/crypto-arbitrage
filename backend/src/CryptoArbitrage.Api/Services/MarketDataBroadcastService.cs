using CryptoArbitrage.Api.Hubs;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CryptoArbitrage.Api.Services;

/// <summary>
/// Configuration options for the crypto arbitrage system.
/// </summary>
public class CryptoArbitrageOptions
{
    public string[] DefaultExchanges { get; set; } = Array.Empty<string>();
    public string[] DefaultTradingPairs { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Background service that monitors market data and broadcasts price updates via SignalR.
/// </summary>
public class MarketDataBroadcastService : BackgroundService
{
    private readonly ILogger<MarketDataBroadcastService> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CryptoArbitrageOptions _options;
    private readonly Dictionary<TradingPair, (PriceQuote? BestBid, PriceQuote? BestAsk)> _lastBestBidAsk;
    private readonly TimeSpan _broadcastInterval = TimeSpan.FromMilliseconds(500); // 2Hz updates
    private bool _defaultTradingPairsInitialized = false;

    public MarketDataBroadcastService(
        ILogger<MarketDataBroadcastService> logger,
        IMarketDataService marketDataService,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CryptoArbitrageOptions> options)
    {
        _logger = logger;
        _marketDataService = marketDataService;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _lastBestBidAsk = new Dictionary<TradingPair, (PriceQuote?, PriceQuote?)>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market data broadcast service starting");
        
        try
        {
            await BroadcastMarketDataAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Market data broadcast service canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market data broadcast service encountered an error");
        }
        finally
        {
            _logger.LogInformation("Market data broadcast service stopped");
        }
    }

    private async Task BroadcastMarketDataAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_broadcastInterval);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Initialize default trading pairs if not done yet
                if (!_defaultTradingPairsInitialized)
                {
                    await InitializeDefaultTradingPairsAsync(stoppingToken);
                    _defaultTradingPairsInitialized = true;
                }

                // Get all active trading pairs
                var activeTradingPairs = _marketDataService.GetActiveTradingPairs();
                
                if (activeTradingPairs == null || !activeTradingPairs.Any())
                {
                    continue;
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MarketDataHub>>();

                foreach (var tradingPair in activeTradingPairs)
                {
                    await BroadcastTradingPairDataAsync(hubContext, tradingPair, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during market data broadcast cycle");
            }
        }
    }

    private async Task InitializeDefaultTradingPairsAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Initializing default trading pairs for market data monitoring");

            foreach (var tradingPairString in _options.DefaultTradingPairs)
            {
                if (TradingPair.TryParse(tradingPairString, out var tradingPair))
                {
                    _logger.LogInformation("Starting market data monitoring for {TradingPair}", tradingPair);
                    
                    // Subscribe to market data on all default exchanges for this trading pair
                    foreach (var exchangeId in _options.DefaultExchanges)
                    {
                        try
                        {
                            await _marketDataService.SubscribeToOrderBookAsync(exchangeId, tradingPair, stoppingToken);
                            _logger.LogInformation("Subscribed to market data for {TradingPair} on {ExchangeId}", tradingPair, exchangeId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to subscribe to market data for {TradingPair} on {ExchangeId}", tradingPair, exchangeId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid trading pair format in configuration: {TradingPairString}", tradingPairString);
                }
            }

            _logger.LogInformation("Default trading pairs initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default trading pairs");
        }
    }

    private async Task BroadcastTradingPairDataAsync(
        IHubContext<MarketDataHub> hubContext, 
        TradingPair tradingPair, 
        CancellationToken stoppingToken)
    {
        try
        {
            // Get current best bid/ask
            var currentBestBidAsk = _marketDataService.GetBestBidAskAcrossExchanges(tradingPair);
            
            if (!currentBestBidAsk.HasValue)
            {
                return;
            }

            var (currentBestBid, currentBestAsk) = currentBestBidAsk.Value;

            // Check if there's been a change since last broadcast
            var hasChanged = false;
            if (_lastBestBidAsk.TryGetValue(tradingPair, out var lastBestBidAsk))
            {
                hasChanged = !ArePriceQuotesEqual(lastBestBidAsk.BestBid, currentBestBid) ||
                           !ArePriceQuotesEqual(lastBestBidAsk.BestAsk, currentBestAsk);
            }
            else
            {
                hasChanged = true; // First time we're seeing this trading pair
            }

            if (hasChanged)
            {
                // Update our cache
                _lastBestBidAsk[tradingPair] = (currentBestBid, currentBestAsk);

                // Broadcast the update
                var groupName = $"quotes_{tradingPair.BaseCurrency}_{tradingPair.QuoteCurrency}";
                
                var update = new
                {
                    tradingPair = new
                    {
                        baseCurrency = tradingPair.BaseCurrency,
                        quoteCurrency = tradingPair.QuoteCurrency
                    },
                    bestBid = currentBestBid.HasValue ? new
                    {
                        exchangeId = currentBestBid.Value.ExchangeId,
                        price = currentBestBid.Value.BestBidPrice,
                        quantity = currentBestBid.Value.BestBidQuantity,
                        timestamp = currentBestBid.Value.Timestamp
                    } : null,
                    bestAsk = currentBestAsk.HasValue ? new
                    {
                        exchangeId = currentBestAsk.Value.ExchangeId,
                        price = currentBestAsk.Value.BestAskPrice,
                        quantity = currentBestAsk.Value.BestAskQuantity,
                        timestamp = currentBestAsk.Value.Timestamp
                    } : null,
                    spread = currentBestAsk?.BestAskPrice - currentBestBid?.BestBidPrice,
                    spreadPercentage = currentBestBid?.BestBidPrice > 0
                        ? ((currentBestAsk?.BestAskPrice - currentBestBid?.BestBidPrice) / currentBestBid?.BestBidPrice) * 100
                        : 0,
                    timestamp = DateTime.UtcNow
                };

                await hubContext.Clients.Group(groupName).SendAsync("BestBidAskUpdate", update, stoppingToken);

                _logger.LogDebug("Broadcasted best bid/ask update for {TradingPair}: " +
                               "Best Bid: {BestBidExchange} @ {BestBidPrice}, " +
                               "Best Ask: {BestAskExchange} @ {BestAskPrice}, " +
                               "Spread: {Spread}",
                    tradingPair,
                    currentBestBid?.ExchangeId,
                    currentBestBid?.BestBidPrice,
                    currentBestAsk?.ExchangeId,
                    currentBestAsk?.BestAskPrice,
                    update.spread);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting data for trading pair {TradingPair}", tradingPair);
        }
    }

    private static bool ArePriceQuotesEqual(PriceQuote? quote1, PriceQuote? quote2)
    {
        if (quote1 == null && quote2 == null) return true;
        if (quote1 == null || quote2 == null) return false;

        return quote1.Value.ExchangeId == quote2.Value.ExchangeId &&
               quote1.Value.BestBidPrice == quote2.Value.BestBidPrice &&
               quote1.Value.BestAskPrice == quote2.Value.BestAskPrice &&
               quote1.Value.BestBidQuantity == quote2.Value.BestBidQuantity &&
               quote1.Value.BestAskQuantity == quote2.Value.BestAskQuantity;
    }
} 