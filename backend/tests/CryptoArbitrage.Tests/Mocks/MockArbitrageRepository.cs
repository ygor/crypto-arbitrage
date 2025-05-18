using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Tests.Mocks;

/// <summary>
/// A mock implementation of IArbitrageRepository for testing purposes.
/// </summary>
public class MockArbitrageRepository : IArbitrageRepository
{
    private readonly ConcurrentDictionary<string, ArbitrageOpportunity> _opportunities = new();
    private readonly ConcurrentDictionary<string, TradeResult> _tradeResults = new();
    private readonly ConcurrentDictionary<DateTimeOffset, ArbitrageStatistics> _statisticsCache = new();

    /// <summary>
    /// Saves an arbitrage opportunity asynchronously.
    /// </summary>
    /// <param name="opportunity">The opportunity to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        // Ensure the opportunity has an ID
        if (string.IsNullOrEmpty(opportunity.Id))
        {
            opportunity.Id = Guid.NewGuid().ToString();
        }

        _opportunities[opportunity.Id] = opportunity;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves a trade result asynchronously.
    /// </summary>
    /// <param name="opportunity">The opportunity that was executed.</param>
    /// <param name="buyResult">The result of the buy operation.</param>
    /// <param name="sellResult">The result of the sell operation.</param>
    /// <param name="profit">The realized profit or loss.</param>
    /// <param name="timestamp">The timestamp of the execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveTradeResultAsync(
        ArbitrageOpportunity opportunity,
        TradeResult buyResult,
        TradeResult sellResult,
        decimal profit,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default)
    {
        // Create a combined trade result
        var tradeResult = new TradeResult
        {
            Id = Guid.NewGuid().ToString(),
            OpportunityId = opportunity.Id,
            BuyExchangeId = opportunity.BuyExchangeId,
            SellExchangeId = opportunity.SellExchangeId,
            ProfitAmount = profit,
            Timestamp = timestamp,
            IsSuccess = true
            // Additional properties would be set here in a real implementation
        };

        _tradeResults[tradeResult.Id] = tradeResult;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets arbitrage opportunities within the specified time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of arbitrage opportunities.</returns>
    public Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var opportunities = _opportunities.Values
            .Where(o => o.Timestamp >= start && o.Timestamp <= end)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ArbitrageOpportunity>>(opportunities);
    }

    /// <summary>
    /// Gets trade results within the specified time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of trade results with their associated opportunities.</returns>
    public Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>();

        foreach (var tradeResult in _tradeResults.Values.Where(t => t.Timestamp >= start && t.Timestamp <= end))
        {
            if (_opportunities.TryGetValue(tradeResult.OpportunityId, out var opportunity))
            {
                // In a mock, we're simplifying by using the same trade result for both buy and sell
                results.Add((opportunity, tradeResult, tradeResult, tradeResult.ProfitAmount, tradeResult.Timestamp));
            }
        }

        return Task.FromResult<IReadOnlyCollection<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>>(results);
    }

    /// <summary>
    /// Gets arbitrage statistics for the specified time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Arbitrage statistics for the specified time range.</returns>
    public Task<ArbitrageStatistics> GetStatisticsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        // Try to get cached statistics
        if (_statisticsCache.TryGetValue(start, out var cachedStats))
        {
            return Task.FromResult(cachedStats);
        }

        // Calculate statistics
        var tradeResults = _tradeResults.Values
            .Where(tr => tr.Timestamp >= start && tr.Timestamp <= end)
            .ToList();

        var opportunities = _opportunities.Values
            .Where(o => o.Timestamp >= start && o.Timestamp <= end)
            .ToList();

        var stats = new ArbitrageStatistics
        {
            StartTime = start,
            EndTime = end,
            TotalOpportunitiesDetected = opportunities.Count,
            TotalTradesExecuted = tradeResults.Count,
            SuccessfulTrades = tradeResults.Count(tr => tr.IsSuccess),
            FailedTrades = tradeResults.Count(tr => !tr.IsSuccess),
            TotalProfit = tradeResults.Sum(tr => tr.ProfitAmount),
            TotalVolume = tradeResults.Sum(tr => tr.TotalValue)
        };

        // Cache the statistics
        _statisticsCache[start] = stats;

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Saves arbitrage statistics.
    /// </summary>
    /// <param name="statistics">The statistics to save.</param>
    /// <param name="timestamp">The timestamp for the statistics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveStatisticsAsync(ArbitrageStatistics statistics, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        _statisticsCache[timestamp] = statistics;
        return Task.CompletedTask;
    }

    // Additional interface implementation methods

    public Task<ArbitrageOpportunity> SaveOpportunityAsync(ArbitrageOpportunity opportunity)
    {
        if (string.IsNullOrEmpty(opportunity.Id))
        {
            opportunity.Id = Guid.NewGuid().ToString();
        }

        _opportunities[opportunity.Id] = opportunity;
        return Task.FromResult(opportunity);
    }

    public Task<List<ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        var query = _opportunities.Values.AsEnumerable();

        if (timeSpan.HasValue)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(timeSpan.Value);
            query = query.Where(o => o.Timestamp >= cutoff);
        }

        var result = query.OrderByDescending(o => o.Timestamp).Take(limit).ToList();
        return Task.FromResult(result);
    }

    public Task<List<ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        var result = _opportunities.Values
            .Where(o => o.Timestamp >= start && o.Timestamp <= end)
            .OrderByDescending(o => o.Timestamp)
            .Take(limit)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<TradeResult> SaveTradeResultAsync(TradeResult tradeResult)
    {
        if (string.IsNullOrEmpty(tradeResult.Id))
        {
            tradeResult.Id = Guid.NewGuid().ToString();
        }

        _tradeResults[tradeResult.Id] = tradeResult;
        return Task.FromResult(tradeResult);
    }

    public Task<List<TradeResult>> GetRecentTradesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        var query = _tradeResults.Values.AsEnumerable();

        if (timeSpan.HasValue)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(timeSpan.Value);
            query = query.Where(t => t.Timestamp >= cutoff);
        }

        var result = query.OrderByDescending(t => t.Timestamp).Take(limit).ToList();
        return Task.FromResult(result);
    }

    public Task<List<TradeResult>> GetTradesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        var result = _tradeResults.Values
            .Where(t => t.Timestamp >= start && t.Timestamp <= end)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<TradeResult?> GetTradeByIdAsync(string id)
    {
        _tradeResults.TryGetValue(id, out var result);
        return Task.FromResult(result);
    }

    public Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId)
    {
        var result = _tradeResults.Values
            .Where(t => t.OpportunityId == opportunityId)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<ArbitrageStatistics> GetCurrentDayStatisticsAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return GetStatisticsAsync(today, tomorrow);
    }

    public Task<ArbitrageStatistics> GetLastDayStatisticsAsync()
    {
        var yesterday = DateTimeOffset.UtcNow.Date.AddDays(-1);
        var today = DateTimeOffset.UtcNow.Date;
        return GetStatisticsAsync(yesterday, today);
    }

    public Task<ArbitrageStatistics> GetLastWeekStatisticsAsync()
    {
        var weekAgo = DateTimeOffset.UtcNow.Date.AddDays(-7);
        var today = DateTimeOffset.UtcNow.Date;
        return GetStatisticsAsync(weekAgo, today);
    }

    public Task<ArbitrageStatistics> GetLastMonthStatisticsAsync()
    {
        var monthAgo = DateTimeOffset.UtcNow.Date.AddMonths(-1);
        var today = DateTimeOffset.UtcNow.Date;
        return GetStatisticsAsync(monthAgo, today);
    }

    public Task<int> DeleteOldOpportunitiesAsync(DateTimeOffset olderThan)
    {
        var opportunitiesToRemove = _opportunities.Values
            .Where(o => o.Timestamp < olderThan)
            .ToList();

        foreach (var opportunity in opportunitiesToRemove)
        {
            _opportunities.TryRemove(opportunity.Id, out _);
        }

        return Task.FromResult(opportunitiesToRemove.Count);
    }

    public Task<int> DeleteOldTradesAsync(DateTimeOffset olderThan)
    {
        var tradesToRemove = _tradeResults.Values
            .Where(t => t.Timestamp < olderThan)
            .ToList();

        foreach (var trade in tradesToRemove)
        {
            _tradeResults.TryRemove(trade.Id, out _);
        }

        return Task.FromResult(tradesToRemove.Count);
    }
} 