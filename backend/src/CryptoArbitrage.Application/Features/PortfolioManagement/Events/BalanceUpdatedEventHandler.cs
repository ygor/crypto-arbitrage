using MediatR;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Events;

/// <summary>
/// Handler for BalanceUpdatedEvent.
/// </summary>
public class BalanceUpdatedEventHandler : INotificationHandler<BalanceUpdatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BalanceUpdatedEventHandler> _logger;

    public BalanceUpdatedEventHandler(
        IMediator mediator,
        ILogger<BalanceUpdatedEventHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(BalanceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing BalanceUpdatedEvent for exchange {ExchangeId}: {BalanceCount} balances updated, {ChangeCount} significant changes",
            notification.ExchangeId,
            notification.UpdatedBalances.Count,
            notification.SignificantChanges.Count);

        try
        {
            // Log significant balance changes
            foreach (var change in notification.SignificantChanges)
            {
                _logger.LogInformation(
                    "Significant balance change in {Currency} on {ExchangeId}: {PreviousAmount} → {NewAmount} ({ChangePercentage}%)",
                    change.Currency,
                    notification.ExchangeId,
                    change.PreviousAmount,
                    change.NewAmount,
                    change.ChangePercentage);
            }

            // TODO: Here you could add additional logic such as:
            // - Sending notifications for large balance changes
            // - Updating cached portfolio metrics
            // - Triggering risk assessment if changes are significant
            // - Logging to external monitoring systems

            // Example: Check for risk threshold violations
            await CheckForRiskThresholds(notification, cancellationToken);

            _logger.LogDebug("Successfully processed BalanceUpdatedEvent for {ExchangeId}", notification.ExchangeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process BalanceUpdatedEvent for exchange {ExchangeId}",
                notification.ExchangeId);
            
            // Don't rethrow - this is an event handler and should not fail the main flow
        }
    }

    private async Task CheckForRiskThresholds(BalanceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Example risk check: if any single currency exceeds 50% of total balance
            var totalValue = notification.UpdatedBalances.AsEnumerable().Sum(b => b.Total * GetEstimatedPrice(b.Currency));
            
            foreach (var balance in notification.UpdatedBalances)
            {
                var balanceValue = balance.Total * GetEstimatedPrice(balance.Currency);
                var percentage = totalValue > 0 ? (balanceValue / totalValue) * 100 : 0;

                if (percentage > 50) // 50% threshold
                {
                    await _mediator.Publish(new RiskThresholdExceededEvent
                    {
                        RiskType = "Asset Concentration",
                        CurrentValue = percentage,
                        ThresholdValue = 50,
                        Severity = RiskSeverity.High,
                        RiskMessage = $"Asset concentration risk: {balance.Currency} represents {percentage:F1}% of portfolio on {notification.ExchangeId}",
                        AffectedAssets = new[] { balance.Currency },
                        RecommendedActions = new[] { "Consider diversifying portfolio", "Reduce exposure to " + balance.Currency },
                        RequiresImmediateAction = percentage > 75,
                        PortfolioValueAtDetection = totalValue
                    }, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check risk thresholds for balance update");
        }
    }

    private static decimal GetEstimatedPrice(string currency)
    {
        // Simplified price estimation - in real implementation, you'd use market data
        return currency.ToUpperInvariant() switch
        {
            "BTC" => 50000m,
            "ETH" => 3000m,
            "USDT" or "USDC" or "USD" => 1m,
            "EUR" => 1.1m,
            _ => 1m
        };
    }
} 