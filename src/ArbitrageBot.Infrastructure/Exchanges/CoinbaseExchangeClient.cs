using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArbitrageBot.Infrastructure.Exchanges;

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
    }
    
    /// <inheritdoc />
    public override bool SupportsStreaming => true;
    
    /// <inheritdoc />
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            Logger.LogDebug("Client for {ExchangeId} is already connected", ExchangeId);
            return;
        }
        
        Logger.LogInformation("Connecting to {ExchangeId}...", ExchangeId);
        
        try
        {
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
        
        // Check if we have a cached order book
        if (OrderBooks.TryGetValue(tradingPair, out var orderBook))
        {
            return orderBook;
        }
        
        // Fetch a new order book
        var fetchedOrderBook = await FetchOrderBookAsync(tradingPair, cancellationToken);
        if (fetchedOrderBook != null)
        {
            OrderBooks[tradingPair] = fetchedOrderBook;
            return fetchedOrderBook;
        }
        
        throw new InvalidOperationException($"Failed to get order book for {tradingPair} on {ExchangeId}");
    }
    
    /// <inheritdoc />
    public override async Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        Logger.LogInformation("Subscribing to order book for {TradingPair} ({Symbol}) on Coinbase", tradingPair, symbol);
        
        try
        {
            // Create a channel for this trading pair if it doesn't exist
            if (!OrderBookChannels.TryGetValue(tradingPair, out _))
            {
                OrderBookChannels[tradingPair] = Channel.CreateUnbounded<OrderBook>();
                
                // Add to subscribed pairs
                _subscribedPairs[symbol] = tradingPair;
                
                // Subscribe to WebSocket feed
                var subscribeMessage = new
                {
                    type = "subscribe",
                    product_ids = new[] { symbol },
                    channels = new[] { "level2", "heartbeat" }
                };
                
                var messageJson = System.Text.Json.JsonSerializer.Serialize(subscribeMessage);
                await SendWebSocketMessageAsync(messageJson, cancellationToken);
                
                // Get an initial snapshot via REST API
                var initialOrderBook = await FetchOrderBookAsync(tradingPair, cancellationToken);
                if (initialOrderBook != null)
                {
                    OrderBooks[tradingPair] = initialOrderBook;
                    await OrderBookChannels[tradingPair].Writer.WriteAsync(initialOrderBook, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to order book for {TradingPair} on Coinbase", tradingPair);
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        Logger.LogInformation("Unsubscribing from order book for {TradingPair} ({Symbol}) on Coinbase", tradingPair, symbol);
        
        try
        {
            // Remove from subscribed pairs
            if (_subscribedPairs.Remove(symbol))
            {
                // Unsubscribe from WebSocket feed
                var unsubscribeMessage = new
                {
                    type = "unsubscribe",
                    product_ids = new[] { symbol },
                    channels = new[] { "level2", "heartbeat" }
                };
                
                var messageJson = System.Text.Json.JsonSerializer.Serialize(unsubscribeMessage);
                await SendWebSocketMessageAsync(messageJson, cancellationToken);
            }
            
            // Remove the channel and complete it
            if (OrderBookChannels.TryGetValue(tradingPair, out var channel))
            {
                channel.Writer.Complete();
                OrderBookChannels.Remove(tradingPair);
            }
            
            // Remove from order books
            OrderBooks.Remove(tradingPair);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from order book for {TradingPair} on Coinbase", tradingPair);
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
                        break;
                    case "error":
                        if (root.TryGetProperty("message", out var errorMessageElement))
                        {
                            Logger.LogError("Received error from Coinbase WebSocket: {Message}", errorMessageElement.GetString());
                        }
                        else
                        {
                            Logger.LogError("Received error from Coinbase WebSocket: {Message}", message);
                        }
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
                // Get an initial snapshot
                orderBook = await FetchOrderBookAsync(tradingPair, cancellationToken);
                if (orderBook == null)
                {
                    return;
                }
                
                OrderBooks[tradingPair] = orderBook;
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
                return;
            }
            
            var productId = productIdElement.GetString();
            if (productId == null || !_subscribedPairs.TryGetValue(productId, out var tradingPair))
            {
                return;
            }
            
            var bids = new List<OrderBookEntry>();
            var asks = new List<OrderBookEntry>();
            
            // Process bids
            if (root.TryGetProperty("bids", out var bidsElement) && bidsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var bid in bidsElement.EnumerateArray())
                {
                    if (bid.ValueKind != JsonValueKind.Array || bid.GetArrayLength() != 2)
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
                    
                    bids.Add(new OrderBookEntry(price, size));
                }
            }
            
            // Process asks
            if (root.TryGetProperty("asks", out var asksElement) && asksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ask in asksElement.EnumerateArray())
                {
                    if (ask.ValueKind != JsonValueKind.Array || ask.GetArrayLength() != 2)
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
                    
                    asks.Add(new OrderBookEntry(price, size));
                }
            }
            
            // Sort the order book
            bids = bids.OrderByDescending(e => e.Price).Take(100).ToList();
            asks = asks.OrderBy(e => e.Price).Take(100).ToList();
            
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
        return await GetAllBalancesAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Balance>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        try
        {
            var endpoint = "/accounts";
            var accounts = await SendAuthenticatedRequestAsync<JArray>(HttpMethod.Get, endpoint, null, cancellationToken);
            
            if (accounts == null)
            {
                return Balances.Values.ToList().AsReadOnly();
            }
            
            foreach (var account in accounts)
            {
                var currency = account["currency"]?.ToString();
                var available = decimal.Parse(account["available"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
                var hold = decimal.Parse(account["hold"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
                var total = available + hold;
                
                if (currency != null)
                {
                    Balances[currency] = new Balance(ExchangeId, currency, available, hold, total);
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
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        var sideStr = ExchangeUtils.FormatOrderSide(orderSide, ExchangeId);
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        Logger.LogInformation("Placing market {Side} order for {Quantity} {TradingPair} on Coinbase", 
            orderSide, quantity, tradingPair);
        
        try
        {
            var orderRequest = new
            {
                client_oid = clientOrderId,
                product_id = symbol,
                side = sideStr,
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
    
    // Private helper methods
    
    private async Task<OrderBook?> FetchOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken)
    {
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        var endpoint = $"/products/{symbol}/book?level=2";
        
        try
        {
            JObject? response = await _httpClient.GetFromJsonAsync<JObject>(
                $"{_baseUrl}{endpoint}", 
                cancellationToken);
            
            if (response == null)
            {
                return null;
            }
            
            var bids = ParseOrderBookLevels(response["bids"] as JArray)
                .OrderByDescending(x => x.Price)
                .Select(level => new OrderBookEntry(level.Price, level.Quantity))
                .ToList();
            
            var asks = ParseOrderBookLevels(response["asks"] as JArray)
                .OrderBy(x => x.Price)
                .Select(level => new OrderBookEntry(level.Price, level.Quantity))
                .ToList();
            
            var timestamp = response["time"] != null 
                ? DateTime.Parse(response["time"]?.ToString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) 
                : DateTime.UtcNow;
            
            return new OrderBook(
                ExchangeId,
                tradingPair,
                timestamp,
                bids,
                asks);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching order book for {TradingPair} on Coinbase", tradingPair);
            return null;
        }
    }
    
    private List<OrderBookLevel> ParseOrderBookLevels(JArray? levels)
    {
        var result = new List<OrderBookLevel>();
        
        if (levels == null)
        {
            return result;
        }
        
        foreach (var level in levels)
        {
            if (level.Type != JTokenType.Array)
            {
                continue;
            }
            
            var array = (JArray)level;
            if (array.Count < 2)
            {
                continue;
            }
            
            var price = decimal.Parse(array[0].ToString(), CultureInfo.InvariantCulture);
            var quantity = decimal.Parse(array[1].ToString(), CultureInfo.InvariantCulture);
            
            result.Add(new OrderBookLevel(price, quantity));
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
                return await response.Content.ReadFromJsonAsync<T>(options: null, cancellationToken);
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
} 