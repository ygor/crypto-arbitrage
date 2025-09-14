using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Configuration settings for arbitrage trading.
/// </summary>
public class ArbitrageConfiguration
{
    /// <summary>
    /// Gets or sets whether the arbitrage system is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether paper trading is enabled.
    /// </summary>
    public bool PaperTradingEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether automated trade execution is enabled.
    /// </summary>
    public bool AutoExecuteTrades { get; set; }

    /// <summary>
    /// Gets or sets whether automated trade execution is enabled (alternative property).
    /// </summary>
    public bool AutoTradeEnabled { get; set; }

    /// <summary>
    /// Gets or sets the minimum profit percentage required to execute a trade.
    /// </summary>
    public decimal MinProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the minimum profit percentage required to execute a trade.
    /// </summary>
    public decimal MinimumProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the maximum trade amount in base currency.
    /// </summary>
    public decimal MaxTradeAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent trades.
    /// </summary>
    public int MaxConcurrentTrades { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent arbitrage operations.
    /// </summary>
    public int MaxConcurrentArbitrageOperations { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of concurrent operations (alias for MaxConcurrentArbitrageOperations).
    /// </summary>
    public int MaxConcurrentOperations 
    { 
        get => MaxConcurrentArbitrageOperations; 
        set => MaxConcurrentArbitrageOperations = value; 
    }

    /// <summary>
    /// Gets or sets the maximum number of concurrent executions.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries for failed trades.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use market orders instead of limit orders.
    /// </summary>
    public bool UseMarketOrders { get; set; }

    /// <summary>
    /// Gets or sets the maximum slippage percentage allowed.
    /// </summary>
    public decimal MaxSlippagePercentage { get; set; }

    /// <summary>
    /// Gets or sets the minimum order book depth required.
    /// </summary>
    public int MinOrderBookDepth { get; set; }

    /// <summary>
    /// Gets or sets the maximum time to wait for order execution in milliseconds.
    /// </summary>
    public int MaxOrderExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum time to wait for execution in milliseconds.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the risk profile for the arbitrage system.
    /// </summary>
    public RiskProfile RiskProfile { get; set; } = new RiskProfile();

    /// <summary>
    /// Gets or sets the list of trading pairs to monitor.
    /// </summary>
    public List<TradingPair> TradingPairs { get; set; } = new List<TradingPair>();

    /// <summary>
    /// Gets or sets the list of enabled exchanges.
    /// </summary>
    public List<string> EnabledExchanges { get; set; } = new List<string> { "coinbase", "kraken" };
    
    /// <summary>
    /// Gets or sets the list of enabled trading pairs as strings.
    /// </summary>
    public List<string> EnabledTradingPairs { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets whether live trading is enabled (not paper trading).
    /// </summary>
    public bool IsLiveTradingEnabled { get; set; } = false;
} 