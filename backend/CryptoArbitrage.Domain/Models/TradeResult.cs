using System;
using System.Text.Json.Serialization;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the result of a trade execution on an exchange.
/// </summary>
public class TradeResult
{
    /// <summary>
    /// Gets or sets the ID of the trade result.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the ID of the trading opportunity.
    /// </summary>
    public string OpportunityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the trading pair for the trade.
    /// </summary>
    public string TradingPair { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the ID of the buy exchange.
    /// </summary>
    public string BuyExchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the ID of the sell exchange.
    /// </summary>
    public string SellExchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the buy price for the trade.
    /// </summary>
    public decimal BuyPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the sell price for the trade.
    /// </summary>
    public decimal SellPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the quantity for the trade.
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the status of the trade.
    /// </summary>
    public TradeStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the profit amount for the trade.
    /// </summary>
    public decimal ProfitAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the profit percentage for the trade.
    /// </summary>
    public decimal ProfitPercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the fees for the trade.
    /// </summary>
    public decimal Fees { get; set; }
    
    /// <summary>
    /// Gets or sets the execution time in milliseconds for the trade.
    /// </summary>
    public decimal ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Gets or sets the buy trade result details.
    /// </summary>
    public BuyTradeResult? BuyResult { get; set; }
    
    /// <summary>
    /// Gets or sets the sell trade result details.
    /// </summary>
    public SellTradeResult? SellResult { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the trade failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the trade was executed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the client order ID that we assigned to the order.
    /// </summary>
    public string ClientOrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the trade type.
    /// </summary>
    public TradeType TradeType { get; set; }
    
    /// <summary>
    /// Gets or sets the executed price.
    /// </summary>
    public decimal ExecutedPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the requested price for the trade.
    /// </summary>
    public decimal RequestedPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the executed quantity.
    /// </summary>
    public decimal ExecutedQuantity { get; set; }
    
    /// <summary>
    /// Gets or sets the requested quantity for the trade.
    /// </summary>
    public decimal RequestedQuantity { get; set; }
    
    /// <summary>
    /// Gets or sets the total value.
    /// </summary>
    public decimal TotalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the fee.
    /// </summary>
    public decimal Fee { get; set; }
    
    /// <summary>
    /// Gets or sets the fee currency.
    /// </summary>
    public string FeeCurrency { get; set; } = string.Empty;
    
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
            ExecutionTimeMs = (decimal)executionTimeMs
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
            TradingPair = tradeExecution.TradingPair.ToString(),
            TradeType = tradeExecution.Side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            ExecutedPrice = tradeExecution.Price,
            ExecutedQuantity = tradeExecution.Quantity,
            TotalValue = tradeExecution.Price * tradeExecution.Quantity,
            Fee = tradeExecution.Fee,
            FeeCurrency = tradeExecution.FeeCurrency,
            ExecutionTimeMs = (decimal)executionTimeMs,
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
            ExecutionTimeMs = (decimal)executionTimeMs
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

/// <summary>
/// Represents the status of a trade.
/// </summary>
public enum TradeStatus
{
    Pending,
    Executing,
    Completed,
    Failed
}

/// <summary>
/// Represents the result of a buy trade execution.
/// </summary>
public class BuyTradeResult
{
    /// <summary>
    /// Gets or sets the order ID assigned by the exchange.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the client order ID that we assigned to the order.
    /// </summary>
    public string ClientOrderId { get; set; } = string.Empty;
    
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
    public string FeeCurrency { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the trade was executed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the trade failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the result of a sell trade execution.
/// </summary>
public class SellTradeResult
{
    /// <summary>
    /// Gets or sets the order ID assigned by the exchange.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the client order ID that we assigned to the order.
    /// </summary>
    public string ClientOrderId { get; set; } = string.Empty;
    
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
    public string FeeCurrency { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the trade was executed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the trade failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
} 