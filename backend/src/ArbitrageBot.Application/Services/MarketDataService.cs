using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ArbitrageBot.Application.Services;

/// <summary>
/// Service for handling market data from multiple exchanges.
/// </summary>
public class MarketDataService : IMarketDataService
{
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<MarketDataService> _logger;
    
    private readonly ConcurrentDictionary<(string ExchangeId, TradingPair TradingPair), OrderBook> _orderBooks = new();
    private readonly ConcurrentDictionary<TradingPair, HashSet<string>> _activeExchanges = new();
    private readonly ConcurrentDictionary<(string ExchangeId, TradingPair TradingPair), Task> _subscriptionTasks = new();
    private readonly ConcurrentDictionary<string, IExchangeClient> _connectedClients = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataService"/> class.
    /// </summary>
    /// <param name="exchangeFactory">The exchange factory.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public MarketDataService(
        IExchangeFactory exchangeFactory,
        IConfigurationService configurationService,
        ILogger<MarketDataService> logger)
    {
        _exchangeFactory = exchangeFactory;
        _configurationService = configurationService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task SubscribeToOrderBookAsync(
        string exchangeId, 
        TradingPair tradingPair, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Subscribing to order book updates for {TradingPair} on {ExchangeId}", 
            tradingPair, exchangeId);
        
        var key = (exchangeId, tradingPair);
        
        // Check if already subscribed
        if (_subscriptionTasks.ContainsKey(key))
        {
            _logger.LogWarning("Already subscribed to order book updates for {TradingPair} on {ExchangeId}", 
                tradingPair, exchangeId);
            return;
        }
        
        try
        {
            // Get the exchange client
            var client = await GetConnectedClientAsync(exchangeId, cancellationToken);
            
            // Check if this exchange supports streaming
            if (!client.SupportsStreaming)
            {
                _logger.LogError("Exchange {ExchangeId} does not support real-time streaming, which is required", exchangeId);
                throw new NotSupportedException($"Exchange {exchangeId} does not support real-time streaming, which is required");
            }
            
            // Start processing order book updates
            var processingTask = ProcessOrderBookUpdatesAsync(client, tradingPair, cancellationToken);
            _subscriptionTasks[key] = processingTask;
            
            // Add to active exchanges for this trading pair
            if (!_activeExchanges.TryGetValue(tradingPair, out var exchanges))
            {
                exchanges = new HashSet<string>();
                _activeExchanges[tradingPair] = exchanges;
            }
            
            exchanges.Add(exchangeId);
            
            _logger.LogInformation("Subscribed to order book updates for {TradingPair} on {ExchangeId}", 
                tradingPair, exchangeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to order book updates for {TradingPair} on {ExchangeId}", 
                tradingPair, exchangeId);
            
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task SubscribeToOrderBookOnAllExchangesAsync(
        TradingPair tradingPair, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Subscribing to order book updates for {TradingPair} on all exchanges", tradingPair);
        
        // Get all exchange configurations
        var exchangeConfigs = await _configurationService.GetAllExchangeConfigurationsAsync(cancellationToken);
        
        // Subscribe to each enabled exchange
        foreach (var config in exchangeConfigs.Where(c => c.IsEnabled))
        {
            try
            {
                // Create client to check if it supports streaming
                var client = _exchangeFactory.CreateClient(config.ExchangeId);
                if (client.SupportsStreaming)
                {
                    await SubscribeToOrderBookAsync(config.ExchangeId, tradingPair, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Skipping exchange {ExchangeId} as it does not support real-time streaming", config.ExchangeId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to order book updates for {TradingPair} on {ExchangeId}", 
                    tradingPair, config.ExchangeId);
            }
        }
        
        _logger.LogInformation("Subscribed to order book updates for {TradingPair} on {ExchangeCount} exchanges", 
            tradingPair, _activeExchanges.TryGetValue(tradingPair, out var exchanges) ? exchanges.Count : 0);
    }
    
    /// <inheritdoc />
    public OrderBook? GetLatestOrderBook(string exchangeId, TradingPair tradingPair)
    {
        var key = (exchangeId, tradingPair);
        
        if (_orderBooks.TryGetValue(key, out var orderBook))
        {
            return orderBook;
        }
        
        return null;
    }
    
    /// <inheritdoc />
    public async IAsyncEnumerable<PriceQuote> GetPriceQuotesAsync(
        ExchangeId exchangeId, 
        TradingPair tradingPair, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string exchangeIdStr = exchangeId.Value;
        var key = (exchangeIdStr, tradingPair);
        
        // Check if we're subscribed to this order book
        if (!_subscriptionTasks.ContainsKey(key))
        {
            // If not, subscribe now
            await SubscribeToOrderBookAsync(exchangeIdStr, tradingPair, cancellationToken);
        }
        
        // Get the exchange client
        var client = await GetConnectedClientAsync(exchangeIdStr, cancellationToken);
        
        // Stream the order book updates directly from the exchange client
        await foreach (var orderBook in client.GetOrderBookUpdatesAsync(tradingPair, cancellationToken))
        {
            // Update our local cache
            _orderBooks[key] = orderBook;
            
            // Convert to price quote and yield
            var quote = orderBook.ToPriceQuote();
            if (quote.HasValue)
            {
                yield return quote.Value;
            }
        }
    }
    
    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyCollection<PriceQuote>> GetAggregatedPriceQuotesAsync(
        TradingPair tradingPair, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Check if we have any active exchanges for this trading pair
        if (!_activeExchanges.TryGetValue(tradingPair, out var exchanges) || exchanges.Count == 0)
        {
            // If not, subscribe to all exchanges
            await SubscribeToOrderBookOnAllExchangesAsync(tradingPair, cancellationToken);
            
            // Get the updated list of exchanges
            if (!_activeExchanges.TryGetValue(tradingPair, out exchanges) || exchanges.Count == 0)
            {
                yield break; // No exchanges available
            }
        }
        
        // Set up a channel to receive updates from all exchanges
        var channel = Channel.CreateUnbounded<List<PriceQuote>>();
        
        // Set up a completion task to signal when we're done
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var completionTask = Task.Run(async () =>
        {
            try
            {
                // Set up tasks to forward updates from each exchange
                var forwardingTasks = new List<Task>();
                
                foreach (var exchangeId in exchanges)
                {
                    var forwardingTask = Task.Run(async () =>
                    {
                        try
                        {
                            // Get the client
                            var client = await GetConnectedClientAsync(exchangeId, cts.Token);
                            
                            // Stream updates from this exchange
                            await foreach (var orderBook in client.GetOrderBookUpdatesAsync(tradingPair, cts.Token))
                            {
                                // Update our local cache
                                _orderBooks[(exchangeId, tradingPair)] = orderBook;
                                
                                // Get the current quotes from all exchanges
                                var quotes = new List<PriceQuote>();
                                foreach (var exchId in exchanges)
                                {
                                    var latestOrderBook = GetLatestOrderBook(exchId, tradingPair);
                                    if (latestOrderBook != null)
                                    {
                                        var quote = latestOrderBook.ToPriceQuote();
                                        if (quote.HasValue)
                                        {
                                            quotes.Add(quote.Value);
                                        }
                                    }
                                }
                                
                                if (quotes.Count > 0)
                                {
                                    await channel.Writer.WriteAsync(quotes, cts.Token);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                        {
                            // Expected when cancellation is requested
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error forwarding order book updates for {TradingPair} on {ExchangeId}", 
                                tradingPair, exchangeId);
                        }
                    }, cts.Token);
                    
                    forwardingTasks.Add(forwardingTask);
                }
                
                // Wait for all forwarding tasks to complete
                await Task.WhenAll(forwardingTasks);
            }
            finally
            {
                // Complete the channel when all tasks are done
                channel.Writer.Complete();
            }
        }, cts.Token);
        
        // Stream the aggregated quotes from the channel
        try
        {
            await foreach (var quotes in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return quotes;
            }
        }
        finally
        {
            // Cancel the completion task
            cts.Cancel();
            cts.Dispose();
        }
    }
    
    /// <inheritdoc />
    public (PriceQuote? BestBid, PriceQuote? BestAsk)? GetBestBidAskAcrossExchanges(TradingPair tradingPair)
    {
        // Check if we have any active exchanges for this trading pair
        if (!_activeExchanges.TryGetValue(tradingPair, out var exchanges) || exchanges.Count == 0)
        {
            return null;
        }
        
        PriceQuote? bestBid = null;
        PriceQuote? bestAsk = null;
        
        // Find the highest bid price and lowest ask price across all exchanges
        foreach (var exchangeId in exchanges)
        {
            var orderBook = GetLatestOrderBook(exchangeId, tradingPair);
            if (orderBook != null)
            {
                var quote = orderBook.ToPriceQuote();
                if (quote.HasValue)
                {
                    var currentQuote = quote.Value;
                    
                    if (bestBid == null || currentQuote.BestBidPrice > bestBid.Value.BestBidPrice)
                    {
                        bestBid = currentQuote;
                    }
                    
                    if (bestAsk == null || currentQuote.BestAskPrice < bestAsk.Value.BestAskPrice)
                    {
                        bestAsk = currentQuote;
                    }
                }
            }
        }
        
        if (bestBid == null || bestAsk == null)
        {
            return null;
        }
        
        return (bestBid, bestAsk);
    }
    
    /// <inheritdoc />
    public async Task UnsubscribeFromOrderBookAsync(
        string exchangeId, 
        TradingPair tradingPair, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unsubscribing from order book updates for {TradingPair} on {ExchangeId}", 
            tradingPair, exchangeId);
        
        var key = (exchangeId, tradingPair);
        
        // Remove from active exchanges
        if (_activeExchanges.TryGetValue(tradingPair, out var exchanges))
        {
            exchanges.Remove(exchangeId);
            
            if (exchanges.Count == 0)
            {
                _activeExchanges.TryRemove(tradingPair, out _);
            }
        }
        
        // Attempt to cancel the subscription task
        if (_subscriptionTasks.TryRemove(key, out var task))
        {
            try
            {
                // Get the client and unsubscribe
                if (_connectedClients.TryGetValue(exchangeId, out var client))
                {
                    await client.UnsubscribeFromOrderBookAsync(tradingPair, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from order book updates for {TradingPair} on {ExchangeId}", 
                    tradingPair, exchangeId);
            }
        }
        
        // Remove the order book
        _orderBooks.TryRemove(key, out _);
        
        _logger.LogInformation("Unsubscribed from order book updates for {TradingPair} on {ExchangeId}", 
            tradingPair, exchangeId);
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> GetActiveExchanges(TradingPair tradingPair)
    {
        if (_activeExchanges.TryGetValue(tradingPair, out var exchanges))
        {
            return exchanges.ToList().AsReadOnly();
        }
        
        return Array.Empty<string>();
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<TradingPair> GetActiveTradingPairs()
    {
        return _activeExchanges.Keys.ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Processes order book updates from an exchange.
    /// </summary>
    /// <param name="client">The exchange client.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessOrderBookUpdatesAsync(
        IExchangeClient client, 
        TradingPair tradingPair, 
        CancellationToken cancellationToken)
    {
        var exchangeId = client.ExchangeId;
        var key = (exchangeId, tradingPair);
        
        _logger.LogInformation("Starting to process order book updates for {TradingPair} on {ExchangeId}", 
            tradingPair, exchangeId);
        
        try
        {
            // Subscribe to order book updates on the exchange
            await client.SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            // Process the real-time stream of order book updates
            await foreach (var orderBook in client.GetOrderBookUpdatesAsync(tradingPair, cancellationToken))
            {
                // Store the latest order book
                _orderBooks[key] = orderBook;
                
                _logger.LogTrace("Received order book update for {TradingPair} on {ExchangeId} at {Timestamp}", 
                    tradingPair, exchangeId, orderBook.Timestamp);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order book updates for {TradingPair} on {ExchangeId}", 
                tradingPair, exchangeId);
        }
        finally
        {
            // Attempt to unsubscribe
            try
            {
                await client.UnsubscribeFromOrderBookAsync(tradingPair, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from order book updates for {TradingPair} on {ExchangeId}", 
                    tradingPair, exchangeId);
            }
            
            // Cleanup when the task completes
            _subscriptionTasks.TryRemove(key, out _);
            
            _logger.LogInformation("Stopped processing order book updates for {TradingPair} on {ExchangeId}", 
                tradingPair, exchangeId);
        }
    }
    
    /// <summary>
    /// Gets a connected exchange client.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A connected exchange client.</returns>
    private async Task<IExchangeClient> GetConnectedClientAsync(string exchangeId, CancellationToken cancellationToken)
    {
        // Check if we already have a connected client
        if (_connectedClients.TryGetValue(exchangeId, out var existingClient))
        {
            if (existingClient.IsConnected)
            {
                return existingClient;
            }
            
            // Remove the disconnected client
            _connectedClients.TryRemove(exchangeId, out _);
        }
        
        // Create and connect a new client
        var client = _exchangeFactory.CreateClient(exchangeId);
        
        // Connect to the exchange
        await client.ConnectAsync(cancellationToken);
        
        // Cache the connected client
        _connectedClients[exchangeId] = client;
        
        return client;
    }
} 