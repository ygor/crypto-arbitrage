namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents an executed trade.
/// </summary>
public readonly record struct TradeExecution
{
    /// <summary>
    /// Gets the unique identifier for this trade.
    /// </summary>
    public string TradeId { get; }
    
    /// <summary>
    /// Gets the order ID for this trade (alias for TradeId).
    /// </summary>
    public string OrderId => TradeId;
    
    /// <summary>
    /// Gets the exchange ID where the trade was executed.
    /// </summary>
    public string ExchangeId { get; }
    
    /// <summary>
    /// Gets the trading pair for this trade.
    /// </summary>
    public TradingPair TradingPair { get; }
    
    /// <summary>
    /// Gets the side of the trade (buy or sell).
    /// </summary>
    public OrderSide Side { get; }
    
    /// <summary>
    /// Gets the type of the order.
    /// </summary>
    public OrderType OrderType { get; }
    
    /// <summary>
    /// Gets the price at which the trade was executed.
    /// </summary>
    public decimal Price { get; }
    
    /// <summary>
    /// Gets the quantity of the base currency that was traded.
    /// </summary>
    public decimal Quantity { get; }
    
    /// <summary>
    /// Gets the fee amount paid for this trade.
    /// </summary>
    public decimal Fee { get; }
    
    /// <summary>
    /// Gets the currency in which the fee was paid.
    /// </summary>
    public string FeeCurrency { get; }
    
    /// <summary>
    /// Gets the timestamp when the trade was executed (in UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; }
    
    /// <summary>
    /// Gets the ID of the arbitrage opportunity that led to this trade, if any.
    /// </summary>
    public string? ArbitrageOpportunityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TradeExecution"/> struct.
    /// </summary>
    /// <param name="tradeId">The unique identifier for this trade.</param>
    /// <param name="exchangeId">The exchange ID where the trade was executed.</param>
    /// <param name="tradingPair">The trading pair for this trade.</param>
    /// <param name="side">The side of the trade (buy or sell).</param>
    /// <param name="orderType">The type of the order.</param>
    /// <param name="price">The price at which the trade was executed.</param>
    /// <param name="quantity">The quantity of the base currency that was traded.</param>
    /// <param name="fee">The fee amount paid for this trade.</param>
    /// <param name="feeCurrency">The currency in which the fee was paid.</param>
    /// <param name="timestamp">The timestamp when the trade was executed (defaults to UTC now).</param>
    /// <param name="arbitrageOpportunityId">The ID of the arbitrage opportunity that led to this trade, if any.</param>
    /// <exception cref="ArgumentException">Thrown when the parameters are invalid.</exception>
    public TradeExecution(
        string tradeId,
        string exchangeId,
        TradingPair tradingPair,
        OrderSide side,
        OrderType orderType,
        decimal price,
        decimal quantity,
        decimal fee,
        string feeCurrency,
        DateTimeOffset? timestamp = null,
        string? arbitrageOpportunityId = null)
    {
        if (string.IsNullOrWhiteSpace(tradeId))
        {
            throw new ArgumentException("Trade ID cannot be null or empty.", nameof(tradeId));
        }

        if (price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.", nameof(price));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (fee < 0)
        {
            throw new ArgumentException("Fee cannot be negative.", nameof(fee));
        }

        if (string.IsNullOrWhiteSpace(feeCurrency))
        {
            throw new ArgumentException("Fee currency cannot be null or empty.", nameof(feeCurrency));
        }

        TradeId = tradeId;
        ExchangeId = exchangeId;
        TradingPair = tradingPair;
        Side = side;
        OrderType = orderType;
        Price = price;
        Quantity = quantity;
        Fee = fee;
        FeeCurrency = feeCurrency.ToUpperInvariant();
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        ArbitrageOpportunityId = arbitrageOpportunityId;
    }
    
    /// <summary>
    /// Gets the total value of the trade in the quote currency.
    /// </summary>
    public decimal TotalValue => Price * Quantity;
    
    /// <summary>
    /// Gets the fee rate as a percentage of the total value.
    /// </summary>
    public decimal FeeRate => Fee / TotalValue * 100m;
    
    /// <summary>
    /// Gets a description of the trade for logging purposes.
    /// </summary>
    public string Description => $"{Side} {Quantity} {TradingPair.BaseCurrency} at {Price} {TradingPair.QuoteCurrency} on {ExchangeId}";
} 