using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// Implementation of the configuration service that manages application settings.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    
    // In-memory storage for configurations
    // In a real application, these would be persisted to a database or configuration files
    private readonly ConcurrentDictionary<string, object> _configurationStore = new();
    
    // Default configurations
    private ArbitrageConfiguration _defaultArbitrageConfig = new();
    private NotificationConfiguration _defaultNotificationConfig = new();
    private RiskProfile _defaultRiskProfile = RiskProfile.CreateBalanced();
    private readonly Dictionary<ExchangeId, ExchangeConfiguration> _defaultExchangeConfigs = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        // Initialize default configurations
        InitializeDefaultConfigurations();
        InitializeDefaultExchangeConfigurations();
    }
    
    /// <summary>
    /// Loads all configuration from the source and initializes the service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded arbitrage configuration.</returns>
    public async Task<ArbitrageConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading configuration");
        
        try
        {
            // Get the main configuration
            var configuration = await GetConfigurationAsync(cancellationToken);
            
            if (configuration == null)
            {
                _logger.LogWarning("Configuration not found, using default configuration");
                configuration = _defaultArbitrageConfig;
            }
            
            // Initialize other configurations
            await GetRiskProfileAsync(cancellationToken);
            await GetAllExchangeConfigurationsAsync(cancellationToken);
            await GetNotificationConfigurationAsync(cancellationToken);
            
            _logger.LogInformation("Configuration loaded successfully");
            
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            return _defaultArbitrageConfig;
        }
    }
    
    /// <inheritdoc />
    public Task<ArbitrageConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting configuration");
        
        if (_configurationStore.TryGetValue("ArbitrageConfiguration", out var configObj) && 
            configObj is ArbitrageConfiguration config)
        {
            return Task.FromResult<ArbitrageConfiguration?>(config);
        }
        
        // Return default configuration if not found
        _logger.LogInformation("Configuration not found, using default configuration");
        return Task.FromResult<ArbitrageConfiguration?>(_defaultArbitrageConfig);
    }
    
    /// <inheritdoc />
    public Task UpdateConfigurationAsync(ArbitrageConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating arbitrage configuration");
        
        _configurationStore["ArbitrageConfiguration"] = configuration;
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<ExchangeConfiguration?> GetExchangeConfigurationAsync(string exchangeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(exchangeId))
        {
            return Task.FromResult<ExchangeConfiguration?>(null);
        }

        var key = $"exchange_config_{exchangeId}";
        
        if (_configurationStore.TryGetValue(key, out var config) && config is ExchangeConfiguration exchangeConfig)
        {
            return Task.FromResult<ExchangeConfiguration?>(exchangeConfig);
        }

        // Return the default configuration for the exchange
        if (_defaultExchangeConfigs.TryGetValue(new ExchangeId(exchangeId), out var defaultConfig))
        {
            _configurationStore[key] = defaultConfig;
            return Task.FromResult<ExchangeConfiguration?>(defaultConfig);
        }
        
        return Task.FromResult<ExchangeConfiguration?>(null);
    }
    
    /// <inheritdoc />
    public Task<ExchangeConfiguration?> GetExchangeConfigurationAsync(ExchangeId exchangeId, CancellationToken cancellationToken = default)
    {
        return GetExchangeConfigurationAsync(exchangeId.Value, cancellationToken);
    }
    
    /// <inheritdoc />
    public Task<IReadOnlyCollection<ExchangeConfiguration>> GetAllExchangeConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all exchange configurations");
        
        var configs = new List<ExchangeConfiguration>();
        
        // Get all exchange configurations from the store
        foreach (var key in _configurationStore.Keys.Where(k => k.StartsWith("exchange_config_")))
        {
            if (_configurationStore.TryGetValue(key, out var configObj) && 
                configObj is ExchangeConfiguration config)
            {
                configs.Add(config);
            }
        }
        
        // Add default configurations for exchanges that are not in the store
        foreach (var (exchangeId, defaultConfig) in _defaultExchangeConfigs)
        {
            var key = $"exchange_config_{exchangeId.Value}";
            if (!_configurationStore.ContainsKey(key))
            {
                configs.Add(defaultConfig);
                _configurationStore[key] = defaultConfig;
            }
        }
        
        return Task.FromResult<IReadOnlyCollection<ExchangeConfiguration>>(configs);
    }
    
    /// <inheritdoc />
    public Task UpdateExchangeConfigurationAsync(ExchangeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating exchange configuration for {ExchangeId}", configuration.ExchangeId);
        
        var key = $"exchange_config_{configuration.ExchangeId}";
        _configurationStore[key] = configuration;
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<NotificationConfiguration> GetNotificationConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting notification configuration");
        
        if (_configurationStore.TryGetValue("NotificationConfiguration", out var configObj) && 
            configObj is NotificationConfiguration config)
        {
            return Task.FromResult(config);
        }
        
        _logger.LogInformation("Notification configuration not found, using default notification configuration");
        return Task.FromResult(_defaultNotificationConfig);
    }
    
    /// <inheritdoc />
    public Task UpdateNotificationConfigurationAsync(NotificationConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating notification configuration");
        
        _configurationStore["NotificationConfiguration"] = configuration;
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<RiskProfile> GetRiskProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting risk profile");
        
        if (_configurationStore.TryGetValue("RiskProfile", out var configObj) && 
            configObj is RiskProfile profile)
        {
            return Task.FromResult(profile);
        }
        
        _logger.LogInformation("Risk profile not found, using default risk profile");
        return Task.FromResult(_defaultRiskProfile);
    }
    
    /// <inheritdoc />
    public Task UpdateRiskProfileAsync(RiskProfile riskProfile, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating risk profile");
        
        _configurationStore["RiskProfile"] = riskProfile;
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Initializes the default configurations.
    /// </summary>
    private void InitializeDefaultConfigurations()
    {
        // Initialize default arbitrage configuration
        _defaultArbitrageConfig = new ArbitrageConfiguration
        {
            IsEnabled = true,
            AutoTradeEnabled = false, // Disable auto-trading by default for safety
            TradingPairs = new List<TradingPair>
            {
                TradingPair.BTCUSDT,
                TradingPair.ETHUSDT
            },
            RiskProfile = RiskProfile.CreateConservative(), // Use conservative risk profile by default
            MaxConcurrentArbitrageOperations = 3,
            MinimumProfitPercentage = 0.5m,
            MaxExecutionTimeMs = 3000,
            PollingIntervalMs = 100
        };
        
        // Initialize default notification configuration
        _defaultNotificationConfig = new NotificationConfiguration
        {
            EmailEnabled = false,
            SmsEnabled = false,
            WebhookEnabled = false,
            MinimumErrorSeverityForNotification = ErrorSeverity.Medium,
            NotifyOnArbitrageOpportunities = true,
            NotifyOnCompletedTrades = true,
            NotifyOnFailedTrades = true,
            SendDailyStatistics = true,
            Email = new EmailConfiguration
            {
                SmtpServer = "smtp.example.com",
                SmtpPort = 587,
                UseSsl = true,
                Username = "notifications@example.com",
                Password = "placeholder_password", // This should be securely stored in a real application
                FromAddress = "notifications@example.com",
                ToAddresses = new List<string> { "admin@example.com" }
            },
            Sms = new SmsConfiguration
            {
                Provider = "Twilio",
                AccountId = "placeholder_account_id",
                AuthToken = "placeholder_auth_token", // This should be securely stored in a real application
                FromNumber = "+1234567890",
                ToNumbers = new List<string> { "+1234567890" }
            },
            Webhook = new WebhookConfiguration
            {
                Url = "https://example.com/webhook",
                AuthToken = "placeholder_auth_token", // This should be securely stored in a real application
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "X-API-Key", "placeholder_api_key" } // This should be securely stored in a real application
                }
            }
        };
        
        // Initialize default risk profile
        _defaultRiskProfile = RiskProfile.CreateBalanced();
        
        // Initialize default exchange configurations
        InitializeDefaultExchangeConfigurations();
    }
    
    /// <summary>
    /// Initializes the default exchange configurations.
    /// </summary>
    private void InitializeDefaultExchangeConfigurations()
    {
        
        // Coinbase configuration
        var coinbaseConfig = new ExchangeConfiguration
        {
            ExchangeId = ExchangeId.Coinbase.Value,
            IsEnabled = true,
            ApiUrl = "https://api.exchange.coinbase.com",
            WebSocketUrl = "wss://ws-feed.exchange.coinbase.com",
            ApiKey = "placeholder_api_key", // This should be securely stored in a real application
            ApiSecret = "placeholder_api_secret", // This should be securely stored in a real application
            SupportedTradingPairs = new List<TradingPair>
            {
                TradingPair.BTCUSD,
                TradingPair.ETHUSD
            },
            ApiTimeoutMs = 5000,
            WebSocketReconnectIntervalMs = 1000,
            RateLimits = new ExchangeRateLimits
            {
                RequestsPerMinute = 300, // Coinbase is more conservative with rate limits
                OrdersPerMinute = 30,
                MarketDataRequestsPerMinute = 150
            }
        };
        _defaultExchangeConfigs[ExchangeId.Coinbase] = coinbaseConfig;
        
        // Kraken configuration
        var krakenConfig = new ExchangeConfiguration
        {
            ExchangeId = ExchangeId.Kraken.Value,
            IsEnabled = true,
            ApiUrl = "https://api.kraken.com",
            WebSocketUrl = "wss://ws.kraken.com",
            ApiKey = "placeholder_api_key", // This should be securely stored in a real application
            ApiSecret = "placeholder_api_secret", // This should be securely stored in a real application
            SupportedTradingPairs = new List<TradingPair>
            {
                TradingPair.BTCUSD,
                TradingPair.ETHUSD,
                TradingPair.XRPUSDT
            },
            ApiTimeoutMs = 5000,
            WebSocketReconnectIntervalMs = 1000,
            RateLimits = new ExchangeRateLimits
            {
                RequestsPerMinute = 60,
                OrdersPerMinute = 15,
                MarketDataRequestsPerMinute = 30
            }
        };
        _defaultExchangeConfigs[ExchangeId.Kraken] = krakenConfig;
    }

    /// <summary>
    /// Gets all supported exchanges.
    /// </summary>
    /// <returns>A collection of supported exchange identifiers.</returns>
    public IReadOnlyCollection<string> GetSupportedExchanges()
    {
        return _defaultExchangeConfigs.Keys.Select(k => k.Value).ToList();
    }
} 