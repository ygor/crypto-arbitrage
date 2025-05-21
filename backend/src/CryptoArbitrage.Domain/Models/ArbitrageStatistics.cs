using System;
using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Statistical information about arbitrage operations.
/// </summary>
public class ArbitrageStatistics
{
    /// <summary>
    /// Gets or sets the unique identifier of these statistics.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the trading pair these statistics are for.
    /// </summary>
    public string TradingPair { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when these statistics were created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the start time for the statistics period.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }
    
    /// <summary>
    /// Gets or sets the end time for the statistics period.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }
    
    // Opportunity-related statistics
    /// <summary>
    /// Gets or sets the total number of opportunities detected.
    /// </summary>
    public int TotalOpportunitiesCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of qualified opportunities.
    /// </summary>
    public int QualifiedOpportunitiesCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of trades executed.
    /// </summary>
    public int TotalTradesCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of successful trades.
    /// </summary>
    public int SuccessfulTradesCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of failed trades.
    /// </summary>
    public int FailedTradesCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total profit made from all successful trades.
    /// </summary>
    public decimal TotalProfitAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the average profit per trade.
    /// </summary>
    public decimal AverageProfitAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the highest profit from a single trade.
    /// </summary>
    public decimal HighestProfitAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the lowest profit from a single trade.
    /// </summary>
    public decimal LowestProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public decimal AverageExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Gets or sets the total fees paid across all trades.
    /// </summary>
    public decimal TotalFeesAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the total volume traded.
    /// </summary>
    public decimal TotalVolume { get; set; }
    
    /// <summary>
    /// Gets or sets the average profit percentage.
    /// </summary>
    public decimal AverageProfitPercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the highest profit percentage from a single trade.
    /// </summary>
    public decimal HighestProfitPercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the success rate as a percentage.
    /// </summary>
    public decimal SuccessRate { get; set; }
    
    // Time-based metrics
    /// <summary>
    /// Gets or sets the opportunities per hour.
    /// </summary>
    public int OpportunitiesPerHour => CalculatePerHour(TotalOpportunitiesCount);
    
    /// <summary>
    /// Gets or sets the trades per hour.
    /// </summary>
    public int TradesPerHour => CalculatePerHour(TotalTradesCount);
    
    /// <summary>
    /// Gets or sets the profit per hour.
    /// </summary>
    public decimal ProfitPerHour => CalculatePerHour(TotalProfitAmount);
    
    /// <summary>
    /// Gets or sets the duration in hours.
    /// </summary>
    public double DurationInHours => (EndTime - StartTime).TotalHours;
    
    // Dictionary collections for statistics
    /// <summary>
    /// Gets or sets the opportunities by exchange pair.
    /// </summary>
    public Dictionary<string, int> OpportunitiesByExchangePair { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the opportunities by trading pair.
    /// </summary>
    public Dictionary<string, int> OpportunitiesByTradingPair { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Gets or sets the opportunities by hour.
    /// </summary>
    public Dictionary<string, int> OpportunitiesByHour { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Gets or sets the trades by hour.
    /// </summary>
    public Dictionary<string, int> TradesByHour { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Gets or sets the profit by hour.
    /// </summary>
    public Dictionary<string, decimal> ProfitByHour { get; set; } = new Dictionary<string, decimal>();
    
    // Properties for backward compatibility
    /// <summary>
    /// Gets or sets the total number of opportunities detected (backward compatibility).
    /// </summary>
    public int TotalOpportunitiesDetected { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of trades executed (backward compatibility).
    /// </summary>
    public int TotalTradesExecuted { get; set; }
    
    /// <summary>
    /// Gets or sets the number of successful trades (backward compatibility).
    /// </summary>
    public int SuccessfulTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the number of failed trades (backward compatibility).
    /// </summary>
    public int FailedTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the total profit (backward compatibility).
    /// </summary>
    public decimal TotalProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the total fees (backward compatibility).
    /// </summary>
    public decimal TotalFees { get; set; }
    
    /// <summary>
    /// Gets or sets the average profit (backward compatibility).
    /// </summary>
    public decimal AverageProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the highest profit (backward compatibility).
    /// </summary>
    public decimal HighestProfit { get; set; }
    
    // Exchange-specific statistics
    /// <summary>
    /// Gets or sets the statistics by exchange.
    /// </summary>
    public Dictionary<string, ExchangeStatistics> StatisticsByExchange { get; set; } = new();
    
    // Trading pair-specific statistics
    /// <summary>
    /// Gets or sets the statistics by trading pair.
    /// </summary>
    public Dictionary<string, TradingPairStatistics> StatisticsByTradingPair { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when these statistics were last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the most frequent trading pairs.
    /// </summary>
    public List<string> MostFrequentTradingPairs { get; set; } = new List<string>();
    
    private int CalculatePerHour(int count)
    {
        if (DurationInHours <= 0) return 0;
        return (int)(count / DurationInHours);
    }
    
    private decimal CalculatePerHour(decimal value)
    {
        if (DurationInHours <= 0) return 0;
        return value / (decimal)DurationInHours;
    }
}

/// <summary>
/// Represents the statistics for a specific exchange.
/// </summary>
public class ExchangeStatistics
{
    /// <summary>
    /// Gets or sets the exchange ID.
    /// </summary>
    public string ExchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the total number of trades.
    /// </summary>
    public int TotalTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the total profit from this exchange.
    /// </summary>
    public decimal TotalProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the total volume traded on this exchange.
    /// </summary>
    public decimal TotalVolume { get; set; }
    
    /// <summary>
    /// Gets or sets the total fees paid on this exchange.
    /// </summary>
    public decimal TotalFees { get; set; }
    
    /// <summary>
    /// Gets or sets the average execution time for trades on this exchange.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }
}

/// <summary>
/// Represents the statistics for a specific trading pair.
/// </summary>
public class TradingPairStatistics
{
    /// <summary>
    /// Gets or sets the trading pair.
    /// </summary>
    public string TradingPair { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the total number of trades for this trading pair.
    /// </summary>
    public int TotalTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the number of successful trades for this trading pair.
    /// </summary>
    public int SuccessfulTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the number of failed trades for this trading pair.
    /// </summary>
    public int FailedTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the total profit from this trading pair.
    /// </summary>
    public decimal TotalProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the total volume traded for this trading pair.
    /// </summary>
    public decimal TotalVolume { get; set; }
    
    /// <summary>
    /// Gets or sets the highest spread percentage observed.
    /// </summary>
    public decimal HighestSpreadPercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the average spread percentage observed.
    /// </summary>
    public decimal AverageSpreadPercentage { get; set; }
} 