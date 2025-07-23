using MediatR;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.TradeExecution.Events;

/// <summary>
/// Handler for TradeFailedEvent.
/// </summary>
public class TradeFailedEventHandler : INotificationHandler<TradeFailedEvent>
{
    private readonly IArbitrageRepository _repository;
    private readonly ILogger<TradeFailedEventHandler> _logger;

    public TradeFailedEventHandler(
        IArbitrageRepository repository,
        ILogger<TradeFailedEventHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TradeFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Processing TradeFailedEvent for opportunity {OpportunityId}: {ErrorMessage}",
            notification.OpportunityId,
            notification.ErrorMessage);

        try
        {
            // Update opportunity status to failed
            notification.Opportunity.MarkAsFailed();
            await _repository.SaveOpportunityAsync(notification.Opportunity, cancellationToken);

            // Log failure metrics for monitoring/alerting
            _logger.LogError(
                "Trade execution failed for opportunity {OpportunityId} after {ExecutionTimeMs}ms: {ErrorMessage}",
                notification.OpportunityId,
                notification.ExecutionTimeMs,
                notification.ErrorMessage);

            // TODO: Could add additional failure handling here:
            // - Send notifications/alerts
            // - Update failure statistics
            // - Trigger retry logic if appropriate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process TradeFailedEvent for opportunity {OpportunityId}",
                notification.OpportunityId);
            
            // Don't rethrow - this is an event handler and should not fail the main flow
        }
    }
} 