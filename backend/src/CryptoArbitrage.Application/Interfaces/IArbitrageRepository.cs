using CryptoArbitrage.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Interfaces;

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

    // Opportunity methods
    Task<ArbitrageOpportunity> SaveOpportunityAsync(ArbitrageOpportunity opportunity);
    Task<List<ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 100, TimeSpan? timeSpan = null);
    Task<List<ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100);
    
    // Trade methods
    Task<TradeResult> SaveTradeResultAsync(TradeResult tradeResult);
    Task<List<TradeResult>> GetRecentTradesAsync(int limit = 100, TimeSpan? timeSpan = null);
    Task<List<TradeResult>> GetTradesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100);
    Task<TradeResult?> GetTradeByIdAsync(string id);
    Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId);
    
    // Statistics methods
    Task<ArbitrageStatistics> GetCurrentDayStatisticsAsync();
    Task<ArbitrageStatistics> GetLastDayStatisticsAsync();
    Task<ArbitrageStatistics> GetLastWeekStatisticsAsync();
    Task<ArbitrageStatistics> GetLastMonthStatisticsAsync();
    
    // Cleanup methods
    Task<int> DeleteOldOpportunitiesAsync(DateTimeOffset olderThan);
    Task<int> DeleteOldTradesAsync(DateTimeOffset olderThan);

    /// <summary>
    /// Gets the arbitrage statistics for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get statistics for.</param>
    /// <param name="fromDate">Optional start date for filtering.</param>
    /// <param name="toDate">Optional end date for filtering.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The arbitrage statistics.</returns>
    Task<ArbitrageStatistics> GetArbitrageStatisticsAsync(
        string tradingPair,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves or updates arbitrage statistics.
    /// </summary>
    /// <param name="statistics">The statistics to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveArbitrageStatisticsAsync(ArbitrageStatistics statistics, CancellationToken cancellationToken = default);
} 