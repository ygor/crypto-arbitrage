using MediatR;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CryptoArbitrage.Application.Features.Configuration.Queries.GetConfiguration;

/// <summary>
/// Handler for retrieving application configuration.
/// </summary>
public class GetConfigurationHandler : IRequestHandler<GetConfigurationQuery, GetConfigurationResult>
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<GetConfigurationHandler> _logger;

    public GetConfigurationHandler(
        IConfigurationService configurationService,
        ILogger<GetConfigurationHandler> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetConfigurationResult> Handle(GetConfigurationQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();

        _logger.LogInformation(
            "Retrieving configuration - IncludeRisk: {IncludeRisk}, IncludeExchanges: {IncludeExchanges}, ForceRefresh: {ForceRefresh}",
            request.IncludeRiskProfile, request.IncludeExchangeConfigs, request.ForceRefresh);

        try
        {
            // Get main arbitrage configuration
            var arbitrageConfig = await _configurationService.GetConfigurationAsync(cancellationToken);
            if (arbitrageConfig == null)
            {
                warnings.Add("Arbitrage configuration not found, using defaults");
            }

            // Get risk profile if requested
            Domain.Models.RiskProfile? riskProfile = null;
            if (request.IncludeRiskProfile)
            {
                riskProfile = await _configurationService.GetRiskProfileAsync(cancellationToken);
                if (riskProfile == null)
                {
                    warnings.Add("Risk profile not found, using defaults");
                }
            }

            // Get exchange configurations if requested
            var exchangeConfigs = Array.Empty<Domain.Models.ExchangeConfiguration>();
            if (request.IncludeExchangeConfigs)
            {
                var exchangeConfigList = await _configurationService.GetAllExchangeConfigurationsAsync(cancellationToken);
                exchangeConfigs = exchangeConfigList.ToArray();

                // Filter sensitive data if not requested
                if (!request.IncludeSensitiveData)
                {
                    // Remove sensitive fields from exchange configs
                    exchangeConfigs = exchangeConfigs.Select(config => new Domain.Models.ExchangeConfiguration
                    {
                        ExchangeId = config.ExchangeId,
                        IsEnabled = config.IsEnabled,
                        MaxRequestsPerSecond = config.MaxRequestsPerSecond,
                        BaseUrl = config.BaseUrl,
                        ApiUrl = config.ApiUrl,
                        WebSocketUrl = config.WebSocketUrl,
                        SupportedTradingPairs = config.SupportedTradingPairs,
                        RateLimits = config.RateLimits,
                        ApiTimeoutMs = config.ApiTimeoutMs,
                        WebSocketReconnectIntervalMs = config.WebSocketReconnectIntervalMs,
                        AdditionalApiParams = config.AdditionalApiParams,
                        AdditionalAuthParams = config.AdditionalAuthParams,
                        // Remove sensitive fields
                        ApiKey = "***HIDDEN***",
                        ApiSecret = "***HIDDEN***"
                    }).ToArray();
                }
            }

            // Get notification configuration if requested
            Domain.Models.NotificationConfiguration? notificationConfig = null;
            if (request.IncludeNotificationConfig)
            {
                notificationConfig = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
                if (notificationConfig != null && !request.IncludeSensitiveData)
                {
                    // Filter sensitive notification data
                    var filtered = new Domain.Models.NotificationConfiguration
                    {
                        EmailEnabled = notificationConfig.EmailEnabled,
                        SmsEnabled = notificationConfig.SmsEnabled,
                        WebhookEnabled = notificationConfig.WebhookEnabled,
                        MinimumErrorSeverityForNotification = notificationConfig.MinimumErrorSeverityForNotification,
                        NotifyOnArbitrageOpportunities = notificationConfig.NotifyOnArbitrageOpportunities,
                        NotifyOnCompletedTrades = notificationConfig.NotifyOnCompletedTrades,
                        NotifyOnFailedTrades = notificationConfig.NotifyOnFailedTrades,
                        SendDailyStatistics = notificationConfig.SendDailyStatistics
                    };

                    if (notificationConfig.Email != null)
                    {
                        filtered.Email = new Domain.Models.EmailConfiguration
                        {
                            SmtpServer = notificationConfig.Email.SmtpServer,
                            SmtpPort = notificationConfig.Email.SmtpPort,
                            UseSsl = notificationConfig.Email.UseSsl,
                            Username = notificationConfig.Email.Username,
                            Password = "***HIDDEN***",
                            FromAddress = notificationConfig.Email.FromAddress,
                            ToAddresses = notificationConfig.Email.ToAddresses
                        };
                    }

                    if (notificationConfig.Webhook != null)
                    {
                        filtered.Webhook = new Domain.Models.WebhookConfiguration
                        {
                            Url = notificationConfig.Webhook.Url,
                            AuthToken = "***HIDDEN***",
                            Headers = notificationConfig.Webhook.Headers?.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Key.ToLower().Contains("token") || kvp.Key.ToLower().Contains("key") 
                                    ? "***HIDDEN***" 
                                    : kvp.Value)
                        };
                    }

                    notificationConfig = filtered;
                }
            }

            // Get system status if requested
            SystemStatus? systemStatus = null;
            if (request.IncludeSystemStatus)
            {
                systemStatus = await GetSystemStatusAsync(cancellationToken);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Configuration retrieved successfully in {ElapsedMs}ms - Warnings: {WarningCount}",
                stopwatch.ElapsedMilliseconds, warnings.Count);

            return GetConfigurationResult.Success(
                arbitrageConfig,
                stopwatch.ElapsedMilliseconds,
                riskProfile,
                exchangeConfigs,
                notificationConfig,
                systemStatus,
                warnings);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error retrieving configuration");
            return GetConfigurationResult.Failure(
                $"Failed to retrieve configuration: {ex.Message}",
                stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<SystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, these would come from various system components
            return new SystemStatus
            {
                IsRunning = true, // Would check actual arbitrage service status
                Mode = "Paper Trading", // Would get from configuration
                ActiveOperations = 0, // Would get from arbitrage service
                ConnectedExchanges = 2, // Would get from exchange connections
                Uptime = TimeSpan.FromHours(1), // Would calculate actual uptime
                LastConfigUpdate = DateTime.UtcNow.AddMinutes(-30), // Would track actual updates
                HealthStatus = "Healthy", // Would perform health checks
                Issues = Array.Empty<string>() // Would collect current issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get system status");
            return new SystemStatus
            {
                IsRunning = false,
                Mode = "Unknown",
                HealthStatus = "Unknown",
                Issues = new[] { "Failed to retrieve system status" }
            };
        }
    }
} 