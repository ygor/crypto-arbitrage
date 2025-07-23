using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Events;

/// <summary>
/// Event published when a trade is cancelled.
/// </summary>
public record TradeCancelledEvent : INotification
{
    /// <summary>
    /// The trade ID that was cancelled.
    /// </summary>
    public required string TradeId { get; init; }

    /// <summary>
    /// The opportunity ID associated with the cancelled trade.
    /// </summary>
    public required string OpportunityId { get; init; }

    /// <summary>
    /// The reason for cancellation.
    /// </summary>
    public required string CancellationReason { get; init; }

    /// <summary>
    /// The original arbitrage opportunity.
    /// </summary>
    public ArbitrageOpportunity? Opportunity { get; init; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime EventTimestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// User who initiated the cancellation.
    /// </summary>
    public string? CancelledBy { get; init; }

    /// <summary>
    /// Whether the cancellation was automatic (due to timeout, etc.) or manual.
    /// </summary>
    public bool IsAutomaticCancellation { get; init; }
} 