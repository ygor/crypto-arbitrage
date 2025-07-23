using MediatR;

namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.CancelTrade;

/// <summary>
/// Command to cancel an active trade.
/// </summary>
public record CancelTradeCommand : IRequest<CancelTradeResult>
{
    /// <summary>
    /// The trade ID to cancel.
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// The opportunity ID associated with the trade.
    /// </summary>
    public string? OpportunityId { get; init; }

    /// <summary>
    /// The reason for cancellation.
    /// </summary>
    public required string CancellationReason { get; init; }

    /// <summary>
    /// User who initiated the cancellation.
    /// </summary>
    public string? CancelledBy { get; init; }
} 