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
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Base implementation of <see cref="IExchangeClient"/>.
/// </summary>
public abstract class BaseExchangeClient : IExchangeClient
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
    /// Gets or sets the WebSocket client.
    /// </summary>
    protected ClientWebSocket? WebSocketClient;
    
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
    /// Gets or sets a value indicating whether the WebSocket is processing messages.
    /// </summary>
    protected bool _isProcessingMessages;
    
    /// <summary>
    /// Gets or sets the WebSocket processing task.
    /// </summary>
    protected Task? _webSocketProcessingTask;
    
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
            if (config?.WebSocketUrl != null)
            {
                WebSocketUrl = config.WebSocketUrl;
                
                // Initialize WebSocket client
                WebSocketClient = new ClientWebSocket();
                _webSocketCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                // Connect to WebSocket
                await WebSocketClient.ConnectAsync(new Uri(WebSocketUrl), _webSocketCts.Token);
                
                // Start processing WebSocket messages
                _isProcessingMessages = true;
                _webSocketProcessingTask = Task.Run(() => ProcessWebSocketMessagesAsync(_webSocketCts.Token), _webSocketCts.Token);
                
                Logger.LogInformation("WebSocket connected to {ExchangeId} at {WebSocketUrl}", ExchangeId, WebSocketUrl);
            }
            
            _isConnected = true;
            Logger.LogInformation("Connected to {ExchangeId}", ExchangeId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error connecting to {ExchangeId}", ExchangeId);
            throw;
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
            // Cancel WebSocket processing
            if (_webSocketCts != null && !_webSocketCts.IsCancellationRequested)
            {
                _webSocketCts.Cancel();
            }
            
            // Close WebSocket connection
            if (WebSocketClient != null && WebSocketClient.State == WebSocketState.Open)
            {
                await WebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", cancellationToken);
            }
            
            // Wait for WebSocket processing task to complete
            if (_webSocketProcessingTask != null)
            {
                try
                {
                    await _webSocketProcessingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error waiting for WebSocket processing task to complete for {ExchangeId}", ExchangeId);
                }
                
                _webSocketProcessingTask = null;
            }
            
            // Dispose WebSocket resources
            WebSocketClient?.Dispose();
            WebSocketClient = null;
            
            _webSocketCts?.Dispose();
            _webSocketCts = null;
            
            _isProcessingMessages = false;
            _isConnected = false;
            
            Logger.LogInformation("Disconnected from {ExchangeId}", ExchangeId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disconnecting from {ExchangeId}", ExchangeId);
            throw;
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
            OrderBookChannels[tradingPair] = Channel.CreateUnbounded<OrderBook>();
            
            // Derived classes should override this to implement subscription logic
            // They should fetch an initial snapshot and then handle updates
        }
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public virtual Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
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
    /// Processes WebSocket messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ProcessWebSocketMessagesAsync(CancellationToken cancellationToken)
    {
        if (WebSocketClient == null)
        {
            Logger.LogError("WebSocket client is null");
            return;
        }
        
        var buffer = new byte[16384]; // 16 KB buffer
        var messageBuilder = new StringBuilder();
        
        try
        {
            while (WebSocketClient.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                messageBuilder.Clear();
                
                do
                {
                    result = await WebSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await WebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                        break;
                    }
                    
                    var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(messageChunk);
                }
                while (!result.EndOfMessage);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                
                var message = messageBuilder.ToString();
                
                try
                {
                    // Process the message - derived classes should override this
                    await ProcessWebSocketMessageAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
            Logger.LogInformation("WebSocket processing cancelled for {ExchangeId}", ExchangeId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing WebSocket messages for {ExchangeId}", ExchangeId);
            
            // Try to reconnect
            if (!cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("Attempting to reconnect WebSocket for {ExchangeId}", ExchangeId);
                
                try
                {
                    await ReconnectWebSocketAsync(cancellationToken);
                }
                catch (Exception reconnectEx)
                {
                    Logger.LogError(reconnectEx, "Error reconnecting WebSocket for {ExchangeId}", ExchangeId);
                }
            }
        }
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
    /// Sends a WebSocket message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task SendWebSocketMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (WebSocketClient == null || WebSocketClient.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }
        
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await WebSocketClient.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
        
        Logger.LogTrace("Sent WebSocket message: {Message}", message);
    }
    
    /// <summary>
    /// Reconnects the WebSocket connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ReconnectWebSocketAsync(CancellationToken cancellationToken)
    {
        if (WebSocketUrl == null)
        {
            Logger.LogError("WebSocketUrl is null, cannot reconnect");
            return;
        }
        
        // Disconnect the existing WebSocket
        try
        {
            if (WebSocketClient != null && WebSocketClient.State == WebSocketState.Open)
            {
                await WebSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error closing existing WebSocket connection for {ExchangeId}", ExchangeId);
        }
        
        // Dispose the old WebSocket
        WebSocketClient?.Dispose();
        
        // Create a new WebSocket
        WebSocketClient = new ClientWebSocket();
        
        // Connect to WebSocket
        await WebSocketClient.ConnectAsync(new Uri(WebSocketUrl), cancellationToken);
        
        Logger.LogInformation("WebSocket reconnected to {ExchangeId} at {WebSocketUrl}", ExchangeId, WebSocketUrl);
        
        // Resubscribe to existing order books
        foreach (var tradingPair in OrderBookChannels.Keys.ToList())
        {
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
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
} 