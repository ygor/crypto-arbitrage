namespace ArbitrageBot.Domain.Models;

/// <summary>
/// Represents a price quote for a trading pair on a specific exchange.
/// </summary>
public readonly struct PriceQuote
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PriceQuote"/> struct.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="timestamp">The timestamp of the quote in UTC.</param>
    /// <param name="bestBidPrice">The best bid (buy) price.</param>
    /// <param name="bestBidQuantity">The best bid (buy) quantity available.</param>
    /// <param name="bestAskPrice">The best ask (sell) price.</param>
    /// <param name="bestAskQuantity">The best ask (sell) quantity available.</param>
    public PriceQuote(
        string exchangeId,
        TradingPair tradingPair,
        DateTime timestamp,
        decimal bestBidPrice,
        decimal bestBidQuantity,
        decimal bestAskPrice,
        decimal bestAskQuantity)
    {
        ExchangeId = exchangeId;
        TradingPair = tradingPair;
        Timestamp = timestamp;
        BestBidPrice = bestBidPrice;
        BestBidQuantity = bestBidQuantity;
        BestAskPrice = bestAskPrice;
        BestAskQuantity = bestAskQuantity;
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
    /// Gets the timestamp of the quote in UTC.
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Gets the best bid (buy) price.
    /// </summary>
    public decimal BestBidPrice { get; }
    
    /// <summary>
    /// Gets the best bid (buy) quantity available.
    /// </summary>
    public decimal BestBidQuantity { get; }
    
    /// <summary>
    /// Gets the best ask (sell) price.
    /// </summary>
    public decimal BestAskPrice { get; }
    
    /// <summary>
    /// Gets the best ask (sell) quantity available.
    /// </summary>
    public decimal BestAskQuantity { get; }
    
    /// <summary>
    /// Gets the spread between the best ask and best bid prices.
    /// </summary>
    public decimal Spread => BestAskPrice - BestBidPrice;
    
    /// <summary>
    /// Gets the spread as a percentage of the best bid price.
    /// </summary>
    public decimal SpreadPercentage => BestBidPrice > 0 ? (Spread / BestBidPrice) * 100m : 0m;
    
    /// <summary>
    /// Gets the mid price (average of best bid and best ask).
    /// </summary>
    public decimal MidPrice => (BestBidPrice + BestAskPrice) / 2m;
} 