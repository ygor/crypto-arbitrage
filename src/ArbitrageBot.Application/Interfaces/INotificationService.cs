using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Service interface for sending notifications about arbitrage events.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification about a detected arbitrage opportunity.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity that was detected.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyOpportunityDetectedAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification about a completed arbitrage trade.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity that was executed.</param>
    /// <param name="buyResult">The result of the buy operation.</param>
    /// <param name="sellResult">The result of the sell operation.</param>
    /// <param name="profit">The realized profit (or loss).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyArbitrageCompletedAsync(
        ArbitrageOpportunity opportunity, 
        TradeResult buyResult, 
        TradeResult sellResult, 
        decimal profit,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification about a failed arbitrage trade.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity that failed.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyArbitrageFailedAsync(
        ArbitrageOpportunity opportunity, 
        string errorMessage,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification about a system error.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    /// <param name="severity">The severity of the error.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifySystemErrorAsync(
        Exception error, 
        ErrorSeverity severity,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification about a system error.
    /// </summary>
    /// <param name="title">The error title.</param>
    /// <param name="message">The error message.</param>
    /// <param name="severity">The severity of the error.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifySystemErrorAsync(
        string title,
        string message,
        ErrorSeverity severity,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification with daily statistics.
    /// </summary>
    /// <param name="statistics">The daily statistics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyDailyStatisticsAsync(
        ArbitrageStatistics statistics,
        CancellationToken cancellationToken = default);
} 