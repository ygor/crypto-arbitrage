using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ArbitrageBot.Infrastructure.Services;

/// <summary>
/// Repository for persisting arbitrage-related data.
/// </summary>
public class ArbitrageRepository : IArbitrageRepository
{
    private readonly ILogger<ArbitrageRepository> _logger;
    
    // In-memory storage for data
    // In a real application, these would be persisted to a database
    private readonly ConcurrentDictionary<string, ArbitrageOpportunity> _opportunities = new();
    private readonly ConcurrentDictionary<string, (ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)> _tradeResults = new();
    private readonly ConcurrentDictionary<DateTimeOffset, ArbitrageStatistics> _statistics = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ArbitrageRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ArbitrageRepository(ILogger<ArbitrageRepository> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        var id = GenerateOpportunityId(opportunity);
        _opportunities[id] = opportunity;
        
        _logger.LogDebug("Saved arbitrage opportunity with ID {Id}", id);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task SaveTradeResultAsync(
        ArbitrageOpportunity opportunity, 
        TradeResult buyResult, 
        TradeResult sellResult, 
        decimal profit, 
        DateTimeOffset timestamp, 
        CancellationToken cancellationToken = default)
    {
        var id = GenerateTradeResultId(opportunity, timestamp);
        _tradeResults[id] = (opportunity, buyResult, sellResult, profit, timestamp);
        
        _logger.LogDebug("Saved trade result with ID {Id}", id);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        CancellationToken cancellationToken = default)
    {
        var opportunities = _opportunities.Values
            .Where(o => o.Timestamp >= start && o.Timestamp <= end)
            .ToList()
            .AsReadOnly();
        
        _logger.LogDebug("Retrieved {Count} opportunities between {Start} and {End}", 
            opportunities.Count, start, end);
        
        return Task.FromResult<IReadOnlyCollection<ArbitrageOpportunity>>(opportunities);
    }
    
    /// <inheritdoc />
    public Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        CancellationToken cancellationToken = default)
    {
        var results = _tradeResults.Values
            .Where(r => r.Timestamp >= start && r.Timestamp <= end)
            .ToList()
            .AsReadOnly();
        
        _logger.LogDebug("Retrieved {Count} trade results between {Start} and {End}", 
            results.Count, start, end);
        
        return Task.FromResult<IReadOnlyCollection<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>>(results);
    }
    
    /// <inheritdoc />
    public Task<ArbitrageStatistics> GetStatisticsAsync(
        DateTimeOffset start, 
        DateTimeOffset end, 
        CancellationToken cancellationToken = default)
    {
        // Check if we have a statistics record that matches exactly
        if (_statistics.TryGetValue(start, out var stats) && stats.EndTime == end)
        {
            return Task.FromResult(stats);
        }
        
        // Otherwise, compute basic statistics from trade results
        var results = _tradeResults.Values
            .Where(r => r.Timestamp >= start && r.Timestamp <= end)
            .ToList();
        
        var statistics = new ArbitrageStatistics
        {
            StartTime = start,
            EndTime = end,
            TotalTradesExecuted = results.Count,
            SuccessfulTrades = results.Count(r => r.BuyResult.IsSuccess && r.SellResult.IsSuccess),
            FailedTrades = results.Count(r => !r.BuyResult.IsSuccess || !r.SellResult.IsSuccess),
            TotalProfit = results.Sum(r => r.Profit)
        };
        
        // Additional statistics calculation would be implemented here
        
        return Task.FromResult(statistics);
    }
    
    /// <inheritdoc />
    public Task SaveStatisticsAsync(
        ArbitrageStatistics statistics, 
        DateTimeOffset timestamp, 
        CancellationToken cancellationToken = default)
    {
        _statistics[timestamp] = statistics;
        
        _logger.LogDebug("Saved statistics for timestamp {Timestamp}", timestamp);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Generates a unique ID for an arbitrage opportunity.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity.</param>
    /// <returns>A unique ID.</returns>
    private string GenerateOpportunityId(ArbitrageOpportunity opportunity)
    {
        return $"{opportunity.TradingPair}_{opportunity.BuyExchangeId}_{opportunity.SellExchangeId}_{opportunity.Timestamp.Ticks}";
    }
    
    /// <summary>
    /// Generates a unique ID for a trade result.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>A unique ID.</returns>
    private string GenerateTradeResultId(ArbitrageOpportunity opportunity, DateTimeOffset timestamp)
    {
        return $"TR_{opportunity.TradingPair}_{opportunity.BuyExchangeId}_{opportunity.SellExchangeId}_{timestamp.Ticks}";
    }
} 