using MediatR;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.TradeExecution.Events;

/// <summary>
/// Handler for TradeExecutedEvent.
/// </summary>
public class TradeExecutedEventHandler : INotificationHandler<TradeExecutedEvent>
{
    private readonly IArbitrageRepository _repository;
    private readonly ILogger<TradeExecutedEventHandler> _logger;

    public TradeExecutedEventHandler(
        IArbitrageRepository repository,
        ILogger<TradeExecutedEventHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TradeExecutedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing TradeExecutedEvent for trade {TradeId} on opportunity {OpportunityId}",
            notification.TradeResult.Id,
            notification.TradeResult.OpportunityId);

        try
        {
            // Save the trade result to repository
            await _repository.SaveTradeResultAsync(notification.TradeResult);

            // Update opportunity status
            notification.Opportunity.MarkAsExecuted();
            await _repository.SaveOpportunityAsync(notification.Opportunity, cancellationToken);

            _logger.LogInformation(
                "Successfully processed TradeExecutedEvent for trade {TradeId}. Profit: {ProfitAmount} ({ProfitPercentage}%)",
                notification.TradeResult.Id,
                notification.TradeResult.ProfitAmount,
                notification.TradeResult.ProfitPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process TradeExecutedEvent for trade {TradeId} on opportunity {OpportunityId}",
                notification.TradeResult.Id,
                notification.TradeResult.OpportunityId);
            
            // Don't rethrow - this is an event handler and should not fail the main flow
        }
    }
} 