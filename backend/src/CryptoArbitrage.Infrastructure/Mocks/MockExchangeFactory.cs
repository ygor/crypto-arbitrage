using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Infrastructure.Mocks;

/// <summary>
/// A mock implementation of IExchangeFactory for testing purposes.
/// </summary>
public class MockExchangeFactory : IExchangeFactory
{
    private readonly ILogger<MockExchangeFactory> _logger;
    private readonly Dictionary<string, IExchangeClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MockExchangeFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MockExchangeFactory(ILogger<MockExchangeFactory> logger)
    {
        _logger = logger;
        
        // Initialize with some default mock clients
        _clients["binance"] = new MockExchangeClient("binance", logger);
        _clients["coinbase"] = new MockExchangeClient("coinbase", logger);
        _clients["kraken"] = new MockExchangeClient("kraken", logger);
    }
    
    /// <inheritdoc />
    public IExchangeClient CreateClient(string exchangeId)
    {
        _logger.LogInformation("Creating mock exchange client for {ExchangeId}", exchangeId);
        
        if (_clients.TryGetValue(exchangeId, out var client))
        {
            return client;
        }
        
        var newClient = new MockExchangeClient(exchangeId, _logger);
        _clients[exchangeId] = newClient;
        return newClient;
    }
    
    /// <inheritdoc />
    public async Task<IExchangeClient> CreateExchangeClientAsync(string exchangeId)
    {
        _logger.LogInformation("Creating mock exchange client asynchronously for {ExchangeId}", exchangeId);
        return await Task.FromResult(CreateClient(exchangeId));
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> GetSupportedExchanges()
    {
        return new[]
        {
            "binance",
            "coinbase",
            "kraken",
            "kucoin",
            "okx"
        };
    }
}

/// <summary>
/// A mock implementation of IExchangeClient for testing purposes.
/// </summary>
public class MockExchangeClient : IExchangeClient
{
    private readonly string _exchangeId;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, decimal> _balances = new();
    private readonly ConcurrentDictionary<string, OrderBook> _orderBooks = new();
    private readonly ConcurrentDictionary<string, Order> _orders = new();
    private readonly Random _random = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MockExchangeClient"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="logger">The logger.</param>
    public MockExchangeClient(string exchangeId, ILogger logger)
    {
        _exchangeId = exchangeId;
        _logger = logger;
        
        // Initialize default balances
        _balances["BTC"] = 1.0m;
        _balances["ETH"] = 10.0m;
        _balances["USDT"] = 10000.0m;
        _balances["USDC"] = 10000.0m;
        
        // Initialize with some fake connection state
        IsConnected = false;
        IsAuthenticated = false;
        SupportsStreaming = true;
    }
    
    /// <summary>
    /// Gets the exchange identifier.
    /// </summary>
    public string ExchangeId => _exchangeId;

    /// <summary>
    /// Gets a value indicating whether the client is connected.
    /// </summary>
    public bool IsConnected { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether the client is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether the exchange supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; }

    /// <summary>
    /// Connects to the exchange asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to mock exchange {ExchangeId}", _exchangeId);
        IsConnected = true;
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Disconnects from the exchange asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting from mock exchange {ExchangeId}", _exchangeId);
        IsConnected = false;
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Authenticates with the exchange asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Authenticating with mock exchange {ExchangeId}", _exchangeId);
        IsAuthenticated = true;
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting fee schedule for {ExchangeId} (mock)", _exchangeId);
        
        var feeSchedule = new FeeSchedule(
            _exchangeId,
            0.001m, // 0.1% maker fee rate
            0.002m, // 0.2% taker fee rate
            0.0005m // Standard withdrawal fee
        );
        
        return Task.FromResult(feeSchedule);
    }
    
    /// <summary>
    /// Gets an order book snapshot for a trading pair asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="depth">The depth of the order book.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns the order book snapshot.</returns>
    public Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting order book snapshot for {TradingPair} on {ExchangeId}", tradingPair, _exchangeId);
        
        decimal basePrice = GetRandomPrice(tradingPair);
        
        var bids = new List<OrderBookEntry>();
        var asks = new List<OrderBookEntry>();
        
        // Generate bids slightly below the base price
        for (int i = 0; i < depth; i++)
        {
            var price = basePrice * (1 - 0.001m * (i + 1));
            var quantity = _random.Next(1, 10) * 0.1m;
            bids.Add(new OrderBookEntry(price, quantity));
        }
        
        // Generate asks slightly above the base price
        for (int i = 0; i < depth; i++)
        {
            var price = basePrice * (1 + 0.001m * (i + 1));
            var quantity = _random.Next(1, 10) * 0.1m;
            asks.Add(new OrderBookEntry(price, quantity));
        }
        
        var orderBook = new OrderBook(
            _exchangeId,
            tradingPair,
            DateTime.UtcNow,
            bids,
            asks
        );
        
        _orderBooks[tradingPair.ToString()] = orderBook;
        
        return Task.FromResult(orderBook);
    }
    
    /// <summary>
    /// Gets updates to an order book asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns the order book updates.</returns>
    public async IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(TradingPair tradingPair, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_orderBooks.TryGetValue(tradingPair.ToString(), out var orderBook))
        {
            yield return orderBook;
        }
        else
        {
            orderBook = await GetOrderBookSnapshotAsync(tradingPair, 10, cancellationToken);
            yield return orderBook;
        }
    }
    
    /// <summary>
    /// Subscribes to order book updates for a trading pair asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Subscribing to order book updates for {TradingPair} on {ExchangeId}", tradingPair, _exchangeId);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Unsubscribes from order book updates for a trading pair asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unsubscribing from order book updates for {TradingPair} on {ExchangeId}", tradingPair, _exchangeId);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets the supported trading pairs asynchronously.
    /// </summary>
    /// <returns>A task that returns the supported trading pairs.</returns>
    public Task<IReadOnlyCollection<TradingPair>> GetSupportedTradingPairsAsync()
    {
        _logger.LogInformation("Getting supported trading pairs for {ExchangeId}", _exchangeId);
        
        var pairs = new[]
        {
            new TradingPair("BTC", "USDT"),
            new TradingPair("ETH", "USDT"),
            new TradingPair("BTC", "USDC"),
            new TradingPair("ETH", "USDC"),
            new TradingPair("ETH", "BTC")
        };
        
        return Task.FromResult<IReadOnlyCollection<TradingPair>>(pairs);
    }
    
    /// <summary>
    /// Places a market order asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="orderSide">The order side.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns the order.</returns>
    public Task<Order> PlaceMarketOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Placing market {Side} order for {Quantity} {TradingPair} on {ExchangeId}",
            orderSide, quantity, tradingPair, _exchangeId);
            
        var orderId = Guid.NewGuid().ToString();
        decimal price = GetPriceForOrder(tradingPair, orderSide);
        
        var order = new Order(
            orderId,
            _exchangeId,
            tradingPair,
            orderSide,
            OrderType.Market,
            OrderStatus.Filled,
            price,
            quantity,
            DateTime.UtcNow
        );
        
        // Additional properties can be set using the UpdateStatus method
        order.UpdateStatus(OrderStatus.Filled, quantity, price);
        
        _orders[orderId] = order;
        
        // Update balances
        if (orderSide == OrderSide.Buy)
        {
            _balances.AddOrUpdate(tradingPair.BaseCurrency, quantity, (k, v) => v + quantity);
            _balances.AddOrUpdate(tradingPair.QuoteCurrency, price * quantity, (k, v) => v - (price * quantity));
        }
        else
        {
            _balances.AddOrUpdate(tradingPair.BaseCurrency, quantity, (k, v) => v - quantity);
            _balances.AddOrUpdate(tradingPair.QuoteCurrency, price * quantity, (k, v) => v + (price * quantity));
        }
        
        return Task.FromResult(order);
    }
    
    /// <summary>
    /// Places a limit order asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="side">The order side.</param>
    /// <param name="price">The price.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="orderType">The order type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns the order.</returns>
    public Task<TradeResult> PlaceLimitOrderAsync(TradingPair tradingPair, OrderSide side, decimal price, decimal quantity, OrderType orderType = OrderType.Limit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Placing limit {Side} order for {Quantity} {TradingPair} at price {Price} on {ExchangeId}", 
            side, quantity, tradingPair, price, _exchangeId);
            
        var orderId = Guid.NewGuid().ToString();
        
        // Create mock trade result
        var tradeResult = CreateTradeResult(tradingPair, side, price, quantity);
        
        return Task.FromResult(tradeResult);
    }
    
    /// <summary>
    /// Places a market buy order asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity.</param>
    /// <returns>A task that returns the order.</returns>
    public Task<Order> PlaceMarketBuyOrderAsync(TradingPair tradingPair, decimal quantity)
    {
        _logger.LogInformation("Placing market buy order for {Quantity} {TradingPair} on {ExchangeId}",
            quantity, tradingPair, _exchangeId);
        
        // Get the current order book to determine the price
        if (!_orderBooks.TryGetValue(tradingPair.ToString(), out var orderBook))
        {
            // Create a new order book if it doesn't exist
            GetOrderBookSnapshotAsync(tradingPair, 10).Wait();
            orderBook = _orderBooks[tradingPair.ToString()];
        }
        
        // Get the best ask price (lowest sell offer)
        var price = orderBook.Asks[0].Price;
        
        // Create a unique order ID
        var orderId = Guid.NewGuid().ToString("N");
        
        // Create the order
        var order = new Order(
            orderId,
            _exchangeId,
            tradingPair,
            OrderSide.Buy,
            OrderType.Market,
            OrderStatus.Filled,
            price,
            quantity,
            DateTime.UtcNow
        );
        
        // Store the order
        _orders[orderId] = order;
        
        return Task.FromResult(order);
    }
    
    /// <summary>
    /// Places a market sell order asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity.</param>
    /// <returns>A task that returns the order.</returns>
    public Task<Order> PlaceMarketSellOrderAsync(TradingPair tradingPair, decimal quantity)
    {
        _logger.LogInformation("Placing market sell order for {Quantity} {TradingPair} on {ExchangeId}",
            quantity, tradingPair, _exchangeId);
        
        // Get the current order book to determine the price
        if (!_orderBooks.TryGetValue(tradingPair.ToString(), out var orderBook))
        {
            // Create a new order book if it doesn't exist
            GetOrderBookSnapshotAsync(tradingPair, 10).Wait();
            orderBook = _orderBooks[tradingPair.ToString()];
        }
        
        // Get the best bid price (highest buy offer)
        var price = orderBook.Bids[0].Price;
        
        // Create a unique order ID
        var orderId = Guid.NewGuid().ToString("N");
        
        // Create the order
        var order = new Order(
            orderId,
            _exchangeId,
            tradingPair,
            OrderSide.Sell,
            OrderType.Market,
            OrderStatus.Filled,
            price,
            quantity,
            DateTime.UtcNow
        );
        
        // Store the order
        _orders[orderId] = order;
        
        return Task.FromResult(order);
    }
    
    /// <summary>
    /// Places a limit buy order asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="price">The price.</param>
    /// <param name="quantity">The quantity.</param>
    /// <returns>A task that returns the order.</returns>
    public Task<Order> PlaceLimitBuyOrderAsync(TradingPair tradingPair, decimal price, decimal quantity)
    {
        _logger.LogInformation("Placing limit buy order for {Quantity} {TradingPair} at {Price} on {ExchangeId}",
            quantity, tradingPair, price, _exchangeId);
        
        // Create a unique order ID
        var orderId = Guid.NewGuid().ToString("N");
        
        // Create the order
        var order = new Order(
            orderId,
            _exchangeId,
            tradingPair,
            OrderSide.Buy,
            OrderType.Limit,
            OrderStatus.New,
            price,
            quantity, 
            DateTime.UtcNow
        );
        
        // Store the order
        _orders[orderId] = order;
        
        return Task.FromResult(order);
    }
    
    /// <summary>
    /// Places a limit sell order asynchronously.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="price">The price.</param>
    /// <param name="quantity">The quantity.</param>
    /// <returns>A task that returns the order.</returns>
    public Task<Order> PlaceLimitSellOrderAsync(TradingPair tradingPair, decimal price, decimal quantity)
    {
        _logger.LogInformation("Placing limit sell order for {Quantity} {TradingPair} at {Price} on {ExchangeId}",
            quantity, tradingPair, price, _exchangeId);
        
        // Create a unique order ID
        var orderId = Guid.NewGuid().ToString("N");
        
        // Create the order
        var order = new Order(
            orderId,
            _exchangeId,
            tradingPair,
            OrderSide.Sell,
            OrderType.Limit,
            OrderStatus.New,
            price,
            quantity,
            DateTime.UtcNow
        );
        
        // Store the order
        _orders[orderId] = order;
        
        return Task.FromResult(order);
    }
    
    /// <summary>
    /// Gets an order status asynchronously.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>A task that returns the order or null if not found.</returns>
    public Task<Order?> GetOrderStatusAsync(string orderId)
    {
        _logger.LogInformation("Getting order status for {OrderId} on {ExchangeId}", orderId, _exchangeId);
        
        if (_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult<Order?>(order);
        }
        
        return Task.FromResult<Order?>(null);
    }
    
    /// <summary>
    /// Cancels an order asynchronously.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>A task that returns true if the order was cancelled, false otherwise.</returns>
    public Task<bool> CancelOrderAsync(string orderId)
    {
        _logger.LogInformation("Cancelling order {OrderId} on {ExchangeId}", orderId, _exchangeId);
        
        if (_orders.TryGetValue(orderId, out var order))
        {
            order.UpdateStatus(OrderStatus.Canceled);
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
    
    /// <summary>
    /// Gets the balances asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns the balances.</returns>
    public Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting balances for {ExchangeId}", _exchangeId);
        
        var balances = new List<Balance>();
        
        foreach (var kvp in _balances)
        {
            var balance = new Balance(
                _exchangeId,
                kvp.Key,
                kvp.Value,
                kvp.Value,
                0m,
                DateTimeOffset.UtcNow
            );
            
            balances.Add(balance);
        }
        
        return Task.FromResult<IReadOnlyCollection<Balance>>(balances);
    }
    
    /// <summary>
    /// Gets the balance for a specific asset asynchronously.
    /// </summary>
    /// <param name="asset">The asset.</param>
    /// <returns>A task that returns the balance or null if not found.</returns>
    public Task<Balance?> GetBalanceAsync(string asset)
    {
        _logger.LogInformation("Getting balance for {Asset} on {ExchangeId}", asset, _exchangeId);
        
        if (_balances.TryGetValue(asset, out var amount))
        {
            var balance = new Balance(
                _exchangeId,
                asset,
                amount,
                amount,
                0m,
                DateTimeOffset.UtcNow
            );
            
            return Task.FromResult<Balance?>(balance);
        }
        
        return Task.FromResult<Balance?>(null);
    }
    
    /// <summary>
    /// Gets a random price for a trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <returns>A random price.</returns>
    private decimal GetRandomPrice(TradingPair tradingPair)
    {
        if (tradingPair.BaseCurrency == "BTC" && tradingPair.QuoteCurrency == "USDT")
            return 29000 + (decimal)_random.NextDouble() * 2000;
        
        if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "USDT")
            return 1800 + (decimal)_random.NextDouble() * 200;
        
        if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "BTC")
            return 0.06m + (decimal)_random.NextDouble() * 0.01m;
        
        return 100 + (decimal)_random.NextDouble() * 50;
    }
    
    /// <summary>
    /// Gets the price for an order based on the side.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="side">The order side.</param>
    /// <returns>The price.</returns>
    private decimal GetPriceForOrder(TradingPair tradingPair, OrderSide side)
    {
        if (!_orderBooks.TryGetValue(tradingPair.ToString(), out var orderBook))
        {
            GetOrderBookSnapshotAsync(tradingPair, 10).Wait();
            orderBook = _orderBooks[tradingPair.ToString()];
        }
        
        if (side == OrderSide.Buy)
        {
            return orderBook.Asks[0].Price;
        }
        else
        {
            return orderBook.Bids[0].Price;
        }
    }
    
    /// <summary>
    /// Creates a trade result.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="side">The order side.</param>
    /// <param name="price">The price.</param>
    /// <param name="quantity">The quantity.</param>
    /// <returns>The trade result.</returns>
    private TradeResult CreateTradeResult(TradingPair tradingPair, OrderSide side, decimal price, decimal quantity)
    {
        string orderId = Guid.NewGuid().ToString();
        decimal fee = price * quantity * 0.001m; // Mock 0.1% fee
        
        bool isSuccess = _random.Next(0, 10) < 9; // 90% success rate for mock trades
        
        var result = new TradeResult
        {
            Id = Guid.NewGuid().ToString(),
            OrderId = orderId,
            ClientOrderId = $"mock-{orderId}",
            TradeType = side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            RequestedPrice = price,
            ExecutedPrice = isSuccess ? price : 0,
            RequestedQuantity = quantity,
            ExecutedQuantity = isSuccess ? quantity : 0,
            TotalValue = isSuccess ? price * quantity : 0,
            Fee = isSuccess ? fee : 0,
            FeeCurrency = side == OrderSide.Buy ? tradingPair.BaseCurrency : tradingPair.QuoteCurrency,
            Timestamp = DateTimeOffset.UtcNow,
            IsSuccess = isSuccess,
            ErrorMessage = isSuccess ? null : "Simulated trade failure"
        };
        
        return result;
    }
}

/// <summary>
/// Represents an update to an order book.
/// </summary>
public class OrderBookUpdate
{
    /// <summary>
    /// Gets or sets the exchange ID.
    /// </summary>
    public string ExchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the trading pair.
    /// </summary>
    public TradingPair TradingPair { get; set; } = new TradingPair("BTC", "USDT");
    
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the bids.
    /// </summary>
    public List<OrderBookEntry> Bids { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the asks.
    /// </summary>
    public List<OrderBookEntry> Asks { get; set; } = new();
} 