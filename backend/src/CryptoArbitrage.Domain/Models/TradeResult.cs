using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the result of a trade execution.
/// </summary>
public class TradeResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the trade result.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client order identifier.
    /// </summary>
    public string? ClientOrderId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the opportunity that triggered this trade.
    /// </summary>
    public Guid OpportunityId { get; set; }

    /// <summary>
    /// Gets or sets the exchange identifier where the trade was executed.
    /// </summary>
    public string ExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trading pair for the trade.
    /// </summary>
    public string TradingPair { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the order.
    /// </summary>
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Gets or sets the side of the order (buy or sell).
    /// </summary>
    public OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the type of trade.
    /// </summary>
    public TradeType TradeType { get; set; }

    /// <summary>
    /// Gets or sets the requested price for the order.
    /// </summary>
    public decimal RequestedPrice { get; set; }

    /// <summary>
    /// Gets or sets the actual price at which the order was executed.
    /// </summary>
    public decimal ExecutedPrice { get; set; }

    /// <summary>
    /// Gets or sets the requested quantity for the order.
    /// </summary>
    public decimal RequestedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the actual quantity that was executed.
    /// </summary>
    public decimal ExecutedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the total value of the trade.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Gets or sets the fee for the trade.
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the status of the trade.
    /// </summary>
    public TradeStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the buy exchange identifier for arbitrage trades.
    /// </summary>
    public string BuyExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sell exchange identifier for arbitrage trades.
    /// </summary>
    public string SellExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buy price for arbitrage trades.
    /// </summary>
    public decimal BuyPrice { get; set; }

    /// <summary>
    /// Gets or sets the sell price for arbitrage trades.
    /// </summary>
    public decimal SellPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity for arbitrage trades.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the profit amount for arbitrage trades.
    /// </summary>
    public decimal ProfitAmount { get; set; }

    /// <summary>
    /// Gets or sets the profit percentage for arbitrage trades.
    /// </summary>
    public decimal ProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the total fees for the trade.
    /// </summary>
    public decimal Fees { get; set; }

    /// <summary>
    /// Gets or sets the currency in which the fees were charged.
    /// </summary>
    public string FeeCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets whether the trade was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets any error message if the trade failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the trade execution details.
    /// </summary>
    public TradeExecution? TradeExecution { get; set; }

    /// <summary>
    /// Gets or sets the result of the buy order for arbitrage trades.
    /// </summary>
    public TradeSubResult? BuyResult { get; set; }

    /// <summary>
    /// Gets or sets the result of the sell order for arbitrage trades.
    /// </summary>
    public TradeSubResult? SellResult { get; set; }

    /// <summary>
    /// Gets or sets the sub-results of the trade.
    /// </summary>
    public List<TradeSubResult> SubResults { get; set; } = new();

    /// <summary>
    /// Creates a successful trade result with minimal information.
    /// </summary>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A successful trade result.</returns>
    public static TradeResult Success(double executionTimeMs)
    {
        return new TradeResult
        {
            IsSuccess = true,
            Timestamp = DateTime.UtcNow,
            ExecutionTimeMs = (long)executionTimeMs,
            Status = TradeStatus.Completed
        };
    }

    /// <summary>
    /// Creates a successful trade result from order details.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="side">The order side.</param>
    /// <param name="price">The executed price.</param>
    /// <param name="quantity">The executed quantity.</param>
    /// <param name="fee">The fee amount.</param>
    /// <param name="feeCurrency">The fee currency.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A successful trade result.</returns>
    public static TradeResult Success(
        string orderId,
        string tradingPair,
        OrderSide side,
        decimal price,
        decimal quantity,
        decimal fee,
        string feeCurrency,
        double executionTimeMs)
    {
        return new TradeResult
        {
            IsSuccess = true,
            OrderId = orderId,
            Timestamp = DateTime.UtcNow,
            TradingPair = tradingPair,
            Side = side,
            ExecutedPrice = price,
            ExecutedQuantity = quantity,
            Fees = fee,
            FeeCurrency = feeCurrency,
            ExecutionTimeMs = (long)executionTimeMs,
            Status = TradeStatus.Completed
        };
    }

    /// <summary>
    /// Creates a failed trade result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A failed trade result.</returns>
    public static TradeResult Failure(string errorMessage, double executionTimeMs)
    {
        return new TradeResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            ExecutionTimeMs = (long)executionTimeMs,
            Status = TradeStatus.Failed
        };
    }

    /// <summary>
    /// Creates a failed trade result with an error message from an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A failed trade result.</returns>
    public static TradeResult Failure(Exception exception, double executionTimeMs)
    {
        return Failure(exception.Message, executionTimeMs);
    }

    /// <summary>
    /// Creates a successful trade result from a trade execution.
    /// </summary>
    /// <param name="execution">The trade execution.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A successful trade result.</returns>
    public static TradeResult Success(TradeExecution execution, double executionTimeMs)
    {
        return new TradeResult
        {
            IsSuccess = true,
            OrderId = execution.OrderId,
            ExecutedPrice = execution.Price,
            ExecutedQuantity = execution.Quantity,
            Fee = execution.Fee,
            FeeCurrency = execution.FeeCurrency,
            Timestamp = execution.Timestamp,
            ExecutionTimeMs = (long)executionTimeMs,
            Status = TradeStatus.Completed,
            Side = execution.Side
        };
    }
} 