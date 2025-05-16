namespace ArbitrageBot.Domain.Models;

/// <summary>
/// Represents the statistics for arbitrage operations.
/// </summary>
public class ArbitrageStatistics
{
    /// <summary>
    /// Gets or sets the total number of arbitrage opportunities detected.
    /// </summary>
    public int TotalOpportunitiesDetected { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of arbitrage trades executed.
    /// </summary>
    public int TotalTradesExecuted { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of successful trades.
    /// </summary>
    public int SuccessfulTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of failed trades.
    /// </summary>
    public int FailedTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the total profit in quote currency.
    /// </summary>
    public decimal TotalProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the highest profit from a single arbitrage trade.
    /// </summary>
    public decimal HighestProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the lowest profit (or highest loss) from a single arbitrage trade.
    /// </summary>
    public decimal LowestProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the average profit per trade.
    /// </summary>
    public decimal AverageProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the total volume traded in quote currency.
    /// </summary>
    public decimal TotalVolume { get; set; }
    
    /// <summary>
    /// Gets or sets the total fees paid in quote currency.
    /// </summary>
    public decimal TotalFees { get; set; }
    
    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Gets or sets the success rate as a percentage.
    /// </summary>
    public decimal SuccessRate { get; set; }
    
    /// <summary>
    /// Gets or sets the profit factor (total profit / total loss).
    /// </summary>
    public decimal ProfitFactor { get; set; }
    
    /// <summary>
    /// Gets or sets the start time for the statistics period.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }
    
    /// <summary>
    /// Gets or sets the end time for the statistics period.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }
    
    /// <summary>
    /// Gets or sets the statistics by exchange.
    /// </summary>
    public Dictionary<string, ExchangeStatistics> StatisticsByExchange { get; set; } = new Dictionary<string, ExchangeStatistics>();
    
    /// <summary>
    /// Gets or sets the statistics by trading pair.
    /// </summary>
    public Dictionary<string, TradingPairStatistics> StatisticsByTradingPair { get; set; } = new Dictionary<string, TradingPairStatistics>();
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