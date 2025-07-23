using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Api.Hubs;

namespace CryptoArbitrage.Api.Services;

/// <summary>
/// Service for broadcasting arbitrage and trading updates via SignalR.
/// </summary>
public class SignalRBroadcastService
{
    private readonly IHubContext<ArbitrageHub> _hubContext;
    private readonly ILogger<SignalRBroadcastService> _logger;
    private readonly IMediator _mediator;

    public SignalRBroadcastService(
        IHubContext<ArbitrageHub> hubContext,
        ILogger<SignalRBroadcastService> logger,
        IMediator mediator)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Broadcasts an arbitrage opportunity to connected clients.
    /// </summary>
    public async Task BroadcastOpportunityAsync(object opportunity)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("OpportunityUpdate", opportunity);
            _logger.LogDebug("Broadcasted opportunity update to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting opportunity update");
        }
    }

    /// <summary>
    /// Broadcasts a trade result to connected clients.
    /// </summary>
    public async Task BroadcastTradeResultAsync(object tradeResult)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TradeUpdate", tradeResult);
            _logger.LogDebug("Broadcasted trade result to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting trade result");
        }
    }
} 