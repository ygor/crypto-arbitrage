namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.CancelTrade;

/// <summary>
/// Result of cancelling a trade command.
/// </summary>
public record CancelTradeResult
{
    /// <summary>
    /// Whether the trade cancellation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The trade ID that was cancelled.
    /// </summary>
    public string? TradeId { get; init; }

    /// <summary>
    /// Error message if cancellation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The reason for cancellation.
    /// </summary>
    public string? CancellationReason { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CancelTradeResult Success(string tradeId, string cancellationReason)
    {
        return new CancelTradeResult
        {
            IsSuccess = true,
            TradeId = tradeId,
            CancellationReason = cancellationReason
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CancelTradeResult Failure(string errorMessage, string? tradeId = null)
    {
        return new CancelTradeResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            TradeId = tradeId
        };
    }
} 