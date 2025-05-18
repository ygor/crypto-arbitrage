using System;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents an order on a cryptocurrency exchange.
/// </summary>
public class Order
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="side">The order side (buy or sell).</param>
    /// <param name="type">The order type.</param>
    /// <param name="status">The order status.</param>
    /// <param name="price">The order price.</param>
    /// <param name="quantity">The order quantity.</param>
    /// <param name="timestamp">The timestamp when the order was created.</param>
    public Order(
        string id,
        string exchangeId,
        TradingPair tradingPair,
        OrderSide side,
        OrderType type,
        OrderStatus status,
        decimal price,
        decimal quantity,
        DateTime timestamp)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ExchangeId = exchangeId ?? throw new ArgumentNullException(nameof(exchangeId));
        TradingPair = tradingPair;
        Side = side;
        Type = type;
        Status = status;
        Price = price;
        Quantity = quantity;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the order identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the exchange identifier.
    /// </summary>
    public string ExchangeId { get; }

    /// <summary>
    /// Gets the trading pair.
    /// </summary>
    public TradingPair TradingPair { get; }

    /// <summary>
    /// Gets the order side (buy or sell).
    /// </summary>
    public OrderSide Side { get; }

    /// <summary>
    /// Gets the order type.
    /// </summary>
    public OrderType Type { get; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets the order price.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// Gets the order quantity.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Gets or sets the filled quantity.
    /// </summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Gets or sets the average fill price.
    /// </summary>
    public decimal AverageFillPrice { get; set; }

    /// <summary>
    /// Gets the total cost/proceeds of the order.
    /// </summary>
    public decimal TotalValue => Price * Quantity;

    /// <summary>
    /// Gets the timestamp when the order was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets or sets the timestamp when the order was last updated.
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets a value indicating whether the order is completely filled.
    /// </summary>
    public bool IsCompletelyFilled => Status == OrderStatus.Filled && FilledQuantity >= Quantity;

    /// <summary>
    /// Gets a value indicating whether the order is active.
    /// </summary>
    public bool IsActive => Status == OrderStatus.New || Status == OrderStatus.PartiallyFilled;

    /// <summary>
    /// Updates the order status.
    /// </summary>
    /// <param name="status">The new status.</param>
    /// <param name="filledQuantity">The filled quantity.</param>
    /// <param name="averageFillPrice">The average fill price.</param>
    public void UpdateStatus(OrderStatus status, decimal filledQuantity = 0, decimal averageFillPrice = 0)
    {
        Status = status;
        FilledQuantity = filledQuantity;
        
        if (averageFillPrice > 0)
        {
            AverageFillPrice = averageFillPrice;
        }
        
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// The order has been created but not yet processed.
    /// </summary>
    New,
    
    /// <summary>
    /// The order has been partially filled.
    /// </summary>
    PartiallyFilled,
    
    /// <summary>
    /// The order has been filled completely.
    /// </summary>
    Filled,
    
    /// <summary>
    /// The order has been canceled.
    /// </summary>
    Canceled,
    
    /// <summary>
    /// The order has been rejected.
    /// </summary>
    Rejected,
    
    /// <summary>
    /// The order has expired.
    /// </summary>
    Expired
} 