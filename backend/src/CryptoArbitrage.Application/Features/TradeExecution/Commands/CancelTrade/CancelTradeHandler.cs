using MediatR;
using CryptoArbitrage.Application.Features.TradeExecution.Events;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.CancelTrade;

/// <summary>
/// Handler for cancelling trades.
/// </summary>
public class CancelTradeHandler : IRequestHandler<CancelTradeCommand, CancelTradeResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CancelTradeHandler> _logger;

    public CancelTradeHandler(
        IMediator mediator,
        ILogger<CancelTradeHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CancelTradeResult> Handle(CancelTradeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling trade {TradeId}: {CancellationReason}", 
            request.TradeId, request.CancellationReason);

        try
        {
            // Note: This is a basic implementation
            // In a real system, you would need to:
            // 1. Check if the trade exists and is cancellable
            // 2. Call exchange APIs to cancel orders
            // 3. Update trade status in the repository
            // 4. Handle partial fills and settlements

            // For now, just publish the cancellation event
            await _mediator.Publish(new TradeCancelledEvent
            {
                TradeId = request.TradeId,
                OpportunityId = request.OpportunityId ?? string.Empty,
                CancellationReason = request.CancellationReason,
                CancelledBy = request.CancelledBy,
                IsAutomaticCancellation = false
            }, cancellationToken);

            _logger.LogInformation("Trade {TradeId} cancelled successfully", request.TradeId);
            
            return CancelTradeResult.Success(request.TradeId, request.CancellationReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling trade {TradeId}", request.TradeId);
            return CancelTradeResult.Failure($"Failed to cancel trade: {ex.Message}", request.TradeId);
        }
    }
} 