using MediatR;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Events;

/// <summary>
/// Handler for RiskThresholdExceededEvent.
/// </summary>
public class RiskThresholdExceededEventHandler : INotificationHandler<RiskThresholdExceededEvent>
{
    private readonly ILogger<RiskThresholdExceededEventHandler> _logger;

    public RiskThresholdExceededEventHandler(ILogger<RiskThresholdExceededEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(RiskThresholdExceededEvent notification, CancellationToken cancellationToken)
    {
        var logLevel = notification.Severity switch
        {
            RiskSeverity.Critical => LogLevel.Critical,
            RiskSeverity.High => LogLevel.Error,
            RiskSeverity.Medium => LogLevel.Warning,
            RiskSeverity.Low => LogLevel.Information,
            _ => LogLevel.Warning
        };

        _logger.Log(logLevel,
            "RISK ALERT: {RiskType} threshold exceeded - Current: {CurrentValue}, Threshold: {ThresholdValue}, Severity: {Severity}",
            notification.RiskType,
            notification.CurrentValue,
            notification.ThresholdValue,
            notification.Severity);

        _logger.LogWarning("Risk Details: {RiskMessage}", notification.RiskMessage);

        try
        {
            // Log affected assets
            if (notification.AffectedAssets.Any())
            {
                _logger.LogInformation("Affected assets: {AffectedAssets}", string.Join(", ", notification.AffectedAssets));
            }

            // Log recommended actions
            if (notification.RecommendedActions.Any())
            {
                _logger.LogInformation("Recommended actions:");
                foreach (var action in notification.RecommendedActions)
                {
                    _logger.LogInformation("  - {RecommendedAction}", action);
                }
            }

            // Handle critical risks that require immediate action
            if (notification.RequiresImmediateAction)
            {
                _logger.LogCritical(
                    "IMMEDIATE ACTION REQUIRED: {RiskType} risk detected with severity {Severity}",
                    notification.RiskType,
                    notification.Severity);
            }

            await Task.CompletedTask;

            _logger.LogDebug("Successfully processed RiskThresholdExceededEvent for {RiskType}", notification.RiskType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process RiskThresholdExceededEvent for {RiskType}",
                notification.RiskType);
        }
    }
} 