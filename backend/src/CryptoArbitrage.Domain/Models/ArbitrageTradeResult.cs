using System;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the result of an arbitrage trade execution, including both buy and sell operations.
/// </summary>
public class ArbitrageTradeResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArbitrageTradeResult"/> class.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity that was executed.</param>
    public ArbitrageTradeResult(ArbitrageOpportunity opportunity)
    {
        Opportunity = opportunity ?? throw new ArgumentNullException(nameof(opportunity));
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the arbitrage opportunity that was executed.
    /// </summary>
    public ArbitrageOpportunity Opportunity { get; }

    /// <summary>
    /// Gets the timestamp when the trade was executed.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the trade was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result of the buy operation.
    /// </summary>
    public TradeResult? BuyResult { get; set; }

    /// <summary>
    /// Gets or sets the result of the sell operation.
    /// </summary>
    public TradeResult? SellResult { get; set; }

    /// <summary>
    /// Gets or sets the profit amount from the trade.
    /// </summary>
    public decimal ProfitAmount { get; set; }

    /// <summary>
    /// Gets or sets the profit percentage from the trade.
    /// </summary>
    public decimal ProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the error message if the trade failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
} 