using System;

namespace ArbitrageBot.Domain.Models;

/// <summary>
/// Represents the result of a trade execution on an exchange.
/// </summary>
public class TradeResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the trade was executed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the trade failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the order ID assigned by the exchange.
    /// </summary>
    public string? OrderId { get; set; }
    
    /// <summary>
    /// Gets or sets the client order ID that we assigned to the order.
    /// </summary>
    public string? ClientOrderId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the trading pair for the trade.
    /// </summary>
    public TradingPair TradingPair { get; set; }
    
    /// <summary>
    /// Gets or sets the trade type (buy or sell).
    /// </summary>
    public TradeType TradeType { get; set; }
    
    /// <summary>
    /// Gets or sets the requested price for the trade.
    /// </summary>
    public decimal RequestedPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the executed price for the trade.
    /// </summary>
    public decimal ExecutedPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the requested quantity for the trade.
    /// </summary>
    public decimal RequestedQuantity { get; set; }
    
    /// <summary>
    /// Gets or sets the executed quantity for the trade.
    /// </summary>
    public decimal ExecutedQuantity { get; set; }
    
    /// <summary>
    /// Gets or sets the total value of the trade.
    /// </summary>
    public decimal TotalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the fee charged by the exchange.
    /// </summary>
    public decimal Fee { get; set; }
    
    /// <summary>
    /// Gets or sets the currency of the fee.
    /// </summary>
    public string? FeeCurrency { get; set; }
    
    /// <summary>
    /// Gets or sets the time it took to execute the trade in milliseconds.
    /// </summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the trade execution details.
    /// </summary>
    public TradeExecution? TradeExecution { get; set; }
    
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
            Timestamp = DateTimeOffset.UtcNow,
            ExecutionTimeMs = executionTimeMs
        };
    }
    
    /// <summary>
    /// Creates a successful trade result.
    /// </summary>
    /// <param name="tradeExecution">The trade execution details.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A successful trade result.</returns>
    public static TradeResult Success(TradeExecution tradeExecution, double executionTimeMs)
    {
        return new TradeResult
        {
            IsSuccess = true,
            OrderId = tradeExecution.OrderId,
            Timestamp = DateTimeOffset.UtcNow,
            TradingPair = tradeExecution.TradingPair,
            TradeType = tradeExecution.Side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            ExecutedPrice = tradeExecution.Price,
            ExecutedQuantity = tradeExecution.Quantity,
            TotalValue = tradeExecution.Price * tradeExecution.Quantity,
            Fee = tradeExecution.Fee,
            FeeCurrency = tradeExecution.FeeCurrency,
            ExecutionTimeMs = executionTimeMs,
            TradeExecution = tradeExecution
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
            Timestamp = DateTimeOffset.UtcNow,
            ExecutionTimeMs = executionTimeMs
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
} 