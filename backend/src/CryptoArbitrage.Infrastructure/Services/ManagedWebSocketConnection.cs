using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// A managed WebSocket connection with exponential backoff reconnection, health monitoring, and circuit breaker pattern.
/// </summary>
public class ManagedWebSocketConnection : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly WebSocketConnectionManagerOptions _options;
    private readonly Action<ClientWebSocketOptions>? _configureOptions;
    private readonly Random _random = new();
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly Timer? _heartbeatTimer;
    
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _messageProcessingTask;
    private bool _disposed;
    
    // Connection state
    private int _reconnectionAttempts;
    private DateTimeOffset? _lastMessageReceived;
    private DateTimeOffset? _circuitBreakerTriggeredAt;
    private long _totalMessagesReceived;
    private long _totalMessagesSent;
    private string? _lastError;

    public string ConnectionId { get; }
    public string Url { get; }
    
    // Events for message handling
    public event Func<string, CancellationToken, Task>? OnMessageReceived;
    public event Func<Exception, CancellationToken, Task>? OnError;
    public event Func<CancellationToken, Task>? OnConnected;
    public event Func<CancellationToken, Task>? OnDisconnected;

    public ManagedWebSocketConnection(
        string connectionId,
        string url,
        ILogger logger,
        WebSocketConnectionManagerOptions options,
        Action<ClientWebSocketOptions>? configureOptions = null)
    {
        ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        Url = url ?? throw new ArgumentNullException(nameof(url));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configureOptions = configureOptions;
        
        // Start heartbeat timer if enabled
        if (_options.HeartbeatIntervalSeconds > 0)
        {
            _heartbeatTimer = new Timer(SendHeartbeat, null,
                TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds),
                TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds));
        }
    }

    #region Public Properties

    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;
    
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    
    public bool IsHealthy => IsConnected && 
                           !IsCircuitBreakerTriggered && 
                           (_lastMessageReceived == null || 
                            DateTimeOffset.UtcNow - _lastMessageReceived.Value < TimeSpan.FromSeconds(_options.MaxIdleTimeSeconds));
    
    public DateTimeOffset? ConnectedAt { get; private set; }
    
    public DateTimeOffset? LastHeartbeat { get; private set; }
    
    public int ReconnectionAttempts => _reconnectionAttempts;
    
    public long TotalMessagesReceived => _totalMessagesReceived;
    
    public long TotalMessagesSent => _totalMessagesSent;
    
    public string? LastError => _lastError;
    
    private bool IsCircuitBreakerTriggered => 
        _circuitBreakerTriggeredAt.HasValue && 
        DateTimeOffset.UtcNow - _circuitBreakerTriggeredAt.Value < TimeSpan.FromSeconds(_options.CircuitBreakerRecoveryTimeSeconds);

    #endregion

    #region Connection Management

    /// <summary>
    /// Connects to the WebSocket with exponential backoff and circuit breaker protection.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                _logger.LogDebug("Connection {ConnectionId} is already connected", ConnectionId);
                return;
            }

            if (IsCircuitBreakerTriggered)
            {
                _logger.LogWarning("Circuit breaker is active for connection {ConnectionId}, skipping connection attempt", ConnectionId);
                throw new InvalidOperationException($"Circuit breaker is active for connection {ConnectionId}");
            }

            await ConnectInternalAsync(cancellationToken);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Connecting to WebSocket: {ConnectionId} -> {Url}", ConnectionId, Url);
            
            // Dispose existing WebSocket if any
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();

            // Create new WebSocket and cancellation token
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Configure WebSocket options
            _configureOptions?.Invoke(_webSocket.Options);
            
            // Set timeouts
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.ConnectionTimeoutMs));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            // Connect to WebSocket
            await _webSocket.ConnectAsync(new Uri(Url), linkedCts.Token);
            
            // Reset state on successful connection
            ConnectedAt = DateTimeOffset.UtcNow;
            _lastMessageReceived = DateTimeOffset.UtcNow;
            _reconnectionAttempts = 0;
            _lastError = null;
            
            // Start message processing
            _messageProcessingTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
            
            _logger.LogInformation("Successfully connected to WebSocket: {ConnectionId}", ConnectionId);
            
            // Notify connection established
            if (OnConnected != null)
            {
                try
                {
                    await OnConnected(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnConnected handler for connection {ConnectionId}", ConnectionId);
                }
            }
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _logger.LogError(ex, "Failed to connect to WebSocket: {ConnectionId}", ConnectionId);
            
            // Trigger reconnection
            _ = Task.Run(() => HandleReconnectionAsync(cancellationToken), cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Handles reconnection with exponential backoff and circuit breaker.
    /// </summary>
    private async Task HandleReconnectionAsync(CancellationToken cancellationToken)
    {
        if (_disposed || IsCircuitBreakerTriggered)
            return;

        _reconnectionAttempts++;
        
        if (_reconnectionAttempts >= _options.MaxReconnectionAttempts)
        {
            _logger.LogError("Maximum reconnection attempts ({MaxAttempts}) reached for connection {ConnectionId}, triggering circuit breaker", 
                _options.MaxReconnectionAttempts, ConnectionId);
            
            _circuitBreakerTriggeredAt = DateTimeOffset.UtcNow;
            return;
        }

        // Calculate delay with exponential backoff and jitter
        var delay = CalculateReconnectionDelay(_reconnectionAttempts);
        
        _logger.LogInformation("Attempting reconnection {Attempt}/{MaxAttempts} for connection {ConnectionId} in {Delay}ms", 
            _reconnectionAttempts, _options.MaxReconnectionAttempts, ConnectionId, delay);

        try
        {
            await Task.Delay(delay, cancellationToken);
            
            if (!_disposed && !cancellationToken.IsCancellationRequested)
            {
                await ConnectAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed for connection {ConnectionId}", 
                _reconnectionAttempts, ConnectionId);
        }
    }

    /// <summary>
    /// Calculates reconnection delay with exponential backoff and jitter.
    /// </summary>
    private int CalculateReconnectionDelay(int attempt)
    {
        // Exponential backoff: delay = initialDelay * 2^(attempt-1)
        var exponentialDelay = _options.InitialReconnectionDelayMs * Math.Pow(2, attempt - 1);
        
        // Cap at maximum delay
        var cappedDelay = Math.Min(exponentialDelay, _options.MaxReconnectionDelayMs);
        
        // Add jitter to prevent thundering herd
        var jitter = cappedDelay * _options.JitterFactor * (_random.NextDouble() - 0.5);
        
        return (int)Math.Max(_options.InitialReconnectionDelayMs, cappedDelay + jitter);
    }

    #endregion

    #region Message Processing

    /// <summary>
    /// Processes incoming WebSocket messages.
    /// </summary>
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        if (_webSocket == null)
            return;

        var buffer = new byte[16384]; // 16KB buffer
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                messageBuilder.Clear();

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket close message received for connection {ConnectionId}", ConnectionId);
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close received", cancellationToken);
                        break;
                    }
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messageChunk);
                    }
                }
                while (!result.EndOfMessage && _webSocket.State == WebSocketState.Open);

                if (result.MessageType == WebSocketMessageType.Close || _webSocket.State != WebSocketState.Open)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text && messageBuilder.Length > 0)
                {
                    var message = messageBuilder.ToString();
                    await HandleMessageReceived(message, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Message processing cancelled for connection {ConnectionId}", ConnectionId);
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error in message processing for connection {ConnectionId}: {Error}", 
                ConnectionId, ex.Message);
            
            await HandleConnectionError(ex, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in message processing for connection {ConnectionId}", ConnectionId);
            
            await HandleConnectionError(ex, cancellationToken);
        }
        finally
        {
            // Notify disconnection
            if (OnDisconnected != null)
            {
                try
                {
                    await OnDisconnected(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnDisconnected handler for connection {ConnectionId}", ConnectionId);
                }
            }
        }
    }

    /// <summary>
    /// Handles a received message.
    /// </summary>
    private async Task HandleMessageReceived(string message, CancellationToken cancellationToken)
    {
        try
        {
            _lastMessageReceived = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref _totalMessagesReceived);
            
            _logger.LogTrace("Message received on connection {ConnectionId}: {MessageLength} chars", 
                ConnectionId, message.Length);

            if (OnMessageReceived != null)
            {
                await OnMessageReceived(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling received message for connection {ConnectionId}", ConnectionId);
            
            await HandleMessageError(ex, cancellationToken);
        }
    }

    /// <summary>
    /// Handles connection errors.
    /// </summary>
    private async Task HandleConnectionError(Exception exception, CancellationToken cancellationToken)
    {
        _lastError = exception.Message;
        
        try
        {
            if (OnError != null)
            {
                await OnError(exception, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnError handler for connection {ConnectionId}", ConnectionId);
        }

        // Trigger reconnection if not disposed
        if (!_disposed)
        {
            _ = Task.Run(() => HandleReconnectionAsync(cancellationToken), cancellationToken);
        }
    }

    /// <summary>
    /// Handles message processing errors.
    /// </summary>
    private async Task HandleMessageError(Exception exception, CancellationToken cancellationToken)
    {
        _lastError = exception.Message;
        
        try
        {
            if (OnError != null)
            {
                await OnError(exception, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnError handler for connection {ConnectionId}", ConnectionId);
        }
    }

    #endregion

    #region Message Sending

    /// <summary>
    /// Sends a message through the WebSocket connection.
    /// </summary>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"WebSocket connection {ConnectionId} is not open");
        }

        try
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.SendTimeoutMs));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text,
                true,
                linkedCts.Token);

            Interlocked.Increment(ref _totalMessagesSent);
            
            _logger.LogTrace("Message sent on connection {ConnectionId}: {MessageLength} chars", 
                ConnectionId, message.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message on connection {ConnectionId}", ConnectionId);
            throw;
        }
    }

    #endregion

    #region Health Monitoring

    /// <summary>
    /// Performs a health check on the connection.
    /// </summary>
    public async Task PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _webSocket?.State != WebSocketState.Open)
            return;

        try
        {
            // Check if we've received messages recently
            if (_lastMessageReceived.HasValue)
            {
                var timeSinceLastMessage = DateTimeOffset.UtcNow - _lastMessageReceived.Value;
                if (timeSinceLastMessage > TimeSpan.FromSeconds(_options.MaxIdleTimeSeconds))
                {
                    _logger.LogWarning("Connection {ConnectionId} appears unhealthy - no messages received for {TimeSinceLastMessage}", 
                        ConnectionId, timeSinceLastMessage);
                }
            }

            // Reset circuit breaker if recovery time has passed
            if (IsCircuitBreakerTriggered && 
                _circuitBreakerTriggeredAt.HasValue && 
                DateTimeOffset.UtcNow - _circuitBreakerTriggeredAt.Value >= TimeSpan.FromSeconds(_options.CircuitBreakerRecoveryTimeSeconds))
            {
                _logger.LogInformation("Circuit breaker recovery time elapsed for connection {ConnectionId}, resetting", ConnectionId);
                _circuitBreakerTriggeredAt = null;
                _reconnectionAttempts = 0;
                
                // Attempt reconnection
                _ = Task.Run(() => ConnectAsync(cancellationToken), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check for connection {ConnectionId}", ConnectionId);
        }
    }

    /// <summary>
    /// Sends a heartbeat message if configured.
    /// </summary>
    private async void SendHeartbeat(object? state)
    {
        if (_disposed || _webSocket?.State != WebSocketState.Open)
            return;

        try
        {
            var heartbeat = JsonSerializer.Serialize(new { type = "ping", timestamp = DateTimeOffset.UtcNow });
            await SendMessageAsync(heartbeat);
            
            LastHeartbeat = DateTimeOffset.UtcNow;
            _logger.LogTrace("Heartbeat sent for connection {ConnectionId}", ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat for connection {ConnectionId}", ConnectionId);
        }
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the managed WebSocket connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        _logger.LogInformation("Disposing managed WebSocket connection: {ConnectionId}", ConnectionId);

        // Stop heartbeat timer
        _heartbeatTimer?.Dispose();
        
        // Cancel operations
        _cancellationTokenSource?.Cancel();
        
        // Wait for message processing to complete
        if (_messageProcessingTask != null)
        {
            try
            {
                await _messageProcessingTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error waiting for message processing task to complete for connection {ConnectionId}", ConnectionId);
            }
        }

        // Close WebSocket
        if (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing WebSocket for connection {ConnectionId}", ConnectionId);
            }
        }

        // Dispose resources
        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
        _connectionSemaphore.Dispose();
        
        _logger.LogInformation("Disposed managed WebSocket connection: {ConnectionId}", ConnectionId);
    }

    #endregion
} 