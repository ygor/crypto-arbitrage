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

namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Exchange client implementation for Kraken.
/// </summary>
public class KrakenExchangeClient : BaseExchangeClient
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<TradingPair, Channel<OrderBook>> _orderBookChannels = new();
    
    private string? _apiKey;
    private string? _apiSecret;
    private readonly string _baseUrl = "https://api.kraken.com";
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
    }
    
    /// <inheritdoc />
    public override bool SupportsStreaming => false;
    
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
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId, Logger);
        
        try
        {
            Logger.LogInformation("Subscribing to order book for {TradingPair} ({Symbol}) on Kraken", tradingPair, symbol);
            
            // Get initial snapshot
            var orderBook = await FetchKrakenOrderBookAsync(tradingPair, 20, cancellationToken);
            if (orderBook != null)
            {
                OrderBooks[tradingPair] = orderBook;
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
                tradingPair.QuoteCurrency);
            
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
        // Delegate to the FetchKrakenOrderBookAsync method to avoid code duplication
        return await FetchKrakenOrderBookAsync(tradingPair, depth, cancellationToken);
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
} 