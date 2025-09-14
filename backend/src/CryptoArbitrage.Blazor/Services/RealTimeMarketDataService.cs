using Microsoft.AspNetCore.SignalR;
using CryptoArbitrage.Blazor.Hubs;
using CryptoArbitrage.Blazor.ViewModels;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Application.Interfaces;
using System.Collections.Concurrent;

namespace CryptoArbitrage.Blazor.Services;

public interface IRealTimeMarketDataService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<RealTimeMarketDataViewModel?> GetCurrentMarketDataAsync(string tradingPair);
}

public class RealTimeMarketDataService : BackgroundService, IRealTimeMarketDataService
{
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly IMarketDataAggregator _marketDataAggregator;
    private readonly ILogger<RealTimeMarketDataService> _logger;
    
    // Cache for current market data
    private readonly ConcurrentDictionary<string, RealTimeMarketDataViewModel> _marketDataCache = new();
    private readonly ConcurrentDictionary<string, ExchangeOrderBookViewModel> _orderBookCache = new();
    
    private readonly List<string> _tradingPairs = new() { "BTC/USDT", "ETH/USDT", "ETH/BTC" };
    private readonly List<string> _exchanges = new() { "coinbase", "kraken" };
    
    private readonly Timer _updateTimer;
    
    public RealTimeMarketDataService(
        IHubContext<MarketDataHub> hubContext,
        IMarketDataAggregator marketDataAggregator,
        ILogger<RealTimeMarketDataService> logger)
    {
        _hubContext = hubContext;
        _marketDataAggregator = marketDataAggregator;
        _logger = logger;
        
        // Update every 500ms for real-time feel
        _updateTimer = new Timer(ProcessAndBroadcastUpdates, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task<RealTimeMarketDataViewModel?> GetCurrentMarketDataAsync(string tradingPair)
    {
        return _marketDataCache.TryGetValue(tradingPair, out var data) ? data : null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Real-Time Market Data Service");
        
        // Start the timer for periodic updates
        _updateTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CollectAndCacheMarketData();
                await Task.Delay(1000, stoppingToken); // Collect data every second
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Real-Time Market Data Service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Real-Time Market Data Service");
        }
        finally
        {
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private async Task CollectAndCacheMarketData()
    {
        try
        {
            foreach (var tradingPair in _tradingPairs)
            {
                var marketDataViewModel = new RealTimeMarketDataViewModel
                {
                    TradingPair = tradingPair,
                    LastUpdated = DateTime.UtcNow,
                    Exchanges = new List<ExchangeOrderBookViewModel>(),
                    ArbitrageSpreads = new List<ArbitrageSpreadViewModel>()
                };

                // Collect data from each exchange
                var exchangeDataTasks = _exchanges.Select(async exchangeId =>
                {
                    try
                    {
                        var priceQuotes = await _marketDataAggregator.GetLatestPricesAsync(tradingPair);
                        var relevantQuote = priceQuotes.FirstOrDefault(q => q.ExchangeId == exchangeId);
                        
                        if (!string.IsNullOrEmpty(relevantQuote.ExchangeId))
                        {
                            return CreateOrderBookViewModel(relevantQuote);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting data from {ExchangeId} for {TradingPair}", exchangeId, tradingPair);
                    }
                    
                    return null;
                }).ToArray();

                var exchangeResults = await Task.WhenAll(exchangeDataTasks);
                marketDataViewModel.Exchanges = exchangeResults.Where(r => r != null).Cast<ExchangeOrderBookViewModel>().ToList();

                // Calculate arbitrage spreads
                if (marketDataViewModel.Exchanges.Count >= 2)
                {
                    marketDataViewModel.ArbitrageSpreads = CalculateArbitrageSpreads(marketDataViewModel.Exchanges, tradingPair);
                }

                // Cache the data
                _marketDataCache.AddOrUpdate(tradingPair, marketDataViewModel, (key, old) => marketDataViewModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting market data");
        }
    }

    private ExchangeOrderBookViewModel CreateOrderBookViewModel(PriceQuote priceQuote)
    {
        var spread = priceQuote.AskPrice - priceQuote.BidPrice;
        var spreadPercentage = priceQuote.BidPrice > 0 ? (spread / priceQuote.BidPrice) * 100 : 0;

        return new ExchangeOrderBookViewModel
        {
            ExchangeId = priceQuote.ExchangeId,
            TradingPair = priceQuote.TradingPair.ToString(),
            Timestamp = priceQuote.Timestamp,
            IsConnected = true,
            IsRealTime = true,
            
            BestBidPrice = priceQuote.BidPrice,
            BestBidQuantity = priceQuote.BestBidQuantity,
            BestAskPrice = priceQuote.AskPrice,
            BestAskQuantity = priceQuote.BestAskQuantity,
            
            Spread = spread,
            SpreadPercentage = spreadPercentage,
            
            // Simplified order book entries
            TopBids = new List<OrderBookEntryViewModel>
            {
                new()
                {
                    Price = priceQuote.BidPrice,
                    Quantity = priceQuote.BestBidQuantity,
                    Total = priceQuote.BidPrice * priceQuote.BestBidQuantity,
                    Side = OrderSide.Buy,
                    VolumePercentage = 100
                }
            },
            TopAsks = new List<OrderBookEntryViewModel>
            {
                new()
                {
                    Price = priceQuote.AskPrice,
                    Quantity = priceQuote.BestAskQuantity,
                    Total = priceQuote.AskPrice * priceQuote.BestAskQuantity,
                    Side = OrderSide.Sell,
                    VolumePercentage = 100
                }
            },
            
            TotalBidVolume = priceQuote.BestBidQuantity,
            TotalAskVolume = priceQuote.BestAskQuantity
        };
    }

    private List<ArbitrageSpreadViewModel> CalculateArbitrageSpreads(List<ExchangeOrderBookViewModel> exchanges, string tradingPair)
    {
        var spreads = new List<ArbitrageSpreadViewModel>();
        
        for (int i = 0; i < exchanges.Count; i++)
        {
            for (int j = i + 1; j < exchanges.Count; j++)
            {
                var exchange1 = exchanges[i];
                var exchange2 = exchanges[j];
                
                // Calculate both directions
                var spread1 = CalculateSpread(exchange1, exchange2, tradingPair);
                var spread2 = CalculateSpread(exchange2, exchange1, tradingPair);
                
                if (spread1.IsViable) spreads.Add(spread1);
                if (spread2.IsViable) spreads.Add(spread2);
            }
        }
        
        return spreads.OrderByDescending(s => s.ProfitPercentage).ToList();
    }

    private ArbitrageSpreadViewModel CalculateSpread(ExchangeOrderBookViewModel buyExchange, ExchangeOrderBookViewModel sellExchange, string tradingPair)
    {
        var buyPrice = buyExchange.BestAskPrice; // We buy at ask price
        var sellPrice = sellExchange.BestBidPrice; // We sell at bid price
        
        var profitPerUnit = sellPrice - buyPrice;
        var profitPercentage = buyPrice > 0 ? (profitPerUnit / buyPrice) * 100 : 0;
        
        var maxQuantity = Math.Min(buyExchange.BestAskQuantity, sellExchange.BestBidQuantity);
        var estimatedProfit = profitPerUnit * maxQuantity;
        
        var isViable = profitPercentage > 0.1m; // Minimum 0.1% profit to be considered viable
        
        return new ArbitrageSpreadViewModel
        {
            TradingPair = tradingPair,
            BuyExchange = buyExchange.ExchangeId,
            SellExchange = sellExchange.ExchangeId,
            BuyPrice = buyPrice,
            SellPrice = sellPrice,
            ProfitPerUnit = profitPerUnit,
            ProfitPercentage = profitPercentage,
            MaxTradeQuantity = maxQuantity,
            EstimatedProfit = estimatedProfit,
            DetectedAt = DateTime.UtcNow,
            IsViable = isViable,
            ViabilityReason = isViable ? "Profitable" : "Insufficient profit margin",
            ProfitabilityClass = GetProfitabilityClass(profitPercentage),
            ProfitabilityScore = Math.Max(0, Math.Min(100, (double)profitPercentage * 10))
        };
    }

    private string GetProfitabilityClass(decimal profitPercentage)
    {
        return profitPercentage switch
        {
            >= 1.0m => "profit-excellent",
            >= 0.5m => "profit-good",
            >= 0.1m => "profit-fair",
            _ => "profit-poor"
        };
    }

    private async void ProcessAndBroadcastUpdates(object? state)
    {
        try
        {
            foreach (var marketData in _marketDataCache.Values)
            {
                // Broadcast order book updates
                foreach (var exchange in marketData.Exchanges)
                {
                    var orderBookUpdate = new OrderBookUpdateMessage
                    {
                        ExchangeId = exchange.ExchangeId,
                        TradingPair = exchange.TradingPair,
                        OrderBook = exchange
                    };
                    
                    await _hubContext.Clients.Group("MarketData")
                        .SendAsync("OrderBookUpdate", orderBookUpdate);
                }
                
                // Broadcast arbitrage opportunities
                foreach (var spread in marketData.ArbitrageSpreads.Where(s => s.IsViable))
                {
                    var opportunityUpdate = new ArbitrageOpportunityUpdateMessage
                    {
                        Opportunity = spread
                    };
                    
                    await _hubContext.Clients.Group("MarketData")
                        .SendAsync("ArbitrageOpportunityUpdate", opportunityUpdate);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting market data updates");
        }
    }
    
    public override void Dispose()
    {
        _updateTimer?.Dispose();
        base.Dispose();
    }
} 