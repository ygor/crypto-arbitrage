using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Exceptions;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Exchange client implementation for Coinbase Pro.
/// </summary>
public class CoinbaseExchangeClient : BaseExchangeClient
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, TradingPair> _subscribedPairs = new();
    
    private string? _apiKey;
    private string? _apiSecret;
    private string? _passphrase;
    private readonly string _baseUrl = "https://api.exchange.coinbase.com";
    private readonly string _wsUrl = "wss://ws-feed.exchange.coinbase.com";
    
    private class CoinbaseOrderBook
    {
        public JArray Bids { get; set; } = new JArray();
        public JArray Asks { get; set; } = new JArray();
        public string Time { get; set; } = string.Empty;
    }
    
    private class CoinbaseAccount
    {
        public string Id { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Balance { get; set; } = string.Empty;
        public string Available { get; set; } = string.Empty;
        public string Hold { get; set; } = string.Empty;
    }

    private class CoinbaseAccountsResponse
    {
        public List<CoinbaseAccount> Accounts { get; set; } = new();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CoinbaseExchangeClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public CoinbaseExchangeClient(
        HttpClient httpClient,
        IConfigurationService configurationService,
        ILogger<CoinbaseExchangeClient> logger)
        : base("coinbase", configurationService, logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        
        // Set WebSocket URL in base class
        WebSocketUrl = _wsUrl;
        
        // Log streaming requirement
        Logger.LogInformation("Coinbase exchange requires authenticated WebSocket connection for real-time order book data");
    }
    
    /// <inheritdoc />
    public override bool SupportsStreaming => true;
    
    /// <inheritdoc />
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            return;
        }
        
        try
        {
            Logger.LogInformation("Connecting to {ExchangeId} WebSocket at {WebSocketUrl}", ExchangeId, _wsUrl);
            
            // Get exchange configuration to retrieve credentials
            var exchangeConfig = await ConfigurationService.GetExchangeConfigurationAsync(ExchangeId, cancellationToken);
            
            if (exchangeConfig != null)
            {
                // Load credentials if they exist, but don't require them for public data feeds
                _apiKey = exchangeConfig.ApiKey;
                _apiSecret = exchangeConfig.ApiSecret;
                
                // Try to get passphrase from additional auth params
                if (exchangeConfig.AdditionalAuthParams != null)
                {
                    exchangeConfig.AdditionalAuthParams.TryGetValue("passphrase", out string? passphrase);
                    _passphrase = passphrase;
                }
                
                // Log what type of connection we're making
                if (!string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_apiSecret) && !string.IsNullOrEmpty(_passphrase))
                {
                    Logger.LogInformation("Credentials found, will use authenticated connection when needed");
                }
                else
                {
                    Logger.LogInformation("No credentials provided or incomplete credentials, will use public connections only");
                }
            }
            
            // Initialize WebSocket client
            WebSocketClient = new ClientWebSocket();
            _webSocketCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Connect to WebSocket
            await WebSocketClient.ConnectAsync(new Uri(_wsUrl), _webSocketCts.Token);
            
            // Start processing WebSocket messages
            _isProcessingMessages = true;
            _webSocketProcessingTask = Task.Run(() => ProcessWebSocketMessagesAsync(_webSocketCts.Token), _webSocketCts.Token);
            
            _isConnected = true;
            Logger.LogInformation("Connected to {ExchangeId} WebSocket at {WebSocketUrl}", ExchangeId, _wsUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error connecting to {ExchangeId}", ExchangeId);
            throw;
        }
    }
    
    /// <inheritdoc />
    protected override async Task AuthenticateWithCredentialsAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        Logger.LogInformation("Authenticating with Coinbase");
        
        try
        {
            // For Coinbase, we also need a passphrase
            var exchangeConfig = await ConfigurationService.GetExchangeConfigurationAsync(ExchangeId, cancellationToken);
            
            if (exchangeConfig == null)
            {
                throw new InvalidOperationException("Coinbase configuration not found");
            }
            
            // Initialize with empty string and try to get from config
            string passphrase = string.Empty;
            bool hasPassphrase = false;
            
            // Check for additional auth params and passphrase
            if (exchangeConfig.AdditionalAuthParams != null)
            {
                hasPassphrase = exchangeConfig.AdditionalAuthParams.TryGetValue("passphrase", out string? configPassphrase);
                if (hasPassphrase && configPassphrase != null)
                {
                    passphrase = configPassphrase;
                }
            }
            
            if (!hasPassphrase || string.IsNullOrEmpty(passphrase))
            {
                throw new InvalidOperationException("Coinbase passphrase not found in configuration");
            }
            
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passphrase = passphrase;
            
            // Test authentication by getting account information
            var endpoint = "/accounts";
            JArray? response = await SendAuthenticatedRequestAsync<JArray>(HttpMethod.Get, endpoint, null, cancellationToken);
            
            if (response != null)
            {
                Logger.LogInformation("Authentication with Coinbase successful");
                
                // Cache balances
                await GetAllBalancesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Authentication with Coinbase failed: Empty response");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating with Coinbase");
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        // First check if we already have an order book for this pair
        if (OrderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            return orderBook;
        }
        
        // Get properly formatted Coinbase trading pair
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        Logger.LogInformation("Getting order book snapshot for {TradingPair} ({Symbol}) from Coinbase", 
            tradingPair, symbol);
        
        try 
        {
            // For public order book data, we don't need authentication
            // Subscribe to the public level2 feed
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            // Wait for a short time to receive the initial snapshot
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
            
            // Wait for the order book to be received
            while (!OrderBooks.TryGetValue(tradingPair, out orderBook) && !linkedCts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, linkedCts.Token);
            }
            
            if (orderBook == null)
            {
                throw new ExchangeClientException(ExchangeId, 
                    $"Failed to get order book for {tradingPair} ({symbol}) on Coinbase: Timed out waiting for WebSocket snapshot");
            }
            
            return orderBook;
        }
        catch (OperationCanceledException)
        {
            throw new ExchangeClientException(ExchangeId, 
                $"Failed to get order book for {tradingPair} ({symbol}) on Coinbase: Operation was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting order book snapshot for {TradingPair} ({Symbol})", tradingPair, symbol);
            throw new ExchangeClientException(ExchangeId, 
                $"Failed to get order book for {tradingPair} ({symbol}) on Coinbase: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches order book data from Coinbase's public REST API.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="depth">The depth of the order book.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The order book.</returns>
    public async Task<OrderBook> FetchCoinbaseOrderBookAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        
        // Coinbase Pro public API endpoint for order book
        var level = depth <= 50 ? 2 : 3; // level=2 for top 50, level=3 for full book
        var url = $"{_baseUrl}/products/{symbol}/book?level={level}";
        
        Logger.LogInformation("Fetching order book from Coinbase public API for {TradingPair} ({Symbol}) at {Url}", 
            tradingPair, symbol, url);
        
        try 
        {
            var response = await _httpClient.GetFromJsonAsync<CoinbaseOrderBook>(url, cancellationToken);
            
            if (response == null)
            {
                Logger.LogWarning("Received null response from Coinbase order book request for {TradingPair}", tradingPair);
                throw new Exception($"Failed to fetch order book from Coinbase for {tradingPair}");
            }
            
            // Parse the response
            var bids = ParseOrderBookLevels(response.Bids, OrderSide.Buy);
            var asks = ParseOrderBookLevels(response.Asks, OrderSide.Sell);
            
            // Limit to requested depth
            bids = bids.Take(depth).ToList();
            asks = asks.Take(depth).ToList();
            
            Logger.LogInformation("Successfully fetched order book from Coinbase public API for {TradingPair}: {BidCount} bids, {AskCount} asks", 
                tradingPair, bids.Count, asks.Count);
            
            var orderBook = new OrderBook(ExchangeId, tradingPair, DateTime.UtcNow, bids, asks);
            
            // Cache the order book
            OrderBooks[tradingPair] = orderBook;
            
            return orderBook;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP error fetching order book from Coinbase for {TradingPair} ({Symbol}): {ErrorMessage}", 
                tradingPair, symbol, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching order book from Coinbase for {TradingPair} ({Symbol}): {ErrorMessage}", 
                tradingPair, symbol, ex.Message);
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        if (OrderBookChannels.ContainsKey(tradingPair))
        {
            Logger.LogInformation("Already subscribed to order book for {TradingPair} on Coinbase", tradingPair);
            return;
        }

        var channel = Channel.CreateUnbounded<OrderBook>();
        OrderBookChannels[tradingPair] = channel;

        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        
        try
        {
            Logger.LogInformation("Subscribing to order book for {TradingPair} ({Symbol}) on Coinbase", tradingPair, symbol);
            
            // Validate symbol format for Coinbase (should be BASE-QUOTE)
            if (!symbol.Contains('-'))
            {
                Logger.LogWarning("Invalid Coinbase symbol format: {Symbol}. Expected format: BASE-QUOTE (e.g., BTC-USDT)", symbol);
                // Try to correct the symbol format
                symbol = $"{baseCurrency}-{quoteCurrency}";
                Logger.LogInformation("Corrected symbol to: {Symbol}", symbol);
            }
            
            // Check if the symbol is supported by fetching available products first
            var isSymbolValid = await ValidateTradingPairAsync(symbol, cancellationToken);
            if (!isSymbolValid)
            {
                Logger.LogError("Trading pair {Symbol} is not supported by Coinbase or does not exist", symbol);
                CleanupOrderBookResources(tradingPair);
                throw new InvalidOperationException($"Trading pair {symbol} is not supported by Coinbase");
            }

            // Add to subscribed pairs
            _subscribedPairs[symbol] = tradingPair;

            // Create subscription message for public level2 data (no authentication needed)
            var subscribeMessage = new
            {
                type = "subscribe",
                product_ids = new[] { symbol },
                channels = new[]
                {
                    new
                    {
                        name = "level2",
                        product_ids = new[] { symbol }
                    },
                    new
                    {
                        name = "heartbeat",
                        product_ids = new[] { symbol }
                    }
                }
            };

            var messageJson = JsonConvert.SerializeObject(subscribeMessage);
            
            // Send subscription message
            if (WebSocketClient?.State == WebSocketState.Open)
            {
                Logger.LogInformation("Sending subscription for {Symbol} with message: {Message}", symbol, messageJson);
                await SendWebSocketMessageAsync(messageJson, cancellationToken);
                Logger.LogInformation("Subscription request sent for {Symbol}", symbol);
                
                // Wait a moment to see if we get a subscription confirmation or error
                await Task.Delay(1000, cancellationToken);
            }
            else
            {
                Logger.LogWarning("WebSocket not connected, cannot subscribe to {Symbol}", symbol);
                CleanupOrderBookResources(tradingPair);
                throw new InvalidOperationException($"WebSocket not connected for {ExchangeId}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to order book for {TradingPair} ({Symbol}) on Coinbase", tradingPair, symbol);
            // Clean up on error
            _subscribedPairs.Remove(symbol);
            CleanupOrderBookResources(tradingPair);
            throw;
        }
    }
    
    /// <summary>
    /// Validates if a trading pair is supported by Coinbase by checking available products.
    /// </summary>
    /// <param name="symbol">The trading pair symbol (e.g., BTC-USDT).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the trading pair is valid, false otherwise.</returns>
    private async Task<bool> ValidateTradingPairAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Validating trading pair {Symbol} with Coinbase API", symbol);
            
            // Fetch the specific product to check if it exists
            var response = await _httpClient.GetAsync($"/products/{symbol}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                Logger.LogDebug("Trading pair {Symbol} is valid on Coinbase", symbol);
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogWarning("Trading pair {Symbol} not found on Coinbase (404)", symbol);
                return false;
            }
            else
            {
                Logger.LogWarning("Error validating trading pair {Symbol}: HTTP {StatusCode}", symbol, response.StatusCode);
                // Assume it's valid if we can't validate due to other errors
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not validate trading pair {Symbol}, assuming it's valid", symbol);
            // Assume it's valid if validation fails
            return true;
        }
    }
    
    /// <inheritdoc />
    public override async Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || WebSocketClient == null)
        {
            Logger.LogWarning("Cannot unsubscribe - client for {ExchangeId} is not connected", ExchangeId);
            CleanupOrderBookResources(tradingPair);
            return;
        }
        
        // Get properly formatted Coinbase trading pair
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        Logger.LogInformation("Unsubscribing from order book for {TradingPair} ({Symbol}) on Coinbase", tradingPair, symbol);
        
        try
        {
            // Check if we need to send an unsubscribe message
            if (_subscribedPairs.Remove(symbol) && WebSocketClient.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Unsubscribe from WebSocket feed with a timeout to prevent hanging
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // Short timeout for shutdown
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                    
                    // Unsubscribe from WebSocket feed
                    var unsubscribeMessage = new
                    {
                        type = "unsubscribe",
                        product_ids = new[] { symbol },
                        channels = new object[] 
                        { 
                            "level2",
                            "heartbeat"
                        }
                    };
                    
                    var messageJson = System.Text.Json.JsonSerializer.Serialize(unsubscribeMessage);
                    await SendWebSocketMessageAsync(messageJson, linkedCts.Token);
                    Logger.LogDebug("Successfully sent unsubscribe message for {Symbol}", symbol);
                }
                catch (OperationCanceledException)
                {
                    // This is expected during shutdown, just continue with cleanup
                    Logger.LogDebug("Unsubscribe message canceled (likely during shutdown) for {TradingPair} ({Symbol})", tradingPair, symbol);
                }
                catch (Exception ex)
                {
                    // Just log the error but continue with cleanup
                    Logger.LogWarning(ex, "Error sending unsubscribe message for {TradingPair} ({Symbol}), continuing with cleanup", tradingPair, symbol);
                }
            }
            
            // Always clean up resources even if sending the message fails
            CleanupOrderBookResources(tradingPair);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from order book for {TradingPair} ({Symbol}) on Coinbase", tradingPair, symbol);
            
            // Still try to clean up resources
            try
            {
                CleanupOrderBookResources(tradingPair);
            }
            catch (Exception cleanupEx)
            {
                Logger.LogError(cleanupEx, "Error cleaning up resources for {TradingPair}", tradingPair);
            }
        }
    }
    
    /// <summary>
    /// Cleans up order book resources for a trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    private void CleanupOrderBookResources(TradingPair tradingPair)
    {
        // Remove the channel and complete it
        if (OrderBookChannels.TryGetValue(tradingPair, out var channel))
        {
            try
            {
                channel.Writer.Complete();
                OrderBookChannels.Remove(tradingPair);
                Logger.LogDebug("Removed order book channel for {TradingPair}", tradingPair);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error completing channel for {TradingPair}", tradingPair);
            }
        }
        
        // Remove from order books
        if (OrderBooks.Remove(tradingPair))
        {
            Logger.LogDebug("Removed order book for {TradingPair}", tradingPair);
        }
    }
    
    /// <inheritdoc />
    protected override async Task ProcessWebSocketMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // Parse the message
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            
            // Check if it's a level2 message
            if (root.TryGetProperty("type", out var typeElement))
            {
                var messageType = typeElement.GetString();
                
                switch (messageType)
                {
                    case "l2update":
                        await ProcessLevel2UpdateAsync(root, cancellationToken);
                        break;
                    case "snapshot":
                        await ProcessSnapshotAsync(root, cancellationToken);
                        break;
                    case "heartbeat":
                        // Just a heartbeat, no need to process
                        break;
                    case "subscriptions":
                        Logger.LogInformation("Received subscriptions confirmation from Coinbase: {Message}", message);
                        
                        // Log subscribed channels for diagnostic purposes
                        if (root.TryGetProperty("channels", out var channelsElement) && 
                            channelsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var channel in channelsElement.EnumerateArray())
                            {
                                if (channel.TryGetProperty("name", out var nameElement) && 
                                    channel.TryGetProperty("product_ids", out var productIdsElement) &&
                                    productIdsElement.ValueKind == JsonValueKind.Array)
                                {
                                    var channelName = nameElement.GetString();
                                    var productIds = string.Join(", ", productIdsElement.EnumerateArray()
                                        .Select(p => p.GetString())
                                        .Where(p => p != null));
                                    
                                    Logger.LogInformation("Coinbase subscribed to channel {ChannelName} for products: {ProductIds}", 
                                        channelName, productIds);
                                }
                            }
                        }
                        break;
                    case "error":
                        await ProcessErrorMessageAsync(root, message, cancellationToken);
                        break;
                    default:
                        Logger.LogWarning("Received unknown message type from Coinbase WebSocket: {Type}, {Message}", messageType, message);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing WebSocket message from Coinbase: {Message}", message);
        }
    }
    
    private async Task ProcessLevel2UpdateAsync(JsonElement root, CancellationToken cancellationToken)
    {
        try
        {
            if (!root.TryGetProperty("product_id", out var productIdElement))
            {
                return;
            }
            
            var productId = productIdElement.GetString();
            if (productId == null || !_subscribedPairs.TryGetValue(productId, out var tradingPair))
            {
                return;
            }
            
            // Check if we have the order book
            if (!OrderBooks.TryGetValue(tradingPair, out var orderBook))
            {
                // Skip this update if we don't have an order book yet
                // The snapshot will come first in the WebSocket feed
                Logger.LogWarning("Received level2 update for {TradingPair} but no order book snapshot yet", tradingPair);
                return;
            }
            
            // Process the updates
            if (root.TryGetProperty("changes", out var changesElement) && changesElement.ValueKind == JsonValueKind.Array)
            {
                // Convert to mutable lists for updating
                var bids = new List<OrderBookEntry>(orderBook.Bids);
                var asks = new List<OrderBookEntry>(orderBook.Asks);
                
                foreach (var change in changesElement.EnumerateArray())
                {
                    if (change.ValueKind != JsonValueKind.Array || change.GetArrayLength() != 3)
                    {
                        continue;
                    }
                    
                    var side = change[0].GetString();
                    var priceStr = change[1].GetString();
                    var sizeStr = change[2].GetString();
                    
                    if (side == null || priceStr == null || sizeStr == null)
                    {
                        continue;
                    }
                    
                    if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        continue;
                    }
                    
                    if (!decimal.TryParse(sizeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var size))
                    {
                        continue;
                    }
                    
                    // Update or remove the entry
                    if (side == "buy")
                    {
                        UpdateOrderBookSide(bids, price, size);
                    }
                    else if (side == "sell")
                    {
                        UpdateOrderBookSide(asks, price, size);
                    }
                }
                
                // Sort the order book
                bids = bids.OrderByDescending(e => e.Price).Take(100).ToList();
                asks = asks.OrderBy(e => e.Price).Take(100).ToList();
                
                // Create a new order book
                var updatedOrderBook = new OrderBook(
                    ExchangeId,
                    tradingPair,
                    DateTime.UtcNow,
                    bids,
                    asks);
                
                // Update the cached order book
                OrderBooks[tradingPair] = updatedOrderBook;
                
                // Publish the updated order book
                if (OrderBookChannels.TryGetValue(tradingPair, out var channel))
                {
                    await channel.Writer.WriteAsync(updatedOrderBook, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing level2 update from Coinbase");
        }
    }
    
    private async Task ProcessSnapshotAsync(JsonElement root, CancellationToken cancellationToken)
    {
        try
        {
            if (!root.TryGetProperty("product_id", out var productIdElement))
            {
                Logger.LogWarning("Received snapshot without product_id from Coinbase");
                return;
            }
            
            var productId = productIdElement.GetString();
            if (productId == null || !_subscribedPairs.TryGetValue(productId, out var tradingPair))
            {
                Logger.LogWarning("Received snapshot for unknown product_id from Coinbase: {ProductId}", productId);
                return;
            }
            
            Logger.LogInformation("Received order book snapshot from Coinbase WebSocket for {ProductId}", productId);
            
            var bids = new List<OrderBookEntry>();
            var asks = new List<OrderBookEntry>();
            
            // Process bids
            if (root.TryGetProperty("bids", out var bidsElement) && bidsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var bid in bidsElement.EnumerateArray())
                {
                    if (bid.ValueKind != JsonValueKind.Array || bid.GetArrayLength() < 2)
                    {
                        continue;
                    }
                    
                    var priceStr = bid[0].GetString();
                    var sizeStr = bid[1].GetString();
                    
                    if (priceStr == null || sizeStr == null)
                    {
                        continue;
                    }
                    
                    if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        continue;
                    }
                    
                    if (!decimal.TryParse(sizeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var size))
                    {
                        continue;
                    }
                    
                    if (price > 0 && size > 0)
                    {
                        bids.Add(new OrderBookEntry(price, size));
                    }
                }
            }
            
            // Process asks
            if (root.TryGetProperty("asks", out var asksElement) && asksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ask in asksElement.EnumerateArray())
                {
                    if (ask.ValueKind != JsonValueKind.Array || ask.GetArrayLength() < 2)
                    {
                        continue;
                    }
                    
                    var priceStr = ask[0].GetString();
                    var sizeStr = ask[1].GetString();
                    
                    if (priceStr == null || sizeStr == null)
                    {
                        continue;
                    }
                    
                    if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        continue;
                    }
                    
                    if (!decimal.TryParse(sizeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var size))
                    {
                        continue;
                    }
                    
                    if (price > 0 && size > 0)
                    {
                        asks.Add(new OrderBookEntry(price, size));
                    }
                }
            }
            
            // Sort the order book
            bids = bids.OrderByDescending(e => e.Price).Take(100).ToList();
            asks = asks.OrderBy(e => e.Price).Take(100).ToList();
            
            Logger.LogInformation("Successfully processed order book snapshot for {ProductId} with {BidCount} bids and {AskCount} asks", 
                productId, bids.Count, asks.Count);
            
            // Create a new order book
            var orderBook = new OrderBook(
                ExchangeId,
                tradingPair,
                DateTime.UtcNow,
                bids,
                asks);
            
            // Update the cached order book
            OrderBooks[tradingPair] = orderBook;
            
            // Publish the updated order book
            if (OrderBookChannels.TryGetValue(tradingPair, out var channel))
            {
                await channel.Writer.WriteAsync(orderBook, cancellationToken);
                Logger.LogInformation("Published initial order book for {TradingPair} from WebSocket snapshot", tradingPair);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing snapshot from Coinbase");
        }
    }
    
    private void UpdateOrderBookSide(List<OrderBookEntry> entries, decimal price, decimal size)
    {
        // Find the entry with the matching price
        var index = entries.FindIndex(e => e.Price == price);
        
        if (size == 0)
        {
            // Remove the entry if size is 0
            if (index >= 0)
            {
                entries.RemoveAt(index);
            }
        }
        else
        {
            // Update or add the entry
            if (index >= 0)
            {
                entries[index] = new OrderBookEntry(price, size);
            }
            else
            {
                entries.Add(new OrderBookEntry(price, size));
            }
        }
    }
    
    /// <inheritdoc />
    public override async Task<Balance> GetBalanceAsync(string currency, CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        // Normalize currency
        currency = currency.ToUpperInvariant();
        
        // If we have a cached balance, return it
        if (Balances.TryGetValue(currency, out var balance))
        {
            return balance;
        }
        
        // Otherwise, get all balances
        await GetAllBalancesAsync(cancellationToken);
        
        // Return the balance or a zero balance if not found
        return Balances.TryGetValue(currency, out balance)
            ? balance
            : new Balance(ExchangeId, currency, 0, 0, 0);
    }
    
    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        List<Balance> balances = new List<Balance>();
        
        try
        {
            // Fetch accounts from Coinbase API
            var accounts = await FetchAccountsInternalAsync(cancellationToken);
            
            if (accounts == null)
            {
                Logger.LogWarning("No accounts returned from Coinbase");
                return Array.Empty<Balance>();
            }
            
            // Map to Balance objects
            foreach (var account in accounts)
            {
                var currency = account.Currency;
                if (string.IsNullOrEmpty(currency))
                {
                    continue;
                }
                
                if (decimal.TryParse(account.Balance, NumberStyles.Any, CultureInfo.InvariantCulture, out var total) &&
                    decimal.TryParse(account.Available, NumberStyles.Any, CultureInfo.InvariantCulture, out var available) &&
                    decimal.TryParse(account.Hold, NumberStyles.Any, CultureInfo.InvariantCulture, out var hold))
                {
                    var balance = new Balance(ExchangeId, currency, total, available, hold);
                    balances.Add(balance);
                    Balances[currency] = balance;
                }
            }
            
            Logger.LogInformation("Retrieved {Count} balances from Coinbase", balances.Count);
            return balances;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting balances from Coinbase");
            throw;
        }
    }
    
    /// <summary>
    /// Fetches the accounts from Coinbase.
    /// </summary>
    private async Task<List<CoinbaseAccount>?> FetchAccountsInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendAuthenticatedRequestAsync<CoinbaseAccountsResponse>(
                HttpMethod.Get,
                "/accounts",
                null,
                cancellationToken);
            
            return response?.Accounts;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching accounts from Coinbase");
            return null;
        }
    }
    
    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Balance>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        try
        {
            var accounts = await FetchAccountsInternalAsync(cancellationToken);
            
            if (accounts == null || accounts.Count == 0)
            {
                return Balances.Values.ToList().AsReadOnly();
            }
            
            foreach (var account in accounts)
            {
                if (decimal.TryParse(account.Available, NumberStyles.Any, CultureInfo.InvariantCulture, out var available) &&
                    decimal.TryParse(account.Hold, NumberStyles.Any, CultureInfo.InvariantCulture, out var hold))
                {
                    var total = available + hold;
                    
                    if (!string.IsNullOrEmpty(account.Currency))
                    {
                        Balances[account.Currency] = new Balance(ExchangeId, account.Currency, total, available, hold);
                    }
                }
            }
            
            return Balances.Values.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting balances from Coinbase");
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        try
        {
            // Attempt to get the actual fee structure from Coinbase
            var endpoint = "/fees";
            var response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Get, endpoint, null, cancellationToken);
            
            if (response != null && 
                response.TryGetValue("maker_fee_rate", out var makerFeeToken) &&
                response.TryGetValue("taker_fee_rate", out var takerFeeToken))
            {
                var makerFee = decimal.Parse(makerFeeToken.ToString(), CultureInfo.InvariantCulture);
                var takerFee = decimal.Parse(takerFeeToken.ToString(), CultureInfo.InvariantCulture);
                
                return new FeeSchedule(ExchangeId, makerFee, takerFee);
            }
            
            // Fall back to default fees if we can't get the actual fee structure
            Logger.LogWarning("Could not get fee structure from Coinbase, using default fees");
            return new FeeSchedule(ExchangeId, 0.0050m, 0.0050m);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting fee schedule from Coinbase");
            
            // Fall back to default fees
            return new FeeSchedule(ExchangeId, 0.0050m, 0.0050m);
        }
    }
    
    /// <inheritdoc />
    public override async Task<Order> PlaceMarketOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal quantity, 
        CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        var side = ExchangeUtils.FormatOrderSide(orderSide, ExchangeId);
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        Logger.LogInformation("Placing market {Side} order for {Quantity} {TradingPair} on Coinbase", 
            orderSide, quantity, tradingPair);
        
        try
        {
            var orderRequest = new
            {
                client_oid = clientOrderId,
                product_id = symbol,
                side = side,
                type = "market",
                size = quantity.ToString(CultureInfo.InvariantCulture)
            };
            
            var endpoint = "/orders";
            JObject? response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, orderRequest, cancellationToken);
            
            if (response == null)
            {
                Logger.LogError("Failed to place market order on Coinbase");
                throw new Exception("Failed to place market order on Coinbase");
            }
            
            var orderId = response["id"]?.ToString() ?? clientOrderId;
            var price = decimal.Parse(response["price"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            var filledSize = decimal.Parse(response["filled_size"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            
            Logger.LogInformation("Market order placed on Coinbase: OrderId={OrderId}, Price={Price}, FilledSize={FilledSize}", 
                orderId, price, filledSize);
            
            var order = new Order(
                orderId,
                ExchangeId,
                tradingPair,
                orderSide,
                OrderType.Market,
                filledSize >= quantity ? OrderStatus.Filled : OrderStatus.PartiallyFilled,
                price,
                quantity,
                DateTime.UtcNow);
            
            // Set filled quantity
            order.FilledQuantity = filledSize;
            order.AverageFillPrice = price;
            
            return order;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error placing market order on Coinbase");
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task<TradeResult> PlaceLimitOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal price, 
        decimal quantity, 
        OrderType orderType = OrderType.Limit, 
        CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        var side = ExchangeUtils.FormatOrderSide(orderSide, ExchangeId);
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        Logger.LogInformation("Placing limit {Side} order for {Quantity} {Price} {TradingPair} on Coinbase", 
            orderSide, quantity, price, tradingPair);
        
        try
        {
            var orderRequest = new
            {
                client_oid = clientOrderId,
                product_id = symbol,
                side = side,
                type = "limit",
                price = price.ToString(CultureInfo.InvariantCulture),
                size = quantity.ToString(CultureInfo.InvariantCulture)
            };
            
            var endpoint = "/orders";
            JObject? response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, orderRequest, cancellationToken);
            
            if (response == null)
            {
                Logger.LogError("Failed to place limit order on Coinbase");
                return TradeResult.Failure("Failed to place limit order on Coinbase", 0);
            }
            
            var orderId = response["id"]?.ToString() ?? clientOrderId;
            var filledSize = decimal.Parse(response["filled_size"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            
            Logger.LogInformation("Limit order placed on Coinbase: OrderId={OrderId}, FilledSize={FilledSize}", 
                orderId, filledSize);
            
            // Create a trade result
            return new TradeResult
            {
                IsSuccess = true,
                OrderId = orderId,
                ClientOrderId = clientOrderId,
                Timestamp = DateTimeOffset.UtcNow.DateTime,
                TradingPair = tradingPair.ToString(),
                TradeType = orderSide == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                RequestedPrice = price,
                ExecutedPrice = price,
                RequestedQuantity = quantity,
                ExecutedQuantity = filledSize,
                TotalValue = price * quantity,
                Fee = (price * quantity) * 0.005m, // Approximate fee
                FeeCurrency = tradingPair.QuoteCurrency
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error placing limit order on Coinbase");
            return TradeResult.Failure(ex, 0);
        }
    }
    
    /// <inheritdoc />
    public async Task<Order?> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var endpoint = $"/orders/{orderId}";
        
        try
        {
            Logger.LogInformation("Requesting order status for {OrderId} from Coinbase at endpoint {Endpoint}", 
                orderId, endpoint);
                
            JObject? response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Get, endpoint, null, cancellationToken);
            
            if (response == null)
            {
                Logger.LogWarning("Received null response from Coinbase order status request for {OrderId}", orderId);
                return null;
            }
            
            var status = response["status"]?.ToString();
            var filledSize = decimal.Parse(response["filled_size"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            var price = decimal.Parse(response["price"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            var filledAt = response["filled_at"] != null 
                ? DateTime.Parse(response["filled_at"]?.ToString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) 
                : DateTime.UtcNow;
            var productId = response["product_id"]?.ToString() ?? "";
            
            // Extract base and quote from product ID
            var currencies = productId.Split('-');
            var baseCurrency = currencies.Length > 0 ? currencies[0] : "";
            var quoteCurrency = currencies.Length > 1 ? currencies[1] : "";
            var tradingPair = new TradingPair(baseCurrency, quoteCurrency);
            
            Logger.LogInformation("Successfully fetched order status for {OrderId} from Coinbase", orderId);
            
            var side = response["side"]?.ToString()?.ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell;
            
            return new Order(
                orderId,
                ExchangeId,
                tradingPair,
                side,
                OrderType.Limit,
                status == "filled" ? OrderStatus.Filled : OrderStatus.PartiallyFilled,
                price,
                filledSize,
                filledAt);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP error fetching order status for {OrderId} from Coinbase: {ErrorMessage}", 
                orderId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching order status for {OrderId} from Coinbase: {ErrorMessage}", 
                orderId, ex.Message);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var endpoint = $"/orders/{orderId}/cancel";
        
        try
        {
            Logger.LogInformation("Canceling order {OrderId} on Coinbase at endpoint {Endpoint}", 
                orderId, endpoint);
                
            await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, null, cancellationToken);
            
            Logger.LogInformation("Order {OrderId} canceled on Coinbase", orderId);
            
            return true;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP error canceling order {OrderId} on Coinbase: {ErrorMessage}", 
                orderId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error canceling order {OrderId} on Coinbase: {ErrorMessage}", 
                orderId, ex.Message);
            return false;
        }
    }
    
    // Private helper methods
    
    private List<OrderBookEntry> ParseOrderBookLevels(JArray levels, OrderSide side)
    {
        var result = new List<OrderBookEntry>();
        
        foreach (var level in levels)
        {
            try
            {
                if (level != null && level.Count() >= 2)
                {
                    // Get the price and size as strings first, then convert to decimal
                    string priceStr = level[0].ToString();
                    string sizeStr = level[1].ToString();
                    
                    if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                        decimal.TryParse(sizeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var size))
                    {
                        if (price > 0 && size > 0)
                        {
                            result.Add(new OrderBookEntry(price, size));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but continue processing other levels
                Logger.LogWarning(ex, "Error parsing order book level: {Level}", level?.ToString() ?? "null");
            }
        }
        
        // Sort bids descending, asks ascending
        if (side == OrderSide.Buy)
        {
            result = result.OrderByDescending(x => x.Price).ToList();
        }
        else
        {
            result = result.OrderBy(x => x.Price).ToList();
        }
        
        return result;
    }
    
    private async Task<T?> SendAuthenticatedRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        object? requestBody,
        CancellationToken cancellationToken)
        where T : class
    {
        if (_apiKey == null || _apiSecret == null || _passphrase == null)
        {
            throw new InvalidOperationException("API credentials not set");
        }
        
        try
        {
            // Generate the timestamp
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            // Create the request message
            var requestUri = new Uri(_baseUrl + endpoint);
            var requestMessage = new HttpRequestMessage(method, requestUri);
            
            // Add request body if provided
            string requestBodyString = "";
            if (requestBody != null)
            {
                requestBodyString = JsonConvert.SerializeObject(requestBody);
                requestMessage.Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json");
            }
            
            // Generate the signature
            var signature = GenerateCoinbaseSignature(
                timestamp,
                method.ToString().ToUpperInvariant(),
                endpoint,
                requestBodyString,
                _apiSecret);
            
            // Add headers
            requestMessage.Headers.Add("CB-ACCESS-KEY", _apiKey);
            requestMessage.Headers.Add("CB-ACCESS-SIGN", signature);
            requestMessage.Headers.Add("CB-ACCESS-TIMESTAMP", timestamp);
            requestMessage.Headers.Add("CB-ACCESS-PASSPHRASE", _passphrase);
            
            // Send the request
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            
            // Check for successful response
            if (response.IsSuccessStatusCode)
            {
                // For Newtonsoft.Json types, use Newtonsoft deserializer instead of System.Text.Json
                if (typeof(T) == typeof(JObject) || typeof(T) == typeof(JArray) || typeof(T) == typeof(JToken))
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    return JsonConvert.DeserializeObject<T>(content);
                }
                
                // For other types use the standard System.Text.Json deserializer
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            
            // Handle error response
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogError("Coinbase API error: {StatusCode} - {ErrorContent}", 
                response.StatusCode, errorContent);
            
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending authenticated request to Coinbase");
            return null;
        }
    }
    
    private string GenerateCoinbaseSignature(
        string timestamp,
        string method,
        string endpoint,
        string requestBody,
        string apiSecret)
    {
        // The message string is: timestamp + method + requestPath + body
        var messageString = $"{timestamp}{method}{endpoint}{requestBody}";
        
        // Convert the API secret from base64
        var decodedSecret = Convert.FromBase64String(apiSecret);
        
        // Compute the HMAC-SHA256 signature
        using var hmac = new HMACSHA256(decodedSecret);
        var messageBytes = Encoding.UTF8.GetBytes(messageString);
        var signatureBytes = hmac.ComputeHash(messageBytes);
        
        // Convert the signature to a base64 string
        return Convert.ToBase64String(signatureBytes);
    }

    // Add a new method to generate the signature for WebSocket subscription
    private string GenerateCoinbaseSubscriptionSignature(string timestamp, string symbol)
    {
        // The signature is created by base64-decoding the API secret, creating an HMAC-SHA256 with 
        // the decoded secret, and signing the message string with it.
        // The message string format is: timestamp + "GET" + "/users/self/verify"
        
        var message = $"{timestamp}GET/users/self/verify";
        
        // Convert the API secret from base64
        var decodedSecret = Convert.FromBase64String(_apiSecret);
        
        // Compute the HMAC-SHA256 signature
        using var hmac = new HMACSHA256(decodedSecret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signatureBytes = hmac.ComputeHash(messageBytes);
        
        // Convert the signature to a base64 string
        return Convert.ToBase64String(signatureBytes);
    }

    private async Task ProcessErrorMessageAsync(JsonElement root, string message, CancellationToken cancellationToken)
    {
        if (root.TryGetProperty("message", out var errorMessageElement))
        {
            var errorMessage = errorMessageElement.GetString();
            Logger.LogError("Received error from Coinbase WebSocket: {Message}", errorMessage);
            
            // Log additional diagnostic information
            var reason = root.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() : "Unknown reason";
            
            if (errorMessage != null && errorMessage.Contains("subscribe", StringComparison.OrdinalIgnoreCase))
            {
                // Extract subscription details for better debugging
                Logger.LogError("Coinbase subscription failed: {ErrorMessage}, Reason: {Reason}, Raw message: {RawMessage}", 
                    errorMessage, reason, message);
                
                // Check which products we tried to subscribe to
                foreach (var pair in _subscribedPairs)
                {
                    Logger.LogWarning("Currently attempting to subscribe to: {Symbol} for trading pair {TradingPair}", 
                        pair.Key, pair.Value);
                }
            }
            
            // If the error is about authentication, throw an exception
            if (errorMessage != null && (
                errorMessage.Contains("authentication", StringComparison.OrdinalIgnoreCase) || 
                errorMessage.Contains("auth", StringComparison.OrdinalIgnoreCase) || 
                errorMessage.Contains("signature", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Coinbase WebSocket authentication failed: {errorMessage}");
            }
        }
        else
        {
            Logger.LogError("Received error from Coinbase WebSocket: {Message}", message);
        }
    }
} 