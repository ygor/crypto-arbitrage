using System.Diagnostics;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ArbitrageBot.Application.Services;

/// <summary>
/// Service for executing trades across different exchanges.
/// </summary>
public class TradingService : ITradingService
{
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IConfigurationService _configurationService;
    private readonly IPaperTradingService _paperTradingService;
    private readonly ILogger<TradingService> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TradingService"/> class.
    /// </summary>
    /// <param name="exchangeFactory">The exchange factory.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="paperTradingService">The paper trading service.</param>
    /// <param name="logger">The logger.</param>
    public TradingService(
        IExchangeFactory exchangeFactory,
        IConfigurationService configurationService,
        IPaperTradingService paperTradingService,
        ILogger<TradingService> logger)
    {
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _paperTradingService = paperTradingService ?? throw new ArgumentNullException(nameof(paperTradingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> PlaceMarketBuyOrderAsync(
        string exchangeId, 
        TradingPair tradingPair, 
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        // Check if paper trading is enabled
        if (_paperTradingService.IsPaperTradingEnabled)
        {
            _logger.LogInformation("Using paper trading for market buy order: {Quantity} {TradingPair} on {ExchangeId}", 
                quantity, tradingPair, exchangeId);
            
            return await _paperTradingService.SimulateMarketBuyOrderAsync(
                exchangeId, tradingPair, quantity, cancellationToken);
        }
        
        _logger.LogInformation("Placing market buy order: {Quantity} {TradingPair} on {ExchangeId}", 
            quantity, tradingPair, exchangeId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Get the exchange client
            var client = await GetAuthenticatedClientAsync(exchangeId, cancellationToken);
            
            // Place the order
            var order = await client.PlaceMarketOrderAsync(
                tradingPair, 
                OrderSide.Buy, 
                quantity, 
                cancellationToken);
            
            stopwatch.Stop();
            
            // Create trade result from order
            var result = CreateTradeResultFromOrder(order, OrderSide.Buy, stopwatch.ElapsedMilliseconds);
            
            // Log the result
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Market buy order executed successfully: {Quantity} {TradingPair} on {ExchangeId} in {ElapsedMs}ms", 
                    quantity, tradingPair, exchangeId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "Market buy order failed: {Quantity} {TradingPair} on {ExchangeId} in {ElapsedMs}ms: {ErrorMessage}", 
                    quantity, tradingPair, exchangeId, stopwatch.ElapsedMilliseconds, result.ErrorMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Error placing market buy order: {Quantity} {TradingPair} on {ExchangeId}", 
                quantity, tradingPair, exchangeId);
            
            return TradeResult.Failure(ex, stopwatch.ElapsedMilliseconds);
        }
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> PlaceMarketSellOrderAsync(
        string exchangeId, 
        TradingPair tradingPair, 
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        // Check if paper trading is enabled
        if (_paperTradingService.IsPaperTradingEnabled)
        {
            _logger.LogInformation("Using paper trading for market sell order: {Quantity} {TradingPair} on {ExchangeId}", 
                quantity, tradingPair, exchangeId);
            
            return await _paperTradingService.SimulateMarketSellOrderAsync(
                exchangeId, tradingPair, quantity, cancellationToken);
        }
        
        _logger.LogInformation("Placing market sell order: {Quantity} {TradingPair} on {ExchangeId}", 
            quantity, tradingPair, exchangeId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Get the exchange client
            var client = await GetAuthenticatedClientAsync(exchangeId, cancellationToken);
            
            // Place the order
            var order = await client.PlaceMarketOrderAsync(
                tradingPair, 
                OrderSide.Sell, 
                quantity, 
                cancellationToken);
            
            stopwatch.Stop();
            
            // Create trade result from order
            var result = CreateTradeResultFromOrder(order, OrderSide.Sell, stopwatch.ElapsedMilliseconds);
            
            // Log the result
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Market sell order executed successfully: {Quantity} {TradingPair} on {ExchangeId} in {ElapsedMs}ms", 
                    quantity, tradingPair, exchangeId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "Market sell order failed: {Quantity} {TradingPair} on {ExchangeId} in {ElapsedMs}ms: {ErrorMessage}", 
                    quantity, tradingPair, exchangeId, stopwatch.ElapsedMilliseconds, result.ErrorMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Error placing market sell order: {Quantity} {TradingPair} on {ExchangeId}", 
                quantity, tradingPair, exchangeId);
            
            return TradeResult.Failure(ex, stopwatch.ElapsedMilliseconds);
        }
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> PlaceLimitBuyOrderAsync(
        string exchangeId, 
        TradingPair tradingPair, 
        decimal price,
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Placing limit buy order: {Quantity} {TradingPair} @ {Price} on {ExchangeId}", 
            quantity, tradingPair, price, exchangeId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var client = await GetAuthenticatedClientAsync(exchangeId, cancellationToken);
            var result = await client.PlaceLimitOrderAsync(tradingPair, OrderSide.Buy, price, quantity, OrderType.Limit, cancellationToken);
            
            stopwatch.Stop();
            
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Limit buy order placed successfully: {Quantity} {TradingPair} @ {Price} on {ExchangeId} in {ElapsedMs}ms", 
                    quantity, tradingPair, price, exchangeId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "Limit buy order failed: {Quantity} {TradingPair} @ {Price} on {ExchangeId} in {ElapsedMs}ms: {ErrorMessage}", 
                    quantity, tradingPair, price, exchangeId, stopwatch.ElapsedMilliseconds, result.ErrorMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Error placing limit buy order: {Quantity} {TradingPair} @ {Price} on {ExchangeId}", 
                quantity, tradingPair, price, exchangeId);
            
            return TradeResult.Failure(ex, stopwatch.ElapsedMilliseconds);
        }
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> PlaceLimitSellOrderAsync(
        string exchangeId, 
        TradingPair tradingPair, 
        decimal price,
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Placing limit sell order: {Quantity} {TradingPair} @ {Price} on {ExchangeId}", 
            quantity, tradingPair, price, exchangeId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var client = await GetAuthenticatedClientAsync(exchangeId, cancellationToken);
            var result = await client.PlaceLimitOrderAsync(tradingPair, OrderSide.Sell, price, quantity, OrderType.Limit, cancellationToken);
            
            stopwatch.Stop();
            
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Limit sell order placed successfully: {Quantity} {TradingPair} @ {Price} on {ExchangeId} in {ElapsedMs}ms", 
                    quantity, tradingPair, price, exchangeId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "Limit sell order failed: {Quantity} {TradingPair} @ {Price} on {ExchangeId} in {ElapsedMs}ms: {ErrorMessage}", 
                    quantity, tradingPair, price, exchangeId, stopwatch.ElapsedMilliseconds, result.ErrorMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Error placing limit sell order: {Quantity} {TradingPair} @ {Price} on {ExchangeId}", 
                quantity, tradingPair, price, exchangeId);
            
            return TradeResult.Failure(ex, stopwatch.ElapsedMilliseconds);
        }
    }
    
    /// <inheritdoc />
    public async Task<(TradeResult BuyResult, TradeResult SellResult)> ExecuteArbitrageAsync(
        ArbitrageOpportunity opportunity, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing arbitrage: {TradingPair} | Buy: {BuyExchange} ({BuyPrice}) | Sell: {SellExchange} ({SellPrice})",
            opportunity.TradingPair,
            opportunity.BuyExchangeId,
            opportunity.BuyPrice,
            opportunity.SellExchangeId,
            opportunity.SellPrice);
        
        try
        {
            // Calculate the maximum trade size
            decimal maxTradeQuantity = Math.Min(opportunity.EffectiveQuantity, GetMaxTradeSize(opportunity));
            
            _logger.LogInformation("Calculated max trade size: {MaxTradeQuantity} {BaseCurrency}",
                maxTradeQuantity, opportunity.TradingPair.BaseCurrency);
            
            // Place the buy order
            var buyTask = PlaceMarketBuyOrderAsync(
                opportunity.BuyExchangeId, 
                opportunity.TradingPair, 
                maxTradeQuantity, 
                cancellationToken);
            
            // Wait for the buy order to complete before placing the sell order
            var buyResult = await buyTask;
            
            TradeResult sellResult;
            
            if (buyResult.IsSuccess)
            {
                // Adjust sell quantity based on the actual bought quantity, accounting for fees
                decimal actualBuyQuantity = buyResult.ExecutedQuantity;
                
                // Place the sell order
                sellResult = await PlaceMarketSellOrderAsync(
                    opportunity.SellExchangeId, 
                    opportunity.TradingPair, 
                    actualBuyQuantity, 
                    cancellationToken);
            }
            else
            {
                // Buy failed, don't place the sell order
                sellResult = TradeResult.Failure("Buy order failed, sell order was not placed", 0);
            }
            
            return (buyResult, sellResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing arbitrage opportunity");
            
            return (
                TradeResult.Failure("Exception during arbitrage execution: " + ex.Message, 0),
                TradeResult.Failure("Exception during arbitrage execution: " + ex.Message, 0)
            );
        }
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>> GetAllBalancesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exchanges = _exchangeFactory.GetSupportedExchanges();
            var result = new Dictionary<string, IReadOnlyCollection<Balance>>();
            
            foreach (var exchangeId in exchanges)
            {
                var client = await GetAuthenticatedClientAsync(exchangeId, cancellationToken);
                var balances = await client.GetBalancesAsync(cancellationToken);
                result[exchangeId] = balances;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all balances");
            return new Dictionary<string, IReadOnlyCollection<Balance>>();
        }
    }
    
    /// <inheritdoc />
    public async Task<Balance?> GetBalanceAsync(
        string exchangeId, 
        string asset, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync(exchangeId, cancellationToken);
            
            // Since IExchangeClient doesn't have GetBalanceAsync directly, we get all balances and filter
            var balances = await client.GetBalancesAsync(cancellationToken);
            
            // Find the specific asset
            var balance = balances.FirstOrDefault(b => b.Currency.Equals(asset, StringComparison.OrdinalIgnoreCase));
            
            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance for {Asset} on {ExchangeId}", asset, exchangeId);
            return null;
        }
    }
    
    /// <summary>
    /// Gets an authenticated exchange client for the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated exchange client.</returns>
    private async Task<IExchangeClient> GetAuthenticatedClientAsync(string exchangeId, CancellationToken cancellationToken = default)
    {
        // Create the client instance
        var client = _exchangeFactory.CreateClient(exchangeId);
        
        // Ensure it's authenticated
        if (!client.IsAuthenticated)
        {
            await client.AuthenticateAsync(cancellationToken);
        }
        
        return client;
    }
    
    /// <summary>
    /// Determines the maximum trade size for the arbitrage opportunity.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity.</param>
    /// <returns>The maximum trade size.</returns>
    private decimal GetMaxTradeSize(ArbitrageOpportunity opportunity)
    {
        // Get the effective quantity from the opportunity 
        // This is already min(buyQty, sellQty)
        var effectiveQty = opportunity.EffectiveQuantity;
        
        // Apply any additional constraints
        var maxQty = effectiveQty;
        
        return maxQty;
    }
    
    /// <summary>
    /// Creates a trade result from an order.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="side">The order side.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>The trade result.</returns>
    private static TradeResult CreateTradeResultFromOrder(Order order, OrderSide side, long executionTimeMs = 0)
    {
        var result = new TradeResult
        {
            IsSuccess = order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled,
            OrderId = order.Id,
            Timestamp = order.Timestamp,
            TradingPair = order.TradingPair,
            TradeType = side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            RequestedPrice = order.Price,
            ExecutedPrice = order.AverageFillPrice > 0 ? order.AverageFillPrice : order.Price,
            RequestedQuantity = order.Quantity,
            ExecutedQuantity = order.FilledQuantity,
            TotalValue = order.Price * order.Quantity,
            Fee = 0, // Fee information not available in Order
            FeeCurrency = order.TradingPair.QuoteCurrency,
            ErrorMessage = order.Status == OrderStatus.Rejected ? "Order was rejected by the exchange" : null,
            ExecutionTimeMs = executionTimeMs
        };
        
        return result;
    }
} 