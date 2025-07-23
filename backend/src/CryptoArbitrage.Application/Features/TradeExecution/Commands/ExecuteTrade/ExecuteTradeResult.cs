using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.ExecuteTrade;

/// <summary>
/// Result of executing a trade command.
/// </summary>
public record ExecuteTradeResult
{
    /// <summary>
    /// Whether the trade execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The trade result details if execution was successful.
    /// </summary>
    public TradeResult? TradeResult { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Validation warnings that were ignored during forced execution.
    /// </summary>
    public IReadOnlyList<string> ValidationWarnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The opportunity ID that was executed.
    /// </summary>
    public string? OpportunityId { get; init; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ExecuteTradeResult Success(
        TradeResult tradeResult, 
        long executionTimeMs, 
        IReadOnlyList<string>? warnings = null)
    {
        return new ExecuteTradeResult
        {
            IsSuccess = true,
            TradeResult = tradeResult,
            OpportunityId = tradeResult.OpportunityId.ToString(),
            ExecutionTimeMs = executionTimeMs,
            ValidationWarnings = warnings ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ExecuteTradeResult Failure(
        string errorMessage, 
        string? opportunityId = null, 
        long executionTimeMs = 0)
    {
        return new ExecuteTradeResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            OpportunityId = opportunityId,
            ExecutionTimeMs = executionTimeMs
        };
    }
} 