using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Domain.Models.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Interfaces;

/// <summary>
/// Service interface for executing trades.
/// </summary>
public interface ITradeExecutionService
{
    /// <summary>
    /// Executes a trade for the given opportunity.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity to execute.</param>
    /// <param name="quantity">Optional quantity override. If null, uses the opportunity's effective quantity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the trade execution.</returns>
    Task<TradeResult?> ExecuteTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal? quantity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates a trade for the given opportunity.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity to simulate.</param>
    /// <param name="quantity">The quantity to simulate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The simulated trade result.</returns>
    Task<TradeResult> SimulateTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a trade is executed.
    /// </summary>
    event Func<TradeResult, Task> OnTradeExecuted;

    /// <summary>
    /// Event raised when an error occurs during trade execution.
    /// </summary>
    event Func<Domain.Models.Events.ErrorEventArgs, Task> OnError;
} 