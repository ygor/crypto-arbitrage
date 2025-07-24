using System;
using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Configuration settings for a specific exchange
/// </summary>
public class ExchangeConfiguration
{
    /// <summary>
    /// Unique identifier for the exchange configuration
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Exchange identifier (e.g., "coinbase", "kraken", "binance")
    /// </summary>
    public string ExchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this exchange is enabled for trading
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// API key for the exchange (encrypted)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API secret for the exchange (encrypted)
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to use the sandbox/testnet environment
    /// </summary>
    public bool UseSandbox { get; set; }
    
    /// <summary>
    /// Maximum trade amount for this exchange
    /// </summary>
    public decimal MaxTradeAmount { get; set; }
    
    /// <summary>
    /// Trading fee percentage for this exchange
    /// </summary>
    public decimal TradingFeePercent { get; set; }
    
    // Additional properties required by GetConfigurationHandler
    public int MaxRequestsPerSecond { get; set; } = 10;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string WebSocketUrl { get; set; } = string.Empty;
    public List<TradingPair> SupportedTradingPairs { get; set; } = new List<TradingPair>();
    public ExchangeRateLimits RateLimits { get; set; } = new ExchangeRateLimits();
    public int ApiTimeoutMs { get; set; } = 5000;
    public int WebSocketReconnectIntervalMs { get; set; } = 5000;
    public Dictionary<string, string> AdditionalApiParams { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> AdditionalAuthParams { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
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