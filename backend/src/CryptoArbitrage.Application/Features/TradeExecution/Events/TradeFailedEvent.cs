using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Events;

/// <summary>
/// Event published when a trade execution fails.
/// </summary>
public record TradeFailedEvent : INotification
{
    /// <summary>
    /// The opportunity ID that failed to execute.
    /// </summary>
    public required string OpportunityId { get; init; }

    /// <summary>
    /// The error message describing the failure.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// The original arbitrage opportunity.
    /// </summary>
    public required ArbitrageOpportunity Opportunity { get; init; }

    /// <summary>
    /// Total execution time before failure in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime EventTimestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional exception details if available.
    /// </summary>
    public Exception? Exception { get; init; }
} 