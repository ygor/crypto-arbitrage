namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents a risk profile for arbitrage trading.
/// </summary>
public class RiskProfile
{
    public string Name { get; set; } = "Default";
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the risk type (Conservative, Balanced, Aggressive).
    /// </summary>
    public string Type { get; set; } = "Balanced";
    
    // Profit Settings
    public decimal MinProfitPercentage { get; set; } = 0.5m;
    public decimal MinProfitAmount { get; set; } = 10;
    public decimal MinimumProfitPercentage { get; set; } = 0.5m;
    
    // Risk Settings
    public decimal MaxSlippagePercentage { get; set; } = 0.5m;
    public decimal RiskTolerance { get; set; } = 0.5m; // 0 = Low risk, 1 = High risk
    public int MaxRetryAttempts { get; set; } = 3;
    public decimal MaxSpreadVolatility { get; set; } = 5.0m;
    public decimal StopLossPercentage { get; set; } = 1.0m;
    public decimal DailyLossLimitPercent { get; set; } = 5.0m;
    public bool UsePriceProtection { get; set; } = true;
    
    // Capital allocation
    public decimal MaxTradeAmount { get; set; } = 1000;
    public decimal MaxAssetExposurePercentage { get; set; } = 10.0m;
    public decimal MaxTotalExposurePercentage { get; set; } = 50.0m;
    public decimal DynamicSizingFactor { get; set; } = 0.5m; // 0 = Static sizing, 1 = Fully dynamic
    public decimal MaxCapitalPerTradePercent { get; set; } = 10.0m;
    public decimal MaxCapitalPerAssetPercent { get; set; } = 25.0m;
    
    // Execution settings
    public decimal ExecutionAggressiveness { get; set; } = 0.5m; // 0 = Passive, 1 = Aggressive
    public decimal MaxExecutionTimeMs { get; set; } = 5000; // Max allowed time for execution in ms
    public decimal OrderBookDepthFactor { get; set; } = 0.5m; // How deep in the order book to look
    public int CooldownPeriodMs { get; set; } = 5000; // Cooldown after failed trade
    public int MaxConcurrentTrades { get; set; } = 3;
    public int TradeCooldownMs { get; set; } = 1000;
    
    // Adaptive settings
    public bool UseAdaptiveParameters { get; set; } = false;
    public decimal MarketVolatilityFactor { get; set; } = 0.5m; // How much to adjust for market volatility
    public decimal SuccessRateInfluence { get; set; } = 0.5m; // How much past success rate influences decisions
    
    // Exchange/market specific risks
    public decimal ExchangeCounterpartyRiskFactor { get; set; } = 0.5m; // Relative risk assigned to exchanges
    public decimal MarketLiquidityRiskFactor { get; set; } = 0.5m; // How important liquidity is in decision making
    
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