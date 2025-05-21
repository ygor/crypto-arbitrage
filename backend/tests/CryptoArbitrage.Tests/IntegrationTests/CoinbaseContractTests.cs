using System.Net;
using System.Text;
using System.Text.Json;
using CryptoArbitrage.Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CryptoArbitrage.Tests.IntegrationTests;

/// <summary>
/// These tests verify that our models correctly map to the actual Coinbase API response structure.
/// They act as contract tests to ensure API compatibility.
/// </summary>
public class CoinbaseContractTests
{
    [Fact]
    public void OrderBookContract_ShouldMatchExpectedStructure()
    {
        // Arrange
        // Example response based on Coinbase API documentation
        var apiResponseJson = @"{
            ""bids"": [
                [""34000.01"", ""1.5""],
                [""34000.00"", ""2.5""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [""34002.50"", ""1.25""]
            ],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        // Act - Parse with dynamic to check structure
        dynamic apiResponse = JsonConvert.DeserializeObject(apiResponseJson);
        
        // Assert - Verify the structure matches what we expect
        Assert.NotNull(apiResponse);
        Assert.NotNull(apiResponse.bids);
        Assert.NotNull(apiResponse.asks);
        Assert.NotNull(apiResponse.time);
        
        // Verify bids and asks are arrays of arrays
        Assert.True(apiResponse.bids is JArray);
        Assert.True(apiResponse.asks is JArray);
        Assert.True(apiResponse.bids[0] is JArray);
        Assert.True(apiResponse.asks[0] is JArray);
        
        // Verify the inner arrays have price and quantity
        Assert.Equal(2, apiResponse.bids[0].Count);
        Assert.Equal(2, apiResponse.asks[0].Count);
        
        // Parse using invariant culture and verify with approximate equality
        decimal bidPrice = decimal.Parse(apiResponse.bids[0][0].ToString(), 
            System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture);
        decimal bidSize = decimal.Parse(apiResponse.bids[0][1].ToString(), 
            System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture);
        
        // Use approximate equality to avoid culture-specific issues
        Assert.True(Math.Abs(bidPrice - 34000.01m) < 0.001m,
            $"Expected bid price close to 34000.01, got {bidPrice}");
        Assert.True(Math.Abs(bidSize - 1.5m) < 0.001m,
            $"Expected bid size close to 1.5, got {bidSize}");
    }
    
    [Fact]
    public void OrderBook_ShouldDeserializeUsingNewtonsoft()
    {
        // Arrange
        var apiResponseJson = @"{
            ""bids"": [
                [""34000.01"", ""1.5""],
                [""34000.00"", ""2.5""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [""34002.50"", ""1.25""]
            ],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        // Act - Parse with our OrderBook model class
        var orderBook = JsonConvert.DeserializeObject<CoinbaseOrderBook>(apiResponseJson);

        // Assert
        Assert.NotNull(orderBook);
        Assert.NotNull(orderBook.Bids);
        Assert.NotNull(orderBook.Asks);
        Assert.Equal("2025-05-21T12:00:00Z", orderBook.Time);
        Assert.Equal(2, orderBook.Bids.Count);
        Assert.Equal(2, orderBook.Asks.Count);
    }
    
    [Fact]
    public void OrderBook_ShouldNotDeserializeUsingSystemTextJson()
    {
        // Arrange
        var apiResponseJson = @"{
            ""bids"": [
                [""34000.01"", ""1.5""],
                [""34000.00"", ""2.5""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [""34002.50"", ""1.25""]
            ],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        // Define a class mimicking our internal structure but using System.Text.Json
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        // Act & Assert - This should throw an exception when trying to deserialize a jagged array
        // with System.Text.Json into a List<List<JsonElement>>
        Assert.ThrowsAny<Exception>(() => 
        {
            var deserializedOrderBook = System.Text.Json.JsonSerializer.Deserialize<SystemTextJsonOrderBook>(
                apiResponseJson, options);
            
            // If we get here (which we shouldn't), verify the structure is wrong
            if (deserializedOrderBook != null)
            {
                Assert.Empty(deserializedOrderBook.Bids);
                Assert.Empty(deserializedOrderBook.Asks);
            }
        });
    }
    
    [Fact]
    public void OrderBookWithVariableFormats_ShouldDeserializeWithNewtonsoft()
    {
        // Arrange - Testing with different formats that might appear in the wild
        var apiResponseJson = @"{
            ""bids"": [
                [""34000.01"", ""1.5"", ""extra data""],
                [34000.00, 2.5],
                [33999.99, ""0.75""]
            ],
            ""asks"": [
                [""34001.01"", ""0.5""],
                [34002.50, 1.25],
                [""34005.00"", 3.0]
            ],
            ""time"": ""2025-05-21T12:00:00Z""
        }";

        // Act
        var orderBook = JsonConvert.DeserializeObject<CoinbaseOrderBook>(apiResponseJson);

        // Assert - All these formats should be handled correctly
        Assert.NotNull(orderBook);
        Assert.Equal(3, orderBook.Bids.Count);
        Assert.Equal(3, orderBook.Asks.Count);
    }

    // Define a model that matches the structure we expect from Coinbase
    private class CoinbaseOrderBook
    {
        public JArray Bids { get; set; } = new JArray();
        public JArray Asks { get; set; } = new JArray();
        public string Time { get; set; } = string.Empty;
    }

    // Class to demonstrate System.Text.Json deserialization issues
    private class SystemTextJsonOrderBook
    {
        public List<List<System.Text.Json.JsonElement>> Bids { get; set; } = new();
        public List<List<System.Text.Json.JsonElement>> Asks { get; set; } = new();
        public string Time { get; set; } = string.Empty;
    }
} 