using System;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the execution details of a trade.
/// </summary>
public class TradeExecution
{
    /// <summary>
    /// Gets or sets the execution identifier.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the price at which the trade was executed.
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Gets or sets the quantity of the trade.
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the side of the trade (buy or sell).
    /// </summary>
    public OrderSide Side { get; set; }
    
    /// <summary>
    /// Gets or sets the fee amount for the trade.
    /// </summary>
    public decimal Fee { get; set; }
    
    /// <summary>
    /// Gets or sets the currency in which the fee was charged.
    /// </summary>
    public string FeeCurrency { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this is a maker trade.
    /// </summary>
    public bool IsMaker { get; set; }
    
    /// <summary>
    /// Gets or sets the total value of the trade (Price * Quantity).
    /// </summary>
    public decimal TotalValue => Price * Quantity;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TradeExecution"/> class.
    /// </summary>
    public TradeExecution()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TradeExecution"/> class with the specified parameters.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="side">The order side.</param>
    /// <param name="orderType">The order type.</param>
    /// <param name="price">The execution price.</param>
    /// <param name="quantity">The execution quantity.</param>
    /// <param name="fee">The fee amount.</param>
    /// <param name="feeCurrency">The fee currency.</param>
    /// <param name="timestamp">The execution timestamp.</param>
    public TradeExecution(
        string orderId,
        string exchangeId,
        TradingPair tradingPair,
        OrderSide side,
        OrderType orderType,
        decimal price,
        decimal quantity,
        decimal fee,
        string feeCurrency,
        DateTimeOffset timestamp)
    {
        ExecutionId = Guid.NewGuid().ToString();
        OrderId = orderId;
        Price = price;
        Quantity = quantity;
        Side = side;
        Fee = fee;
        FeeCurrency = feeCurrency;
        Timestamp = timestamp.DateTime;
        IsMaker = orderType == OrderType.Limit;
    }
} 