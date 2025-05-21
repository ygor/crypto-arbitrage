using System.Net;
using System.Text;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Infrastructure.Exchanges;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CryptoArbitrage.Tests.IntegrationTests;

public class CoinbaseExchangeClientTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<CoinbaseExchangeClient>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public CoinbaseExchangeClientTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockLogger = new Mock<ILogger<CoinbaseExchangeClient>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.exchange.coinbase.com")
        };
    }

    [Fact]
    public async Task GetOrderBookSnapshotAsync_WithValidResponse_ReturnsOrderBook()
    {
        // Arrange
        var validResponse = @"{
            ""bids"": [
                [""34000.01"", ""1.5""],
                [""34000.00"", ""2.5""],
                [""33999.99"", ""0.75""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [""34002.50"", ""1.25""],
                [""34005.00"", ""3.0""]
            ],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        SetupMockHttpResponse("/products/BTC-USD/book?level=2", HttpStatusCode.OK, validResponse);

        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var tradingPair = new TradingPair("BTC", "USDT"); // This gets converted to BTC-USD

        // Act
        await client.ConnectAsync();
        var result = await client.GetOrderBookSnapshotAsync(tradingPair);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("coinbase", result.ExchangeId);
        Assert.Equal(tradingPair, result.TradingPair);
        Assert.Equal(3, result.Bids.Count);
        Assert.Equal(3, result.Asks.Count);
        
        // Check bid prices are in descending order
        Assert.True(result.Bids[0].Price > result.Bids[1].Price);
        Assert.True(result.Bids[1].Price > result.Bids[2].Price);
        
        // Check ask prices are in ascending order
        Assert.True(result.Asks[0].Price < result.Asks[1].Price);
        Assert.True(result.Asks[1].Price < result.Asks[2].Price);
    }

    [Fact]
    public async Task GetOrderBookSnapshotAsync_WithEmptyBidsAsks_HandlesGracefully()
    {
        // Arrange
        var emptyResponse = @"{
            ""bids"": [],
            ""asks"": [],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        SetupMockHttpResponse("/products/BTC-USD/book?level=2", HttpStatusCode.OK, emptyResponse);

        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var tradingPair = new TradingPair("BTC", "USDT");

        // Act
        await client.ConnectAsync();
        var result = await client.GetOrderBookSnapshotAsync(tradingPair);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Bids);
        Assert.Empty(result.Asks);
    }

    [Fact]
    public async Task GetOrderBookSnapshotAsync_WithMalformedJson_HandlesError()
    {
        // Arrange
        var malformedResponse = @"{
            ""bids"": [
                [""not-a-number"", ""1.5""],
                [""34000.00"", ""text-not-number""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""]
            ],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        SetupMockHttpResponse("/products/BTC-USD/book?level=2", HttpStatusCode.OK, malformedResponse);

        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var tradingPair = new TradingPair("BTC", "USDT");

        // Act
        await client.ConnectAsync();
        var result = await client.GetOrderBookSnapshotAsync(tradingPair);

        // Assert
        Assert.NotNull(result);
        // Should filter out the invalid data but keep the valid one
        Assert.Empty(result.Bids);
        Assert.Single(result.Asks);
    }

    [Fact]
    public async Task GetOrderBookSnapshotAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        SetupMockHttpResponse("/products/BTC-USD/book?level=2", HttpStatusCode.BadRequest, "Bad Request");

        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var tradingPair = new TradingPair("BTC", "USDT");

        // Act & Assert
        await client.ConnectAsync();
        
        // The client should throw an exception when it gets a bad HTTP response
        var exception = await Assert.ThrowsAsync<CryptoArbitrage.Domain.Exceptions.ExchangeClientException>(() => 
            client.GetOrderBookSnapshotAsync(tradingPair)
        );
        
        // Verify the exception message
        Assert.Contains("Failed to get order book", exception.Message);
        
        // Verify logger was called with warning about the error response
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to get order book from Coinbase")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrderBookSnapshotAsync_WithNestedJsonStructure_HandlesCorrectly()
    {
        // Arrange - Test with a different JSON structure that might cause issues
        var complexResponse = @"{
            ""bids"": [
                [""34000.01"", ""1.5"", ""12345""],
                [""34000.00"", ""2.5"", {""extra"": ""data""}],
                [""33999.99"", ""0.75"", [1, 2, 3]]
            ],
            ""asks"": [
                [""34001.01"", ""0.5"", null],
                [""34002.50"", ""1.25""],
                [""34005.00"", ""3.0"", ""extra""]
            ],
            ""time"": ""2025-05-21T12:00:00Z"",
            ""extra_field"": {""nested"": ""data""}
        }";

        SetupMockHttpResponse("/products/BTC-USD/book?level=2", HttpStatusCode.OK, complexResponse);

        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var tradingPair = new TradingPair("BTC", "USDT");

        // Act
        await client.ConnectAsync();
        var result = await client.GetOrderBookSnapshotAsync(tradingPair);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Bids.Count);
        Assert.Equal(3, result.Asks.Count);
        
        // Verify the first values were parsed correctly despite extra data
        Assert.Equal(34000.01m, result.Bids[0].Price);
        Assert.Equal(1.5m, result.Bids[0].Quantity);
    }

    [Fact]
    public async Task GetOrderBookSnapshotAsync_WithCurrencyConversion_HandlesCorrectly()
    {
        // Arrange - This verifies that USDT is properly converted to USD for Coinbase
        var validResponse = @"{
            ""bids"": [[""34000.01"", ""1.5""]],
            ""asks"": [[""34001.01"", ""0.5""]],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        // The API should be called with BTC-USD not BTC-USDT
        SetupMockHttpResponse("/products/BTC-USD/book?level=2", HttpStatusCode.OK, validResponse);

        var client = new CoinbaseExchangeClient(_httpClient, _mockConfigService.Object, _mockLogger.Object);
        var tradingPair = new TradingPair("BTC", "USDT"); // Should be converted to BTC-USD

        // Act
        await client.ConnectAsync();
        var result = await client.GetOrderBookSnapshotAsync(tradingPair);

        // Assert
        Assert.NotNull(result);
        // Verify the API was called with the correct converted currency
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Get && 
                req.RequestUri.ToString().Contains("BTC-USD")),
            ItExpr.IsAny<CancellationToken>());
    }

    #region Helper Methods

    private void SetupMockHttpResponse(string requestUri, HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString().Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    #endregion
} 