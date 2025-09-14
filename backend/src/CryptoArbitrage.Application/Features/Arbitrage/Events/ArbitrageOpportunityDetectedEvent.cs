using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Arbitrage.Events;

/// <summary>
/// Event published when an arbitrage opportunity is detected.
/// </summary>
public record ArbitrageOpportunityDetectedEvent(
    ArbitrageOpportunity Opportunity
) : INotification
{
    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event identifier for tracking.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();
} 