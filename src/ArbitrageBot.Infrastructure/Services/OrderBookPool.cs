using System.Collections.Concurrent;
using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Infrastructure.Services;

/// <summary>
/// Provides a pool of reusable OrderBook instances to reduce garbage collection pressure.
/// </summary>
public class OrderBookPool
{
    private readonly ConcurrentBag<OrderBook> _pool = new();
    private readonly int _maxPoolSize;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBookPool"/> class.
    /// </summary>
    /// <param name="maxPoolSize">The maximum number of instances to keep in the pool.</param>
    public OrderBookPool(int maxPoolSize = 100)
    {
        _maxPoolSize = maxPoolSize;
    }
    
    /// <summary>
    /// Gets an order book from the pool or creates a new one.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="bids">The bid levels.</param>
    /// <param name="asks">The ask levels.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>An order book.</returns>
    public OrderBook Get(
        ExchangeId exchangeId,
        TradingPair tradingPair,
        IEnumerable<OrderBookLevel> bids,
        IEnumerable<OrderBookLevel> asks,
        DateTimeOffset? timestamp = null)
    {
        if (_pool.TryTake(out var orderBook))
        {
            // Convert OrderBookLevel to OrderBookEntry
            var bidEntries = bids.Select(level => new OrderBookEntry(level.Price, level.Quantity)).ToList();
            var askEntries = asks.Select(level => new OrderBookEntry(level.Price, level.Quantity)).ToList();
            
            return new OrderBook(
                exchangeId.Value,
                tradingPair,
                timestamp?.DateTime ?? DateTime.UtcNow,
                bidEntries,
                askEntries);
        }
        
        // Convert OrderBookLevel to OrderBookEntry
        var newBidEntries = bids.Select(level => new OrderBookEntry(level.Price, level.Quantity)).ToList();
        var newAskEntries = asks.Select(level => new OrderBookEntry(level.Price, level.Quantity)).ToList();
        
        return new OrderBook(
            exchangeId.Value,
            tradingPair,
            timestamp?.DateTime ?? DateTime.UtcNow,
            newBidEntries,
            newAskEntries);
    }
    
    /// <summary>
    /// Returns an OrderBook instance to the pool for reuse.
    /// </summary>
    /// <param name="orderBook">The OrderBook instance to return to the pool.</param>
    public void Return(OrderBook orderBook)
    {
        // Only add to the pool if we're under the size limit
        if (_pool.Count < _maxPoolSize)
        {
            _pool.Add(orderBook);
        }
    }
} 