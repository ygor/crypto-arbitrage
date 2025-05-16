namespace ArbitrageBot.Domain.Models;

/// <summary>
/// Represents a risk profile for arbitrage trading.
/// </summary>
public class RiskProfile
{
    /// <summary>
    /// Gets or sets the minimum profit percentage required for an arbitrage opportunity to be considered.
    /// </summary>
    public decimal MinimumProfitPercentage { get; set; } = 0.5m;
    
    /// <summary>
    /// Gets or sets the maximum trade amount per execution in quote currency.
    /// </summary>
    public decimal MaxTradeAmount { get; set; } = 100m;
    
    /// <summary>
    /// Gets or sets the maximum exposure per trading pair in quote currency.
    /// </summary>
    public decimal MaxExposurePerTradingPair { get; set; } = 500m;
    
    /// <summary>
    /// Gets or sets the maximum total exposure across all trading pairs in quote currency.
    /// </summary>
    public decimal MaxTotalExposure { get; set; } = 1000m;
    
    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds for a single arbitrage operation.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 3000;
    
    /// <summary>
    /// Gets or sets a value indicating whether to verify opportunities before execution.
    /// </summary>
    public bool VerifyOpportunitiesBeforeExecution { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the maximum amount of capital to allocate per trade as a percentage of available funds.
    /// </summary>
    public decimal MaxCapitalPerTradePercent { get; set; } = 10.0m;
    
    /// <summary>
    /// Gets or sets the maximum amount of capital to allocate per asset as a percentage of total capital.
    /// </summary>
    public decimal MaxCapitalPerAssetPercent { get; set; } = 25.0m;
    
    /// <summary>
    /// Gets or sets the maximum tolerable slippage percentage.
    /// </summary>
    public decimal MaxSlippagePercentage { get; set; } = 0.2m;
    
    /// <summary>
    /// Gets or sets the stop loss percentage. Set to 0 to disable stop loss.
    /// </summary>
    public decimal StopLossPercentage { get; set; } = 1.0m;
    
    /// <summary>
    /// Gets or sets the maximum number of concurrent trades.
    /// </summary>
    public int MaxConcurrentTrades { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the cooldown period in milliseconds between trades.
    /// </summary>
    public int TradeCooldownMs { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the daily loss limit as a percentage of total capital. Trading stops when this is reached.
    /// </summary>
    public decimal DailyLossLimitPercent { get; set; } = 5.0m;
    
    /// <summary>
    /// Gets or sets the trusted exchanges for which higher risk parameters can be applied.
    /// </summary>
    public ICollection<ExchangeId> TrustedExchanges { get; set; } = new List<ExchangeId>();
    
    /// <summary>
    /// Gets or sets the blacklisted exchanges on which no trades should be executed.
    /// </summary>
    public ICollection<ExchangeId> BlacklistedExchanges { get; set; } = new List<ExchangeId>();
    
    /// <summary>
    /// Gets or sets a value indicating whether to use price protection strategies.
    /// </summary>
    public bool UsePriceProtection { get; set; } = true;
    
    /// <summary>
    /// Creates a conservative risk profile with lower risk parameters.
    /// </summary>
    /// <returns>A conservative risk profile.</returns>
    public static RiskProfile CreateConservative()
    {
        return new RiskProfile
        {
            MaxCapitalPerTradePercent = 5.0m,
            MaxCapitalPerAssetPercent = 15.0m,
            MinimumProfitPercentage = 1.0m,
            MaxSlippagePercentage = 0.1m,
            MaxExecutionTimeMs = 2000,
            StopLossPercentage = 0.5m,
            MaxConcurrentTrades = 2,
            TradeCooldownMs = 2000,
            DailyLossLimitPercent = 3.0m,
            UsePriceProtection = true
        };
    }
    
    /// <summary>
    /// Creates an aggressive risk profile with higher risk parameters.
    /// </summary>
    /// <returns>An aggressive risk profile.</returns>
    public static RiskProfile CreateAggressive()
    {
        return new RiskProfile
        {
            MaxCapitalPerTradePercent = 20.0m,
            MaxCapitalPerAssetPercent = 50.0m,
            MinimumProfitPercentage = 0.3m,
            MaxSlippagePercentage = 0.3m,
            MaxExecutionTimeMs = 5000,
            StopLossPercentage = 2.0m,
            MaxConcurrentTrades = 5,
            TradeCooldownMs = 500,
            DailyLossLimitPercent = 10.0m,
            UsePriceProtection = false
        };
    }
    
    /// <summary>
    /// Creates a balanced risk profile with moderate risk parameters.
    /// </summary>
    /// <returns>A balanced risk profile.</returns>
    public static RiskProfile CreateBalanced()
    {
        return new RiskProfile(); // Returns default values which are balanced
    }
} 