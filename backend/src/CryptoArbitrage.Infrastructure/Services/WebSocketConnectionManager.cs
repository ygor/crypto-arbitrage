using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// Enhanced WebSocket connection manager with reconnection strategies, health monitoring, and circuit breaker pattern.
/// </summary>
public class WebSocketConnectionManager : IDisposable
{
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, ManagedWebSocketConnection> _connections = new();
    private readonly Timer _healthCheckTimer;
    private readonly WebSocketConnectionManagerOptions _options;
    private bool _disposed;

    public WebSocketConnectionManager(
        ILogger<WebSocketConnectionManager> logger,
        WebSocketConnectionManagerOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new WebSocketConnectionManagerOptions();
        
        // Start health check timer
        _healthCheckTimer = new Timer(PerformHealthChecks, null, 
            TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds), 
            TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds));
        
        _logger.LogInformation("WebSocket Connection Manager initialized with health check interval: {HealthCheckInterval}s", 
            _options.HealthCheckIntervalSeconds);
    }

    /// <summary>
    /// Gets or creates a managed WebSocket connection.
    /// </summary>
    public async Task<ManagedWebSocketConnection> GetConnectionAsync(
        string connectionId,
        string url,
        Action<ClientWebSocketOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(connectionId, out var existingConnection) && 
            existingConnection.IsHealthy)
        {
            _logger.LogDebug("Reusing existing healthy connection: {ConnectionId}", connectionId);
            return existingConnection;
        }

        // Remove unhealthy connection if it exists
        if (existingConnection != null)
        {
            _logger.LogInformation("Removing unhealthy connection: {ConnectionId}", connectionId);
            _connections.TryRemove(connectionId, out _);
            await existingConnection.DisposeAsync();
        }

        // Create new managed connection
        var managedConnection = new ManagedWebSocketConnection(
            connectionId, url, _logger, _options, configureOptions);

        // Attempt initial connection
        await managedConnection.ConnectAsync(cancellationToken);
        
        // Store the connection
        _connections[connectionId] = managedConnection;
        
        _logger.LogInformation("Created new managed WebSocket connection: {ConnectionId} -> {Url}", 
            connectionId, url);
        
        return managedConnection;
    }

    /// <summary>
    /// Removes and disposes a connection.
    /// </summary>
    public async Task RemoveConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            _logger.LogInformation("Removing connection: {ConnectionId}", connectionId);
            await connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets the status of all managed connections.
    /// </summary>
    public IReadOnlyDictionary<string, WebSocketConnectionStatus> GetConnectionStatuses()
    {
        var statuses = new Dictionary<string, WebSocketConnectionStatus>();
        
        foreach (var kvp in _connections)
        {
            statuses[kvp.Key] = new WebSocketConnectionStatus
            {
                ConnectionId = kvp.Key,
                Url = kvp.Value.Url,
                State = kvp.Value.State,
                IsHealthy = kvp.Value.IsHealthy,
                IsConnected = kvp.Value.IsConnected,
                ConnectedAt = kvp.Value.ConnectedAt,
                LastHeartbeat = kvp.Value.LastHeartbeat,
                ReconnectionAttempts = kvp.Value.ReconnectionAttempts,
                TotalMessagesReceived = kvp.Value.TotalMessagesReceived,
                TotalMessagesSent = kvp.Value.TotalMessagesSent,
                LastError = kvp.Value.LastError
            };
        }
        
        return statuses;
    }

    /// <summary>
    /// Performs health checks on all managed connections.
    /// </summary>
    private async void PerformHealthChecks(object? state)
    {
        if (_disposed) return;

        try
        {
            var healthCheckTasks = new List<Task>();
            
            foreach (var connection in _connections.Values)
            {
                healthCheckTasks.Add(PerformConnectionHealthCheck(connection));
            }
            
            if (healthCheckTasks.Count > 0)
            {
                await Task.WhenAll(healthCheckTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health checks");
        }
    }

    /// <summary>
    /// Performs health check on a specific connection.
    /// </summary>
    private async Task PerformConnectionHealthCheck(ManagedWebSocketConnection connection)
    {
        try
        {
            await connection.PerformHealthCheckAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for connection: {ConnectionId}", connection.ConnectionId);
        }
    }

    /// <summary>
    /// Disposes all connections and resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _logger.LogInformation("Disposing WebSocket Connection Manager with {ConnectionCount} connections", 
                _connections.Count);

            // Stop health check timer
            _healthCheckTimer?.Dispose();

            // Dispose all connections
            var disposeTasks = _connections.Values.Select(c => c.DisposeAsync().AsTask());
            Task.WhenAll(disposeTasks).GetAwaiter().GetResult();
            
            _connections.Clear();
            _disposed = true;
            
            _logger.LogInformation("WebSocket Connection Manager disposed");
        }
    }
}

/// <summary>
/// Configuration options for the WebSocket Connection Manager.
/// </summary>
public class WebSocketConnectionManagerOptions
{
    /// <summary>
    /// Interval for performing health checks on connections (in seconds).
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of reconnection attempts before triggering circuit breaker.
    /// </summary>
    public int MaxReconnectionAttempts { get; set; } = 10;
    
    /// <summary>
    /// Initial reconnection delay in milliseconds.
    /// </summary>
    public int InitialReconnectionDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum reconnection delay in milliseconds.
    /// </summary>
    public int MaxReconnectionDelayMs { get; set; } = 30000;
    
    /// <summary>
    /// Jitter factor for randomizing reconnection delays (0.0 to 1.0).
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;
    
    /// <summary>
    /// Timeout for connection attempts in milliseconds.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;
    
    /// <summary>
    /// Timeout for sending messages in milliseconds.
    /// </summary>
    public int SendTimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// Heartbeat interval in seconds (0 to disable).
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum time without receiving a message before considering connection unhealthy.
    /// </summary>
    public int MaxIdleTimeSeconds { get; set; } = 120;
    
    /// <summary>
    /// Circuit breaker recovery time in seconds.
    /// </summary>
    public int CircuitBreakerRecoveryTimeSeconds { get; set; } = 300;
}

/// <summary>
/// Status information for a WebSocket connection.
/// </summary>
public class WebSocketConnectionStatus
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public WebSocketState State { get; set; }
    public bool IsHealthy { get; set; }
    public bool IsConnected { get; set; }
    public DateTimeOffset? ConnectedAt { get; set; }
    public DateTimeOffset? LastHeartbeat { get; set; }
    public int ReconnectionAttempts { get; set; }
    public long TotalMessagesReceived { get; set; }
    public long TotalMessagesSent { get; set; }
    public string? LastError { get; set; }
} 