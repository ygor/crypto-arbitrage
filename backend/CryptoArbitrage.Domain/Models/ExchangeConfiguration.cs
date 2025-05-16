namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the configuration for a cryptocurrency exchange.
/// </summary>
public class ExchangeConfiguration
{
    /// <summary>
    /// Gets or sets the exchange identifier.
    /// </summary>
    public string ExchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the exchange is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the API key for the exchange.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the API secret for the exchange.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the additional API parameters for the exchange.
    /// </summary>
    public string? AdditionalApiParams { get; set; }
    
    /// <summary>
    /// Gets or sets the additional authentication parameters for the exchange.
    /// </summary>
    public Dictionary<string, string>? AdditionalAuthParams { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of requests per second allowed.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets the base URL for the API.
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the API URL.
    /// </summary>
    public string? ApiUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the WebSocket URL.
    /// </summary>
    public string? WebSocketUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the supported trading pairs.
    /// </summary>
    public List<TradingPair>? SupportedTradingPairs { get; set; }
    
    /// <summary>
    /// Gets or sets the rate limits.
    /// </summary>
    public ExchangeRateLimits? RateLimits { get; set; }
    
    /// <summary>
    /// Gets or sets the API timeout in milliseconds.
    /// </summary>
    public int ApiTimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// Gets or sets the WebSocket reconnect interval in milliseconds.
    /// </summary>
    public int WebSocketReconnectIntervalMs { get; set; } = 5000;
}

/// <summary>
/// Represents the rate limits for a cryptocurrency exchange.
/// </summary>
public class ExchangeRateLimits
{
    /// <summary>
    /// Gets or sets the maximum number of requests per minute.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;
    
    /// <summary>
    /// Gets or sets the maximum number of orders per minute.
    /// </summary>
    public int OrdersPerMinute { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets the maximum number of market data requests per minute.
    /// </summary>
    public int MarketDataRequestsPerMinute { get; set; } = 30;
} 