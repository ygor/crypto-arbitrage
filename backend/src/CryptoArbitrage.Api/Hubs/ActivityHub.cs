using CryptoArbitrage.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace CryptoArbitrage.Api.Hubs;

/// <summary>
/// SignalR hub for broadcasting activity log entries in real-time.
/// </summary>
public class ActivityHub : Hub
{
    private readonly ILogger<ActivityHub> _logger;

    public ActivityHub(ILogger<ActivityHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to ActivityHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from ActivityHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
} 