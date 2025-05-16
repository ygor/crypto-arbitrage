using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace ArbitrageBot.Infrastructure.Services;

/// <summary>
/// Manages a pool of WebSocket connections for efficient reuse.
/// </summary>
public class WebSocketConnectionPool : IDisposable
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<ClientWebSocket>> _pool = new();
    private readonly ConcurrentDictionary<ClientWebSocket, string> _activeConnections = new();
    private readonly ILogger<WebSocketConnectionPool> _logger;
    private readonly int _maxConnectionsPerEndpoint;
    private readonly SemaphoreSlim _connectionSemaphore;
    private bool _disposed;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnectionPool"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="maxConnectionsPerEndpoint">The maximum number of connections per endpoint.</param>
    /// <param name="maxTotalConnections">The maximum total number of connections across all endpoints.</param>
    public WebSocketConnectionPool(
        ILogger<WebSocketConnectionPool> logger,
        int maxConnectionsPerEndpoint = 5,
        int maxTotalConnections = 20)
    {
        _logger = logger;
        _maxConnectionsPerEndpoint = maxConnectionsPerEndpoint;
        _connectionSemaphore = new SemaphoreSlim(maxTotalConnections, maxTotalConnections);
    }
    
    /// <summary>
    /// Gets a WebSocket connection from the pool, or creates a new one if none are available.
    /// </summary>
    /// <param name="url">The WebSocket URL to connect to.</param>
    /// <param name="configureOptions">Optional action to configure the WebSocket options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A connected WebSocket client.</returns>
    public async Task<ClientWebSocket> GetConnectionAsync(
        string url,
        Action<ClientWebSocketOptions>? configureOptions = null,
        CancellationToken cancellationToken = default)
    {
        // Ensure we don't exceed the total connection limit
        await _connectionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Try to get a connection from the pool
            if (_pool.TryGetValue(url, out var connections) && 
                connections.TryTake(out var socket) && 
                socket.State == WebSocketState.Open)
            {
                _logger.LogDebug("Reusing existing WebSocket connection to {Url}", url);
                return socket;
            }
            
            // Create a new connection
            _logger.LogDebug("Creating new WebSocket connection to {Url}", url);
            var webSocket = new ClientWebSocket();
            
            // Configure the WebSocket
            configureOptions?.Invoke(webSocket.Options);
            
            // Connect to the endpoint
            await webSocket.ConnectAsync(new Uri(url), cancellationToken);
            
            // Track the connection
            _activeConnections[webSocket] = url;
            
            return webSocket;
        }
        catch (Exception)
        {
            // Release the semaphore if we fail to create a connection
            _connectionSemaphore.Release();
            throw;
        }
    }
    
    /// <summary>
    /// Returns a WebSocket connection to the pool for reuse.
    /// </summary>
    /// <param name="webSocket">The WebSocket connection to return.</param>
    public void ReturnConnection(ClientWebSocket webSocket)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            // Don't reuse closed or failed connections
            CloseAndReleaseConnection(webSocket);
            return;
        }
        
        if (_activeConnections.TryGetValue(webSocket, out var url))
        {
            var connections = _pool.GetOrAdd(url, _ => new ConcurrentBag<ClientWebSocket>());
            
            // Only add to the pool if we're under the per-endpoint limit
            if (connections.Count < _maxConnectionsPerEndpoint)
            {
                _logger.LogDebug("Returning WebSocket connection to pool for {Url}", url);
                connections.Add(webSocket);
            }
            else
            {
                // Close and release if we've reached the limit
                CloseAndReleaseConnection(webSocket);
            }
        }
        else
        {
            // If we can't determine the URL, just close and release
            CloseAndReleaseConnection(webSocket);
        }
    }
    
    /// <summary>
    /// Closes all connections in the pool.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CloseAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Closing all WebSocket connections");
        
        foreach (var kvp in _pool.ToArray())
        {
            var url = kvp.Key;
            var connections = kvp.Value;
            
            while (connections.TryTake(out var webSocket))
            {
                await CloseWebSocketAsync(webSocket, cancellationToken);
                _activeConnections.TryRemove(webSocket, out _);
                _connectionSemaphore.Release();
            }
        }
        
        // Close any active connections that aren't in the pool
        foreach (var webSocket in _activeConnections.Keys.ToArray())
        {
            await CloseWebSocketAsync(webSocket, cancellationToken);
            _activeConnections.TryRemove(webSocket, out _);
            _connectionSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Disposes the object, releasing all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Disposes the object, releasing all resources.
    /// </summary>
    /// <param name="disposing">Whether we're explicitly disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Close all connections asynchronously
                CloseAllConnectionsAsync().GetAwaiter().GetResult();
                
                // Dispose the semaphore
                _connectionSemaphore.Dispose();
            }
            
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Closes a WebSocket connection and releases the semaphore.
    /// </summary>
    /// <param name="webSocket">The WebSocket connection to close.</param>
    private void CloseAndReleaseConnection(ClientWebSocket webSocket)
    {
        // Close the connection
        CloseWebSocketAsync(webSocket).GetAwaiter().GetResult();
        
        // Remove from active connections
        _activeConnections.TryRemove(webSocket, out _);
        
        // Release the semaphore
        _connectionSemaphore.Release();
    }
    
    /// <summary>
    /// Closes a WebSocket connection.
    /// </summary>
    /// <param name="webSocket">The WebSocket connection to close.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CloseWebSocketAsync(ClientWebSocket webSocket, CancellationToken cancellationToken = default)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                // Try to close the connection cleanly
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection returned to pool", cancellationToken);
            }
            
            // Dispose the WebSocket
            webSocket.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing WebSocket connection");
            
            // Make sure we dispose even if closing fails
            try
            {
                webSocket.Dispose();
            }
            catch
            {
                // Ignore any errors during dispose
            }
        }
    }
} 