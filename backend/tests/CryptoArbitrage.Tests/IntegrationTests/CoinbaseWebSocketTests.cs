using System.Reflection;
using System.Text.Json;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Exchanges;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CryptoArbitrage.Tests.IntegrationTests;

public class CoinbaseWebSocketTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<CoinbaseExchangeClient>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public CoinbaseWebSocketTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockLogger = new Mock<ILogger<CoinbaseExchangeClient>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.exchange.coinbase.com")
        };
        
        // Setup default HTTP response to prevent real API calls
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"message\": \"mocked response\"}")
            });
    }

    [Fact]
    public async Task ProcessWebSocketMessage_Snapshot_CreatesOrderBook()
    {
        // Arrange
        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        
        // Setup the subscribed pairs dictionary using reflection
        var subscribedPairs = new Dictionary<string, TradingPair> 
        {
            { "BTC-USD", new TradingPair("BTC", "USD") }
        };
        
        var subscribedPairsField = typeof(CoinbaseExchangeClient)
            .GetField("_subscribedPairs", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(subscribedPairsField); // Fail test if field doesn't exist
        subscribedPairsField.SetValue(client, subscribedPairs);
        
        // Setup OrderBookChannels using reflection
        var orderBookChannels = new Dictionary<TradingPair, System.Threading.Channels.Channel<OrderBook>>
        {
            { 
                new TradingPair("BTC", "USD"), 
                System.Threading.Channels.Channel.CreateUnbounded<OrderBook>() 
            }
        };
        
        var orderBooksField = typeof(BaseExchangeClient)
            .GetField("OrderBookChannels", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(orderBooksField); // Fail test if field doesn't exist
        orderBooksField.SetValue(client, orderBookChannels);
        
        // Create a snapshot message that would come from the WebSocket
        var snapshotMessage = @"{
            ""type"": ""snapshot"",
            ""product_id"": ""BTC-USD"",
            ""bids"": [
                [""34000.01"", ""1.5""],
                [""34000.00"", ""2.5""],
                [""33999.99"", ""0.75""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [""34002.50"", ""1.25""],
                [""34005.00"", ""3.0""]
            ]
        }";
        
        // Access the ProcessWebSocketMessageAsync method using reflection
        var method = typeof(CoinbaseExchangeClient).GetMethod(
            "ProcessWebSocketMessageAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method); // Fail test if method doesn't exist
        
        // Act
        var task = method.Invoke(client, new object[] { snapshotMessage, CancellationToken.None });
        Assert.NotNull(task); // Ensure the method returned a task
        await (Task)task;
        
        // Use reflection to access the OrderBooks dictionary
        var orderBooksField2 = typeof(BaseExchangeClient)
            .GetField("OrderBooks", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(orderBooksField2);
        var orderBooksObj = orderBooksField2.GetValue(client);
        Assert.NotNull(orderBooksObj);
        var orderBooks = (Dictionary<TradingPair, OrderBook>)orderBooksObj;
        
        // Assert
        Assert.Single(orderBooks);
        Assert.True(orderBooks.ContainsKey(new TradingPair("BTC", "USD")));
        
        var orderBook = orderBooks[new TradingPair("BTC", "USD")];
        Assert.Equal(3, orderBook.Bids.Count);
        Assert.Equal(3, orderBook.Asks.Count);
        
        // Verify bid sorting (descending)
        Assert.True(orderBook.Bids[0].Price > orderBook.Bids[1].Price);
        Assert.True(orderBook.Bids[1].Price > orderBook.Bids[2].Price);
        
        // Verify ask sorting (ascending)
        Assert.True(orderBook.Asks[0].Price < orderBook.Asks[1].Price);
        Assert.True(orderBook.Asks[1].Price < orderBook.Asks[2].Price);
    }
    
    [Fact]
    public async Task ProcessWebSocketMessage_Level2Update_UpdatesOrderBook()
    {
        // Arrange
        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        
        // Setup the subscribed pairs dictionary using reflection
        var subscribedPairs = new Dictionary<string, TradingPair> 
        {
            { "BTC-USD", new TradingPair("BTC", "USD") }
        };
        
        var subscribedPairsField = typeof(CoinbaseExchangeClient)
            .GetField("_subscribedPairs", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(subscribedPairsField); // Fail test if field doesn't exist
        subscribedPairsField.SetValue(client, subscribedPairs);
        
        // Setup OrderBooks using reflection with initial data
        var initialBids = new List<OrderBookEntry>
        {
            new OrderBookEntry(34000.01m, 1.5m),
            new OrderBookEntry(34000.00m, 2.5m),
            new OrderBookEntry(33999.99m, 0.75m)
        };
        
        var initialAsks = new List<OrderBookEntry>
        {
            new OrderBookEntry(34001.01m, 0.5m),
            new OrderBookEntry(34002.50m, 1.25m),
            new OrderBookEntry(34005.00m, 3.0m)
        };
        
        var initialOrderBook = new OrderBook(
            "coinbase", 
            new TradingPair("BTC", "USD"), 
            DateTime.UtcNow, 
            initialBids, 
            initialAsks);
        
        var orderBooks = new Dictionary<TradingPair, OrderBook>
        {
            { new TradingPair("BTC", "USD"), initialOrderBook }
        };
        
        var orderBooksField = typeof(BaseExchangeClient)
            .GetField("OrderBooks", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(orderBooksField); // Fail test if field doesn't exist
        orderBooksField.SetValue(client, orderBooks);
        
        // Setup OrderBookChannels using reflection
        var orderBookChannels = new Dictionary<TradingPair, System.Threading.Channels.Channel<OrderBook>>
        {
            { 
                new TradingPair("BTC", "USD"), 
                System.Threading.Channels.Channel.CreateUnbounded<OrderBook>() 
            }
        };
        
        var orderBookChannelsField = typeof(BaseExchangeClient)
            .GetField("OrderBookChannels", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(orderBookChannelsField); // Fail test if field doesn't exist
        orderBookChannelsField.SetValue(client, orderBookChannels);
        
        // Initialize internal order book state by processing a snapshot first
        var initSnapshotMessage = @"{
            ""type"": ""snapshot"",
            ""product_id"": ""BTC-USD"",
            ""bids"": [
                [""34000.01"", ""1.5""],
                [""34000.00"", ""2.5""],
                [""33999.99"", ""0.75""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [""34002.50"", ""1.25""],
                [""34005.00"", ""3.0""]
            ]
        }";
        var method = typeof(CoinbaseExchangeClient).GetMethod(
            "ProcessWebSocketMessageAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method); // Fail test if method doesn't exist
        var initTask = method.Invoke(client, new object[] { initSnapshotMessage, CancellationToken.None });
        Assert.NotNull(initTask);
        await (Task)initTask;
        
        // Create a level2 update message that would come from the WebSocket
        var updateMessage = @"{
            ""type"": ""l2update"",
            ""product_id"": ""BTC-USD"",
            ""changes"": [
                [""buy"", ""34000.50"", ""1.75""],
                [""sell"", ""34001.01"", ""0""],
                [""sell"", ""34003.00"", ""0.5""]
            ],
            ""time"": ""2025-05-21T12:01:00Z""
        }";
        
        // Act
        var task = method.Invoke(client, new object[] { updateMessage, CancellationToken.None });
        Assert.NotNull(task); // Ensure the method returned a task
        await (Task)task;
        
        // Get the updated OrderBooks
        var orderBooksAfterUpdate = (Dictionary<TradingPair, OrderBook>)orderBooksField.GetValue(client)!;
        var updatedOrderBook = orderBooksAfterUpdate[new TradingPair("BTC", "USD")];
        
        // Assert
        // Check for the added buy entry
        var newBidEntry = updatedOrderBook.Bids.FirstOrDefault(b => Math.Abs(b.Price - 34000.50m) < 0.001m);
        Assert.NotEqual(0, newBidEntry.Quantity);
        Assert.True(Math.Abs(newBidEntry.Quantity - 1.75m) < 0.001m);
        
        // Check there are no asks with the price 34001.01 - it should be removed because size is 0
        bool hasAskWithPrice = updatedOrderBook.Asks.Any(a => Math.Abs(a.Price - 34001.01m) < 0.001m);
        Assert.False(hasAskWithPrice);
        
        // Check for the added ask entry
        var newAskEntry = updatedOrderBook.Asks.FirstOrDefault(a => Math.Abs(a.Price - 34003.00m) < 0.001m);
        Assert.NotEqual(0, newAskEntry.Quantity);
        Assert.True(Math.Abs(newAskEntry.Quantity - 0.5m) < 0.001m);
    }
    
    [Fact]
    public async Task ProcessWebSocketMessage_Error_LogsMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CoinbaseExchangeClient>>();
        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, mockLogger.Object);
        
        // Create an error message
        var errorMessage = @"{
            ""type"": ""error"",
            ""message"": ""Invalid auth credentials""
        }";
        
        // Access the ProcessWebSocketMessageAsync method using reflection
        var method = typeof(CoinbaseExchangeClient).GetMethod(
            "ProcessWebSocketMessageAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method); // Fail test if method doesn't exist
        
        // Act
        var task = method.Invoke(client, new object[] { errorMessage, CancellationToken.None });
        Assert.NotNull(task); // Ensure the method returned a task
        await (Task)task;
        
        // Assert - verify any log message about error was called at least once
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid auth credentials")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.AtLeastOnce);
    }
    
    [Fact]
    public void UpdateOrderBookSide_WithNewEntry_AddsEntry()
    {
        // Arrange
        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var entries = new List<OrderBookEntry>
        {
            new OrderBookEntry(34000.00m, 1.0m),
            new OrderBookEntry(33999.00m, 2.0m)
        };
        
        // Access the UpdateOrderBookSide method using reflection
        var method = typeof(CoinbaseExchangeClient).GetMethod(
            "UpdateOrderBookSide", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method); // Fail test if method doesn't exist
        
        // Act
        method.Invoke(client, new object[] { entries, 34001.00m, 1.5m });
        
        // Assert
        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => Math.Abs(e.Price - 34001.00m) < 0.001m && Math.Abs(e.Quantity - 1.5m) < 0.001m);
    }
    
    [Fact]
    public void UpdateOrderBookSide_WithZeroSize_RemovesEntry()
    {
        // Arrange
        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var entries = new List<OrderBookEntry>
        {
            new OrderBookEntry(34000.00m, 1.0m),
            new OrderBookEntry(33999.00m, 2.0m)
        };
        
        // Access the UpdateOrderBookSide method using reflection
        var method = typeof(CoinbaseExchangeClient).GetMethod(
            "UpdateOrderBookSide", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method); // Fail test if method doesn't exist
        
        // Act
        method.Invoke(client, new object[] { entries, 34000.00m, 0m });
        
        // Assert
        Assert.Single(entries);
        Assert.DoesNotContain(entries, e => Math.Abs(e.Price - 34000.00m) < 0.001m);
    }
    
    [Fact]
    public void UpdateOrderBookSide_WithExistingPrice_UpdatesQuantity()
    {
        // Arrange
        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var entries = new List<OrderBookEntry>
        {
            new OrderBookEntry(34000.00m, 1.0m),
            new OrderBookEntry(33999.00m, 2.0m)
        };
        
        // Access the UpdateOrderBookSide method using reflection
        var method = typeof(CoinbaseExchangeClient).GetMethod(
            "UpdateOrderBookSide", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method); // Fail test if method doesn't exist
        
        // Act
        method.Invoke(client, new object[] { entries, 34000.00m, 3.5m });
        
        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => Math.Abs(e.Price - 34000.00m) < 0.001m && Math.Abs(e.Quantity - 3.5m) < 0.001m);
    }
} 