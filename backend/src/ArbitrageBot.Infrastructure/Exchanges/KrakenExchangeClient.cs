using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;

namespace ArbitrageBot.Infrastructure.Exchanges;

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
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        Logger.LogInformation("Subscribing to order book for {TradingPair} ({Symbol}) on Kraken", tradingPair, symbol);
        
        try
        {
            // Create a channel for this trading pair if it doesn't exist
            if (!_orderBookChannels.TryGetValue(tradingPair, out var channel))
            {
                channel = Channel.CreateUnbounded<OrderBook>();
                _orderBookChannels[tradingPair] = channel;
                
                // Start a background task to periodically fetch order book updates
                _ = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var orderBook = await FetchOrderBookAsync(tradingPair, cancellationToken);
                            if (orderBook != null)
                            {
                                await channel.Writer.WriteAsync(orderBook, cancellationToken);
                                OrderBooks[tradingPair] = orderBook;
                            }
                            await Task.Delay(1000, cancellationToken);
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                        {
                            Logger.LogError(ex, "Error fetching order book for {TradingPair} on Kraken", tradingPair);
                            await Task.Delay(5000, cancellationToken);
                        }
                    }
                }, cancellationToken);
            }
            
            // Fetch the order book immediately to initialize
            var initialOrderBook = await FetchOrderBookAsync(tradingPair, cancellationToken);
            if (initialOrderBook != null)
            {
                OrderBooks[tradingPair] = initialOrderBook;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to order book for {TradingPair} on Kraken", tradingPair);
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
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        var sideStr = ExchangeUtils.FormatOrderSide(orderSide, ExchangeId);
        var clientOrderId = ExchangeUtils.GenerateClientOrderId(ExchangeId);
        
        Logger.LogInformation("Placing market {Side} order for {Quantity} {TradingPair} on Kraken", 
            orderSide, quantity, tradingPair);
        
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["userref"] = clientOrderId,
                ["pair"] = symbol,
                ["type"] = sideStr,
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
        OrderSide side, 
        decimal price, 
        decimal quantity, 
        OrderType orderType = OrderType.Limit, 
        CancellationToken cancellationToken = default)
    {
        ValidateAuthenticated();
        
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, ExchangeId);
        var sideStr = ExchangeUtils.FormatOrderSide(side, ExchangeId);
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
                ["type"] = sideStr,
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
                side,
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
            Logger.LogError(ex, "Error placing limit order on Kraken");
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
    
    // Private helper methods
    
    private async Task<OrderBook?> FetchOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken)
    {
        try
        {
            var symbol = tradingPair.ToString().Replace("/", "");
            
            var response = await _httpClient.GetFromJsonAsync<JObject>(
                $"{_baseUrl}/public/Depth?pair={symbol}&count=10",
                cancellationToken);
                
            if (response == null || !response.ContainsKey("result"))
            {
                Logger.LogWarning("No valid response received from Kraken for {TradingPair}", tradingPair);
                return null;
            }
            
            // Get the first result (there should be only one since we specified one pair)
            var resultPair = response["result"]?.First;
            if (resultPair == null)
            {
                return null;
            }
            
            var orderBookData = resultPair.First;
            if (orderBookData == null)
            {
                Logger.LogWarning("No order book data found for {TradingPair}", tradingPair);
                return null;
            }
            
            // Extract bids and asks
            var bidsArray = orderBookData["b"] as JArray;
            var asksArray = orderBookData["a"] as JArray;
            
            if (bidsArray == null || asksArray == null)
            {
                return null;
            }
            
            var bids = new List<OrderBookEntry>();
            var asks = new List<OrderBookEntry>();
            
            foreach (var bid in bidsArray)
            {
                if (bid == null || bid.Count() < 2)
                {
                    continue;  // Skip invalid entries
                }
                
                var bidPrice = bid[0]?.ToString();
                var bidVolume = bid[1]?.ToString();
                
                if (string.IsNullOrEmpty(bidPrice) || string.IsNullOrEmpty(bidVolume))
                {
                    continue;  // Skip if price or volume is missing
                }
                
                if (decimal.TryParse(bidPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                    decimal.TryParse(bidVolume, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                {
                    bids.Add(new OrderBookEntry(price, volume));
                }
            }
            
            foreach (var ask in asksArray)
            {
                if (ask == null || ask.Count() < 2)
                {
                    continue;  // Skip invalid entries
                }
                
                var askPrice = ask[0]?.ToString();
                var askVolume = ask[1]?.ToString();
                
                if (string.IsNullOrEmpty(askPrice) || string.IsNullOrEmpty(askVolume))
                {
                    continue;  // Skip if price or volume is missing
                }
                
                if (decimal.TryParse(askPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                    decimal.TryParse(askVolume, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                {
                    asks.Add(new OrderBookEntry(price, volume));
                }
            }
            
            // Sort bids (descending) and asks (ascending)
            bids = bids.OrderByDescending(x => x.Price).ToList();
            asks = asks.OrderBy(x => x.Price).ToList();
            
            return new OrderBook(
                ExchangeId,
                tradingPair,
                DateTime.UtcNow,
                bids,
                asks);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching order book for {TradingPair} on Kraken", tradingPair);
            return null;
        }
    }
    
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
} 