using Microsoft.AspNetCore.SignalR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Blazor.Hubs;

/// <summary>
/// SignalR hub for real-time market data distribution.
/// Pushes order book updates, spreads, and arbitrage opportunities to connected clients.
/// </summary>
public class MarketDataHub : Hub
{
    private readonly ILogger<MarketDataHub> _logger;

    public MarketDataHub(ILogger<MarketDataHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to MarketDataHub", Context.ConnectionId);
        
        // Add the client to the market data group
        await Groups.AddToGroupAsync(Context.ConnectionId, "MarketData");
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from MarketDataHub", Context.ConnectionId);
        
        // Remove the client from the market data group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "MarketData");
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time updates for specific trading pairs.
    /// </summary>
    /// <param name="tradingPairs">Trading pairs to subscribe to</param>
    public async Task SubscribeToTradingPairs(string[] tradingPairs)
    {
        _logger.LogInformation("Client {ConnectionId} subscribing to trading pairs: {TradingPairs}", 
            Context.ConnectionId, string.Join(", ", tradingPairs));
        
        foreach (var tradingPair in tradingPairs)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TradingPair_{tradingPair}");
        }
    }

    /// <summary>
    /// Unsubscribe from real-time updates for specific trading pairs.
    /// </summary>
    /// <param name="tradingPairs">Trading pairs to unsubscribe from</param>
    public async Task UnsubscribeFromTradingPairs(string[] tradingPairs)
    {
        _logger.LogInformation("Client {ConnectionId} unsubscribing from trading pairs: {TradingPairs}", 
            Context.ConnectionId, string.Join(", ", tradingPairs));
        
        foreach (var tradingPair in tradingPairs)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"TradingPair_{tradingPair}");
        }
    }
} 