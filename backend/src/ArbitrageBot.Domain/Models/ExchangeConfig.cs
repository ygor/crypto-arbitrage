namespace ArbitrageBot.Domain.Models;

public class ExchangeConfig
{
    public string ExchangeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string? AdditionalApiKey { get; set; }
    public int MaxRequestsPerSecond { get; set; } = 10;
    public int MaxRequestsPerMinute { get; set; } = 600;
    public decimal TakerFeePercentage { get; set; } = 0.1m;
    public decimal MakerFeePercentage { get; set; } = 0.1m;
    public List<string> SupportedTradingPairs { get; set; } = new();
    public List<string> SupportedBaseCurrencies { get; set; } = new();
    public List<string> SupportedQuoteCurrencies { get; set; } = new();
    public Dictionary<string, decimal> MinOrderSizes { get; set; } = new();
    public bool SupportsMarketOrders { get; set; } = true;
    public bool SupportsLimitOrders { get; set; } = true;
    public bool HasWebsocketSupport { get; set; } = true;
    public string? WebsocketUrl { get; set; }
    public string? RestApiUrl { get; set; }
    public int ConnectionTimeoutMs { get; set; } = 5000;
    public int ReadTimeoutMs { get; set; } = 30000;
    public int ConnectionRetryCount { get; set; } = 3;
    public bool IsTestMode { get; set; } = false;
} 