using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CryptoArbitrage.Application.Interfaces;
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
/// Service for detecting arbitrage opportunities across exchanges.
/// </summary>
public class ArbitrageDetectionService : IArbitrageDetectionService
{
    private readonly IMarketDataService _marketDataService;
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ArbitrageDetectionService> _logger;
    
    private readonly Channel<ArbitrageOpportunity> _opportunitiesChannel;
    private readonly Channel<ArbitrageTradeResult> _tradeResultsChannel;
    private readonly HashSet<TradingPair> _activeTradingPairs = new();
    private readonly Dictionary<string, FeeSchedule> _feeSchedules = new();
    private readonly ConcurrentDictionary<TradingPair, Task> _processingTasks = new();
    private readonly ConcurrentDictionary<TradingPair, CancellationTokenSource> _processingCts = new();
    
    private RiskProfile _riskProfile = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;
    private bool _isRunning;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ArbitrageDetectionService"/> class.
    /// </summary>
    /// <param name="marketDataService">The market data service.</param>
    /// <param name="exchangeFactory">The exchange factory.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public ArbitrageDetectionService(
        IMarketDataService marketDataService,
        IExchangeFactory exchangeFactory,
        IConfigurationService configurationService,
        ILogger<ArbitrageDetectionService> logger)
    {
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _opportunitiesChannel = Channel.CreateUnbounded<ArbitrageOpportunity>(
            new UnboundedChannelOptions 
            { 
                SingleReader = false, 
                SingleWriter = false 
            });
            
        _tradeResultsChannel = Channel.CreateUnbounded<ArbitrageTradeResult>(
            new UnboundedChannelOptions 
            { 
                SingleReader = false, 
                SingleWriter = false 
            });
    }
    
    /// <summary>
    /// Gets a value indicating whether the arbitrage detection service is running.
    /// </summary>
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Gets the risk profile.
    /// </summary>
    public RiskProfile RiskProfile => _riskProfile;
    
    /// <summary>
    /// Updates the risk profile.
    /// </summary>
    /// <param name="riskProfile">The new risk profile.</param>
    public void UpdateRiskProfile(RiskProfile riskProfile)
    {
        _riskProfile = riskProfile;
        _logger.LogInformation("Risk profile updated. Minimum profit percentage: {MinimumProfitPercentage}%", 
            _riskProfile.MinimumProfitPercentage);
    }
    
    /// <inheritdoc />
    public async Task StartAsync(IEnumerable<TradingPair> tradingPairs, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Arbitrage detection service is already running");
            return;
        }
        
        _logger.LogInformation("Starting arbitrage detection service");
        
        // Get the risk profile from configuration
        try 
        {
            var riskProfile = await _configurationService.GetRiskProfileAsync(cancellationToken);
            UpdateRiskProfile(riskProfile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load risk profile, using default");
        }
        
        // Create main cancellation token source
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Subscribe to order book updates for all trading pairs across all exchanges
        foreach (var tradingPair in tradingPairs)
        {
            await AddTradingPairAsync(tradingPair, _cancellationTokenSource.Token);
        }
        
        _isRunning = true;
        _logger.LogInformation("Arbitrage detection service started for {TradingPairCount} trading pairs", 
            _activeTradingPairs.Count);
    }
    
    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Arbitrage detection service is not running");
            return;
        }
        
        _logger.LogInformation("Stopping arbitrage detection service");
        
        // Signal all tasks to stop
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            
            // Cancel all individual processing tasks
            foreach (var cts in _processingCts.Values)
            {
                cts.Cancel();
            }
            
            // Wait for all processing tasks to complete
            var tasks = _processingTasks.Values.ToList();
            if (tasks.Any())
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for arbitrage detection tasks to complete");
                }
            }
            
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
        
        // Unsubscribe from all active trading pairs
        foreach (var tradingPair in _activeTradingPairs.ToList())
        {
            await RemoveTradingPairInternal(tradingPair, cancellationToken);
        }
        
        // Clear all collections
        _processingTasks.Clear();
        _processingCts.Clear();
        _activeTradingPairs.Clear();
        
        _isRunning = false;
        _logger.LogInformation("Arbitrage detection service stopped");
    }
    
    /// <inheritdoc />
    public async IAsyncEnumerable<ArbitrageOpportunity> GetOpportunitiesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var opportunity in _opportunitiesChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return opportunity;
        }
    }
    
    /// <inheritdoc />
    public async IAsyncEnumerable<ArbitrageTradeResult> GetTradeResultsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in _tradeResultsChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }
    }
    
    /// <inheritdoc />
    public async Task AddTradingPairAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        if (_activeTradingPairs.Contains(tradingPair))
        {
            _logger.LogWarning("Trading pair {TradingPair} is already being monitored", tradingPair);
            return;
        }
        
        // Subscribe to the trading pair on all exchanges that support streaming
        await _marketDataService.SubscribeToOrderBookOnAllExchangesAsync(tradingPair, cancellationToken);
        
        // Start processing for this trading pair
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingCts[tradingPair] = cts;
        
        var task = ProcessTradingPairStreamAsync(tradingPair, cts.Token);
        _processingTasks[tradingPair] = task;
        
        _activeTradingPairs.Add(tradingPair);
        
        _logger.LogInformation("Added trading pair {TradingPair} for arbitrage detection", tradingPair);
    }
    
    /// <inheritdoc />
    public async Task RemoveTradingPairAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        await RemoveTradingPairInternal(tradingPair, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task PublishTradeResultAsync(ArbitrageTradeResult tradeResult, CancellationToken cancellationToken = default)
    {
        if (tradeResult == null)
        {
            throw new ArgumentNullException(nameof(tradeResult));
        }

        _logger.LogInformation(
            "Publishing trade result for {TradingPair}, Buy: {BuyExchangeId}, Sell: {SellExchangeId}, Success: {IsSuccess}",
            tradeResult.Opportunity.TradingPair,
            tradeResult.Opportunity.BuyExchangeId,
            tradeResult.Opportunity.SellExchangeId,
            tradeResult.IsSuccess);

        await _tradeResultsChannel.Writer.WriteAsync(tradeResult, cancellationToken);
    }
    
    private async Task RemoveTradingPairInternal(TradingPair tradingPair, CancellationToken cancellationToken)
    {
        if (!_activeTradingPairs.Contains(tradingPair))
        {
            _logger.LogWarning("Trading pair {TradingPair} is not being monitored", tradingPair);
            return;
        }
        
        // Cancel the processing task for this trading pair
        if (_processingCts.TryRemove(tradingPair, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        
        // Wait for the processing task to complete
        if (_processingTasks.TryRemove(tradingPair, out var task))
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for processing task to complete for {TradingPair}", tradingPair);
            }
        }
        
        // Unsubscribe from all exchanges
        var exchanges = _marketDataService.GetActiveExchanges(tradingPair);
        foreach (var exchange in exchanges)
        {
            await _marketDataService.UnsubscribeFromOrderBookAsync(exchange, tradingPair, cancellationToken);
        }
        
        // Remove from active trading pairs
        _activeTradingPairs.Remove(tradingPair);
        
        _logger.LogInformation("Removed trading pair {TradingPair} from arbitrage detection", tradingPair);
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<TradingPair> GetActiveTradingPairs()
    {
        return _activeTradingPairs.ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Processes real-time order book updates for a trading pair and checks for arbitrage opportunities.
    /// </summary>
    /// <param name="tradingPair">The trading pair to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessTradingPairStreamAsync(TradingPair tradingPair, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting real-time arbitrage detection for {TradingPair}", tradingPair);
        
        try
        {
            // Create a channel to collect all order book updates from all exchanges
            var updateChannel = Channel.CreateUnbounded<(string ExchangeId, OrderBook OrderBook)>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            
            // Get active exchanges for this trading pair
            var exchanges = _marketDataService.GetActiveExchanges(tradingPair);
            if (exchanges.Count < 2)
            {
                _logger.LogWarning("Need at least 2 exchanges for arbitrage on {TradingPair}, but only {ExchangeCount} are active", 
                    tradingPair, exchanges.Count);
                return;
            }
            
            // Create a CTS for all forwarding tasks
            using var forwardingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var forwardingTasks = new List<Task>();
            
            // Create forwarding tasks for each exchange
            foreach (var exchangeId in exchanges)
            {
                var forwardingTask = Task.Run(async () =>
                {
                    try
                    {
                        // Use IAsyncEnumerable from MarketDataService to get real-time price quotes
                        await foreach (var quote in _marketDataService.GetPriceQuotesAsync(
                            new ExchangeId(exchangeId), tradingPair, forwardingCts.Token))
                        {
                            // Get the latest order book
                            var orderBook = _marketDataService.GetLatestOrderBook(exchangeId, tradingPair);
                            if (orderBook != null)
                            {
                                // Forward to the update channel
                                await updateChannel.Writer.WriteAsync((exchangeId, orderBook), forwardingCts.Token);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (forwardingCts.Token.IsCancellationRequested)
                    {
                        // Expected when cancellation is requested
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error forwarding order book updates for {TradingPair} on {ExchangeId}", 
                            tradingPair, exchangeId);
                    }
                }, forwardingCts.Token);
                
                forwardingTasks.Add(forwardingTask);
            }
            
            // Dictionary to track the latest order book for each exchange
            var latestOrderBooks = new Dictionary<string, OrderBook>();
            
            // Process updates from the channel
            try
            {
                await foreach (var (exchangeId, orderBook) in updateChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    // Update the latest order book for this exchange
                    latestOrderBooks[exchangeId] = orderBook;
                    
                    // Only check for arbitrage if we have order books from at least 2 exchanges
                    if (latestOrderBooks.Count >= 2)
                    {
                        await CheckForArbitrageOpportunityAsync(tradingPair, latestOrderBooks.Values, cancellationToken);
                    }
                }
            }
            finally
            {
                // Cancel all forwarding tasks
                forwardingCts.Cancel();
                
                try
                {
                    await Task.WhenAll(forwardingTasks);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for forwarding tasks to complete for {TradingPair}", tradingPair);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in arbitrage detection processing for {TradingPair}", tradingPair);
        }
        
        _logger.LogInformation("Real-time arbitrage detection for {TradingPair} stopped", tradingPair);
    }
    
    /// <summary>
    /// Checks for arbitrage opportunities based on the latest order books.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="orderBooks">The latest order books from different exchanges.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CheckForArbitrageOpportunityAsync(
        TradingPair tradingPair, 
        IEnumerable<OrderBook> orderBooks, 
        CancellationToken cancellationToken)
    {
        // Create price quotes from order books
        var quotes = orderBooks
            .Select(ob => ob.ToPriceQuote())
            .Where(q => q.HasValue)
            .Select(q => q!.Value)
            .ToList();
        
        if (quotes.Count < 2)
        {
            return; // Need quotes from at least 2 exchanges
        }
        
        // Find the lowest ask (buy from this exchange)
        var lowestAsk = quotes.MinBy(q => q.BestAskPrice);
        
        // Find the highest bid (sell to this exchange)
        var highestBid = quotes.MaxBy(q => q.BestBidPrice);
        
        // Check if there's a potential arbitrage opportunity
        if (lowestAsk.ExchangeId != highestBid.ExchangeId && 
            lowestAsk.BestAskPrice < highestBid.BestBidPrice)
        {
            // Calculate potential arbitrage opportunity
            var opportunity = new ArbitrageOpportunity(
                tradingPair,
                lowestAsk.ExchangeId,
                lowestAsk.BestAskPrice,
                lowestAsk.BestAskQuantity,
                highestBid.ExchangeId,
                highestBid.BestBidPrice,
                highestBid.BestBidQuantity);
            
            // Check if the opportunity meets the minimum profit percentage requirement
            if (opportunity.SpreadPercentage >= _riskProfile.MinimumProfitPercentage)
            {
                // Log and publish the opportunity
                _logger.LogInformation(
                    "Detected arbitrage opportunity: {TradingPair}, Buy from {BuyExchange} at {BuyPrice}, " +
                    "Sell to {SellExchange} at {SellPrice}, Spread: {SpreadPercentage}%, " +
                    "Est. Profit: {EstimatedProfit}", 
                    tradingPair,
                    lowestAsk.ExchangeId,
                    lowestAsk.BestAskPrice,
                    highestBid.ExchangeId,
                    highestBid.BestBidPrice,
                    opportunity.SpreadPercentage,
                    opportunity.EstimatedProfit);
                
                await _opportunitiesChannel.Writer.WriteAsync(opportunity, cancellationToken);
            }
        }
    }
} 