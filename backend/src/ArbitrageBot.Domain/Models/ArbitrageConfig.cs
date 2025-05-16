namespace ArbitrageBot.Domain.Models;

public class ArbitrageConfig
{
    public bool IsEnabled { get; set; } = true;
    public List<string> EnabledTradingPairs { get; set; } = new();
    public List<string> EnabledBaseCurrencies { get; set; } = new();
    public List<string> EnabledQuoteCurrencies { get; set; } = new();
    public List<string> EnabledExchanges { get; set; } = new();
    public List<ExchangePair> EnabledExchangePairs { get; set; } = new();
    public int ScanIntervalMs { get; set; } = 1000;
    public int MaxConcurrentScans { get; set; } = 5;
    public bool AutoTradeEnabled { get; set; } = false;
    public int MaxDailyTrades { get; set; } = 100;
    public decimal MaxDailyVolume { get; set; } = 10000;
    public decimal MinOrderBookDepth { get; set; } = 1.0m;
    public bool UseWebsockets { get; set; } = true;
    public bool UsePolling { get; set; } = true;
    public int PollingIntervalMs { get; set; } = 1000;
    public OpportunityEvaluationStrategy EvaluationStrategy { get; set; } = OpportunityEvaluationStrategy.ProfitPercentage;
    public ExecutionStrategy ExecutionStrategy { get; set; } = ExecutionStrategy.Sequential;
}

public class ExchangePair
{
    public string BuyExchangeId { get; set; } = string.Empty;
    public string SellExchangeId { get; set; } = string.Empty;
}

public enum OpportunityEvaluationStrategy
{
    ProfitPercentage,
    ProfitAmount,
    RiskAdjustedReturn,
    VolumeWeighted
}

public enum ExecutionStrategy
{
    Sequential,
    Parallel,
    TWAP,
    SmartRouting
} 