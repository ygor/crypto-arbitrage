using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Exchanges;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CryptoArbitrage.Tests.TestInfrastructure;

/// <summary>
/// Extension methods for Random.
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    /// Returns a random decimal between min and max.
    /// </summary>
    /// <param name="random">The random instance.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <returns>A random decimal value.</returns>
    public static decimal NextDecimal(this Random random, decimal min, decimal max)
    {
        return (decimal)(random.NextDouble() * (double)(max - min)) + min;
    }
}

/// <summary>
/// A stub implementation of IExchangeClient for testing purposes.
/// </summary>
public class StubExchangeClient : IExchangeClient
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<StubExchangeClient> _logger;
    private readonly Random _random = new Random();
    
    private bool _isConnected;
    private bool _isAuthenticated;
    private readonly Dictionary<string, OrderBook> _orderBooks = new();
    private readonly Dictionary<string, Balance> _balances = new();
    private readonly Dictionary<string, Channel<OrderBook>> _orderBookChannels = new();
    private readonly Dictionary<string, Task> _orderBookUpdateTasks = new();
    private readonly Dictionary<string, CancellationTokenSource> _orderBookCts = new();
    
    /// <inheritdoc />
    public string ExchangeId { get; }
    
    /// <inheritdoc />
    public bool SupportsStreaming => true;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StubExchangeClient"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public StubExchangeClient(
        string exchangeId,
        IConfigurationService configurationService,
        ILogger<StubExchangeClient> logger)
    {
        ExchangeId = exchangeId;
        _configurationService = configurationService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public bool IsConnected => _isConnected;
    
    /// <inheritdoc />
    public bool IsAuthenticated => _isAuthenticated;
    
    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to {ExchangeId}", ExchangeId);
        
        // Simulate connection delay
        Task.Delay(100, cancellationToken).GetAwaiter().GetResult();
        
        _isConnected = true;
        _logger.LogInformation("Connected to {ExchangeId}", ExchangeId);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting from {ExchangeId}", ExchangeId);
        
        // Simulate disconnection delay
        Task.Delay(50, cancellationToken).GetAwaiter().GetResult();
        
        // Cancel all order book update tasks
        foreach (var cts in _orderBookCts.Values)
        {
            cts.Cancel();
        }
        
        _isConnected = false;
        _isAuthenticated = false;
        
        _logger.LogInformation("Disconnected from {ExchangeId}", ExchangeId);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Must be connected before authenticating");
        }
        
        _logger.LogInformation("Authenticating with {ExchangeId}", ExchangeId);
        
        // Simulate authentication delay
        Task.Delay(200, cancellationToken).GetAwaiter().GetResult();
        
        _isAuthenticated = true;
        
        // Initialize some dummy balances
        _balances["BTC"] = new Balance(ExchangeId, "BTC", 1.0m, 0m, 1.0m);
        _balances["ETH"] = new Balance(ExchangeId, "ETH", 10.0m, 0m, 10.0m);
        _balances["XRP"] = new Balance(ExchangeId, "XRP", 1000.0m, 0m, 1000.0m);
        _balances["USDT"] = new Balance(ExchangeId, "USDT", 50000.0m, 0m, 50000.0m);
        _balances["USD"] = new Balance(ExchangeId, "USD", 50000.0m, 0m, 50000.0m);
        _balances["EUR"] = new Balance(ExchangeId, "EUR", 50000.0m, 0m, 50000.0m);
        
        _logger.LogInformation("Authenticated with {ExchangeId}", ExchangeId);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public async Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        var orderBookKey = tradingPair.ToString();
        
        // Try to get from cache first
        if (_orderBooks.TryGetValue(orderBookKey, out var orderBook))
        {
            return orderBook;
        }
        
        // Generate a new order book
        orderBook = await GetOrderBookAsync(tradingPair, depth, cancellationToken);
        
        // Cache the order book
        _orderBooks[orderBookKey] = orderBook;
        
        return orderBook;
    }
    
    /// <inheritdoc />
    public async Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, _logger);
        _logger.LogInformation("Subscribing to order book for {TradingPair} ({Symbol}) on {ExchangeId}", tradingPair, symbol, ExchangeId);
        
        var key = tradingPair.ToString();
        
        // If already subscribed, do nothing
        if (_orderBookChannels.ContainsKey(key))
        {
            return;
        }
        
        // Create a channel for this trading pair
        var channel = Channel.CreateUnbounded<OrderBook>();
        _orderBookChannels[key] = channel;
        
        // Create a cancellation token source for this trading pair
        var cts = new CancellationTokenSource();
        _orderBookCts[key] = cts;
        
        // Start a background task to generate order book updates
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
        var token = linkedCts.Token;
        
        _orderBookUpdateTasks[key] = Task.Run(async () =>
        {
            try
            {
                var orderBook = GenerateDummyOrderBook(tradingPair);
                
                // Cache the initial order book
                _orderBooks[key] = orderBook;
                
                // Write the initial order book to the channel
                await channel.Writer.WriteAsync(orderBook, token);
                
                // Start generating updates
                var random = new Random();
                while (!token.IsCancellationRequested)
                {
                    // Wait for random interval (100-1000ms)
                    await Task.Delay(random.Next(100, 1000), token);
                    
                    // Generate a new order book
                    orderBook = GenerateDummyOrderBook(tradingPair);
                    
                    // Cache the updated order book
                    _orderBooks[key] = orderBook;
                    
                    // Write the updated order book to the channel
                    await channel.Writer.WriteAsync(orderBook, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                _logger.LogInformation("Order book subscription canceled for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating order book updates for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
            }
            finally
            {
                // Complete the channel
                channel.Writer.Complete();
            }
        }, token);
    }
    
    /// <inheritdoc />
    public Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unsubscribing from order book for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        // Cancel the update task
        if (_orderBookCts.TryGetValue(tradingPair.ToString(), out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _orderBookCts.Remove(tradingPair.ToString());
        }
        
        // Wait for the task to complete
        if (_orderBookUpdateTasks.TryGetValue(tradingPair.ToString(), out var task))
        {
            try
            {
                task.Wait(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for order book update task to complete for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
            }
            finally
            {
                _orderBookUpdateTasks.Remove(tradingPair.ToString());
            }
        }
        
        // Remove the channel
        _orderBookChannels.Remove(tradingPair.ToString());
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public async IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(
        TradingPair tradingPair, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Must be connected before getting order book updates");
        }
        
        // Subscribe if not already subscribed
        if (!_orderBookChannels.TryGetValue(tradingPair.ToString(), out var channel))
        {
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            if (!_orderBookChannels.TryGetValue(tradingPair.ToString(), out channel))
            {
                yield break;
            }
        }
        
        // Stream updates from the channel
        await foreach (var orderBook in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return orderBook;
        }
    }
    
    /// <inheritdoc />
    public Task<Balance> GetBalanceAsync(string currency, CancellationToken cancellationToken = default)
    {
        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Must be authenticated before getting balance");
        }
        
        _logger.LogInformation("Getting balance for {Currency} on {ExchangeId}", currency, ExchangeId);
        
        if (_balances.TryGetValue(currency.ToUpperInvariant(), out var balance))
        {
            return Task.FromResult(balance);
        }
        
        // Return zero balance if currency not found
        return Task.FromResult(new Balance(ExchangeId, currency, 0, 0, 0));
    }
    
    /// <inheritdoc />
    public Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Must be authenticated before getting balances");
        }
        
        _logger.LogInformation("Getting all balances on {ExchangeId}", ExchangeId);
        
        return Task.FromResult<IReadOnlyCollection<Balance>>(_balances.Values.ToList());
    }
    
    /// <inheritdoc />
    public Task<OrderBook> GetOrderBookAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Must be connected before getting order book");
        }
        
        _logger.LogInformation("Getting order book for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        if (!_orderBooks.TryGetValue(tradingPair.ToString(), out var orderBook))
        {
            orderBook = GenerateDummyOrderBook(tradingPair);
            _orderBooks[tradingPair.ToString()] = orderBook;
        }
        
        return Task.FromResult(orderBook);
    }
    
    /// <inheritdoc />
    public async Task<Order> PlaceMarketOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, _logger);
        var side = ExchangeUtils.FormatOrderSide(orderSide, ExchangeId);
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        _logger.LogInformation("Placing market {Side} order for {Quantity} {TradingPair} ({Symbol}) on {ExchangeId}", 
            side, quantity, tradingPair, symbol, ExchangeId);
        
        // Update balances
        UpdateBalances(tradingPair, orderSide, quantity, GetOrderBookPriceOrRandom(tradingPair, orderSide));
        
        // Create a new order
        var now = DateTime.UtcNow;
        var order = new Order(
            Guid.NewGuid().ToString(),
            ExchangeId,
            tradingPair,
            orderSide,
            OrderType.Market,
            OrderStatus.Filled,
            GetOrderBookPriceOrRandom(tradingPair, orderSide),
            quantity,
            now);
        
        // Set filled quantity
        order.FilledQuantity = quantity;
        
        return order;
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> PlaceMarketOrderLegacyAsync(
        TradingPair tradingPair, 
        OrderSide side, 
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        try
        {
            // Log the order placement
            _logger.LogInformation("Placing market {Side} order for {Quantity} {Symbol} on {ExchangeId}",
                side, quantity, tradingPair, ExchangeId);
            
            // Simulate execution time
            await Task.Delay(100, cancellationToken);
            
            // Generate a realistic price based on the order book
            decimal price = GetOrderBookPriceOrRandom(tradingPair, side);
            
            // Update balances
            UpdateBalances(tradingPair, side, quantity, price);
            
            // Generate a unique order ID
            string orderId = Guid.NewGuid().ToString();
            
            // Create a trade execution
            var tradeExecution = new TradeExecution(
                orderId,
                ExchangeId,
                tradingPair,
                side,
                OrderType.Market,
                price,
                quantity,
                price * quantity * 0.001m, // 0.1% fee
                side == OrderSide.Buy ? tradingPair.QuoteCurrency : tradingPair.BaseCurrency,
                DateTimeOffset.UtcNow.DateTime);
            
            // Return a success trade result
            return TradeResult.Success(tradeExecution, 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing market {Side} order for {Quantity} {Symbol} on {ExchangeId}",
                side, quantity, tradingPair, ExchangeId);
            
            return TradeResult.Failure(ex, 100);
        }
    }
    
    /// <inheritdoc />
    public async Task<TradeResult> PlaceLimitOrderAsync(
        TradingPair tradingPair,
        OrderSide orderSide,
        decimal price,
        decimal quantity,
        OrderType orderType = OrderType.Limit,
        CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, _logger);
        var side = ExchangeUtils.FormatOrderSide(orderSide, ExchangeId);
        var type = ExchangeUtils.FormatOrderType(orderType, ExchangeId);
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        _logger.LogInformation("Placing {Type} {Side} order for {Quantity} {TradingPair} ({Symbol}) at {Price} on {ExchangeId}", 
            type, side, quantity, tradingPair, symbol, price, ExchangeId);
        
        // Update balances
        UpdateBalances(tradingPair, orderSide, quantity, price);
        
        // Create a successful trade result
        var tradeResult = new TradeResult
        {
            IsSuccess = true,
            OrderId = Guid.NewGuid().ToString(),
            ClientOrderId = clientOrderId,
            TradingPair = tradingPair.ToString(),
            TradeType = orderSide == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            RequestedPrice = price,
            ExecutedPrice = price,
            RequestedQuantity = quantity,
            ExecutedQuantity = quantity,
            TotalValue = price * quantity,
            Fee = price * quantity * GetExchangeFee(ExchangeId),
            FeeCurrency = tradingPair.QuoteCurrency,
            Timestamp = DateTimeOffset.UtcNow.DateTime,
            ExecutionTimeMs = 500 // Simulate 500ms execution time
        };
        
        return tradeResult;
    }
    
    /// <inheritdoc />
    public Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting fee schedule for {ExchangeId}", ExchangeId);
        
        // Return a standard fee schedule for testing purposes
        return Task.FromResult(new FeeSchedule(
            ExchangeId,
            0.001m, // 0.1% maker fee
            0.002m, // 0.2% taker fee
            0.0m    // No withdrawal fee
        ));
    }
    
    /// <inheritdoc />
    public async Task<decimal> GetTradingFeeRateAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        _logger.LogInformation("Getting trading fee rate for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        // Simulate network delay
        await Task.Delay(50, cancellationToken);
        
        // Retrieve fee schedule
        var feeSchedule = await GetFeeScheduleAsync(cancellationToken);
        
        // Return taker fee rate (more conservative)
        return feeSchedule.TakerFeeRate;
    }
    
    private OrderBook GenerateDummyOrderBook(TradingPair tradingPair)
    {
        var midPrice = GenerateRandomPrice(tradingPair);
        var bids = new List<OrderBookEntry>();
        var asks = new List<OrderBookEntry>();
        
        // Generate 20 bid levels (highest to lowest)
        for (int i = 0; i < 20; i++)
        {
            var price = midPrice * (1 - (i * 0.001m));
            var quantity = _random.NextDecimal(0.1m, 10m);
            bids.Add(new OrderBookEntry(price, quantity));
        }
        
        // Generate 20 ask levels (lowest to highest)
        for (int i = 0; i < 20; i++)
        {
            var price = midPrice * (1 + (i * 0.001m));
            var quantity = _random.NextDecimal(0.1m, 10m);
            asks.Add(new OrderBookEntry(price, quantity));
        }
        
        // Sort bids (highest first) and asks (lowest first)
        bids = bids.OrderByDescending(b => b.Price).ToList();
        asks = asks.OrderBy(a => a.Price).ToList();
        
        return new OrderBook(
            ExchangeId,
            tradingPair,
            DateTime.UtcNow,
            bids,
            asks);
    }
    
    private void UpdateBalances(TradingPair tradingPair, OrderSide orderSide, decimal quantity, decimal price)
    {
        // Implementation depends on the order side
        if (orderSide == OrderSide.Buy)
        {
            // For buy orders: deduct quote currency, add base currency
            var baseCurrency = tradingPair.BaseCurrency;
            var quoteCurrency = tradingPair.QuoteCurrency;
            var cost = price * quantity;
            
            // Update quote currency (decrease)
            if (_balances.TryGetValue(quoteCurrency, out var quoteBalance))
            {
                _balances[quoteCurrency] = new Balance(
                    ExchangeId,
                    quoteCurrency,
                    quoteBalance.Total - cost,
                    quoteBalance.Available - cost,
                    quoteBalance.Reserved,
                    DateTimeOffset.UtcNow.DateTime
                );
            }
            
            // Update base currency (increase)
            if (_balances.TryGetValue(baseCurrency, out var baseBalance))
            {
                _balances[baseCurrency] = new Balance(
                    ExchangeId,
                    baseCurrency,
                    baseBalance.Total + quantity,
                    baseBalance.Available + quantity,
                    baseBalance.Reserved,
                    DateTimeOffset.UtcNow.DateTime
                );
            }
            else
            {
                _balances[baseCurrency] = new Balance(
                    ExchangeId,
                    baseCurrency,
                    quantity,
                    quantity,
                    0m,
                    DateTimeOffset.UtcNow.DateTime
                );
            }
        }
        else // OrderSide.Sell
        {
            // For sell orders: deduct base currency, add quote currency
            var baseCurrency = tradingPair.BaseCurrency;
            var quoteCurrency = tradingPair.QuoteCurrency;
            var proceeds = price * quantity;
            
            // Update base currency (decrease)
            if (_balances.TryGetValue(baseCurrency, out var baseBalance))
            {
                _balances[baseCurrency] = new Balance(
                    ExchangeId,
                    baseCurrency,
                    baseBalance.Total - quantity,
                    baseBalance.Available - quantity,
                    baseBalance.Reserved,
                    DateTimeOffset.UtcNow.DateTime
                );
            }
            
            // Update quote currency (increase)
            if (_balances.TryGetValue(quoteCurrency, out var quoteBalance))
            {
                _balances[quoteCurrency] = new Balance(
                    ExchangeId,
                    quoteCurrency,
                    quoteBalance.Total + proceeds,
                    quoteBalance.Available + proceeds,
                    quoteBalance.Reserved,
                    DateTimeOffset.UtcNow.DateTime
                );
            }
            else
            {
                _balances[quoteCurrency] = new Balance(
                    ExchangeId,
                    quoteCurrency,
                    proceeds,
                    proceeds,
                    0m,
                    DateTimeOffset.UtcNow.DateTime
                );
            }
        }
    }
    
    private decimal GetOrderBookPriceOrRandom(TradingPair tradingPair, OrderSide orderSide)
    {
        // Implementation depends on the trading pair and order side
        // This method should return the price from the order book or generate a random price
        // For simplicity, we'll implement a basic random price generation
        return GenerateRandomPrice(tradingPair);
    }
    
    private decimal GenerateRandomPrice(TradingPair tradingPair)
    {
        // Generate a realistic price based on the trading pair
        string pairString = $"{tradingPair.BaseCurrency}/{tradingPair.QuoteCurrency}";
        decimal basePrice;
        decimal variation;
            
        switch (pairString)
        {
            case "BTC/USDT":
                basePrice = 50000m;
                variation = basePrice * 0.01m * (decimal)(_random.NextDouble() - 0.5);
                break;
            case "ETH/USDT":
                basePrice = 3000m;
                variation = basePrice * 0.01m * (decimal)(_random.NextDouble() - 0.5);
                break;
            case "ETH/BTC":
                basePrice = 0.065m;
                variation = basePrice * 0.01m * (decimal)(_random.NextDouble() - 0.5);
                break;
            default:
                basePrice = 100m;
                variation = basePrice * 0.02m * (decimal)(_random.NextDouble() - 0.5);
                break;
        }
            
        return Math.Max(basePrice + variation, 0.00001m);
    }
    
    /// <summary>
    /// Validates that the user is authenticated and throws an exception if not.
    /// </summary>
    private void ValidateAuthenticated()
    {
        if (!_isAuthenticated)
        {
            throw new InvalidOperationException($"Not authenticated with exchange {ExchangeId}");
        }
    }

    /// <summary>
    /// Gets the fee rate for the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>The fee rate as a decimal (e.g., 0.001 for 0.1%).</returns>
    private decimal GetExchangeFee(string exchangeId)
    {
        // Default fee rates for different exchanges
        return exchangeId.ToLowerInvariant() switch
        {
            "binance" => 0.001m,   // 0.1%
            "coinbase" => 0.0015m, // 0.15%
            "kraken" => 0.0026m,   // 0.26%
            "kucoin" => 0.001m,    // 0.1%
            "okx" => 0.0008m,      // 0.08%
            _ => 0.002m            // 0.2% default
        };
    }

    /// <summary>
    /// Validates that the client is connected and throws an exception if not.
    /// </summary>
    private void ValidateConnected()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Not connected to exchange {ExchangeId}");
        }
    }
} 