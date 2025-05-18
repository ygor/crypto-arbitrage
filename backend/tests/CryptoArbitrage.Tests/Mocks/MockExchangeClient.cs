using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Tests.Mocks;

/// <summary>
/// A mock implementation of IExchangeClient for testing.
/// </summary>
public class MockExchangeClient : IExchangeClient
{
    private readonly Dictionary<TradingPair, OrderBook> _orderBooks = new();
    private readonly Dictionary<string, decimal> _balances = new();
    private readonly Dictionary<string, Order> _orders = new();
    private readonly Random _random = new();
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockExchangeClient"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="logger">Optional logger instance.</param>
    public MockExchangeClient(string exchangeId, ILogger? logger = null)
    {
        ExchangeId = exchangeId;
        _logger = logger;
        
        // Initialize default balances
        _balances["BTC"] = 1.0m;
        _balances["ETH"] = 10.0m;
        _balances["USDT"] = 10000.0m;
        _balances["USDC"] = 10000.0m;
    }

    /// <inheritdoc />
    public string ExchangeId { get; }

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc />
    public bool SupportsStreaming => true;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        IsAuthenticated = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FeeSchedule(
            ExchangeId,
            0.001m, // 0.1% maker fee
            0.002m  // 0.2% taker fee
        ));
    }

    /// <inheritdoc />
    public Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        if (_orderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            return Task.FromResult(orderBook);
        }

        // Generate a random price based on the trading pair
        decimal basePrice = tradingPair.BaseCurrency switch
        {
            "BTC" => 50000m + (_random.Next(-500, 500) / 10m),
            "ETH" => 2000m + (_random.Next(-200, 200) / 10m),
            _ => 100m + (_random.Next(-10, 10) / 10m)
        };
        
        // Create bids (buy orders) slightly below base price
        var bids = new List<OrderBookEntry>();
        for (int i = 0; i < depth; i++)
        {
            var price = basePrice * (1 - 0.001m * (i + 1));
            var quantity = 1m / (i + 1);
            bids.Add(new OrderBookEntry(price, quantity));
        }
        
        // Create asks (sell orders) slightly above base price
        var asks = new List<OrderBookEntry>();
        for (int i = 0; i < depth; i++)
        {
            var price = basePrice * (1 + 0.001m * (i + 1));
            var quantity = 1m / (i + 1);
            asks.Add(new OrderBookEntry(price, quantity));
        }
        
        // Create OrderBook with DateTime
        orderBook = new OrderBook(ExchangeId, tradingPair, DateTime.UtcNow, bids, asks);
        _orderBooks[tradingPair] = orderBook;
        
        return Task.FromResult(orderBook);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(TradingPair tradingPair, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Ensure we have at least an initial order book for the trading pair
        if (!_orderBooks.TryGetValue(tradingPair, out var initialOrderBook))
        {
            initialOrderBook = CreateDefaultOrderBook(tradingPair);
            _orderBooks[tradingPair] = initialOrderBook;
        }
        
        // Yield the initial order book
        yield return initialOrderBook;
        
        // In a real implementation, we would continue to yield updates as they come in
        // For testing, we'll yield a few more with slightly different prices
        for (int i = 0; i < 3 && !cancellationToken.IsCancellationRequested; i++)
        {
            await Task.Delay(100, cancellationToken); // Simulate time passing between updates
            
            var askPrice = initialOrderBook.Asks[0].Price * (1 + 0.001m * (i + 1));
            var bidPrice = initialOrderBook.Bids[0].Price * (1 - 0.001m * (i + 1));
            
            var updatedOrderBook = new OrderBook(
                ExchangeId,
                tradingPair,
                DateTime.UtcNow,
                new List<OrderBookEntry> { new OrderBookEntry(bidPrice, 1.0m) },
                new List<OrderBookEntry> { new OrderBookEntry(askPrice, 1.0m) }
            );
            
            // Update our stored order book
            _orderBooks[tradingPair] = updatedOrderBook;
            
            yield return updatedOrderBook;
        }
    }

    /// <inheritdoc />
    public Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        // Nothing to do in the mock implementation
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        // Nothing to do in the mock implementation
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Order> PlaceMarketOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default)
    {
        // Get or create a realistic price
        var price = GetPriceForOrder(tradingPair, orderSide);
        
        if (price <= 0)
        {
            throw new InvalidOperationException($"Could not determine price for {tradingPair} {orderSide}");
        }
        
        // Create a unique order ID
        var orderId = Guid.NewGuid().ToString("N");
        
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
            DateTime.UtcNow
        );
        
        // Set filled quantity
        order.FilledQuantity = quantity;
        order.AverageFillPrice = price;
        
        // Store the order
        _orders[orderId] = order;
        
        // Update balances
        UpdateBalancesForOrder(order);
        
        _logger?.LogInformation("Placed market order on mock exchange: {OrderId}, {Side} {Quantity} {TradingPair} at {Price}", 
            orderId, orderSide, quantity, tradingPair, price);
            
        return Task.FromResult(order);
    }

    /// <inheritdoc />
    public Task<TradeResult> PlaceLimitOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal price,
        decimal quantity, 
        OrderType orderType = OrderType.Limit, 
        CancellationToken cancellationToken = default)
    {
        // Create a unique order ID
        var orderId = Guid.NewGuid().ToString("N");
        
        _logger?.LogInformation("Placing limit {Side} order for {Quantity} {TradingPair} at {Price} on mock exchange", 
            orderSide, quantity, tradingPair, price);
        
        // Create the order
        var order = new Order(
            orderId,
            ExchangeId,
            tradingPair,
            orderSide,
            OrderType.Limit,
            OrderStatus.New,
            price,
            quantity,
            DateTime.UtcNow
        );
        
        // Store the order
        _orders[orderId] = order;
        
        // For simplicity in mock, we'll immediately fill the order
        order.Status = OrderStatus.Filled;
        order.FilledQuantity = quantity;
        order.AverageFillPrice = price;
        
        // Update balances
        UpdateBalancesForOrder(order);
        
        _logger?.LogInformation("Limit order placed and filled on mock exchange: {OrderId}", orderId);
        
        // Create and return a trade result
        return Task.FromResult(new TradeResult
        {
            IsSuccess = true,
            OrderId = orderId,
            ClientOrderId = string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
            TradingPair = tradingPair.ToString(),
            TradeType = orderSide == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            RequestedPrice = price,
            ExecutedPrice = price,
            RequestedQuantity = quantity,
            ExecutedQuantity = quantity,
            TotalValue = price * quantity,
            Fee = price * quantity * 0.001m, // Simulated 0.1% fee
            FeeCurrency = tradingPair.QuoteCurrency
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Getting balances from mock exchange");
        var balances = new List<Balance>();
        
        foreach (var kvp in _balances)
        {
            balances.Add(new Balance(
                ExchangeId,
                kvp.Key,
                kvp.Value,
                kvp.Value,
                0,
                DateTimeOffset.UtcNow
            ));
        }
        
        return Task.FromResult<IReadOnlyCollection<Balance>>(balances);
    }

    /// <inheritdoc />
    public Task<Order?> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Getting order status for {OrderId} from mock exchange", orderId);
        
        if (_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult<Order?>(order);
        }
        
        _logger?.LogWarning("Order {OrderId} not found on mock exchange", orderId);
        return Task.FromResult<Order?>(null);
    }
    
    /// <inheritdoc />
    public Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Canceling order {OrderId} on mock exchange", orderId);
        
        if (_orders.TryGetValue(orderId, out var order))
        {
            order.Status = OrderStatus.Canceled;
            _logger?.LogInformation("Order {OrderId} canceled on mock exchange", orderId);
            return Task.FromResult(true);
        }
        
        _logger?.LogWarning("Order {OrderId} not found on mock exchange", orderId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<TradingPair>> GetSupportedTradingPairsAsync()
    {
        return Task.FromResult<IReadOnlyCollection<TradingPair>>(new List<TradingPair>
        {
            new TradingPair("BTC", "USDT"),
            new TradingPair("ETH", "USDT"),
            new TradingPair("BTC", "USDC"),
            new TradingPair("ETH", "USDC"),
            new TradingPair("ETH", "BTC")
        });
    }
    
    private decimal GetPriceForOrder(TradingPair tradingPair, OrderSide orderSide)
    {
        // Try to get from order book first
        if (_orderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            if (orderSide == OrderSide.Buy && orderBook.Asks.Count > 0)
            {
                return orderBook.Asks[0].Price;
            }
            else if (orderSide == OrderSide.Sell && orderBook.Bids.Count > 0)
            {
                return orderBook.Bids[0].Price;
            }
        }
        
        // If no order book or empty, generate realistic prices
        if (tradingPair.BaseCurrency == "BTC" && tradingPair.QuoteCurrency == "USDT")
        {
            return _random.Next(45000, 50000);
        }
        else if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "USDT")
        {
            return _random.Next(3000, 3500);
        }
        else if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "BTC")
        {
            return 0.07m + (decimal)_random.NextDouble() * 0.01m;
        }
        
        // Default fallback
        return 100m * (decimal)_random.NextDouble();
    }

    private OrderBook CreateDefaultOrderBook(TradingPair tradingPair)
    {
        decimal basePrice = GetDefaultPriceForPair(tradingPair);
        decimal askPrice = basePrice * 1.001m;
        decimal bidPrice = basePrice * 0.999m;
        
        return new OrderBook(
            ExchangeId,
            tradingPair,
            DateTime.UtcNow,
            new List<OrderBookEntry> { new OrderBookEntry(bidPrice, 1.0m) },
            new List<OrderBookEntry> { new OrderBookEntry(askPrice, 1.0m) }
        );
    }
    
    private decimal GetDefaultPriceForPair(TradingPair tradingPair)
    {
        if (tradingPair.BaseCurrency == "BTC" && tradingPair.QuoteCurrency == "USDT")
        {
            return 50000m;
        }
        else if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "USDT")
        {
            return 3000m;
        }
        else if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "BTC")
        {
            return 0.07m;
        }
        
        return 100m;
    }

    private void UpdateBalancesForOrder(Order order)
    {
        var baseCurrency = order.TradingPair.BaseCurrency;
        var quoteCurrency = order.TradingPair.QuoteCurrency;
        
        if (order.Side == OrderSide.Buy)
        {
            // Buying base currency with quote currency
            if (!_balances.ContainsKey(baseCurrency))
            {
                _balances[baseCurrency] = 0;
            }
            
            if (!_balances.ContainsKey(quoteCurrency))
            {
                _balances[quoteCurrency] = 0;
            }
            
            // Add base currency
            _balances[baseCurrency] += order.FilledQuantity;
            
            // Subtract quote currency
            var quoteCurrencyUsed = order.FilledQuantity * order.AverageFillPrice;
            _balances[quoteCurrency] -= quoteCurrencyUsed;
        }
        else // OrderSide.Sell
        {
            // Selling base currency for quote currency
            if (!_balances.ContainsKey(baseCurrency))
            {
                _balances[baseCurrency] = 0;
            }
            
            if (!_balances.ContainsKey(quoteCurrency))
            {
                _balances[quoteCurrency] = 0;
            }
            
            // Subtract base currency
            _balances[baseCurrency] -= order.FilledQuantity;
            
            // Add quote currency
            var quoteCurrencyReceived = order.FilledQuantity * order.AverageFillPrice;
            _balances[quoteCurrency] += quoteCurrencyReceived;
        }
    }

    // For backward compatibility, keep a variant that returns Dictionary<string, decimal>
    public Task<Dictionary<string, decimal>> GetRawBalancesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Dictionary<string, decimal>(_balances));
    }
} 