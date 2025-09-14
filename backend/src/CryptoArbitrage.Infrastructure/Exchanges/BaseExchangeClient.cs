using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Base implementation of <see cref="IExchangeClient"/>.
/// </summary>
public abstract class BaseExchangeClient : IExchangeClient, IDisposable
{
    /// <summary>
    /// Gets the configuration service.
    /// </summary>
    protected readonly IConfigurationService ConfigurationService;
    
    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected readonly ILogger Logger;
    
    /// <summary>
    /// Gets or sets the order books.
    /// </summary>
    protected readonly Dictionary<TradingPair, OrderBook> OrderBooks = new();
    
    /// <summary>
    /// Gets or sets the balances.
    /// </summary>
    protected readonly Dictionary<string, Balance> Balances = new();
    
    /// <summary>
    /// Gets or sets the managed WebSocket connection.
    /// </summary>
    protected ManagedWebSocketConnection? ManagedConnection;
    
    /// <summary>
    /// Gets or sets the WebSocket connection manager.
    /// </summary>
    protected WebSocketConnectionManager? ConnectionManager;
    
    /// <summary>
    /// Gets or sets the order book channels.
    /// </summary>
    protected readonly Dictionary<TradingPair, Channel<OrderBook>> OrderBookChannels = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether the client is connected.
    /// </summary>
    protected bool _isConnected;
    
    /// <summary>
    /// Gets or sets a value indicating whether the client is authenticated.
    /// </summary>
    protected bool _isAuthenticated;
    
    /// <summary>
    /// Gets or sets the WebSocket URL.
    /// </summary>
    protected string? WebSocketUrl;
    
    /// <summary>
    /// Gets or sets the cancellation token source for the WebSocket connection.
    /// </summary>
    protected CancellationTokenSource? _webSocketCts;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseExchangeClient"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    protected BaseExchangeClient(
        string exchangeId,
        IConfigurationService configurationService,
        ILogger logger)
    {
        ExchangeId = exchangeId;
        ConfigurationService = configurationService;
        Logger = logger;
        
        // Initialize WebSocket connection manager with enhanced options
        var connectionManagerLogger = logger is ILogger<WebSocketConnectionManager> 
            ? (ILogger<WebSocketConnectionManager>)logger 
            : new LoggerAdapter<WebSocketConnectionManager>(logger);
            
        var options = new WebSocketConnectionManagerOptions
        {
            HealthCheckIntervalSeconds = 30,
            MaxReconnectionAttempts = 10,
            InitialReconnectionDelayMs = 1000,
            MaxReconnectionDelayMs = 30000,
            JitterFactor = 0.1,
            ConnectionTimeoutMs = 10000,
            SendTimeoutMs = 5000,
            HeartbeatIntervalSeconds = 30,
            MaxIdleTimeSeconds = 120,
            CircuitBreakerRecoveryTimeSeconds = 300
        };
        
        ConnectionManager = new WebSocketConnectionManager(connectionManagerLogger, options);
    }
    
    /// <inheritdoc />
    public string ExchangeId { get; }
    
    /// <inheritdoc />
    public virtual bool SupportsStreaming => false;
    
    /// <inheritdoc />
    public virtual async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            Logger.LogDebug("Client for {ExchangeId} is already connected", ExchangeId);
            return;
        }
        
        Logger.LogInformation("Connecting to {ExchangeId}...", ExchangeId);
        
        try
        {
            // Load exchange configuration to get WebSocket URL
            var config = await ConfigurationService.GetExchangeConfigurationAsync(ExchangeId, cancellationToken);
            if (config?.WebSocketUrl != null && ConnectionManager != null)
            {
                WebSocketUrl = config.WebSocketUrl;
                _webSocketCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                // Get managed WebSocket connection
                ManagedConnection = await ConnectionManager.GetConnectionAsync(
                    ExchangeId, 
                    WebSocketUrl, 
                    ConfigureWebSocketOptions,
                    cancellationToken);
                
                // Set up event handlers
                ManagedConnection.OnMessageReceived += OnWebSocketMessageReceived;
                ManagedConnection.OnError += OnWebSocketError;
                ManagedConnection.OnConnected += OnWebSocketConnected;
                ManagedConnection.OnDisconnected += OnWebSocketDisconnected;
                
                Logger.LogInformation("Enhanced WebSocket connected to {ExchangeId} at {WebSocketUrl}", ExchangeId, WebSocketUrl);
            }
            
            _isConnected = true;
            Logger.LogInformation("Connected to {ExchangeId} with enhanced WebSocket management", ExchangeId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error connecting to {ExchangeId}", ExchangeId);
            throw;
        }
    }

    /// <summary>
    /// Configures WebSocket options for the exchange.
    /// </summary>
    /// <param name="options">The WebSocket options to configure.</param>
    protected virtual void ConfigureWebSocketOptions(ClientWebSocketOptions options)
    {
        // Default implementation - derived classes can override
        options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Handles WebSocket message received events.
    /// </summary>
    protected virtual async Task OnWebSocketMessageReceived(string message, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessWebSocketMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing WebSocket message for {ExchangeId}: {Message}", ExchangeId, message);
        }
    }

    /// <summary>
    /// Handles WebSocket error events.
    /// </summary>
    protected virtual async Task OnWebSocketError(Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogWarning(exception, "WebSocket error for {ExchangeId}: {Error}", ExchangeId, exception.Message);
        
        // Derived classes can override to handle specific errors
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles WebSocket connected events.
    /// </summary>
    protected virtual async Task OnWebSocketConnected(CancellationToken cancellationToken)
    {
        Logger.LogInformation("WebSocket connection established for {ExchangeId}", ExchangeId);
        
        // Resubscribe to existing subscriptions if any
        await ResubscribeToExistingChannels(cancellationToken);
    }

    /// <summary>
    /// Handles WebSocket disconnected events.
    /// </summary>
    protected virtual async Task OnWebSocketDisconnected(CancellationToken cancellationToken)
    {
        Logger.LogWarning("WebSocket connection lost for {ExchangeId}", ExchangeId);
        
        // Derived classes can override to handle disconnection
        await Task.CompletedTask;
    }

    /// <summary>
    /// Resubscribes to existing channels after reconnection.
    /// </summary>
    protected virtual async Task ResubscribeToExistingChannels(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var tradingPair in OrderBookChannels.Keys.ToList())
            {
                Logger.LogInformation("Resubscribing to {TradingPair} for {ExchangeId}", tradingPair, ExchangeId);
                await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resubscribing to channels for {ExchangeId}", ExchangeId);
        }
    }

    /// <inheritdoc />
    public virtual async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            Logger.LogDebug("Client for {ExchangeId} is not connected", ExchangeId);
            return;
        }
        
        Logger.LogInformation("Disconnecting from {ExchangeId}...", ExchangeId);
        
        try
        {
            // Unsubscribe from all order books first
            foreach (var tradingPair in OrderBookChannels.Keys.ToList())
            {
                try
                {
                    // Don't use ValidateConnected here as we're in the process of disconnecting
                    await UnsubscribeFromOrderBookAsync(tradingPair, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log but continue with disconnection
                    Logger.LogWarning(ex, "Error unsubscribing from {TradingPair} during disconnect for {ExchangeId}", 
                        tradingPair, ExchangeId);
                }
            }
            
            // Cancel operations
            _webSocketCts?.Cancel();
            
            // Unsubscribe from managed connection events
            if (ManagedConnection != null)
            {
                ManagedConnection.OnMessageReceived -= OnWebSocketMessageReceived;
                ManagedConnection.OnError -= OnWebSocketError;
                ManagedConnection.OnConnected -= OnWebSocketConnected;
                ManagedConnection.OnDisconnected -= OnWebSocketDisconnected;
            }
            
            // Remove connection from manager (this will dispose the connection)
            if (ConnectionManager != null)
            {
                await ConnectionManager.RemoveConnectionAsync(ExchangeId);
            }
            
            // Clean up all channels
            foreach (var channel in OrderBookChannels.Values)
            {
                try
                {
                    channel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error completing channel for {ExchangeId}", ExchangeId);
                }
            }
            
            OrderBookChannels.Clear();
            
            // Dispose resources
            try
            {
                _webSocketCts?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error disposing WebSocket CTS for {ExchangeId}", ExchangeId);
            }
            
            _webSocketCts = null;
            ManagedConnection = null;
            
            _isConnected = false;
            
            Logger.LogInformation("Disconnected from {ExchangeId} with enhanced WebSocket management", ExchangeId);
        }
        catch (Exception ex)
        {
            // Log but don't rethrow to ensure cleanup continues
            Logger.LogError(ex, "Error during disconnect for {ExchangeId}", ExchangeId);
            
            // Make sure we set disconnected state even if there were errors
            _isConnected = false;
        }
    }

    /// <inheritdoc />
    public virtual async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuthenticated)
        {
            Logger.LogDebug("Client for {ExchangeId} is already authenticated", ExchangeId);
            return;
        }
        
        Logger.LogInformation("Authenticating with {ExchangeId}...", ExchangeId);
        
        // Get exchange configuration
        var config = await ConfigurationService.GetExchangeConfigurationAsync(ExchangeId, cancellationToken);
        if (config == null)
        {
            throw new InvalidOperationException($"Configuration for {ExchangeId} not found");
        }
        
        // Check if we have API credentials
        if (string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.ApiSecret))
        {
            throw new InvalidOperationException($"API credentials for {ExchangeId} not found in configuration");
        }
        
        // Authenticate with derived class implementation
        await AuthenticateWithCredentialsAsync(config.ApiKey, config.ApiSecret, cancellationToken);
        
        _isAuthenticated = true;
        Logger.LogInformation("Authenticated with {ExchangeId}", ExchangeId);
        
        return;
    }
    
    /// <summary>
    /// Authenticates with the exchange API using the provided credentials.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    /// <param name="apiSecret">The API secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task AuthenticateWithCredentialsAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        // Derived classes should override this method to implement authentication
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public virtual bool IsConnected => _isConnected;
    
    /// <inheritdoc />
    public virtual bool IsAuthenticated => _isAuthenticated;
    
    /// <inheritdoc />
    public virtual Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        // Check if we have a cached order book
        if (OrderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            return Task.FromResult(orderBook);
        }

        // Concrete implementations should override this to fetch an initial snapshot
        throw new NotImplementedException($"GetOrderBookSnapshotAsync is not implemented for {ExchangeId}");
    }
    
    /// <inheritdoc />
    public virtual Task<Order> PlaceMarketOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("PlaceMarketOrderAsync is not implemented in the base class");
    }
    
    /// <inheritdoc />
    public virtual Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetBalancesAsync is not implemented in the base class");
    }
    
    /// <inheritdoc />
    public abstract Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public virtual Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        Logger.LogInformation("Subscribing to order book for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        // Create a channel for this trading pair if it doesn't exist
        if (!OrderBookChannels.TryGetValue(tradingPair, out _))
        {
            OrderBookChannels[tradingPair] = Channel.CreateBounded<OrderBook>(new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            });
            
            // Derived classes should override this to implement subscription logic
            // They should fetch an initial snapshot and then handle updates
        }
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public virtual Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        // Don't validate connection - we may be disconnecting
        if (!_isConnected)
        {
            Logger.LogDebug("Not unsubscribing from order book for {TradingPair} on {ExchangeId} - not connected", tradingPair, ExchangeId);
            return Task.CompletedTask;
        }
        
        Logger.LogInformation("Unsubscribing from order book for {TradingPair} on {ExchangeId}", tradingPair, ExchangeId);
        
        // Derived classes should override this to implement unsubscription logic
        
        // Remove the channel
        if (OrderBookChannels.TryGetValue(tradingPair, out var channel))
        {
            if (OrderBookChannels.Remove(tradingPair))
            {
                channel.Writer.Complete();
            }
        }
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public virtual async IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(
        TradingPair tradingPair, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        // Subscribe if not already subscribed
        if (!OrderBookChannels.TryGetValue(tradingPair, out var channel))
        {
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            if (!OrderBookChannels.TryGetValue(tradingPair, out channel))
            {
                yield break;
            }
        }
        
        // Stream updates from the channel
        await foreach (var orderBook in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return orderBook;
        }
    }
    
    /// <summary>
    /// Gets the balance for a specific currency.
    /// </summary>
    /// <param name="currency">The currency to get the balance for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The balance.</returns>
    public virtual async Task<Balance> GetBalanceAsync(string currency, CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        // Check if we have a cached balance
        if (Balances.TryGetValue(currency, out var balance))
        {
            return balance;
        }
        
        // Get all balances and then return the requested one
        var balances = await GetBalancesAsync(cancellationToken);
        var foundBalance = balances.FirstOrDefault(b => b.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase));
        
        // Create a new Balance if none was found
        if (foundBalance.Currency == null)
        {
            return new Balance(ExchangeId, currency, 0, 0, 0);
        }
        
        return foundBalance;
    }
    
    /// <summary>
    /// Gets all account balances.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All balances.</returns>
    public virtual Task<IReadOnlyCollection<Balance>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        return GetBalancesAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public virtual Task<TradeResult> PlaceLimitOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal price, 
        decimal quantity, 
        OrderType orderType = OrderType.Limit, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("PlaceLimitOrderAsync is not implemented in the base class");
    }
    
    /// <summary>
    /// Gets the latest order book for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get the order book for.</param>
    /// <returns>The latest order book, or null if not available.</returns>
    public virtual OrderBook? GetLatestOrderBook(TradingPair tradingPair)
    {
        return OrderBooks.TryGetValue(tradingPair, out var orderBook) ? orderBook : null;
    }
    
    /// <summary>
    /// Legacy method for WebSocket message processing.
    /// This method is deprecated - message processing is now handled automatically 
    /// by the ManagedWebSocketConnection through event handlers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Obsolete("This method is deprecated. WebSocket messages are now processed automatically through ManagedWebSocketConnection events.")]
    protected virtual async Task ProcessWebSocketMessagesAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Legacy ProcessWebSocketMessagesAsync called for {ExchangeId}. " +
                             "WebSocket messages are now processed automatically through managed connections.", ExchangeId);
        
        // Keep the method for backward compatibility but it doesn't do anything
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Processes a WebSocket message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task ProcessWebSocketMessageAsync(string message, CancellationToken cancellationToken)
    {
        // Derived classes should override this to handle exchange-specific messages
        Logger.LogTrace("Received WebSocket message: {Message}", message);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Sends a WebSocket message through the managed connection.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task SendWebSocketMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (ManagedConnection == null)
        {
            Logger.LogWarning("Cannot send WebSocket message - ManagedConnection is null");
            return;
        }
        
        if (!ManagedConnection.IsConnected)
        {
            Logger.LogWarning("Cannot send WebSocket message - connection is not open (current state: {State})", 
                ManagedConnection.State);
            return;
        }
        
        // Check for cancellation before trying to send
        if (cancellationToken.IsCancellationRequested)
        {
            Logger.LogDebug("Skipping WebSocket message send - cancellation requested");
            return;
        }
        
        try
        {
            await ManagedConnection.SendMessageAsync(message, cancellationToken);
            Logger.LogTrace("Sent WebSocket message: {Message}", message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown, log at debug level
            Logger.LogDebug("WebSocket message send canceled - shutdown in progress");
        }
        catch (InvalidOperationException ex)
        {
            // Connection not available - log gracefully
            Logger.LogWarning(ex, "WebSocket connection not available while sending message: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            // Log other errors but don't rethrow - WebSocket errors shouldn't break the app
            Logger.LogError(ex, "Error sending WebSocket message: {Message}", ex.Message);
        }
    }
    
    /// <summary>
    /// Triggers a manual reconnection of the managed WebSocket connection.
    /// Note: Reconnection is normally handled automatically by the ManagedWebSocketConnection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ReconnectWebSocketAsync(CancellationToken cancellationToken)
    {
        if (WebSocketUrl == null || ConnectionManager == null)
        {
            Logger.LogError("WebSocketUrl or ConnectionManager is null, cannot reconnect for {ExchangeId}", ExchangeId);
            return;
        }
        
        Logger.LogInformation("Manually triggering WebSocket reconnection for {ExchangeId}", ExchangeId);
        
        try
        {
            // Remove the current connection (forces reconnection)
            await ConnectionManager.RemoveConnectionAsync(ExchangeId);
            
            // Get a new managed connection
            ManagedConnection = await ConnectionManager.GetConnectionAsync(
                ExchangeId, 
                WebSocketUrl, 
                ConfigureWebSocketOptions,
                cancellationToken);
            
            // Set up event handlers again
            ManagedConnection.OnMessageReceived += OnWebSocketMessageReceived;
            ManagedConnection.OnError += OnWebSocketError;
            ManagedConnection.OnConnected += OnWebSocketConnected;
            ManagedConnection.OnDisconnected += OnWebSocketDisconnected;
            
            Logger.LogInformation("WebSocket reconnected for {ExchangeId} with enhanced management", ExchangeId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during manual WebSocket reconnection for {ExchangeId}", ExchangeId);
            throw;
        }
    }
    
    /// <summary>
    /// Validates that the client is connected.
    /// </summary>
    protected void ValidateConnected()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException($"Client for {ExchangeId} is not connected");
        }
    }
    
    /// <summary>
    /// Validates that the client is authenticated.
    /// </summary>
    protected void ValidateAuthenticated()
    {
        ValidateConnected();
        
        if (!_isAuthenticated)
        {
            throw new InvalidOperationException($"Client for {ExchangeId} is not authenticated");
        }
    }
    
    /// <summary>
    /// Ensures that the client is authenticated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuthenticated)
        {
            return;
        }
        
        // Get exchange configuration
        var config = await ConfigurationService.GetExchangeConfigurationAsync(ExchangeId, cancellationToken);
        if (config == null)
        {
            throw new InvalidOperationException($"Configuration for {ExchangeId} not found");
        }
        
        // Check if we have API credentials
        if (string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.ApiSecret))
        {
            throw new InvalidOperationException($"API credentials for {ExchangeId} not found in configuration");
        }
        
        // Authenticate
        await AuthenticateAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public virtual async Task<decimal> GetTradingFeeRateAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        // Ensure connected and authenticated
        await EnsureAuthenticatedAsync(cancellationToken);
        
        // Get the fee schedule for the exchange
        var feeSchedule = await GetFeeScheduleAsync(cancellationToken);
        
        // Return the taker fee rate as a default
        return feeSchedule.TakerFeeRate;
    }

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes the exchange client and all managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the exchange client resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // Disconnect gracefully
                if (_isConnected)
                {
                    DisconnectAsync().GetAwaiter().GetResult();
                }
                
                // Dispose connection manager
                ConnectionManager?.Dispose();
                
                // Dispose cancellation token source
                _webSocketCts?.Dispose();
                
                Logger.LogInformation("Disposed exchange client for {ExchangeId}", ExchangeId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing exchange client for {ExchangeId}", ExchangeId);
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #endregion
} 