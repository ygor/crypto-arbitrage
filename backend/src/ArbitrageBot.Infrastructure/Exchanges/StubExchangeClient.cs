using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArbitrageBot.Infrastructure.Exchanges;

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
    private readonly Dictionary<TradingPair, OrderBook> _orderBooks = new();
    private readonly Dictionary<string, Balance> _balances = new();
    private readonly Dictionary<TradingPair, Channel<OrderBook>> _orderBookChannels = new();
    private readonly Dictionary<TradingPair, Task> _orderBookUpdateTasks = new();
    private readonly Dictionary<TradingPair, CancellationTokenSource> _orderBookCts = new();
    
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
    public Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Must be connected before getting order book snapshot");
        }
        
        _logger.LogInformation("Getting order book snapshot for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        if (!_orderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            orderBook = GenerateDummyOrderBook(tradingPair);
            _orderBooks[tradingPair] = orderBook;
        }
        
        return Task.FromResult(orderBook);
    }
    
    /// <inheritdoc />
    public Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Must be connected before subscribing to order book");
        }
        
        _logger.LogInformation("Subscribing to order book for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        // Create initial dummy order book
        var initialOrderBook = GenerateDummyOrderBook(tradingPair);
        _orderBooks[tradingPair] = initialOrderBook;
        
        // Create a channel for this trading pair if it doesn't exist
        if (!_orderBookChannels.TryGetValue(tradingPair, out _))
        {
            var channel = Channel.CreateUnbounded<OrderBook>();
            _orderBookChannels[tradingPair] = channel;
            
            // Create a cancellation token source for this subscription
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _orderBookCts[tradingPair] = cts;
            
            // Start a background task to periodically generate order book updates
            var updateTask = Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        // Generate a new order book with slight variations
                        var orderBook = GenerateDummyOrderBook(tradingPair);
                        _orderBooks[tradingPair] = orderBook;
                        
                        // Write to channel
                        await channel.Writer.WriteAsync(orderBook, cts.Token);
                        
                        // Wait before next update
                        await Task.Delay(_random.Next(100, 500), cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in order book update task for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
                }
                finally
                {
                    // Complete the channel when the task ends
                    channel.Writer.Complete();
                }
            }, cts.Token);
            
            _orderBookUpdateTasks[tradingPair] = updateTask;
        }
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unsubscribing from order book for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        // Cancel the update task
        if (_orderBookCts.TryGetValue(tradingPair, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _orderBookCts.Remove(tradingPair);
        }
        
        // Wait for the task to complete
        if (_orderBookUpdateTasks.TryGetValue(tradingPair, out var task))
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
                _orderBookUpdateTasks.Remove(tradingPair);
            }
        }
        
        // Remove the channel
        _orderBookChannels.Remove(tradingPair);
        
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
        if (!_orderBookChannels.TryGetValue(tradingPair, out var channel))
        {
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            if (!_orderBookChannels.TryGetValue(tradingPair, out channel))
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
        
        if (!_orderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            orderBook = GenerateDummyOrderBook(tradingPair);
            _orderBooks[tradingPair] = orderBook;
        }
        
        return Task.FromResult(orderBook);
    }
    
    /// <inheritdoc />
    public Task<Order> PlaceMarketOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        _logger.LogInformation("Placing stub market {Side} order for {Quantity} {TradingPair}", 
            orderSide, quantity, tradingPair);
        
        try
        {
            // Generate a unique order ID
            var orderId = $"stub-{Guid.NewGuid():N}";
            
            // For a stub implementation, we simulate a successful order
            // Get the current price from the order book or generate a random one
            var price = GetOrderBookPriceOrRandom(tradingPair, orderSide);
            
            // Update balances
            UpdateBalances(tradingPair, orderSide, quantity, price);
            
            // Create the order
            var order = new Order(
                orderId,
                ExchangeId,
                tradingPair,
                orderSide,
                OrderType.Market,
                OrderStatus.Filled,
                price,
                quantity,
                DateTime.UtcNow);
            
            // Set filled quantity and average fill price
            order.FilledQuantity = quantity;
            order.AverageFillPrice = price;
            
            _logger.LogInformation("Stub market order placed: OrderId={OrderId}, Price={Price}, Quantity={Quantity}", 
                orderId, price, quantity);
            
            return Task.FromResult(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing stub market order");
            throw;
        }
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
                DateTimeOffset.UtcNow);
            
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
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            ValidateConnected();
            ValidateAuthenticated();

            _logger.LogInformation("Simulating limit {Side} order for {Quantity} {BaseCurrency} at {Price} {QuoteCurrency} on {ExchangeId}",
                orderSide, quantity, tradingPair.BaseCurrency, price, tradingPair.QuoteCurrency, ExchangeId);

            // Simulate network latency
            await Task.Delay(_random.Next(50, 300), cancellationToken);

            // Ensure order book exists
            if (!_orderBooks.TryGetValue(tradingPair, out var orderBook))
            {
                orderBook = GenerateDummyOrderBook(tradingPair);
                _orderBooks[tradingPair] = orderBook;
            }

            var tradeId = Guid.NewGuid().ToString("N");
            bool wouldExecute = false;
            decimal executionPrice = price;

            if (orderSide == OrderSide.Buy)
            {
                // Buy order: Check if there are any asks at or below our limit price
                var bestAsk = orderBook.Asks.FirstOrDefault();
                wouldExecute = bestAsk.Price <= price && bestAsk.Quantity > 0;
                
                if (wouldExecute)
                {
                    _logger.LogInformation("Buy limit order would execute immediately at {Price} (limit: {LimitPrice})", 
                        bestAsk.Price, price);
                    executionPrice = bestAsk.Price; // Use the actual price we'd get
                }
                else
                {
                    _logger.LogInformation("Buy limit order would be placed on the order book at {LimitPrice}", price);
                }

                // Check balance for quote currency (e.g., USDT in BTC/USDT)
                if (!_balances.TryGetValue(tradingPair.QuoteCurrency, out var quoteBalance))
                {
                    _logger.LogError("No balance found for {Currency} on {ExchangeId}", tradingPair.QuoteCurrency, ExchangeId);
                    return TradeResult.Failure($"No balance found for {tradingPair.QuoteCurrency}", stopwatch.ElapsedMilliseconds);
                }

                // Calculate total cost including fees
                var totalCost = price * quantity;
                var fee = totalCost * GetExchangeFee(ExchangeId);
                var totalWithFees = totalCost + fee;

                if (quoteBalance.Available < totalWithFees)
                {
                    _logger.LogError("Insufficient {Currency} balance ({Available}) for order requiring {Required}",
                        tradingPair.QuoteCurrency, quoteBalance.Available, totalWithFees);
                    return TradeResult.Failure($"Insufficient {tradingPair.QuoteCurrency} balance", stopwatch.ElapsedMilliseconds);
                }

                if (wouldExecute)
                {
                    // If the order would execute immediately, update balances
                    _balances[tradingPair.QuoteCurrency] = quoteBalance.WithAvailable(quoteBalance.Available - totalWithFees);
                    
                    // Add base currency if it doesn't exist
                    if (!_balances.TryGetValue(tradingPair.BaseCurrency, out var baseBalance))
                    {
                        baseBalance = new Balance(ExchangeId, tradingPair.BaseCurrency, 0, 0);
                        _balances[tradingPair.BaseCurrency] = baseBalance;
                    }
                    
                    // Update base currency balance
                    _balances[tradingPair.BaseCurrency] = new Balance(
                        ExchangeId, 
                        tradingPair.BaseCurrency, 
                        baseBalance.Total + quantity, 
                        baseBalance.Available + quantity);

                    _logger.LogInformation("Updated balances after buy: {QuoteCurrency}={QuoteBalance}, {BaseCurrency}={BaseBalance}",
                        tradingPair.QuoteCurrency, _balances[tradingPair.QuoteCurrency].Total,
                        tradingPair.BaseCurrency, _balances[tradingPair.BaseCurrency].Total);

                    // Create trade execution for an executed order
                    var tradeExecution = new TradeExecution(
                        tradeId,
                        ExchangeId,
                        tradingPair,
                        OrderSide.Buy,
                        orderType,
                        executionPrice,
                        quantity,
                        fee,
                        tradingPair.QuoteCurrency,
                        DateTimeOffset.UtcNow,
                        null);

                    return TradeResult.Success(tradeExecution, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    // If the order would be placed on the book, create a success result with no execution
                    return new TradeResult
                    {
                        IsSuccess = true,
                        OrderId = tradeId,
                        ClientOrderId = tradeId,
                        Timestamp = DateTimeOffset.UtcNow,
                        TradingPair = tradingPair,
                        TradeType = TradeType.Buy,
                        RequestedPrice = price,
                        RequestedQuantity = quantity,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }
            }
            else // Sell order
            {
                // Sell order: Check if there are any bids at or above our limit price
                var bestBid = orderBook.Bids.FirstOrDefault();
                wouldExecute = bestBid.Price >= price && bestBid.Quantity > 0;
                
                if (wouldExecute)
                {
                    _logger.LogInformation("Sell limit order would execute immediately at {Price} (limit: {LimitPrice})", 
                        bestBid.Price, price);
                    executionPrice = bestBid.Price; // Use the actual price we'd get
                }
                else
                {
                    _logger.LogInformation("Sell limit order would be placed on the order book at {LimitPrice}", price);
                }

                // Check balance for base currency (e.g., BTC in BTC/USDT)
                if (!_balances.TryGetValue(tradingPair.BaseCurrency, out var baseBalance))
                {
                    _logger.LogError("No balance found for {Currency} on {ExchangeId}", tradingPair.BaseCurrency, ExchangeId);
                    return TradeResult.Failure($"No balance found for {tradingPair.BaseCurrency}", stopwatch.ElapsedMilliseconds);
                }

                if (baseBalance.Available < quantity)
                {
                    _logger.LogError("Insufficient {Currency} balance ({Available}) for order requiring {Required}",
                        tradingPair.BaseCurrency, baseBalance.Available, quantity);
                    return TradeResult.Failure($"Insufficient {tradingPair.BaseCurrency} balance", stopwatch.ElapsedMilliseconds);
                }

                if (wouldExecute)
                {
                    // If the order would execute immediately, update balances
                    _balances[tradingPair.BaseCurrency] = baseBalance.WithAvailable(baseBalance.Available - quantity);
                    
                    // Calculate total proceeds and fees
                    var totalProceeds = executionPrice * quantity;
                    var fee = totalProceeds * GetExchangeFee(ExchangeId);
                    var totalAfterFees = totalProceeds - fee;
                    
                    // Add quote currency if it doesn't exist
                    if (!_balances.TryGetValue(tradingPair.QuoteCurrency, out var quoteBalance))
                    {
                        quoteBalance = new Balance(ExchangeId, tradingPair.QuoteCurrency, 0, 0);
                        _balances[tradingPair.QuoteCurrency] = quoteBalance;
                    }
                    
                    // Update quote currency balance
                    _balances[tradingPair.QuoteCurrency] = new Balance(
                        ExchangeId,
                        tradingPair.QuoteCurrency, 
                        quoteBalance.Total + totalAfterFees, 
                        quoteBalance.Available + totalAfterFees);

                    _logger.LogInformation("Updated balances after sell: {BaseCurrency}={BaseBalance}, {QuoteCurrency}={QuoteBalance}",
                        tradingPair.BaseCurrency, _balances[tradingPair.BaseCurrency].Total,
                        tradingPair.QuoteCurrency, _balances[tradingPair.QuoteCurrency].Total);

                    // Create trade execution for an executed order
                    var tradeExecution = new TradeExecution(
                        tradeId,
                        ExchangeId,
                        tradingPair,
                        OrderSide.Sell,
                        orderType,
                        executionPrice,
                        quantity,
                        fee,
                        tradingPair.QuoteCurrency,
                        DateTimeOffset.UtcNow,
                        null);

                    return TradeResult.Success(tradeExecution, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    // If the order would be placed on the book, create a success result with no execution
                    return new TradeResult
                    {
                        IsSuccess = true,
                        OrderId = tradeId,
                        ClientOrderId = tradeId,
                        Timestamp = DateTimeOffset.UtcNow,
                        TradingPair = tradingPair,
                        TradeType = TradeType.Sell,
                        RequestedPrice = price,
                        RequestedQuantity = quantity,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing limit order on {ExchangeId}: {Message}", ExchangeId, ex.Message);
            return TradeResult.Failure(ex, stopwatch.ElapsedMilliseconds);
        }
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
                    DateTimeOffset.UtcNow
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
                    DateTimeOffset.UtcNow
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
                    DateTimeOffset.UtcNow
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
                    DateTimeOffset.UtcNow
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
                    DateTimeOffset.UtcNow
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
                    DateTimeOffset.UtcNow
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
} 