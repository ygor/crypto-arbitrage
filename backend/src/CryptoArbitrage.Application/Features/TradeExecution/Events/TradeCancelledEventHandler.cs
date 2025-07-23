using MediatR;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.TradeExecution.Events;

/// <summary>
/// Handler for TradeCancelledEvent.
/// </summary>
public class TradeCancelledEventHandler : INotificationHandler<TradeCancelledEvent>
{
    private readonly IArbitrageRepository _repository;
    private readonly ILogger<TradeCancelledEventHandler> _logger;

    public TradeCancelledEventHandler(
        IArbitrageRepository repository,
        ILogger<TradeCancelledEventHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TradeCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing TradeCancelledEvent for trade {TradeId} on opportunity {OpportunityId}: {CancellationReason}",
            notification.TradeId,
            notification.OpportunityId,
            notification.CancellationReason);

        try
        {
            // Update opportunity status if available
            if (notification.Opportunity != null)
            {
                notification.Opportunity.MarkAsFailed(); // Cancelled trades are marked as failed
                await _repository.SaveOpportunityAsync(notification.Opportunity, cancellationToken);
            }

            // Log cancellation for monitoring
            var cancellationType = notification.IsAutomaticCancellation ? "automatic" : "manual";
            _logger.LogInformation(
                "Trade {TradeId} cancelled ({CancellationType}) for opportunity {OpportunityId} by {CancelledBy}: {CancellationReason}",
                notification.TradeId,
                cancellationType,
                notification.OpportunityId,
                notification.CancelledBy ?? "system",
                notification.CancellationReason);

            // TODO: Could add additional cancellation handling here:
            // - Send notifications
            // - Update cancellation statistics
            // - Clean up any pending operations
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process TradeCancelledEvent for trade {TradeId} on opportunity {OpportunityId}",
                notification.TradeId,
                notification.OpportunityId);
            
            // Don't rethrow - this is an event handler and should not fail the main flow
        }
    }
} 