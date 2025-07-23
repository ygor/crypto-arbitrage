using CryptoArbitrage.Domain.Models;
using System.Collections.Generic;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Commands.UpdateBalance;

/// <summary>
/// Result of updating balances from exchanges.
/// </summary>
public record UpdateBalanceResult
{
    /// <summary>
    /// Whether the balance update was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Updated balances by exchange.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyCollection<Balance>> UpdatedBalances { get; init; } 
        = new Dictionary<string, IReadOnlyCollection<Balance>>();

    /// <summary>
    /// Number of exchanges updated.
    /// </summary>
    public int ExchangesUpdated { get; init; }

    /// <summary>
    /// Number of balances updated.
    /// </summary>
    public int BalancesUpdated { get; init; }

    /// <summary>
    /// Error message if update failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Warnings encountered during update.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Duration of the update operation in milliseconds.
    /// </summary>
    public long UpdateDurationMs { get; init; }

    /// <summary>
    /// Timestamp when the update was completed.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static UpdateBalanceResult Success(
        IReadOnlyDictionary<string, IReadOnlyCollection<Balance>> balances,
        long durationMs,
        IReadOnlyList<string>? warnings = null)
    {
        int exchangesUpdated = balances.Count;
        int balancesUpdated = 0;
        foreach (IReadOnlyCollection<Balance> exchangeBalances in balances.Values)
        {
            balancesUpdated += exchangeBalances.Count;
        }

        return new UpdateBalanceResult
        {
            IsSuccess = true,
            UpdatedBalances = balances,
            ExchangesUpdated = exchangesUpdated,
            BalancesUpdated = balancesUpdated,
            UpdateDurationMs = durationMs,
            Warnings = warnings ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static UpdateBalanceResult Failure(string errorMessage, long durationMs = 0)
    {
        return new UpdateBalanceResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            UpdateDurationMs = durationMs
        };
    }
} 