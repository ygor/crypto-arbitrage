using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Interface for persisting arbitrage-related data.
/// </summary>
public interface IArbitrageRepository
{
    /// <summary>
    /// Saves a detected arbitrage opportunity.
    /// </summary>
    /// <param name="opportunity">The opportunity to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the results of an executed arbitrage trade.
    /// </summary>
    /// <param name="opportunity">The opportunity that was executed.</param>
    /// <param name="buyResult">The result of the buy operation.</param>
    /// <param name="sellResult">The result of the sell operation.</param>
    /// <param name="profit">The realized profit or loss.</param>
    /// <param name="timestamp">The timestamp of the execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveTradeResultAsync(
        ArbitrageOpportunity opportunity, 
        TradeResult buyResult, 
        TradeResult sellResult, 
        decimal profit, 
        DateTimeOffset timestamp, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets arbitrage opportunities within the specified time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of arbitrage opportunities.</returns>
    Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets trade results within the specified time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of trade results with their associated opportunities.</returns>
    Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets arbitrage statistics for the specified time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Arbitrage statistics for the specified time range.</returns>
    Task<ArbitrageStatistics> GetStatisticsAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves arbitrage statistics.
    /// </summary>
    /// <param name="statistics">The statistics to save.</param>
    /// <param name="timestamp">The timestamp for the statistics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveStatisticsAsync(
        ArbitrageStatistics statistics, 
        DateTimeOffset timestamp, 
        CancellationToken cancellationToken = default);
} 