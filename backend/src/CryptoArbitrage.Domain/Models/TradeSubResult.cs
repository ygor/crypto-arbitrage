using System;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the result of a single trade (buy or sell) within a larger arbitrage trade.
/// </summary>
public class TradeSubResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client-generated order identifier.
    /// </summary>
    public string ClientOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trading pair for the order.
    /// </summary>
    public string TradingPair { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the side of the order (buy or sell).
    /// </summary>
    public OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the requested quantity for the order.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the requested price for the order.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the actual quantity that was filled.
    /// </summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Gets or sets the average price at which the order was filled.
    /// </summary>
    public decimal AverageFillPrice { get; set; }

    /// <summary>
    /// Gets or sets the fee amount charged for the order.
    /// </summary>
    public decimal FeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency in which the fee was charged.
    /// </summary>
    public string FeeCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the order.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the order was executed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets any error message associated with the order.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the trade was successful.
    /// </summary>
    public bool IsSuccess => Status == OrderStatus.Filled && string.IsNullOrEmpty(ErrorMessage);
} 