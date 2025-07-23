using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Api.Hubs;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Api.Services;

/// <summary>
/// Service for broadcasting market data updates via SignalR.
/// </summary>
public class MarketDataBroadcastService
{
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly ILogger<MarketDataBroadcastService> _logger;
    private readonly IMediator _mediator;

    public MarketDataBroadcastService(
        IHubContext<MarketDataHub> hubContext,
        ILogger<MarketDataBroadcastService> logger,
        IMediator mediator)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Broadcasts market data update to connected clients.
    /// </summary>
    public async Task BroadcastMarketDataAsync(TradingPair tradingPair, PriceQuote? bestBid, PriceQuote? bestAsk)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("MarketDataUpdate", new { 
                TradingPair = tradingPair.ToString(),
                BestBid = bestBid,
                BestAsk = bestAsk,
                Timestamp = DateTime.UtcNow 
            });
            
            _logger.LogDebug("Broadcasted market data update for {TradingPair}", tradingPair);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting market data update");
        }
    }
} 