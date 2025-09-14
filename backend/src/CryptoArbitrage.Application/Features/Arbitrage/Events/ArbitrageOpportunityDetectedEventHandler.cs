using MediatR;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.Arbitrage.Events;

/// <summary>
/// Handler for arbitrage opportunity detected events.
/// </summary>
public class ArbitrageOpportunityDetectedEventHandler : INotificationHandler<ArbitrageOpportunityDetectedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ArbitrageOpportunityDetectedEventHandler> _logger;

    public ArbitrageOpportunityDetectedEventHandler(
        INotificationService notificationService,
        ILogger<ArbitrageOpportunityDetectedEventHandler> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(ArbitrageOpportunityDetectedEvent notification, CancellationToken cancellationToken)
    {
        var opportunity = notification.Opportunity;
        
        _logger.LogInformation(
            "🎯 Arbitrage opportunity detected: {TradingPair} | {BuyExchange} -> {SellExchange} | " +
            "Profit: {ProfitPercentage:F2}% | Amount: {EffectiveQuantity:F8} | " +
            "Buy: {BuyPrice:F8} | Sell: {SellPrice:F8}",
            opportunity.TradingPair,
            opportunity.BuyExchangeId,
            opportunity.SellExchangeId,
            opportunity.ProfitPercentage,
            opportunity.EffectiveQuantity,
            opportunity.BuyPrice,
            opportunity.SellPrice);

        try
        {
            // Send notification for high-profit opportunities
            if (opportunity.ProfitPercentage >= 1.0m)
            {
                var message = $"🚨 High-profit arbitrage opportunity: {opportunity.ProfitPercentage:F2}% profit on {opportunity.TradingPair}";
                var details = $"Buy on {opportunity.BuyExchangeId} at {opportunity.BuyPrice:F8}, " +
                             $"sell on {opportunity.SellExchangeId} at {opportunity.SellPrice:F8}. " +
                             $"Effective quantity: {opportunity.EffectiveQuantity:F8}";

                await _notificationService.SendNotificationAsync(
                    "High-Profit Arbitrage Opportunity",
                    message,
                    details,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for arbitrage opportunity {OpportunityId}", opportunity.Id);
        }
    }
} 