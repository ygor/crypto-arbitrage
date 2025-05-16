using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents account balances across various assets.
/// </summary>
public class AccountBalances
{
    private readonly Dictionary<string, decimal> _balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountBalances"/> class.
    /// </summary>
    public AccountBalances()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountBalances"/> class with initial balances.
    /// </summary>
    /// <param name="balances">The initial balances.</param>
    public AccountBalances(IDictionary<string, decimal> balances)
    {
        if (balances == null) return;
        
        foreach (var balance in balances)
        {
            _balances[balance.Key] = balance.Value;
        }
    }

    /// <summary>
    /// Gets the balance for a specific asset.
    /// </summary>
    /// <param name="asset">The asset symbol.</param>
    /// <returns>The balance amount, or 0 if not found.</returns>
    public decimal GetBalance(string asset)
    {
        return _balances.TryGetValue(asset, out var balance) ? balance : 0m;
    }

    /// <summary>
    /// Sets the balance for a specific asset.
    /// </summary>
    /// <param name="asset">The asset symbol.</param>
    /// <param name="amount">The balance amount.</param>
    public void SetBalance(string asset, decimal amount)
    {
        if (string.IsNullOrEmpty(asset))
            throw new ArgumentException("Asset cannot be null or empty", nameof(asset));
        
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        _balances[asset] = amount;
    }

    /// <summary>
    /// Adjusts the balance for a specific asset.
    /// </summary>
    /// <param name="asset">The asset symbol.</param>
    /// <param name="delta">The amount to adjust by (can be negative).</param>
    /// <returns>The new balance after adjustment.</returns>
    public decimal AdjustBalance(string asset, decimal delta)
    {
        if (string.IsNullOrEmpty(asset))
            throw new ArgumentException("Asset cannot be null or empty", nameof(asset));
        
        var currentBalance = GetBalance(asset);
        var newBalance = currentBalance + delta;
        
        if (newBalance < 0)
            throw new InvalidOperationException($"Insufficient balance for {asset}. Current: {currentBalance}, Requested: {delta}");
        
        _balances[asset] = newBalance;
        return newBalance;
    }

    /// <summary>
    /// Gets all balances.
    /// </summary>
    /// <returns>A dictionary of all balances.</returns>
    public IDictionary<string, decimal> GetAllBalances()
    {
        return new Dictionary<string, decimal>(_balances);
    }

    /// <summary>
    /// Gets all non-zero balances.
    /// </summary>
    /// <returns>A dictionary of all non-zero balances.</returns>
    public IDictionary<string, decimal> GetNonZeroBalances()
    {
        return _balances.Where(b => b.Value > 0).ToDictionary(b => b.Key, b => b.Value);
    }
} 