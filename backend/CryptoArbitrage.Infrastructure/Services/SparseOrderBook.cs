using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// A memory-efficient representation of an order book that only stores significant price levels.
/// </summary>
public class SparseOrderBook
{
    private readonly SortedDictionary<decimal, decimal> _bids;
    private readonly SortedDictionary<decimal, decimal> _asks;
    
    /// <summary>
    /// Gets the exchange ID associated with this order book.
    /// </summary>
    public ExchangeId ExchangeId { get; }
    
    /// <summary>
    /// Gets the trading pair associated with this order book.
    /// </summary>
    public TradingPair TradingPair { get; }
    
    /// <summary>
    /// Gets the timestamp when this order book snapshot was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether this order book has valid data.
    /// </summary>
    public bool IsValid => _bids.Count > 0 && _asks.Count > 0 && GetBestBidPrice() < GetBestAskPrice();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SparseOrderBook"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    public SparseOrderBook(ExchangeId exchangeId, TradingPair tradingPair)
    {
        ExchangeId = exchangeId;
        TradingPair = tradingPair;
        Timestamp = DateTimeOffset.UtcNow;
        
        // Use custom comparer for bids (descending order)
        _bids = new SortedDictionary<decimal, decimal>(Comparer<decimal>.Create((x, y) => y.CompareTo(x)));
        _asks = new SortedDictionary<decimal, decimal>();
    }
    
    /// <summary>
    /// Updates or adds a bid level to the order book.
    /// </summary>
    /// <param name="price">The price level.</param>
    /// <param name="quantity">The quantity at this price level.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateBid(decimal price, decimal quantity)
    {
        if (quantity > 0)
        {
            _bids[price] = quantity;
        }
        else
        {
            _bids.Remove(price);
        }
        Timestamp = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Updates or adds an ask level to the order book.
    /// </summary>
    /// <param name="price">The price level.</param>
    /// <param name="quantity">The quantity at this price level.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateAsk(decimal price, decimal quantity)
    {
        if (quantity > 0)
        {
            _asks[price] = quantity;
        }
        else
        {
            _asks.Remove(price);
        }
        Timestamp = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Updates the order book with a delta update.
    /// </summary>
    /// <param name="isBid">Whether this update is for the bid side.</param>
    /// <param name="price">The price level.</param>
    /// <param name="quantityDelta">The quantity delta (can be negative).</param>
    public void ApplyDelta(bool isBid, decimal price, decimal quantityDelta)
    {
        if (isBid)
        {
            if (_bids.TryGetValue(price, out var currentQuantity))
            {
                var newQuantity = currentQuantity + quantityDelta;
                if (newQuantity <= 0)
                {
                    _bids.Remove(price);
                }
                else
                {
                    _bids[price] = newQuantity;
                }
            }
            else if (quantityDelta > 0)
            {
                _bids[price] = quantityDelta;
            }
        }
        else
        {
            if (_asks.TryGetValue(price, out var currentQuantity))
            {
                var newQuantity = currentQuantity + quantityDelta;
                if (newQuantity <= 0)
                {
                    _asks.Remove(price);
                }
                else
                {
                    _asks[price] = newQuantity;
                }
            }
            else if (quantityDelta > 0)
            {
                _asks[price] = quantityDelta;
            }
        }
        
        Timestamp = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Gets the best bid price.
    /// </summary>
    /// <returns>The best bid price.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetBestBidPrice()
    {
        return _bids.Count > 0 ? _bids.Keys.First() : 0;
    }
    
    /// <summary>
    /// Gets the best ask price.
    /// </summary>
    /// <returns>The best ask price.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetBestAskPrice()
    {
        return _asks.Count > 0 ? _asks.Keys.First() : decimal.MaxValue;
    }
    
    /// <summary>
    /// Gets the best bid quantity.
    /// </summary>
    /// <returns>The best bid quantity.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetBestBidQuantity()
    {
        return _bids.Count > 0 ? _bids.Values.First() : 0;
    }
    
    /// <summary>
    /// Gets the best ask quantity.
    /// </summary>
    /// <returns>The best ask quantity.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetBestAskQuantity()
    {
        return _asks.Count > 0 ? _asks.Values.First() : 0;
    }
    
    /// <summary>
    /// Converts this sparse order book to a price quote.
    /// </summary>
    /// <returns>A price quote, or null if the order book doesn't have valid bids and asks.</returns>
    public PriceQuote? ToPriceQuote()
    {
        if (!IsValid) return null;
        
        var bestBidPrice = GetBestBidPrice();
        var bestBidQuantity = GetBestBidQuantity();
        var bestAskPrice = GetBestAskPrice();
        var bestAskQuantity = GetBestAskQuantity();
        
        return new PriceQuote(
            ExchangeId.Value,
            TradingPair,
            Timestamp.DateTime,
            bestBidPrice,
            bestBidQuantity,
            bestAskPrice,
            bestAskQuantity);
    }
    
    /// <summary>
    /// Converts this sparse order book to a standard OrderBook.
    /// </summary>
    /// <returns>A standard OrderBook.</returns>
    public OrderBook ToOrderBook()
    {
        var bidEntries = _bids
            .Select(kv => new OrderBookEntry(kv.Key, kv.Value))
            .OrderByDescending(e => e.Price)
            .ToList();
            
        var askEntries = _asks
            .Select(kv => new OrderBookEntry(kv.Key, kv.Value))
            .OrderBy(e => e.Price)
            .ToList();
            
        return new OrderBook(
            ExchangeId.Value,
            TradingPair,
            Timestamp.DateTime,
            bidEntries,
            askEntries);
    }
    
    /// <summary>
    /// Gets the total volume available up to a specified price for bids.
    /// </summary>
    /// <param name="lowestPrice">The lowest price to consider for bids.</param>
    /// <returns>The total volume available at or above the specified price.</returns>
    public decimal GetBidVolumeUpToPrice(decimal lowestPrice)
    {
        return _bids
            .Where(kvp => kvp.Key >= lowestPrice)
            .Sum(kvp => kvp.Value);
    }
    
    /// <summary>
    /// Gets the total volume available up to a specified price for asks.
    /// </summary>
    /// <param name="highestPrice">The highest price to consider for asks.</param>
    /// <returns>The total volume available at or below the specified price.</returns>
    public decimal GetAskVolumeUpToPrice(decimal highestPrice)
    {
        return _asks
            .Where(kvp => kvp.Key <= highestPrice)
            .Sum(kvp => kvp.Value);
    }
    
    /// <summary>
    /// Clears all data from the order book.
    /// </summary>
    public void Clear()
    {
        _bids.Clear();
        _asks.Clear();
        Timestamp = DateTimeOffset.UtcNow;
    }
} 