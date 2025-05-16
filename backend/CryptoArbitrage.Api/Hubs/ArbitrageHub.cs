using CryptoArbitrage.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace CryptoArbitrage.Api.Hubs;

/// <summary>
/// SignalR hub for broadcasting arbitrage opportunities in real-time.
/// </summary>
public class ArbitrageHub : Hub
{
    private readonly ILogger<ArbitrageHub> _logger;

    public ArbitrageHub(ILogger<ArbitrageHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to ArbitrageHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from ArbitrageHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends an arbitrage opportunity to all connected clients.
    /// This method is called by the server, not by clients.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendArbitrageOpportunity(ArbitrageOpportunity opportunity)
    {
        _logger.LogDebug("Broadcasting arbitrage opportunity: {OpportunityId}", 
            opportunity.DetectedAt.ToString("yyyyMMddHHmmssfff"));
        await Clients.All.SendAsync("ArbitrageOpportunityDetected", opportunity);
    }
} 