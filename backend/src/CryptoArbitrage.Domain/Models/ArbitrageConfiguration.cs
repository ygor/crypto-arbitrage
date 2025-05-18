using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the configuration for the arbitrage system.
/// </summary>
public class ArbitrageConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the arbitrage system is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets the maximum number of concurrent executions.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of concurrent arbitrage operations.
    /// </summary>
    public int MaxConcurrentArbitrageOperations { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the maximum trade amount per execution in quote currency.
    /// </summary>
    public decimal MaxTradeAmount { get; set; } = 100m;
    
    /// <summary>
    /// Gets or sets a value indicating whether to automatically execute trades.
    /// </summary>
    public bool AutoExecuteTrades { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether auto-trading is enabled.
    /// </summary>
    public bool AutoTradeEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether paper trading mode is enabled.
    /// When enabled, trades are simulated without actually executing them on exchanges.
    /// </summary>
    public bool PaperTradingEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum profit percentage required for an arbitrage opportunity.
    /// </summary>
    public decimal MinimumProfitPercentage { get; set; } = 0.5m;

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the risk profile for the arbitrage system.
    /// </summary>
    public RiskProfile RiskProfile { get; set; } = new RiskProfile();
    
    /// <summary>
    /// Gets or sets the trading pairs to monitor.
    /// </summary>
    public IList<TradingPair> TradingPairs { get; set; } = new List<TradingPair>();
} 