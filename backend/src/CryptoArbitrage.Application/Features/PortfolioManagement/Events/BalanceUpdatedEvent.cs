using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Events;

/// <summary>
/// Event published when portfolio balances are updated.
/// </summary>
public record BalanceUpdatedEvent : INotification
{
    /// <summary>
    /// Exchange ID where balances were updated.
    /// </summary>
    public required string ExchangeId { get; init; }

    /// <summary>
    /// Updated balances for the exchange.
    /// </summary>
    public required IReadOnlyCollection<Balance> UpdatedBalances { get; init; }

    /// <summary>
    /// Previous balances before update (if available).
    /// </summary>
    public IReadOnlyCollection<Balance>? PreviousBalances { get; init; }

    /// <summary>
    /// Significant balance changes (above threshold).
    /// </summary>
    public IReadOnlyList<BalanceChange> SignificantChanges { get; init; } = Array.Empty<BalanceChange>();

    /// <summary>
    /// Timestamp when the balances were updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Update trigger (manual, automatic, trade execution, etc.).
    /// </summary>
    public string UpdateTrigger { get; init; } = "Manual";

    /// <summary>
    /// Whether this was a forced refresh.
    /// </summary>
    public bool WasForcedRefresh { get; init; }
}

/// <summary>
/// Represents a significant balance change.
/// </summary>
public record BalanceChange
{
    /// <summary>
    /// Currency that changed.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Previous balance amount.
    /// </summary>
    public decimal PreviousAmount { get; init; }

    /// <summary>
    /// New balance amount.
    /// </summary>
    public decimal NewAmount { get; init; }

    /// <summary>
    /// Change amount (can be negative).
    /// </summary>
    public decimal ChangeAmount => NewAmount - PreviousAmount;

    /// <summary>
    /// Change percentage.
    /// </summary>
    public decimal ChangePercentage => PreviousAmount > 0 
        ? Math.Round((ChangeAmount / PreviousAmount) * 100, 2) 
        : 0;
} 