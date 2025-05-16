using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArbitrageBot.Infrastructure.Services;

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
        var isEnabled = _configuration.GetValue<bool>("ArbitrageBot:IsEnabled", currentConfig.IsEnabled);
        var autoTradeEnabled = _configuration.GetValue<bool>("ArbitrageBot:AutoTradeEnabled", currentConfig.AutoTradeEnabled);
        var minProfitPercentage = _configuration.GetValue<decimal>("ArbitrageBot:MinimumProfitPercentage", currentConfig.MinimumProfitPercentage);
        var maxConcurrentOperations = _configuration.GetValue<int>("ArbitrageBot:MaxConcurrentOperations", currentConfig.MaxConcurrentArbitrageOperations);
        var pollingIntervalMs = _configuration.GetValue<int>("ArbitrageBot:PollingIntervalMs", currentConfig.PollingIntervalMs);
        
        // Parse trading pairs
        var tradingPairsConfig = _configuration.GetSection("ArbitrageBot:TradingPairs").Get<string[]>();
        var tradingPairs = new List<TradingPair>(currentConfig.TradingPairs);
        
        if (tradingPairsConfig != null && tradingPairsConfig.Length > 0)
        {
            tradingPairs.Clear();
            
            foreach (var pairString in tradingPairsConfig)
            {
                try
                {
                    var pair = TradingPair.Parse(pairString);
                    tradingPairs.Add(pair);
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
        var emailSection = _configuration.GetSection("ArbitrageBot:Notifications:Email");
        var emailEnabled = emailSection.GetValue<bool>("Enabled", currentConfig.EmailEnabled);
        var emailConfig = new EmailConfiguration
        {
            SmtpServer = emailSection.GetValue<string>("SmtpServer", currentConfig.Email.SmtpServer) ?? string.Empty,
            SmtpPort = emailSection.GetValue<int>("SmtpPort", currentConfig.Email.SmtpPort),
            UseSsl = emailSection.GetValue<bool>("UseSsl", currentConfig.Email.UseSsl),
            Username = emailSection.GetValue<string>("Username", currentConfig.Email.Username) ?? string.Empty,
            Password = emailSection.GetValue<string>("Password", currentConfig.Email.Password) ?? string.Empty,
            FromAddress = emailSection.GetValue<string>("FromAddress", currentConfig.Email.FromAddress) ?? string.Empty,
            ToAddresses = emailSection.GetSection("ToAddresses").Get<List<string>>() ?? currentConfig.Email.ToAddresses
        };
        
        // Get webhook notification config
        var webhookSection = _configuration.GetSection("ArbitrageBot:Notifications:Webhook");
        var webhookEnabled = webhookSection.GetValue<bool>("Enabled", currentConfig.WebhookEnabled);
        var webhookConfig = new WebhookConfiguration
        {
            Url = webhookSection.GetValue<string>("Url", currentConfig.Webhook.Url) ?? string.Empty,
            AuthToken = webhookSection.GetValue<string>("AuthToken", currentConfig.Webhook.AuthToken) ?? string.Empty,
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
        var profileSection = _configuration.GetSection("ArbitrageBot:RiskProfile");
        var profileType = profileSection.GetValue<string>("Type", "Balanced");
        
        // Start with the appropriate base profile
        RiskProfile baseProfile = (profileType?.ToLowerInvariant() ?? "balanced") switch
        {
            "conservative" => RiskProfile.CreateConservative(),
            "aggressive" => RiskProfile.CreateAggressive(),
            _ => RiskProfile.CreateBalanced()
        };
        
        // Override specific settings from configuration
        baseProfile.MaxCapitalPerTradePercent = profileSection.GetValue<decimal>("MaxCapitalPerTradePercent", baseProfile.MaxCapitalPerTradePercent);
        baseProfile.MaxCapitalPerAssetPercent = profileSection.GetValue<decimal>("MaxCapitalPerAssetPercent", baseProfile.MaxCapitalPerAssetPercent);
        baseProfile.MinimumProfitPercentage = profileSection.GetValue<decimal>("MinimumProfitPercentage", baseProfile.MinimumProfitPercentage);
        baseProfile.MaxSlippagePercentage = profileSection.GetValue<decimal>("MaxSlippagePercentage", baseProfile.MaxSlippagePercentage);
        baseProfile.StopLossPercentage = profileSection.GetValue<decimal>("StopLossPercentage", baseProfile.StopLossPercentage);
        baseProfile.DailyLossLimitPercent = profileSection.GetValue<decimal>("DailyLossLimitPercent", baseProfile.DailyLossLimitPercent);
        baseProfile.MaxConcurrentTrades = profileSection.GetValue<int>("MaxConcurrentTrades", baseProfile.MaxConcurrentTrades);
        baseProfile.UsePriceProtection = profileSection.GetValue<bool>("UsePriceProtection", baseProfile.UsePriceProtection);
        
        // Save the updated risk profile
        await _configurationService.UpdateRiskProfileAsync(baseProfile, cancellationToken);
        
        _logger.LogInformation("Risk profile loaded: Type={Type}, MaxCapitalPerTrade={MaxCapitalPerTrade}%, MinProfit={MinProfit}%", 
            profileType, baseProfile.MaxCapitalPerTradePercent, baseProfile.MinimumProfitPercentage);
    }
} 