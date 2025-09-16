using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Exceptions;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Services;

/// <summary>
/// 🎯 REAL BUSINESS LOGIC: Market data aggregation service
/// 
/// This service implements ACTUAL market data collection from multiple exchanges
/// for real arbitrage detection using live exchange APIs.
/// </summary>
public class MarketDataAggregatorService : IMarketDataAggregator
{
    private readonly ILogger<MarketDataAggregatorService> _logger;
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IConfigurationService _configurationService;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PriceQuote>> _priceData;
    private readonly ConcurrentDictionary<string, IExchangeClient> _exchangeClients;
    private readonly ConcurrentDictionary<string, bool> _exchangeSeen; // track exchanges we monitor
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Task> _monitoringTasks;
    private bool _isMonitoring = false;

    public MarketDataAggregatorService(
        ILogger<MarketDataAggregatorService> logger,
        IExchangeFactory exchangeFactory,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _priceData = new ConcurrentDictionary<string, ConcurrentDictionary<string, PriceQuote>>();
        _exchangeClients = new ConcurrentDictionary<string, IExchangeClient>();
        _exchangeSeen = new ConcurrentDictionary<string, bool>();
        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTasks = new List<Task>();
    }

    public Task<IEnumerable<PriceQuote>> GetLatestPricesAsync(string tradingPair)
    {
        var prices = new List<PriceQuote>();

        foreach (var exchange in _priceData.Keys)
        {
            if (_priceData[exchange].TryGetValue(tradingPair, out var price))
            {
                // Only return recent prices (within last 30 seconds)
                if (DateTime.UtcNow - price.Timestamp < TimeSpan.FromSeconds(30))
                {
                    prices.Add(price);
                }
            }
        }

        _logger.LogDebug("Retrieved {Count} price quotes for {TradingPair}", prices.Count, tradingPair);
        return Task.FromResult<IEnumerable<PriceQuote>>(prices);
    }

    public async Task StartMonitoringAsync(IEnumerable<string> exchanges, IEnumerable<string> tradingPairs)
    {
        if (_isMonitoring)
        {
            return;
        }

        _logger.LogInformation("Starting market data monitoring for exchanges: {Exchanges}, pairs: {TradingPairs}",
            string.Join(", ", exchanges), string.Join(", ", tradingPairs));

        var exchangeList = exchanges
            .Where(id => _exchangeFactory.GetSupportedExchanges().Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var pairList = tradingPairs.ToList();


        // Initialize price data storage for each exchange
        foreach (var exchange in exchangeList)
        {
            _priceData.TryAdd(exchange, new ConcurrentDictionary<string, PriceQuote>());
            _exchangeSeen[exchange] = true;
        }

        // Initialize exchange clients for supported exchanges
        await InitializeExchangeClientsAsync(exchangeList);

        // Start monitoring tasks for each exchange
        foreach (var exchange in exchangeList)
        {
            var task = MonitorExchangeAsync(exchange, pairList, _cancellationTokenSource.Token);
            _monitoringTasks.Add(task);
        }

        _isMonitoring = true;
        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _logger.LogInformation("Stopping market data monitoring...");

        _cancellationTokenSource.Cancel();

        try
        {
            await Task.WhenAll(_monitoringTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        // Disconnect exchange clients
        foreach (var client in _exchangeClients.Values)
        {
            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting from exchange {ExchangeId}", client.ExchangeId);
            }
        }

        _monitoringTasks.Clear();
        _priceData.Clear();
        _exchangeClients.Clear();
        _isMonitoring = false;

        _logger.LogInformation("Market data monitoring stopped");
    }

    private async Task InitializeExchangeClientsAsync(List<string> exchanges)
    {
        foreach (var exchangeId in exchanges)
        {
            try
            {
                var exchangeConfig = await _configurationService.GetExchangeConfigurationAsync(exchangeId);
                // Always initialize real clients for supported exchanges; auth only if keys provided
                var client = _exchangeFactory.CreateClient(exchangeId);

                // Connect to the exchange
                await client.ConnectAsync();

                // Authenticate if credentials are available
                bool isAuthenticated = false;
                if (!string.IsNullOrEmpty(exchangeConfig?.ApiKey) && !string.IsNullOrEmpty(exchangeConfig.ApiSecret))
                {
                    try
                    {
                        await client.AuthenticateAsync();
                        isAuthenticated = true;
                        _logger.LogInformation("Authenticated with {ExchangeId}", exchangeId);
                    }
                    catch (ArgumentException ex) when (ex.Message.Contains("Base64") || ex.Message.Contains("API"))
                    {
                        _logger.LogWarning("Invalid credentials for {ExchangeId}, continuing with public-only mode: {Message}", exchangeId, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Authentication failed for {ExchangeId}, continuing with public-only mode", exchangeId);
                    }
                }
                else
                {
                    _logger.LogInformation("No credentials provided for {ExchangeId}, using public-only mode", exchangeId);
                }

                _exchangeClients[exchangeId] = client;
                _logger.LogInformation("Connected to {ExchangeId} (WebSocket) - {Mode}", 
                    exchangeId, 
                    isAuthenticated ? "Authenticated" : "Public-only");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize exchange client for {ExchangeId}", exchangeId);
            }
        }
    }

    private async Task MonitorExchangeAsync(string exchangeId, List<string> tradingPairs, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting real-time monitoring for exchange: {ExchangeId}", exchangeId);

        if (_exchangeClients.TryGetValue(exchangeId, out var client))
        {
            // Use real-time WebSocket streams for live data
            await MonitorExchangeWithWebSocketAsync(client, tradingPairs, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No exchange client available for {ExchangeId}", exchangeId);
        }

        _logger.LogInformation("Stopped monitoring exchange: {ExchangeId}", exchangeId);
    }

    private async Task MonitorExchangeWithWebSocketAsync(IExchangeClient client, List<string> tradingPairs, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting real-time WebSocket monitoring for {ExchangeId}", client.ExchangeId);

        try
        {
            // Subscribe to order book updates for all trading pairs
            var subscriptionTasks = new List<Task>();
            var streamTasks = new List<Task>();

            foreach (var tradingPairStr in tradingPairs)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(tradingPairStr))
                    {
                        _logger.LogWarning("Skipping empty trading pair for {ExchangeId}", client.ExchangeId);
                        continue;
                    }

                    var tradingPair = TradingPair.Parse(tradingPairStr);

                    // Subscribe to order book updates
                    var subscribeTask = SubscribeToOrderBookAsync(client, tradingPair, cancellationToken);
                    subscriptionTasks.Add(subscribeTask);

                    // Start consuming the real-time stream
                    var streamTask = ConsumeOrderBookStreamAsync(client, tradingPair, cancellationToken);
                    streamTasks.Add(streamTask);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse trading pair '{TradingPairStr}' for {ExchangeId}", tradingPairStr, client.ExchangeId);
                }
            }

            // Wait for all subscriptions to complete (but don't fail if some fail)
            try
            {
                await Task.WhenAll(subscriptionTasks);
                _logger.LogInformation("Completed subscription attempts for {Count} trading pairs on {ExchangeId}", 
                    tradingPairs.Count, client.ExchangeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Some subscriptions failed for {ExchangeId}, continuing with available ones", client.ExchangeId);
            }

            // Wait for all streaming tasks to complete (or cancellation)
            await Task.WhenAll(streamTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket monitoring cancelled for {ExchangeId}", client.ExchangeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket monitoring for {ExchangeId}", client.ExchangeId);
        }
    }

    private async Task SubscribeToOrderBookAsync(IExchangeClient client, TradingPair tradingPair, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that the client is properly connected before attempting subscription
            if (!client.IsConnected)
            {
                _logger.LogWarning("Skipping subscription for {TradingPair} on {ExchangeId} - client not connected", 
                    tradingPair, client.ExchangeId);
                return;
            }

            await client.SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            _logger.LogInformation("Subscribed to order book for {TradingPair} on {ExchangeId}", 
                tradingPair, client.ExchangeId);
        }
        catch (ExchangeClientException ex)
        {
            _logger.LogWarning("Exchange-specific error subscribing to {TradingPair} on {ExchangeId}: {Message}", 
                tradingPair, client.ExchangeId, ex.Message);
            // Don't throw - continue with other subscriptions
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation subscribing to {TradingPair} on {ExchangeId}: {Message}", 
                tradingPair, client.ExchangeId, ex.Message);
            // Don't throw - continue with other subscriptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to order book for {TradingPair} on {ExchangeId}", 
                tradingPair, client.ExchangeId);
            // Don't throw - continue with other subscriptions
        }
    }

    private async Task ConsumeOrderBookStreamAsync(IExchangeClient client, TradingPair tradingPair, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting real-time order book stream consumption for {TradingPair} on {ExchangeId}", 
            tradingPair, client.ExchangeId);

        try
        {
            await foreach (var orderBook in client.GetOrderBookUpdatesAsync(tradingPair, cancellationToken))
            {
                // Convert order book to price quote
                var priceQuote = ConvertOrderBookToPriceQuote(orderBook);

                // Update price data
                if (_priceData.TryGetValue(client.ExchangeId, out var exchangeData))
                {
                    exchangeData.AddOrUpdate(tradingPair.ToString(), priceQuote, (key, oldValue) => priceQuote);

                    _logger.LogDebug("Real-time price update for {ExchangeId} {TradingPair}: Bid={Bid}, Ask={Ask}",
                        client.ExchangeId, tradingPair, priceQuote.BidPrice, priceQuote.AskPrice);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order book stream cancelled for {TradingPair} on {ExchangeId}", 
                tradingPair, client.ExchangeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming order book stream for {TradingPair} on {ExchangeId}", 
                tradingPair, client.ExchangeId);
            throw;
        }
    }

    private PriceQuote ConvertOrderBookToPriceQuote(OrderBook orderBook)
    {
        if (orderBook.Bids.Any() && orderBook.Asks.Any())
        {
            var bestBid = orderBook.Bids.First();
            var bestAsk = orderBook.Asks.First();

            return new PriceQuote(
                orderBook.ExchangeId,
                orderBook.TradingPair,
                orderBook.Timestamp,
                bestBid.Price,
                bestBid.Quantity,
                bestAsk.Price,
                bestAsk.Quantity
            );
        }
        else
        {
            // Fallback for empty order book
            return new PriceQuote(
                orderBook.ExchangeId,
                orderBook.TradingPair,
                orderBook.Timestamp,
                0, 0, 0, 0
            );
        }
    }

    // NEW METHODS to satisfy IMarketDataAggregator
    public Task<OrderBook> GetOrderBookAsync(string exchangeId, TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        // If we have a connected client, ask it for a snapshot; otherwise build from last price
        if (_exchangeClients.TryGetValue(exchangeId, out var client))
        {
            return client.GetOrderBookSnapshotAsync(tradingPair, cancellationToken: cancellationToken);
        }

        if (_priceData.TryGetValue(exchangeId, out var pairs) && pairs.TryGetValue(tradingPair.ToString(), out var quote))
        {
            var bids = new List<OrderBookEntry> { new OrderBookEntry(quote.BestBidPrice, quote.BestBidQuantity) };
            var asks = new List<OrderBookEntry> { new OrderBookEntry(quote.BestAskPrice, quote.BestAskQuantity) };
            return Task.FromResult(new OrderBook(exchangeId, tradingPair, quote.Timestamp, bids, asks));
        }

        // Fallback empty book
        return Task.FromResult(new OrderBook(exchangeId, tradingPair, DateTime.UtcNow, new List<OrderBookEntry>(), new List<OrderBookEntry>()));
    }

    public Task<IReadOnlyList<string>> GetAvailableExchangesAsync(CancellationToken cancellationToken = default)
    {
        var exchanges = _exchangeSeen.Keys.ToList();
        return Task.FromResult<IReadOnlyList<string>>(exchanges);
    }
} 