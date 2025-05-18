using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// Service for loading configuration from appsettings.json and environment variables.
/// </summary>
public class ConfigurationLoader
{
    private readonly IConfigurationService _configurationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationLoader> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoader"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurationLoader(
        IConfigurationService configurationService,
        IConfiguration configuration,
        ILogger<ConfigurationLoader> logger)
    {
        _configurationService = configurationService;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// Loads the configuration from appsettings.json and environment variables.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading configuration from appsettings.json and environment variables");
        
        try
        {
            // Load arbitrage configuration
            await LoadArbitrageConfigurationAsync(cancellationToken);
            
            // Load exchange configurations
            await LoadExchangeConfigurationsAsync(cancellationToken);
            
            // Load notification configuration
            await LoadNotificationConfigurationAsync(cancellationToken);
            
            // Load risk profile
            await LoadRiskProfileAsync(cancellationToken);
            
            _logger.LogInformation("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            throw;
        }
    }
    
    /// <summary>
    /// Loads the arbitrage configuration.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadArbitrageConfigurationAsync(CancellationToken cancellationToken)
    {
        var currentConfig = await _configurationService.GetConfigurationAsync(cancellationToken);
        
        // Create default config if current is null
        currentConfig ??= new ArbitrageConfiguration();
        
        // Get values from configuration
        var isEnabledSection = _configuration.GetSection("CryptoArbitrage:IsEnabled");
        bool isEnabled = isEnabledSection.Value != null ? bool.Parse(isEnabledSection.Value) : currentConfig.IsEnabled;
        
        var autoTradeEnabledSection = _configuration.GetSection("CryptoArbitrage:AutoTradeEnabled");
        bool autoTradeEnabled = autoTradeEnabledSection.Value != null ? bool.Parse(autoTradeEnabledSection.Value) : currentConfig.AutoTradeEnabled;
        
        var minProfitPercentageSection = _configuration.GetSection("CryptoArbitrage:MinimumProfitPercentage");
        decimal minProfitPercentage = minProfitPercentageSection.Value != null ? decimal.Parse(minProfitPercentageSection.Value) : currentConfig.MinimumProfitPercentage;
        
        var maxConcurrentOperationsSection = _configuration.GetSection("CryptoArbitrage:MaxConcurrentOperations");
        int maxConcurrentOperations = maxConcurrentOperationsSection.Value != null ? int.Parse(maxConcurrentOperationsSection.Value) : currentConfig.MaxConcurrentArbitrageOperations;
        
        var pollingIntervalMsSection = _configuration.GetSection("CryptoArbitrage:PollingIntervalMs");
        int pollingIntervalMs = pollingIntervalMsSection.Value != null ? int.Parse(pollingIntervalMsSection.Value) : currentConfig.PollingIntervalMs;
        
        // Parse trading pairs
        var tradingPairsConfig = _configuration.GetSection("CryptoArbitrage:TradingPairs").GetChildren().Select(x => x.Value).Where(x => x != null).ToArray();
        var tradingPairs = new List<TradingPair>(currentConfig.TradingPairs);
        
        if (tradingPairsConfig != null && tradingPairsConfig.Length > 0)
        {
            tradingPairs.Clear();
            
            foreach (var pairString in tradingPairsConfig)
            {
                try
                {
                    if (pairString != null)
                    {
                        var pair = TradingPair.Parse(pairString);
                        tradingPairs.Add(pair);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid trading pair format: {TradingPair}", pairString);
                }
            }
        }
        
        // Create updated configuration
        var updatedConfig = new ArbitrageConfiguration
        {
            IsEnabled = isEnabled,
            AutoTradeEnabled = autoTradeEnabled,
            MinimumProfitPercentage = minProfitPercentage,
            MaxConcurrentArbitrageOperations = maxConcurrentOperations,
            PollingIntervalMs = pollingIntervalMs,
            TradingPairs = tradingPairs,
            RiskProfile = currentConfig.RiskProfile
        };
        
        // Save the updated configuration
        await _configurationService.UpdateConfigurationAsync(updatedConfig, cancellationToken);
        
        _logger.LogInformation("Arbitrage configuration loaded: Enabled={Enabled}, AutoTrade={AutoTrade}, MinProfit={MinProfit}%, TradingPairs={TradingPairCount}", 
            isEnabled, autoTradeEnabled, minProfitPercentage, tradingPairs.Count);
    }
    
    /// <summary>
    /// Loads the exchange configurations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadExchangeConfigurationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exchanges = _configuration.GetSection("Exchanges").GetChildren();
            
            foreach (var exchange in exchanges)
            {
                var exchangeId = exchange.Key;
                
                var config = new ExchangeConfiguration
                {
                    ExchangeId = exchangeId,
                    IsEnabled = bool.Parse(exchange["Enabled"] ?? "false"),
                    ApiKey = exchange["ApiKey"] ?? string.Empty,
                    ApiSecret = exchange["ApiSecret"] ?? string.Empty,
                    AdditionalApiParams = exchange["AdditionalApiParams"] ?? string.Empty,
                    BaseUrl = exchange["BaseUrl"] ?? string.Empty,
                    ApiUrl = exchange["ApiUrl"] ?? string.Empty,
                    WebSocketUrl = exchange["WebSocketUrl"] ?? string.Empty,
                    MaxRequestsPerSecond = int.Parse(exchange["MaxRequestsPerSecond"] ?? "10"),
                    ApiTimeoutMs = int.Parse(exchange["ApiTimeoutMs"] ?? "30000"),
                    WebSocketReconnectIntervalMs = int.Parse(exchange["WebSocketReconnectIntervalMs"] ?? "5000")
                };
                
                // Parse supported trading pairs if available
                if (exchange.GetSection("SupportedTradingPairs") is IConfigurationSection pairsSection && 
                    pairsSection.GetChildren().Any())
                {
                    config.SupportedTradingPairs = new List<TradingPair>();
                    foreach (var pair in pairsSection.GetChildren())
                    {
                        var pairString = pair.Value;
                        if (!string.IsNullOrEmpty(pairString))
                        {
                            try
                            {
                                var tradingPair = TradingPair.Parse(pairString);
                                config.SupportedTradingPairs.Add(tradingPair);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error parsing trading pair {PairString}", pairString);
                            }
                        }
                    }
                }
                
                // Parse rate limits if available
                if (exchange.GetSection("RateLimits") is IConfigurationSection rateLimitsSection)
                {
                    config.RateLimits = new ExchangeRateLimits
                    {
                        RequestsPerMinute = int.Parse(rateLimitsSection["RequestsPerMinute"] ?? "60"),
                        OrdersPerMinute = int.Parse(rateLimitsSection["OrdersPerMinute"] ?? "10"),
                        MarketDataRequestsPerMinute = int.Parse(rateLimitsSection["MarketDataRequestsPerMinute"] ?? "30")
                    };
                }
                
                // Parse additional auth params if available
                if (exchange.GetSection("AdditionalAuthParams") is IConfigurationSection authParamsSection && 
                    authParamsSection.GetChildren().Any())
                {
                    config.AdditionalAuthParams = new Dictionary<string, string>();
                    foreach (var param in authParamsSection.GetChildren())
                    {
                        config.AdditionalAuthParams[param.Key] = param.Value ?? string.Empty;
                    }
                }
                
                await _configurationService.UpdateExchangeConfigurationAsync(config, cancellationToken);
                _logger.LogInformation("Loaded configuration for exchange {ExchangeId}", exchangeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading exchange configurations");
        }
    }
    
    /// <summary>
    /// Loads the notification configuration.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadNotificationConfigurationAsync(CancellationToken cancellationToken)
    {
        var currentConfig = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
        
        // Get email notification config
        var emailSection = _configuration.GetSection("CryptoArbitrage:Notifications:Email");
        var emailEnabledValue = emailSection.GetSection("Enabled").Value;
        var emailEnabled = emailEnabledValue != null ? bool.Parse(emailEnabledValue) : currentConfig.EmailEnabled;
        
        var emailConfig = new EmailConfiguration
        {
            SmtpServer = emailSection.GetSection("SmtpServer").Value ?? currentConfig.Email.SmtpServer ?? string.Empty,
            SmtpPort = int.TryParse(emailSection.GetSection("SmtpPort").Value, out var port) ? port : currentConfig.Email.SmtpPort,
            UseSsl = bool.TryParse(emailSection.GetSection("UseSsl").Value, out var useSsl) ? useSsl : currentConfig.Email.UseSsl,
            Username = emailSection.GetSection("Username").Value ?? currentConfig.Email.Username ?? string.Empty,
            Password = emailSection.GetSection("Password").Value ?? currentConfig.Email.Password ?? string.Empty,
            FromAddress = emailSection.GetSection("FromAddress").Value ?? currentConfig.Email.FromAddress ?? string.Empty,
            ToAddresses = emailSection.GetSection("ToAddresses").GetChildren().Select(x => x.Value).Where(x => x != null).ToList() ?? currentConfig.Email.ToAddresses
        };
        
        // Get webhook notification config
        var webhookSection = _configuration.GetSection("CryptoArbitrage:Notifications:Webhook");
        var webhookEnabledValue = webhookSection.GetSection("Enabled").Value;
        var webhookEnabled = webhookEnabledValue != null ? bool.Parse(webhookEnabledValue) : currentConfig.WebhookEnabled;
        
        var webhookConfig = new WebhookConfiguration
        {
            Url = webhookSection.GetSection("Url").Value ?? currentConfig.Webhook.Url ?? string.Empty,
            AuthToken = webhookSection.GetSection("AuthToken").Value ?? currentConfig.Webhook.AuthToken ?? string.Empty,
            Headers = currentConfig.Webhook.Headers ?? new Dictionary<string, string>()
        };
        
        // Create updated configuration
        var updatedConfig = new NotificationConfiguration
        {
            EmailEnabled = emailEnabled,
            Email = emailConfig,
            WebhookEnabled = webhookEnabled,
            Webhook = webhookConfig,
            SmsEnabled = currentConfig.SmsEnabled,
            Sms = currentConfig.Sms,
            MinimumErrorSeverityForNotification = currentConfig.MinimumErrorSeverityForNotification,
            NotifyOnArbitrageOpportunities = currentConfig.NotifyOnArbitrageOpportunities,
            NotifyOnCompletedTrades = currentConfig.NotifyOnCompletedTrades,
            NotifyOnFailedTrades = currentConfig.NotifyOnFailedTrades,
            SendDailyStatistics = currentConfig.SendDailyStatistics
        };
        
        // Save the updated configuration
        await _configurationService.UpdateNotificationConfigurationAsync(updatedConfig, cancellationToken);
        
        _logger.LogInformation("Notification configuration loaded: Email={EmailEnabled}, Webhook={WebhookEnabled}", 
            emailEnabled, webhookEnabled);
    }
    
    /// <summary>
    /// Loads the risk profile.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadRiskProfileAsync(CancellationToken cancellationToken)
    {
        var currentProfile = await _configurationService.GetRiskProfileAsync(cancellationToken);
        
        // Get the risk profile type
        var profileSection = _configuration.GetSection("CryptoArbitrage:RiskProfile");
        var profileType = profileSection.GetSection("Type").Value ?? "Balanced";
        
        // Start with the appropriate base profile
        RiskProfile baseProfile = (profileType.ToLowerInvariant()) switch
        {
            "conservative" => RiskProfile.CreateConservative(),
            "aggressive" => RiskProfile.CreateAggressive(),
            _ => RiskProfile.CreateBalanced()
        };
        
        // Override specific settings from configuration
        var maxCapitalPerTradePercentValue = profileSection.GetSection("MaxCapitalPerTradePercent").Value;
        if (decimal.TryParse(maxCapitalPerTradePercentValue, out var maxCapitalPerTradePercent))
        {
            baseProfile.MaxCapitalPerTradePercent = maxCapitalPerTradePercent;
        }
        
        var maxCapitalPerAssetPercentValue = profileSection.GetSection("MaxCapitalPerAssetPercent").Value;
        if (decimal.TryParse(maxCapitalPerAssetPercentValue, out var maxCapitalPerAssetPercent))
        {
            baseProfile.MaxCapitalPerAssetPercent = maxCapitalPerAssetPercent;
        }
        
        var minimumProfitPercentageValue = profileSection.GetSection("MinimumProfitPercentage").Value;
        if (decimal.TryParse(minimumProfitPercentageValue, out var minimumProfitPercentage))
        {
            baseProfile.MinimumProfitPercentage = minimumProfitPercentage;
        }
        
        var maxSlippagePercentageValue = profileSection.GetSection("MaxSlippagePercentage").Value;
        if (decimal.TryParse(maxSlippagePercentageValue, out var maxSlippagePercentage))
        {
            baseProfile.MaxSlippagePercentage = maxSlippagePercentage;
        }
        
        var stopLossPercentageValue = profileSection.GetSection("StopLossPercentage").Value;
        if (decimal.TryParse(stopLossPercentageValue, out var stopLossPercentage))
        {
            baseProfile.StopLossPercentage = stopLossPercentage;
        }
        
        var dailyLossLimitPercentValue = profileSection.GetSection("DailyLossLimitPercent").Value;
        if (decimal.TryParse(dailyLossLimitPercentValue, out var dailyLossLimitPercent))
        {
            baseProfile.DailyLossLimitPercent = dailyLossLimitPercent;
        }
        
        var maxConcurrentTradesValue = profileSection.GetSection("MaxConcurrentTrades").Value;
        if (int.TryParse(maxConcurrentTradesValue, out var maxConcurrentTrades))
        {
            baseProfile.MaxConcurrentTrades = maxConcurrentTrades;
        }
        
        var usePriceProtectionValue = profileSection.GetSection("UsePriceProtection").Value;
        if (bool.TryParse(usePriceProtectionValue, out var usePriceProtection))
        {
            baseProfile.UsePriceProtection = usePriceProtection;
        }
        
        // Save the updated risk profile
        await _configurationService.UpdateRiskProfileAsync(baseProfile, cancellationToken);
        
        _logger.LogInformation("Risk profile loaded: Type={Type}, MaxCapitalPerTrade={MaxCapitalPerTrade}%, MinProfit={MinProfit}%", 
            profileType, baseProfile.MaxCapitalPerTradePercent, baseProfile.MinimumProfitPercentage);
    }
} 