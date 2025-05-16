using ArbitrageBot.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace ArbitrageBot.Api.Hubs;

/// <summary>
/// SignalR hub for broadcasting trade results in real-time.
/// </summary>
public class TradeHub : Hub
{
    private readonly ILogger<TradeHub> _logger;

    public TradeHub(ILogger<TradeHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to TradeHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from TradeHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a trade result to all connected clients.
    /// This method is called by the server, not by clients.
    /// </summary>
    /// <param name="tradeResult">The trade result to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendTradeResult(TradeResult tradeResult)
    {
        _logger.LogDebug("Broadcasting trade result: {TradeId}", tradeResult.OrderId ?? "Unknown");
        await Clients.All.SendAsync("TradeCompleted", tradeResult);
    }
    
    /// <summary>
    /// Sends a complete arbitrage trade result (with buy/sell operations) to all connected clients.
    /// </summary>
    /// <param name="tradeResult">The complete arbitrage trade result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendArbitrageTradeResult(ArbitrageTradeResult tradeResult)
    {
        _logger.LogDebug("Broadcasting arbitrage trade result for opportunity: {OpportunityId}", 
            tradeResult.Opportunity.DetectedAt.ToString("yyyyMMddHHmmssfff"));
        await Clients.All.SendAsync("ArbitrageTradeCompleted", tradeResult);
    }
} 