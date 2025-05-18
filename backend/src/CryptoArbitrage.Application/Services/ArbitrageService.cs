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
/// Service for detecting and executing arbitrage opportunities.
/// </summary>
public class ArbitrageService : IArbitrageService
{
    private readonly IArbitrageDetectionService _detectionService;
    private readonly IConfigurationService _configurationService;
    private readonly IArbitrageRepository _repository;
    private readonly IExchangeFactory _exchangeFactory;
    private readonly ITradingService _tradingService;
    private readonly IMarketDataService _marketDataService;
    private readonly IPaperTradingService _paperTradingService;
    private readonly ILogger<ArbitrageService> _logger;
    private readonly ArbitrageStatistics _statistics = new();
    private Task? _arbitrageProcessingTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private DateTimeOffset _startTime;
    private RiskProfile _riskProfile = new();

    // Implementing events from the interface
    public event Func<ArbitrageOpportunity, Task>? OnOpportunityDetected;
    public event Func<TradeResult, Task>? OnTradeExecuted;
    public event Func<string, Exception, Task>? OnError;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArbitrageService"/> class.
    /// </summary>
    /// <param name="detectionService">The arbitrage detection service.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="repository">The arbitrage repository.</param>
    /// <param name="exchangeFactory">The exchange factory.</param>
    /// <param name="tradingService">The trading service.</param>
    /// <param name="marketDataService">The market data service.</param>
    /// <param name="paperTradingService">The paper trading service.</param>
    /// <param name="logger">The logger.</param>
    public ArbitrageService(
        IArbitrageDetectionService detectionService,
        IConfigurationService configurationService,
        IArbitrageRepository repository,
        IExchangeFactory exchangeFactory,
        ITradingService tradingService,
        IMarketDataService marketDataService,
        IPaperTradingService paperTradingService,
        ILogger<ArbitrageService> logger)
    {
        _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _tradingService = tradingService ?? throw new ArgumentNullException(nameof(tradingService));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _paperTradingService = paperTradingService ?? throw new ArgumentNullException(nameof(paperTradingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc />
    public bool IsRunning => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
    
    /// <inheritdoc />
    public RiskProfile RiskProfile => _detectionService.RiskProfile;
    
    /// <inheritdoc />
    public async Task StartAsync(IEnumerable<TradingPair> tradingPairs, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Arbitrage service is already running");
            return;
        }

        _logger.LogInformation("Starting arbitrage service");
        
        // Get configuration
        var config = await _configurationService.GetConfigurationAsync(cancellationToken);

        // Reset statistics
        _startTime = DateTimeOffset.UtcNow;
        _statistics.StartTime = _startTime;
        _statistics.EndTime = _startTime;
        _statistics.TotalOpportunitiesDetected = 0;
        _statistics.TotalTradesExecuted = 0;
        _statistics.SuccessfulTrades = 0;
        _statistics.FailedTrades = 0;
        _statistics.TotalProfit = 0;
        _statistics.StatisticsByExchange.Clear();
        _statistics.StatisticsByTradingPair.Clear();
        
        // Start the detection service
        await _detectionService.StartAsync(tradingPairs, cancellationToken);
        
        // Start processing detected opportunities
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _arbitrageProcessingTask = Task.Run(
            () => ProcessArbitrageOpportunitiesAsync(_cancellationTokenSource.Token),
            _cancellationTokenSource.Token);
        
        _logger.LogInformation("Arbitrage service started, auto-trading: {AutoTradeEnabled}", config.AutoTradeEnabled);

        // If paper trading is enabled, initialize it
        if (config.PaperTradingEnabled)
        {
            _logger.LogInformation("Initializing paper trading service");
            await _paperTradingService.InitializeAsync(null, cancellationToken);
        }
    }
    
    /// <inheritdoc />
    public async Task StartAsync(RiskProfile riskProfile, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Arbitrage service is already running");
            return;
        }

        _logger.LogInformation("Starting arbitrage service with risk profile: MinProfit={MinProfit}%",
            riskProfile.MinimumProfitPercentage);
            
        _riskProfile = riskProfile;

        try
        {
            // Load configuration to get trading pairs
            var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
            
            if (configuration?.TradingPairs == null || !configuration.TradingPairs.Any())
            {
                _logger.LogWarning("No trading pairs found in configuration");
                return;
            }
            
            // Start with the regular startup method
            await StartAsync(configuration.TradingPairs, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting arbitrage service with risk profile");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!IsRunning)
        {
            _logger.LogWarning("Arbitrage service is not running");
            return;
        }

        _logger.LogInformation("Stopping arbitrage service");

        // Stop the detection service
        await _detectionService.StopAsync(cancellationToken);

        // Stop processing detected opportunities
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            
            try
            {
                if (_arbitrageProcessingTask != null)
                {
                    await _arbitrageProcessingTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping arbitrage processing task");
            }
            
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _arbitrageProcessingTask = null;
        }
        
        // Save final statistics
        _statistics.EndTime = DateTimeOffset.UtcNow;
        await _repository.SaveStatisticsAsync(_statistics, DateTimeOffset.UtcNow, cancellationToken);
        
        _logger.LogInformation("Arbitrage service stopped");
    }
    
    /// <inheritdoc />
    public void UpdateRiskProfile(RiskProfile riskProfile)
    {
        _detectionService.UpdateRiskProfile(riskProfile);
    }
    
    /// <inheritdoc />
    public async IAsyncEnumerable<ArbitrageOpportunity> GetOpportunitiesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var opportunity in _detectionService.GetOpportunitiesAsync(cancellationToken))
        {
            yield return opportunity;
        }
    }
    
    /// <inheritdoc />
    public IAsyncEnumerable<ArbitrageTradeResult> GetTradeResultsAsync(CancellationToken cancellationToken)
    {
        return _detectionService.GetTradeResultsAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public ArbitrageStatistics GetStatistics()
    {
        return _statistics;
    }
    
    /// <summary>
    /// Processes arbitrage opportunities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessArbitrageOpportunitiesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting arbitrage opportunities processing");
        
        try
        {
            await foreach (var opportunity in _detectionService.GetOpportunitiesAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                try
                {
                    // Update statistics
                    _statistics.TotalOpportunitiesDetected++;
                    
                    // Log the opportunity
                    _logger.LogInformation(
                        "Arbitrage opportunity detected: {TradingPair} | Buy: {BuyExchange} ({BuyPrice}) | Sell: {SellExchange} ({SellPrice}) | Profit: {ProfitPercentage}%",
                        opportunity.TradingPair,
                        opportunity.BuyExchangeId,
                        opportunity.BuyPrice,
                        opportunity.SellExchangeId,
                        opportunity.SellPrice,
                        opportunity.SpreadPercentage);
                    
                    // Check if auto-trading is enabled and the opportunity meets the criteria
                    var config = await _configurationService.GetConfigurationAsync(cancellationToken);
                    if (config == null || !config.AutoExecuteTrades || opportunity.SpreadPercentage < _riskProfile.MinimumProfitPercentage)
                    {
                        continue;
                    }
                    
                    // Check if we're in paper trading mode
                    if (_paperTradingService.IsPaperTradingEnabled)
                    {
                        _logger.LogInformation("Executing arbitrage trade in PAPER TRADING mode");
                    }
                    
                    // Execute the trade
                    var tradeResult = await ExecuteArbitrageTradeAsync(opportunity, cancellationToken);
                    
                    // Write the trade result to the channel so subscribers can receive it
                    await _detectionService.PublishTradeResultAsync(tradeResult, cancellationToken);
                    
                    // Update statistics
                    _statistics.TotalTradesExecuted++;
                    if (tradeResult.IsSuccess)
                    {
                        _statistics.SuccessfulTrades++;
                        _statistics.TotalProfit += tradeResult.ProfitAmount;
                    }
                    else
                    {
                        _statistics.FailedTrades++;
                    }
                    
                    // Update exchange statistics
                    UpdateExchangeStatistics(opportunity, tradeResult);
                    
                    // Update trading pair statistics
                    UpdateTradingPairStatistics(opportunity, tradeResult);
                    
                    // Save statistics periodically
                    if (_statistics.TotalTradesExecuted % 10 == 0)
                    {
                        await _repository.SaveStatisticsAsync(_statistics, DateTimeOffset.UtcNow, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing arbitrage opportunity: {TradingPair}", opportunity.TradingPair);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("Arbitrage opportunities processing was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in arbitrage opportunities processing");
        }
        
        _logger.LogInformation("Arbitrage opportunities processing stopped");
    }

    private async Task<ArbitrageTradeResult> ExecuteArbitrageTradeAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing arbitrage trade: {TradingPair} | Buy: {BuyExchange} | Sell: {SellExchange}",
            opportunity.TradingPair, opportunity.BuyExchangeId, opportunity.SellExchangeId);
        
        var tradeResult = new ArbitrageTradeResult(opportunity);
        
        try
        {
            // Get exchange clients
            var buyExchangeClient = _exchangeFactory.CreateClient(opportunity.BuyExchangeId);
            var sellExchangeClient = _exchangeFactory.CreateClient(opportunity.SellExchangeId);
            
            // Calculate trade size
            var config = await _configurationService.GetConfigurationAsync(cancellationToken);
            decimal tradeSize = Math.Min(opportunity.EffectiveQuantity, config?.MaxTradeAmount ?? 100m);
            
            // Execute buy
            var buyOrder = await buyExchangeClient.PlaceMarketOrderAsync(
                opportunity.TradingPair,
                OrderSide.Buy,
                tradeSize,
                cancellationToken);
            
            // We need to wrap the Order in a TradeResult for now
            var buyResult = CreateTradeResultFromOrder(buyOrder, OrderSide.Buy);
            
            if (!buyResult.IsSuccess)
            {
                _logger.LogWarning("Buy trade failed: {ErrorMessage}", buyResult.ErrorMessage);
                tradeResult.ErrorMessage = $"Buy failed: {buyResult.ErrorMessage}";
                return tradeResult;
            }
            
            // Execute sell
            var sellOrder = await sellExchangeClient.PlaceMarketOrderAsync(
                opportunity.TradingPair,
                OrderSide.Sell,
                buyOrder.FilledQuantity > 0 ? buyOrder.FilledQuantity : tradeSize,
                cancellationToken);
            
            // We need to wrap the Order in a TradeResult for now
            var sellResult = CreateTradeResultFromOrder(sellOrder, OrderSide.Sell);
            
            if (!sellResult.IsSuccess)
            {
                _logger.LogWarning("Sell trade failed: {ErrorMessage}", sellResult.ErrorMessage);
                tradeResult.ErrorMessage = $"Sell failed: {sellResult.ErrorMessage}";
                
                // Attempt to revert the buy
                _logger.LogInformation("Attempting to revert buy trade");
                var revertOrder = await buyExchangeClient.PlaceMarketOrderAsync(
                    opportunity.TradingPair,
                    OrderSide.Sell,
                    buyOrder.FilledQuantity > 0 ? buyOrder.FilledQuantity : tradeSize,
                    cancellationToken);
                
                if (revertOrder.Status != OrderStatus.Filled)
                {
                    _logger.LogError("Failed to revert buy trade");
                    tradeResult.ErrorMessage += $". Failed to revert buy trade.";
                }
                
                return tradeResult;
            }
            
            // Calculate profit
            decimal buyVolume = buyOrder.Price * (buyOrder.FilledQuantity > 0 ? buyOrder.FilledQuantity : tradeSize);
            decimal sellVolume = sellOrder.Price * (sellOrder.FilledQuantity > 0 ? sellOrder.FilledQuantity : tradeSize);
            
            // Calculate profit
            decimal profit = sellVolume - buyVolume;
            decimal profitPercentage = profit / buyVolume * 100;
            
            _logger.LogInformation(
                "Arbitrage trade completed: Buy: {BuyVolume} | Sell: {SellVolume} | Profit: {Profit} {Currency} ({ProfitPercentage}%)",
                buyVolume,
                sellVolume,
                profit,
                opportunity.TradingPair.QuoteCurrency,
                profitPercentage);
            
            tradeResult.IsSuccess = true;
            tradeResult.ProfitAmount = profit;
            tradeResult.ProfitPercentage = profitPercentage;
            tradeResult.BuyResult = buyResult;
            tradeResult.SellResult = sellResult;
            
            return tradeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing arbitrage trade");
            tradeResult.ErrorMessage = ex.Message;
            return tradeResult;
        }
    }
    
    private TradeResult CreateTradeResultFromOrder(Order order, OrderSide side)
    {
        var tradeType = side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell;
        
        return new TradeResult
        {
            IsSuccess = order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled,
            OrderId = order.Id,
            ClientOrderId = null,
            Timestamp = order.Timestamp,
            TradingPair = order.TradingPair.ToString(),
            TradeType = tradeType,
            RequestedPrice = order.Price,
            ExecutedPrice = order.AverageFillPrice > 0 ? order.AverageFillPrice : order.Price,
            RequestedQuantity = order.Quantity,
            ExecutedQuantity = order.FilledQuantity,
            TotalValue = order.Price * order.Quantity,
            Fee = 0, // Fee information not available in Order
            FeeCurrency = order.TradingPair.QuoteCurrency,
            ErrorMessage = order.Status == OrderStatus.Rejected ? "Order was rejected by the exchange" : null
        };
    }

    private void UpdateExchangeStatistics(ArbitrageOpportunity opportunity, ArbitrageTradeResult tradeResult)
    {
        // Update buy exchange statistics
        var buyExchangeIdStr = opportunity.BuyExchangeId.ToString();
        if (!_statistics.StatisticsByExchange.TryGetValue(buyExchangeIdStr, out var buyExchangeStats))
        {
            buyExchangeStats = new ExchangeStatistics { ExchangeId = buyExchangeIdStr };
            _statistics.StatisticsByExchange[buyExchangeIdStr] = buyExchangeStats;
        }
        
        buyExchangeStats.TotalTrades++;
        if (tradeResult.IsSuccess)
        {
            buyExchangeStats.TotalProfit += tradeResult.ProfitAmount;
        }
        
        // Update sell exchange statistics
        var sellExchangeIdStr = opportunity.SellExchangeId.ToString();
        if (!_statistics.StatisticsByExchange.TryGetValue(sellExchangeIdStr, out var sellExchangeStats))
        {
            sellExchangeStats = new ExchangeStatistics { ExchangeId = sellExchangeIdStr };
            _statistics.StatisticsByExchange[sellExchangeIdStr] = sellExchangeStats;
        }
        
        sellExchangeStats.TotalTrades++;
    }

    private void UpdateTradingPairStatistics(ArbitrageOpportunity opportunity, ArbitrageTradeResult tradeResult)
    {
        var tradingPairKey = opportunity.TradingPair.ToString();
        
        if (!_statistics.StatisticsByTradingPair.TryGetValue(tradingPairKey, out var tradingPairStats))
        {
            tradingPairStats = new TradingPairStatistics { TradingPair = tradingPairKey };
            _statistics.StatisticsByTradingPair[tradingPairKey] = tradingPairStats;
        }
        
        tradingPairStats.TotalTrades++;
        if (tradeResult.IsSuccess)
        {
            tradingPairStats.SuccessfulTrades++;
            tradingPairStats.TotalProfit += tradeResult.ProfitAmount;
        }
        else
        {
            tradingPairStats.FailedTrades++;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        // Get the configuration with trading pairs
        var configuration = await _configurationService.GetConfigurationAsync();
        
        if (configuration?.TradingPairs == null || !configuration.TradingPairs.Any())
        {
            _logger.LogWarning("No trading pairs found in configuration");
            return;
        }
        
        await StartAsync(configuration.TradingPairs, CancellationToken.None);
    }
    
    /// <inheritdoc />
    public async Task StopAsync()
    {
        await StopAsync(CancellationToken.None);
    }
    
    /// <inheritdoc />
    public async Task<bool> IsRunningAsync()
    {
        return IsRunning;
    }
    
    /// <inheritdoc />
    public async Task RefreshExchangeConfigurationsAsync()
    {
        _logger.LogInformation("Refreshing exchange configurations");
        var configurations = await _configurationService.GetAllExchangeConfigurationsAsync();
        
        foreach (var config in configurations)
        {
            _logger.LogInformation("Refreshed configuration for exchange {ExchangeId}: Enabled={Enabled}", 
                config.ExchangeId, config.IsEnabled);
        }
    }
    
    /// <inheritdoc />
    public async Task RefreshArbitrageConfigurationAsync()
    {
        _logger.LogInformation("Refreshing arbitrage configuration");
        var config = await _configurationService.GetConfigurationAsync();
        
        if (config != null)
        {
            _logger.LogInformation("Refreshed arbitrage configuration: Enabled={Enabled}, AutoTrade={AutoTrade}", 
                config.IsEnabled, config.AutoTradeEnabled);
        }
    }
    
    /// <inheritdoc />
    public async Task RefreshRiskProfileAsync()
    {
        _logger.LogInformation("Refreshing risk profile");
        var profile = await _configurationService.GetRiskProfileAsync();
        
        if (profile != null)
        {
            _riskProfile = profile;
            _logger.LogInformation("Refreshed risk profile: MinProfit={MinProfit}%", 
                profile.MinimumProfitPercentage);
        }
    }
    
    /// <inheritdoc />
    public async Task<ArbitrageOpportunity?> ScanForOpportunityAsync(
        string tradingPair, 
        string buyExchangeId, 
        string sellExchangeId)
    {
        _logger.LogInformation("Scanning for opportunity: {TradingPair} from {BuyExchange} to {SellExchange}", 
            tradingPair, buyExchangeId, sellExchangeId);
        
        try
        {
            var pair = TradingPair.Parse(tradingPair);
            
            // Check if trading pair is supported
            var buyExchange = await _exchangeFactory.CreateExchangeClientAsync(buyExchangeId);
            var sellExchange = await _exchangeFactory.CreateExchangeClientAsync(sellExchangeId);
            
            if (buyExchange == null || sellExchange == null)
            {
                _logger.LogWarning("One or both exchanges not found: {BuyExchange}, {SellExchange}", 
                    buyExchangeId, sellExchangeId);
                return null;
            }
            
            // Get order books
            var buyOrderBook = await buyExchange.GetOrderBookSnapshotAsync(pair, 10);
            var sellOrderBook = await sellExchange.GetOrderBookSnapshotAsync(pair, 10);
            
            if (buyOrderBook == null || sellOrderBook == null)
            {
                _logger.LogWarning("Failed to get order books for {TradingPair}", tradingPair);
                return null;
            }
            
            // Check for arbitrage opportunity
            var bestAsk = buyOrderBook.Asks.FirstOrDefault();
            var bestBid = sellOrderBook.Bids.FirstOrDefault();
            
            if (buyOrderBook.Asks.Count == 0 || sellOrderBook.Bids.Count == 0)
            {
                _logger.LogWarning("Incomplete order book data for {TradingPair}", tradingPair);
                return null;
            }
            
            if (bestAsk.Price < bestBid.Price)
            {
                var bestAskQuantity = bestAsk.Quantity;
                var bestBidQuantity = bestBid.Quantity;
                
                var opportunity = new ArbitrageOpportunity(
                    pair,
                    buyExchangeId,
                    bestAsk.Price,
                    bestAskQuantity,
                    sellExchangeId,
                    bestBid.Price,
                    bestBidQuantity);
                
                _logger.LogInformation("Found arbitrage opportunity: {BuyExchange} to {SellExchange}, Spread: {Spread}%", 
                    buyExchangeId, sellExchangeId, opportunity.SpreadPercentage);
                
                return opportunity;
            }
            
            _logger.LogInformation("No arbitrage opportunity found for {TradingPair}", tradingPair);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for opportunity");
            await RaiseErrorEvent("Error scanning for opportunity", ex);
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<TradeResult?> ExecuteTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal? quantity = null)
    {
        _logger.LogInformation("Executing trade for opportunity: {BuyExchange} to {SellExchange}, Spread: {Spread}%", 
            opportunity.BuyExchangeId, opportunity.SellExchangeId, opportunity.SpreadPercentage);
        
        try
        {
            var config = await _configurationService.GetConfigurationAsync();
            
            if (config?.PaperTradingEnabled == true)
            {
                _logger.LogInformation("Executing paper trade");
                
                // Determine quantity
                var tradeQuantity = quantity ?? opportunity.EffectiveQuantity;
                
                // Execute buy order
                var buyResult = await _paperTradingService.SimulateMarketBuyOrderAsync(
                    opportunity.BuyExchangeId,
                    opportunity.TradingPair,
                    tradeQuantity);
                
                if (!buyResult.IsSuccess)
                {
                    _logger.LogWarning("Paper buy trade failed: {Error}", buyResult.ErrorMessage);
                    return buyResult;
                }
                
                // Execute sell order
                var sellResult = await _paperTradingService.SimulateMarketSellOrderAsync(
                    opportunity.SellExchangeId,
                    opportunity.TradingPair,
                    tradeQuantity);
                
                _logger.LogInformation("Paper trade executed: Buy={BuyPrice}, Sell={SellPrice}, Profit={Profit}", 
                    buyResult.ExecutedPrice, sellResult.ExecutedPrice, 
                    (sellResult.ExecutedPrice - buyResult.ExecutedPrice) * tradeQuantity);
                
                return sellResult; // Return the sell result as the final trade result
            }
            else
            {
                _logger.LogWarning("Live trading not implemented");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing trade");
            await RaiseErrorEvent("Error executing trade", ex);
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<List<ArbitrageOpportunity>> GetLatestOpportunitiesAsync(int count = 10)
    {
        try
        {
            return await _repository.GetRecentOpportunitiesAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest opportunities");
            await RaiseErrorEvent("Error getting latest opportunities", ex);
            return new List<ArbitrageOpportunity>();
        }
    }
    
    /// <inheritdoc />
    public async Task<List<TradeResult>> GetLatestTradesAsync(int count = 10)
    {
        try
        {
            return await _repository.GetRecentTradesAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest trades");
            await RaiseErrorEvent("Error getting latest trades", ex);
            return new List<TradeResult>();
        }
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> SimulateTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal quantity)
    {
        _logger.LogInformation("Simulating trade for opportunity: {BuyExchange} to {SellExchange}, Quantity: {Quantity}", 
            opportunity.BuyExchangeId, opportunity.SellExchangeId, quantity);
        
        try
        {
            // Execute paper trade
            var buyResult = await _paperTradingService.SimulateMarketBuyOrderAsync(
                opportunity.BuyExchangeId,
                opportunity.TradingPair,
                quantity);
            
            if (!buyResult.IsSuccess)
            {
                _logger.LogWarning("Simulated buy trade failed: {Error}", buyResult.ErrorMessage);
                return buyResult;
            }
            
            var sellResult = await _paperTradingService.SimulateMarketSellOrderAsync(
                opportunity.SellExchangeId,
                opportunity.TradingPair,
                quantity);
            
            _logger.LogInformation("Simulated trade executed: Buy={BuyPrice}, Sell={SellPrice}, Profit={Profit}", 
                buyResult.ExecutedPrice, sellResult.ExecutedPrice, 
                (sellResult.ExecutedPrice - buyResult.ExecutedPrice) * quantity);
            
            return sellResult; // Return the sell result as the final trade result
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating trade");
            await RaiseErrorEvent("Error simulating trade", ex);
            
            return TradeResult.Failure(ex, 0);
        }
    }
    
    /// <inheritdoc />
    public async Task RunSimulationAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        ArbitrageConfig? config = null, 
        RiskProfile? riskProfile = null)
    {
        _logger.LogInformation("Running simulation from {Start} to {End}", start, end);
        
        try
        {
            // Implementation would depend on historical data availability
            _logger.LogInformation("Simulation complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running simulation");
            await RaiseErrorEvent("Error running simulation", ex);
        }
    }
    
    private async Task RaiseErrorEvent(string message, Exception exception)
    {
        if (OnError != null)
        {
            try
            {
                await OnError.Invoke(message, exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling OnError event");
            }
        }
    }
} 