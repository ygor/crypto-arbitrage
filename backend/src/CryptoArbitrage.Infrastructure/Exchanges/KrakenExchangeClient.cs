using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Net.WebSockets;
using CryptoArbitrage.Domain.Exceptions;

namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Exchange client implementation for Kraken.
/// </summary>
public class KrakenExchangeClient : BaseExchangeClient
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<TradingPair, Channel<OrderBook>> _orderBookChannels = new();
    private readonly Dictionary<string, TradingPair> _subscribedPairs = new();
    private readonly Dictionary<int, string> _channelIdToSymbol = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<bool>> _subscriptionAcks 
        = new(System.StringComparer.OrdinalIgnoreCase);
    
    private string? _apiKey;
    private string? _apiSecret;
    private readonly string _baseUrl = "https://api.kraken.com";
    private readonly string _wsUrl = "wss://ws.kraken.com";  // Kraken WebSocket API v1 endpoint
    private readonly long _nonce;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="KrakenExchangeClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public KrakenExchangeClient(
        HttpClient httpClient,
        IConfigurationService configurationService,
        ILogger<KrakenExchangeClient> logger)
        : base("kraken", configurationService, logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _nonce = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Set WebSocket URL in base class
        WebSocketUrl = _wsUrl;
        
        // Log streaming info
        Logger.LogInformation("Kraken exchange supports WebSocket streaming for real-time order book data");
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
            // Get exchange configuration to retrieve credentials
            var exchangeConfig = await ConfigurationService.GetExchangeConfigurationAsync(ExchangeId, cancellationToken);
            
            if (exchangeConfig != null)
            {
                // Load credentials if they exist, but don't require them for public data feeds
                _apiKey = exchangeConfig.ApiKey;
                _apiSecret = exchangeConfig.ApiSecret;
                
                // Log what type of connection we're making
                if (!string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_apiSecret))
                {
                    Logger.LogInformation("Credentials found, will use authenticated connection when needed");
                }
                else
                {
                    Logger.LogInformation("No credentials provided, will use public connections only");
                }
            }
            
            // Use the enhanced WebSocket connection management from base class
            await base.ConnectAsync(cancellationToken);
            
            Logger.LogInformation("Connected to {ExchangeId} with enhanced WebSocket management", ExchangeId);
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
        
        Logger.LogInformation("Authenticating with Kraken");
        
        try
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            
            // Test authentication by getting account balance
            var endpoint = "/0/private/Balance";
            var response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, null, cancellationToken);
            
            if (response != null && response["error"] is JArray errorArray && !errorArray.Any())
            {
                Logger.LogInformation("Authentication with Kraken successful");
                
                // Cache balances
                await GetAllBalancesAsync(cancellationToken);
            }
            else
            {
                var errorMessage = FormatKrakenError(response);
                Logger.LogError("Authentication with Kraken failed: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException($"Authentication with Kraken failed: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating with Kraken");
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        if (_orderBookChannels.ContainsKey(tradingPair))
        {
            Logger.LogInformation("Already subscribed to order book for {TradingPair} on Kraken", tradingPair);
            return;
        }
        
        var channel = Channel.CreateUnbounded<OrderBook>();
        _orderBookChannels[tradingPair] = channel;
        
        // For WebSocket API we need a specific format with a slash
        var (baseCurrency, quoteCurrency, _) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        
        // Kraken WebSocket API uses a different format than the REST API
        string symbol;
        if (baseCurrency == "BTC")
        {
            // Kraken uses XBT instead of BTC and needs slash formatting for WebSocket API
            symbol = $"XBT/{quoteCurrency}";
        }
        else
        {
            symbol = $"{baseCurrency}/{quoteCurrency}";
        }
        
        try
        {
            Logger.LogInformation("Subscribing to order book for {TradingPair} ({Symbol}) on Kraken", tradingPair, symbol);
            
            // Add to subscribed pairs
            _subscribedPairs[symbol] = tradingPair;
            
            // Create subscription message for WebSocket - using v1 format
            var subscribeMessage = new
            {
                @event = "subscribe",  // "event" is a reserved keyword in C#, need @ prefix
                reqid = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000),
                pair = new[] { symbol },
                subscription = new
                {
                    name = "book",
                    depth = 25 // Depth level (10, 25, 100, 500, 1000)
                }
            };
            
            var messageJson = JsonConvert.SerializeObject(subscribeMessage);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _subscriptionAcks[symbol] = tcs;
            await SendWebSocketMessageAsync(messageJson, cancellationToken);
            
            Logger.LogInformation("Sent WebSocket subscription for order book {Symbol}", symbol);

            // Wait for explicit ack or timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
            try
            {
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.InfiniteTimeSpan, linked.Token));
                if (completed != tcs.Task || !tcs.Task.Result)
                {
                    Logger.LogWarning("Timed out waiting for subscription ack for {Symbol}", symbol);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Subscription wait canceled for {Symbol}", symbol);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to order book for {TradingPair} ({Symbol}) on Kraken", tradingPair, symbol);
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(
        TradingPair tradingPair, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        if (!_orderBookChannels.TryGetValue(tradingPair, out var channel))
        {
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            if (!_orderBookChannels.TryGetValue(tradingPair, out channel))
            {
                yield break;
            }
        }
        
        // Read all updates from the channel
        await foreach (var orderBook in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return orderBook;
        }
    }
    
    /// <inheritdoc />
    public override async Task<Balance> GetBalanceAsync(string currency, CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        // Normalize currency
        currency = currency.ToUpperInvariant();
        
        // Special mapping for Kraken's format
        var krakenCurrency = MapCurrencyToKrakenFormat(currency);
        
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
    public override async Task<IReadOnlyCollection<Balance>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        try
        {
            var endpoint = "/0/private/Balance";
            var response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, null, cancellationToken);
            
            if (response == null || response["error"] is JArray errorArray && errorArray.Any())
            {
                var errorMessage = FormatKrakenError(response);
                Logger.LogError("Failed to get balances from Kraken: {ErrorMessage}", errorMessage);
                return Balances.Values.ToList().AsReadOnly();
            }
            
            var result = response["result"] as JObject;
            if (result == null)
            {
                Logger.LogError("Invalid response format from Kraken balance API");
                return Balances.Values.ToList().AsReadOnly();
            }
            
            // Clear existing balances
            Balances.Clear();
            
            foreach (var property in result.Properties())
            {
                var krakenCurrency = property.Name;
                var standardCurrency = MapKrakenFormatToCurrency(krakenCurrency);
                var balanceStr = property.Value.ToString();
                
                if (decimal.TryParse(balanceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var balanceAmount))
                {
                    // Kraken only returns total balance, not available/hold amounts
                    Balances[standardCurrency] = new Balance(ExchangeId, standardCurrency, balanceAmount, balanceAmount, 0);
                }
            }
            
            return Balances.Values.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting balances from Kraken");
            throw;
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
        
        Logger.LogInformation("Placing market {Side} order for {Quantity} {TradingPair} on Kraken", 
            orderSide, quantity, tradingPair);
        
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["userref"] = clientOrderId,
                ["pair"] = symbol,
                ["type"] = side,
                ["ordertype"] = "market",
                ["volume"] = quantity.ToString(CultureInfo.InvariantCulture)
            };
            
            var endpoint = "/0/private/AddOrder";
            var response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, parameters, cancellationToken);
            
            if (response == null || response["error"] is JArray errorArray && errorArray.Any())
            {
                var errorMessage = FormatKrakenError(response);
                Logger.LogError("Failed to place market order on Kraken: {ErrorMessage}", errorMessage);
                throw new Exception($"Failed to place market order on Kraken: {errorMessage}");
            }
            
            var result = response["result"] as JObject;
            if (result == null)
            {
                Logger.LogError("Invalid response format from Kraken AddOrder API");
                throw new Exception("Invalid response format from Kraken AddOrder API");
            }
            
            var txid = result["txid"]?.First?.ToString();
            
            if (string.IsNullOrEmpty(txid))
            {
                Logger.LogError("No transaction ID returned from Kraken AddOrder API");
                throw new Exception("No transaction ID returned from Kraken AddOrder API");
            }
            
            Logger.LogInformation("Market order placed on Kraken: OrderId={OrderId}", txid);
            
            // For Kraken, we don't immediately know the executed price and filled amount,
            // so we return with the order in a pending state
            var order = new Order(
                txid,
                ExchangeId,
                tradingPair,
                orderSide,
                OrderType.Market,
                OrderStatus.New, // Pending state
                0, // Price not known yet for market order
                quantity,
                DateTime.UtcNow);
            
            return order;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error placing market order on Kraken");
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task<TradeResult> PlaceLimitOrderAsync(
        TradingPair tradingPair, 
        OrderSide orderSide, 
        decimal quantity, 
        decimal price, 
        OrderType orderType = OrderType.Limit, 
        CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        var side = orderSide == OrderSide.Buy ? "buy" : "sell";
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        Logger.LogInformation("Placing limit {Side} order for {Quantity} {TradingPair} at {Price} on Kraken", 
            side, quantity, tradingPair, price);
        
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["userref"] = clientOrderId,
                ["pair"] = symbol,
                ["type"] = side,
                ["ordertype"] = "limit",
                ["price"] = price.ToString(CultureInfo.InvariantCulture),
                ["volume"] = quantity.ToString(CultureInfo.InvariantCulture)
            };
            
            var endpoint = "/0/private/AddOrder";
            var response = await SendAuthenticatedRequestAsync<JObject>(HttpMethod.Post, endpoint, parameters, cancellationToken);
            
            stopwatch.Stop();
            
            if (response == null || response["error"] is JArray errorArray && errorArray.Any())
            {
                var errorMessage = FormatKrakenError(response);
                Logger.LogError("Failed to place limit order on Kraken: {ErrorMessage}", errorMessage);
                return TradeResult.Failure($"Failed to place limit order on Kraken: {errorMessage}", stopwatch.ElapsedMilliseconds);
            }
            
            var result = response["result"] as JObject;
            if (result == null)
            {
                Logger.LogError("Invalid response format from Kraken AddOrder API");
                return TradeResult.Failure("Invalid response format from Kraken AddOrder API", stopwatch.ElapsedMilliseconds);
            }
            
            var txid = result["txid"]?.First?.ToString();
            
            if (string.IsNullOrEmpty(txid))
            {
                Logger.LogError("No transaction ID returned from Kraken AddOrder API");
                return TradeResult.Failure("No transaction ID returned from Kraken AddOrder API", stopwatch.ElapsedMilliseconds);
            }
            
            Logger.LogInformation("Limit order placed on Kraken: OrderId={OrderId}, Price={Price}, Quantity={Quantity}", 
                txid, price, quantity);
            
            var tradeExecution = new TradeExecution(
                txid,
                ExchangeId,
                tradingPair,
                orderSide,
                orderType,
                price,
                quantity,
                0, // Fee not known until order is filled
                tradingPair.QuoteCurrency,
                DateTimeOffset.UtcNow);
            
            return TradeResult.Success(tradeExecution, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error placing limit order on Kraken for {TradingPair}", tradingPair);
            return TradeResult.Failure(ex, stopwatch.ElapsedMilliseconds);
        }
    }
    
    /// <inheritdoc />
    public override Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting fee schedule for Kraken");
        
        // For now, return a default fee schedule
        var feeSchedule = new FeeSchedule("kraken", 0.0016m, 0.0026m);
        
        return Task.FromResult(feeSchedule);
    }
    
    /// <inheritdoc />
    public override async Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        
        // First check if we already have an order book from WebSocket
        if (OrderBooks.TryGetValue(tradingPair, out var cachedOrderBook))
        {
            return cachedOrderBook;
        }
        
        try
        {
            var (baseCurrency, quoteCurrency, _) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
            
            // Kraken WebSocket API uses a different format than the REST API - with slash
            string symbol;
            if (baseCurrency == "BTC")
            {
                // Kraken uses XBT instead of BTC and needs slash formatting for WebSocket API
                symbol = $"XBT/{quoteCurrency}";
            }
            else
            {
                symbol = $"{baseCurrency}/{quoteCurrency}";
            }
            
            Logger.LogInformation("Getting order book snapshot for {TradingPair} ({Symbol}) from Kraken WebSocket", 
                tradingPair, symbol);
            
            // Subscribe to public order book feed (no authentication needed)
            await SubscribeToOrderBookAsync(tradingPair, cancellationToken);
            
            // Wait for the order book to be received via WebSocket
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
            
            try
            {
                while (!OrderBooks.ContainsKey(tradingPair) && !linkedCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, linkedCts.Token);
                }
                
                if (OrderBooks.TryGetValue(tradingPair, out var orderBook))
                {
                    return orderBook;
                }
                
                // If we get here, it means we timed out
                throw new ExchangeClientException(ExchangeId, 
                    $"Failed to get order book for {tradingPair} ({symbol}) on Kraken: Timed out waiting for WebSocket snapshot");
            }
            catch (OperationCanceledException)
            {
                throw new ExchangeClientException(ExchangeId, 
                    $"Failed to get order book for {tradingPair} ({symbol}) on Kraken: Operation was cancelled");
            }
        }
        catch (ExchangeClientException)
        {
            // Re-throw exchange client exceptions
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting order book snapshot for {TradingPair}", tradingPair);
            throw new ExchangeClientException(ExchangeId, 
                $"Failed to get order book for {tradingPair} on Kraken: {ex.Message}");
        }
    }
    
    /// <inheritdoc />
    public async Task<Order?> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        Logger.LogInformation("Getting order status for {OrderId} from Kraken", orderId);
        
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["txid"] = orderId
            };
            
            var response = await SendAuthenticatedRequestAsync<JObject>(
                HttpMethod.Post, 
                "/private/QueryOrders", 
                parameters, 
                cancellationToken);
            
            if (response == null || !response.ContainsKey("result"))
            {
                Logger.LogWarning("Failed to get order status from Kraken for {OrderId}", orderId);
                return null;
            }
            
            var orderData = response["result"]?[orderId] as JObject;
            if (orderData == null)
            {
                Logger.LogWarning("Order {OrderId} not found on Kraken", orderId);
                return null;
            }
            
            // Extract order details
            var status = orderData["status"]?.ToString() ?? "unknown";
            var pair = orderData["pair"]?.ToString() ?? string.Empty;
            var type = orderData["type"]?.ToString() ?? "buy";
            var price = decimal.Parse(orderData["price"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            var volume = decimal.Parse(orderData["vol"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            var executed = decimal.Parse(orderData["vol_exec"]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            
            // Convert Kraken status to our OrderStatus
            var orderStatus = status switch
            {
                "closed" => OrderStatus.Filled,
                "canceled" => OrderStatus.Canceled,
                "pending" => OrderStatus.New,
                "open" => executed > 0 ? OrderStatus.PartiallyFilled : OrderStatus.New,
                _ => OrderStatus.Rejected
            };
            
            // Convert pair to TradingPair
            // Kraken has pairs like XBTUSDT, so we need to extract the currencies
            var baseCurrency = pair.Substring(0, 3);
            var quoteCurrency = pair.Substring(3);
            
            // Map Kraken's symbols to standard ones
            if (baseCurrency == "XBT") baseCurrency = "BTC";
            if (baseCurrency == "XDG") baseCurrency = "DOGE";
            
            var tradingPair = new TradingPair(baseCurrency, quoteCurrency);
            
            var order = new Order(
                orderId,
                ExchangeId,
                tradingPair,
                type == "buy" ? OrderSide.Buy : OrderSide.Sell,
                OrderType.Limit,
                orderStatus,
                price,
                volume,
                DateTimeOffset.UtcNow.DateTime
            );
            
            // Set filled quantity
            order.FilledQuantity = executed;
            
            Logger.LogInformation("Retrieved order status for {OrderId} from Kraken: {Status}", orderId, orderStatus);
            
            return order;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting order status from Kraken for {OrderId}", orderId);
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        Logger.LogInformation("Canceling order {OrderId} on Kraken", orderId);
        
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["txid"] = orderId
            };
            
            var response = await SendAuthenticatedRequestAsync<JObject>(
                HttpMethod.Post, 
                "/private/CancelOrder", 
                parameters, 
                cancellationToken);
            
            if (response == null || !response.ContainsKey("result"))
            {
                var error = FormatKrakenError(response);
                Logger.LogWarning("Failed to cancel order on Kraken: {Error}", error);
                return false;
            }
            
            var count = response["result"]?["count"]?.Value<int>() ?? 0;
            var success = count > 0;
            
            if (success)
            {
                Logger.LogInformation("Successfully canceled order {OrderId} on Kraken", orderId);
            }
            else
            {
                Logger.LogWarning("Order {OrderId} not found or already canceled on Kraken", orderId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error canceling order {OrderId} on Kraken", orderId);
            return false;
        }
    }
    
    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        ValidateConnected();
        ValidateAuthenticated();
        
        Logger.LogInformation("Getting balances from Kraken");
        
        try
        {
            var response = await SendAuthenticatedRequestAsync<JObject>(
                HttpMethod.Post, 
                "/private/Balance", 
                null, 
                cancellationToken);
            
            if (response == null || !response.ContainsKey("result"))
            {
                var error = FormatKrakenError(response);
                Logger.LogWarning("Failed to get balances from Kraken: {Error}", error);
                return Array.Empty<Balance>();
            }
            
            var result = response["result"] as JObject;
            if (result == null)
            {
                Logger.LogWarning("No balance data found in Kraken response");
                return Array.Empty<Balance>();
            }
            
            var balances = new List<Balance>();
            
            foreach (var property in result.Properties())
            {
                var krakenCurrency = property.Name;
                var amount = decimal.Parse(property.Value.ToString(), CultureInfo.InvariantCulture);
                
                // Map Kraken currency codes to standard ones
                var currency = MapKrakenFormatToCurrency(krakenCurrency);
                
                var balance = new Balance(
                    ExchangeId,
                    currency,
                    amount,  // Total
                    0,       // Hold - Kraken doesn't provide this directly
                    amount   // Available
                );
                
                balances.Add(balance);
                
                // Update cache
                Balances[currency] = balance;
            }
            
            Logger.LogInformation("Retrieved {Count} balances from Kraken", balances.Count);
            
            return balances.AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting balances from Kraken");
            return Array.Empty<Balance>();
        }
    }
    
    /// <inheritdoc />
    public async Task<OrderBook> FetchKrakenOrderBookAsync(TradingPair tradingPair, int depth = 20, CancellationToken cancellationToken = default)
    {
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        var url = $"{_baseUrl}/public/Depth?pair={symbol}&count={depth}";
        
        Logger.LogInformation("Fetching order book from Kraken for {TradingPair} ({Symbol}) at {Url}", 
            tradingPair, symbol, url);
        
        try 
        {
            var response = await _httpClient.GetFromJsonAsync<JsonNode>(url, cancellationToken);
            
            if (response == null)
            {
                Logger.LogWarning("Received null response from Kraken order book request for {TradingPair}", tradingPair);
                throw new Exception($"Failed to fetch order book from Kraken for {tradingPair}");
            }
            
            var result = response["result"];
            if (result == null)
            {
                var errorMessage = response["error"]?.ToString() ?? "Unknown error";
                Logger.LogError("Error fetching order book from Kraken: {ErrorMessage}", errorMessage);
                throw new Exception($"Failed to fetch order book from Kraken: {errorMessage}");
            }
            
            // In Kraken's response, the result is keyed by the asset pair
            var resultKeys = result.AsObject().Select(p => p.Key).ToList();
            if (!resultKeys.Any())
            {
                Logger.LogError("No result keys found in Kraken order book response");
                throw new Exception("No result keys found in Kraken order book response");
            }
            
            var pairResult = result[resultKeys[0]];
            
            List<OrderBookEntry> bids = ParseOrderBookLevels(pairResult["bids"], OrderSide.Buy);
            List<OrderBookEntry> asks = ParseOrderBookLevels(pairResult["asks"], OrderSide.Sell);
            
            Logger.LogInformation("Successfully fetched order book from Kraken for {TradingPair}: {BidCount} bids, {AskCount} asks", 
                tradingPair, bids.Count, asks.Count);
            
            return new OrderBook(ExchangeId, tradingPair, DateTime.UtcNow, bids, asks);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP error fetching order book from Kraken for {TradingPair} ({Symbol}): {ErrorMessage}", 
                tradingPair, symbol, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching order book from Kraken for {TradingPair} ({Symbol}): {ErrorMessage}", 
                tradingPair, symbol, ex.Message);
            throw;
        }
    }
    
    /// <inheritdoc />
    public override async Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || ManagedConnection == null)
        {
            Logger.LogWarning("Cannot unsubscribe - client for {ExchangeId} is not connected", ExchangeId);
            CleanupOrderBookResources(tradingPair);
            return;
        }
        
        // For WebSocket API we need a specific format with a slash
        var (baseCurrency, quoteCurrency, _) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        
        // Kraken WebSocket API uses a different format than the REST API
        string symbol;
        if (baseCurrency == "BTC")
        {
            // Kraken uses XBT instead of BTC and needs slash formatting for WebSocket API
            symbol = $"XBT/{quoteCurrency}";
        }
        else
        {
            symbol = $"{baseCurrency}/{quoteCurrency}";
        }
        
        Logger.LogInformation("Unsubscribing from order book for {TradingPair} ({Symbol}) on Kraken", tradingPair, symbol);
        
        try
        {
            // Only attempt to send the unsubscribe message if the WebSocket is still open and not canceled
            if (_subscribedPairs.Remove(symbol) && ManagedConnection.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Use a timeout to prevent hanging during shutdown
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                    
                    // Send unsubscribe message via WebSocket - using v1 format
                    var unsubscribeMessage = new
                    {
                        @event = "unsubscribe", // Use event property
                        reqid = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000),
                        pair = new[] { symbol },
                        subscription = new
                        {
                            name = "book"
                        }
                    };
                    
                    var messageJson = JsonConvert.SerializeObject(unsubscribeMessage);
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
            CleanupOrderBookResources(tradingPair, symbol);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from order book for {TradingPair} ({Symbol}) on Kraken", tradingPair, symbol);
            
            // Still try to clean up resources
            try
            {
                CleanupOrderBookResources(tradingPair, symbol);
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
    /// <param name="symbol">The symbol used for channel mapping.</param>
    private void CleanupOrderBookResources(TradingPair tradingPair, string? symbol = null)
    {
        // Remove the channel and complete it
        if (_orderBookChannels.TryGetValue(tradingPair, out var channel))
        {
            try
            {
                channel.Writer.Complete();
                _orderBookChannels.Remove(tradingPair);
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
        
        // Clean up channel mapping if exists and if symbol is provided
        if (!string.IsNullOrEmpty(symbol))
        {
            try
            {
                foreach (var channelPair in _channelIdToSymbol.Where(x => x.Value == symbol).ToList())
                {
                    _channelIdToSymbol.Remove(channelPair.Key);
                }
                Logger.LogDebug("Removed channel mappings for {Symbol}", symbol);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error removing channel mappings for {Symbol}", symbol);
            }
        }
    }
    
    // Private helper methods
    
    private async Task<T?> SendAuthenticatedRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        Dictionary<string, string>? parameters,
        CancellationToken cancellationToken)
        where T : class
    {
        if (_apiKey == null || _apiSecret == null)
        {
            throw new InvalidOperationException("API credentials not set");
        }
        
        try
        {
            var nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var formContent = new FormUrlEncodedContent(parameters?.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)) 
                ?? Enumerable.Empty<KeyValuePair<string, string>>());
            
            var formData = await formContent.ReadAsStringAsync(cancellationToken);
            formData = $"nonce={nonce}" + (string.IsNullOrEmpty(formData) ? "" : $"&{formData}");
            
            // Generate the signature
            var signature = GenerateKrakenSignature(endpoint, nonce, formData, _apiSecret);
            
            var requestUri = new Uri(_baseUrl + endpoint);
            using var requestMessage = new HttpRequestMessage(method, requestUri);
            
            requestMessage.Content = new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded");
            requestMessage.Headers.Add("API-Key", _apiKey);
            requestMessage.Headers.Add("API-Sign", signature);
            
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<T>(options: null, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending authenticated request to Kraken: {Endpoint}", endpoint);
            return null;
        }
    }
    
    private string GenerateKrakenSignature(string endpoint, string nonce, string postData, string apiSecret)
    {
        var decodedSecret = Convert.FromBase64String(apiSecret);
        
        // Create the SHA256 hash of the nonce and the post data
        using var sha256 = SHA256.Create();
        var message = Encoding.UTF8.GetBytes(nonce + postData);
        var hash = sha256.ComputeHash(message);
        
        // Create the HMAC-SHA512 of the URI path + hash with the API secret as the key
        using var hmac = new HMACSHA512(decodedSecret);
        var pathBytes = Encoding.UTF8.GetBytes(endpoint);
        var combinedBytes = pathBytes.Concat(hash).ToArray();
        var signature = hmac.ComputeHash(combinedBytes);
        
        return Convert.ToBase64String(signature);
    }
    
    private string FormatKrakenError(JObject? response)
    {
        if (response == null)
        {
            return "Null response from Kraken API";
        }
        
        if (response["error"] is JArray errorArray && errorArray.Any())
        {
            return string.Join(", ", errorArray.Select(e => e.ToString()));
        }
        
        return "Unknown error";
    }
    
    private string MapCurrencyToKrakenFormat(string currency)
    {
        // Kraken uses different symbols for some currencies
        return currency switch
        {
            "BTC" => "XXBT",
            "ETH" => "XETH",
            "USD" => "ZUSD",
            "EUR" => "ZEUR",
            _ => currency
        };
    }
    
    private string MapKrakenFormatToCurrency(string krakenCurrency)
    {
        // Convert Kraken's format back to standard currency codes
        return krakenCurrency switch
        {
            "XXBT" => "BTC",
            "XETH" => "ETH",
            "ZUSD" => "USD",
            "ZEUR" => "EUR",
            _ => krakenCurrency
        };
    }
    
    // Helper method to parse order book levels
    private List<OrderBookEntry> ParseOrderBookLevels(JsonNode? levels, OrderSide side)
    {
        var result = new List<OrderBookEntry>();
        
        if (levels is not JsonArray array)
        {
            return result;
        }
        
        foreach (var level in array.AsArray())
        {
            if (level == null || level.AsArray().Count < 2)
            {
                continue;
            }
            
            var price = decimal.Parse(level[0]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            var quantity = decimal.Parse(level[1]?.ToString() ?? "0", CultureInfo.InvariantCulture);
            
            if (price > 0 && quantity > 0)
            {
                result.Add(new OrderBookEntry(price, quantity));
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
    
    /// <inheritdoc />
    protected override async Task ProcessWebSocketMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // Try to parse as a JSON object first (for events like subscribe/unsubscribe)
            if (message.StartsWith("{"))
            {
                var jObject = JObject.Parse(message);
                
                // Check if it's an event message
                if (jObject.ContainsKey("event"))
                {
                    var eventType = jObject["event"]?.ToString();
                    Logger.LogDebug("Received Kraken WebSocket event: {EventType}", eventType);
                    
                    if (eventType == "subscriptionStatus")
                    {
                        await ProcessSubscriptionStatusAsync(jObject, cancellationToken);
                    }
                    else if (eventType == "error")
                    {
                        var errorMsg = jObject["errorMessage"]?.ToString() ?? "Unknown error";
                        Logger.LogError("Kraken WebSocket error: {ErrorMessage}", errorMsg);
                    }
                }
            }
            // Try to parse as JSON array (for order book updates)
            else if (message.StartsWith("["))
            {
                var jArray = JArray.Parse(message);
                
                // Check if it's an order book message
                if (jArray.Count >= 2 && jArray[1] is JObject)
                {
                    var secondElement = jArray[1] as JObject;
                    
                    // Check if it contains order book data
                    if (secondElement != null && 
                        (secondElement.ContainsKey("as") || secondElement.ContainsKey("a") || 
                         secondElement.ContainsKey("bs") || secondElement.ContainsKey("b")))
                    {
                        await ProcessOrderBookMessageAsync(jArray, cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing WebSocket message from Kraken: {Message}", message);
        }
    }
    
    private async Task ProcessSubscriptionStatusAsync(JObject message, CancellationToken cancellationToken)
    {
        var status = message["status"]?.ToString();
        var channelName = message["channelName"]?.ToString();
        var pair = message["pair"]?.ToString();
        var reqid = message["reqid"]?.ToObject<int>() ?? 0;
        
        if (status == "subscribed" && channelName?.StartsWith("book") == true && !string.IsNullOrEmpty(pair))
        {
            Logger.LogInformation("Successfully subscribed to {ChannelName} for {Pair} on Kraken", channelName, pair);
            
            // If channel ID is provided, store the mapping
            if (message["channelID"] != null && message["channelID"].Type == JTokenType.Integer)
            {
                var channelId = message["channelID"].ToObject<int>();
                _channelIdToSymbol[channelId] = pair;
                Logger.LogDebug("Mapped channel ID {ChannelId} to symbol {Symbol}", channelId, pair);
            }
            if (_subscriptionAcks.TryRemove(pair, out var tcs))
            {
                tcs.TrySetResult(true);
            }
        }
        else if (status == "error")
        {
            var errorMsg = message["errorMessage"]?.ToString() ?? "Unknown error";
            Logger.LogError("Kraken subscription error for {ChannelName} {Pair}: {ErrorMessage}", 
                channelName, pair, errorMsg);
            
            // If the error is about the event not found, let's try to resubscribe with proper format
            if (errorMsg.Contains("Event(s) not found", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Retrying subscription with proper event format");
                // Extract pair from the failed subscription if possible
                if (!string.IsNullOrEmpty(pair) && _subscribedPairs.TryGetValue(pair, out var tradingPair))
                {
                    try 
                    {
                        // Wait a bit before retrying
                        await Task.Delay(1000, cancellationToken);
                        
                        // Create a proper subscription message
                        var retrySubscribeMessage = new
                        {
                            @event = "subscribe",
                            pair = new[] { pair },
                            subscription = new
                            {
                                name = "book",
                                depth = 25
                            }
                        };
                        
                        var retryMessageJson = JsonConvert.SerializeObject(retrySubscribeMessage);
                        await SendWebSocketMessageAsync(retryMessageJson, cancellationToken);
                        Logger.LogInformation("Retried WebSocket subscription for {Symbol}", pair);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error retrying subscription for {Pair}", pair);
                    }
                }
            }
        }
    }
    
    private async Task ProcessOrderBookMessageAsync(JArray message, CancellationToken cancellationToken)
    {
        try
        {
            // Extract channel ID, type, and pair
            var channelId = message[0].Type == JTokenType.Integer ? message[0].ToObject<int>() : 0;
            var channelName = message[message.Count - 2]?.ToString() ?? string.Empty;
            var pair = message[message.Count - 1]?.ToString() ?? string.Empty;
            
            // Check if this is a book channel
            if (!channelName.StartsWith("book"))
            {
                return;
            }
            
            // Get the trading pair from the symbol
            if (!_subscribedPairs.TryGetValue(pair, out var tradingPair))
            {
                Logger.LogWarning("Received order book update for unsubscribed pair: {Pair}", pair);
                return;
            }
            
            // Get the order book data from the message
            var bookData = message[1] as JObject;
            if (bookData == null)
            {
                Logger.LogWarning("Invalid order book data format: {Message}", message);
                return;
            }
            
            bool isSnapshot = bookData.ContainsKey("as") && bookData.ContainsKey("bs");
            
            // Process either snapshot or update
            if (isSnapshot)
            {
                await ProcessOrderBookSnapshotAsync(tradingPair, bookData, cancellationToken);
            }
            else
            {
                await ProcessOrderBookUpdateAsync(tradingPair, bookData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing order book message: {Message}", message);
        }
    }
    
    private async Task ProcessOrderBookSnapshotAsync(TradingPair tradingPair, JObject bookData, CancellationToken cancellationToken)
    {
        try
        {
            // Parse asks and bids
            var asks = ParseOrderBookLevelsFromWebSocket(bookData["as"] as JArray, OrderSide.Sell);
            var bids = ParseOrderBookLevelsFromWebSocket(bookData["bs"] as JArray, OrderSide.Buy);
            
            if (asks.Count == 0 && bids.Count == 0)
            {
                Logger.LogWarning("Received empty order book snapshot for {TradingPair}", tradingPair);
                return;
            }
            
            // Create order book
            var orderBook = new OrderBook(ExchangeId, tradingPair, DateTime.UtcNow, bids, asks);
            
            // Store in local cache
            OrderBooks[tradingPair] = orderBook;
            
            // Publish to subscribers
            if (_orderBookChannels.TryGetValue(tradingPair, out var channel))
            {
                await channel.Writer.WriteAsync(orderBook, cancellationToken);
                Logger.LogInformation("Published order book snapshot for {TradingPair}: {BidCount} bids, {AskCount} asks", 
                    tradingPair, bids.Count, asks.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing order book snapshot for {TradingPair}", tradingPair);
        }
    }
    
    private async Task ProcessOrderBookUpdateAsync(TradingPair tradingPair, JObject bookData, CancellationToken cancellationToken)
    {
        try
        {
            // Check if we have a cached order book to update
            if (!OrderBooks.TryGetValue(tradingPair, out var existingOrderBook))
            {
                Logger.LogWarning("Received order book update for {TradingPair} but no snapshot yet", tradingPair);
                return;
            }
            
            // Create new lists based on the existing order book
            var bids = new List<OrderBookEntry>(existingOrderBook.Bids);
            var asks = new List<OrderBookEntry>(existingOrderBook.Asks);
            bool updated = false;
            
            // Process ask updates
            if (bookData.ContainsKey("a") && bookData["a"] is JArray askUpdates && askUpdates.Count > 0)
            {
                var askEntries = ParseOrderBookLevelsFromWebSocket(askUpdates, OrderSide.Sell);
                foreach (var askEntry in askEntries)
                {
                    // Update the order book (remove entry if quantity is 0)
                    UpdateOrderBookSide(asks, askEntry.Price, askEntry.Quantity);
                    updated = true;
                }
            }
            
            // Process bid updates
            if (bookData.ContainsKey("b") && bookData["b"] is JArray bidUpdates && bidUpdates.Count > 0)
            {
                var bidEntries = ParseOrderBookLevelsFromWebSocket(bidUpdates, OrderSide.Buy);
                foreach (var bidEntry in bidEntries)
                {
                    // Update the order book (remove entry if quantity is 0)
                    UpdateOrderBookSide(bids, bidEntry.Price, bidEntry.Quantity);
                    updated = true;
                }
            }
            
            // If we made updates, update the timestamp and publish
            if (updated)
            {
                // Sort the order book entries
                bids.Sort((a, b) => b.Price.CompareTo(a.Price)); // Descending for bids
                asks.Sort((a, b) => a.Price.CompareTo(b.Price)); // Ascending for asks
                
                // Create a new order book with the updated lists
                var updatedOrderBook = new OrderBook(
                    existingOrderBook.ExchangeId,
                    existingOrderBook.TradingPair,
                    DateTime.UtcNow,
                    bids,
                    asks);
                
                // Store the updated order book
                OrderBooks[tradingPair] = updatedOrderBook;
                
                // Publish to subscribers
                if (_orderBookChannels.TryGetValue(tradingPair, out var channel))
                {
                    await channel.Writer.WriteAsync(updatedOrderBook, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing order book update for {TradingPair}", tradingPair);
        }
    }
    
    private void UpdateOrderBookSide(List<OrderBookEntry> entries, decimal price, decimal size)
    {
        // Find existing entry with the same price
        var existingIndex = entries.FindIndex(e => e.Price == price);
        
        if (existingIndex >= 0)
        {
            if (size == 0)
            {
                // Remove the entry if size is 0
                entries.RemoveAt(existingIndex);
            }
            else
            {
                // Update the quantity
                entries[existingIndex] = new OrderBookEntry(price, size);
            }
        }
        else if (size > 0)
        {
            // Add new entry if it doesn't exist and size > 0
            entries.Add(new OrderBookEntry(price, size));
        }
    }
    
    private List<OrderBookEntry> ParseOrderBookLevelsFromWebSocket(JArray? levels, OrderSide side)
    {
        var result = new List<OrderBookEntry>();
        
        if (levels == null)
        {
            return result;
        }
        
        foreach (var level in levels)
        {
            if (level is JArray levelArray && levelArray.Count >= 2)
            {
                if (decimal.TryParse(levelArray[0]?.ToString() ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                    decimal.TryParse(levelArray[1]?.ToString() ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                {
                    if (price > 0)
                    {
                        result.Add(new OrderBookEntry(price, quantity));
                    }
                }
            }
        }
        
        return result;
    }
} 