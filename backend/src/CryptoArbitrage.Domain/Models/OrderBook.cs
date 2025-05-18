using System.Collections.Immutable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents a level in an order book (price and quantity).
/// </summary>
public readonly record struct OrderBookLevel
{
    /// <summary>
    /// Gets the price of the order book level.
    /// </summary>
    public decimal Price { get; }
    
    /// <summary>
    /// Gets the quantity available at this price level.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBookLevel"/> struct.
    /// </summary>
    /// <param name="price">The price of the order book level.</param>
    /// <param name="quantity">The quantity available at this price level.</param>
    public OrderBookLevel(decimal price, decimal quantity)
    {
        if (price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.", nameof(price));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        Price = price;
        Quantity = quantity;
    }
}

/// <summary>
/// Represents an order book for a trading pair on a specific exchange.
/// </summary>
public class OrderBook
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBook"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="timestamp">The timestamp of the order book.</param>
    /// <param name="bids">The list of bids, sorted from highest to lowest price.</param>
    /// <param name="asks">The list of asks, sorted from lowest to highest price.</param>
    public OrderBook(
        string exchangeId,
        TradingPair tradingPair,
        DateTime timestamp,
        IReadOnlyList<OrderBookEntry> bids,
        IReadOnlyList<OrderBookEntry> asks)
    {
        ExchangeId = exchangeId ?? throw new ArgumentNullException(nameof(exchangeId));
        TradingPair = tradingPair;
        Timestamp = timestamp;
        Bids = bids ?? throw new ArgumentNullException(nameof(bids));
        Asks = asks ?? throw new ArgumentNullException(nameof(asks));
    }

    /// <summary>
    /// Gets the exchange identifier.
    /// </summary>
    public string ExchangeId { get; }

    /// <summary>
    /// Gets the trading pair.
    /// </summary>
    public TradingPair TradingPair { get; }

    /// <summary>
    /// Gets the timestamp of the order book.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the list of bids, sorted from highest to lowest price.
    /// </summary>
    public IReadOnlyList<OrderBookEntry> Bids { get; }

    /// <summary>
    /// Gets the list of asks, sorted from lowest to highest price.
    /// </summary>
    public IReadOnlyList<OrderBookEntry> Asks { get; }
    
    /// <summary>
    /// Gets the best bid (highest price).
    /// </summary>
    /// <returns>The best bid, or null if there are no bids.</returns>
    public OrderBookEntry? GetBestBid() => Bids.Count > 0 ? Bids[0] : null;

    /// <summary>
    /// Gets the best ask (lowest price).
    /// </summary>
    /// <returns>The best ask, or null if there are no asks.</returns>
    public OrderBookEntry? GetBestAsk() => Asks.Count > 0 ? Asks[0] : null;
    
    /// <summary>
    /// Calculates the total volume available up to a specified price for bids.
    /// </summary>
    /// <param name="lowestPrice">The lowest price to consider for bids.</param>
    /// <returns>The total volume available for bids at or above the specified price.</returns>
    public decimal GetBidVolumeUpToPrice(decimal lowestPrice)
    {
        return Bids
            .TakeWhile(bid => bid.Price >= lowestPrice)
            .Sum(bid => bid.Quantity);
    }
    
    /// <summary>
    /// Calculates the total volume available up to a specified price for asks.
    /// </summary>
    /// <param name="highestPrice">The highest price to consider for asks.</param>
    /// <returns>The total volume available for asks at or below the specified price.</returns>
    public decimal GetAskVolumeUpToPrice(decimal highestPrice)
    {
        return Asks
            .TakeWhile(ask => ask.Price <= highestPrice)
            .Sum(ask => ask.Quantity);
    }
}

/// <summary>
/// Represents an entry in an order book.
/// </summary>
public readonly struct OrderBookEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBookEntry"/> struct.
    /// </summary>
    /// <param name="price">The price.</param>
    /// <param name="quantity">The quantity available at this price.</param>
    public OrderBookEntry(decimal price, decimal quantity)
    {
        Price = price;
        Quantity = quantity;
    }

    /// <summary>
    /// Gets the price.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// Gets the quantity available at this price.
    /// </summary>
    public decimal Quantity { get; }
}

/// <summary>
/// Extension methods for <see cref="OrderBook"/>.
/// </summary>
public static class OrderBookExtensions
{
    /// <summary>
    /// Converts an order book to a price quote.
    /// </summary>
    /// <param name="orderBook">The order book to convert.</param>
    /// <returns>A price quote based on the best bid and ask prices in the order book, or null if the order book does not have valid bids or asks.</returns>
    public static PriceQuote? ToPriceQuote(this OrderBook orderBook)
    {
        if (orderBook == null || 
            !orderBook.Bids.Any() || 
            !orderBook.Asks.Any())
        {
            return null;
        }

        var bestBid = orderBook.Bids.First();
        var bestAsk = orderBook.Asks.First();

        if (bestBid.Price <= 0 || bestAsk.Price <= 0 ||
            bestBid.Quantity <= 0 || bestAsk.Quantity <= 0)
        {
            return null;
        }

        return new PriceQuote(
            orderBook.ExchangeId,
            orderBook.TradingPair,
            orderBook.Timestamp,
            bestBid.Price,
            bestBid.Quantity,
            bestAsk.Price,
            bestAsk.Quantity);
    }
} 