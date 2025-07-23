using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Events;

/// <summary>
/// Event published when a trade is executed successfully.
/// </summary>
public record TradeExecutedEvent : INotification
{
    /// <summary>
    /// The trade result details.
    /// </summary>
    public required TradeResult TradeResult { get; init; }

    /// <summary>
    /// The original arbitrage opportunity.
    /// </summary>
    public required ArbitrageOpportunity Opportunity { get; init; }

    /// <summary>
    /// Total execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime EventTimestamp { get; init; } = DateTime.UtcNow;
} 