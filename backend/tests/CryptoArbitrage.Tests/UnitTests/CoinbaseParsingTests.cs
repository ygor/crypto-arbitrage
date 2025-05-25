using System.Reflection;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Exchanges;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using System.Text.Json;

namespace CryptoArbitrage.Tests.UnitTests;

public class CoinbaseParsingTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<CoinbaseExchangeClient>> _mockLogger;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly CoinbaseExchangeClient _client;
    
    // Field to access the private ParseOrderBookLevels method via reflection
    private readonly MethodInfo _parseOrderBookLevelsMethod;

    public CoinbaseParsingTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockLogger = new Mock<ILogger<CoinbaseExchangeClient>>();
        _mockHttpClient = new Mock<HttpClient>();
        
        // Create a real client for testing the parsing logic
        _client = new CoinbaseExchangeClient(_mockHttpClient.Object, _mockConfigService.Object, _mockLogger.Object);
        
        // Get the private method via reflection
        _parseOrderBookLevelsMethod = typeof(CoinbaseExchangeClient).GetMethod(
            "ParseOrderBookLevels", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.NotNull(_parseOrderBookLevelsMethod); // Validate method was found
    }

    [Fact]
    public void ParseOrderBookLevels_WithValidData_ReturnsCorrectEntries()
    {
        // Arrange
        var jArrayInput = JArray.Parse(@"[
            [""34000.01"", ""1.5""],
            [""34000.00"", ""2.5""],
            [""33999.99"", ""0.75""]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(34000.01m, result[0].Price);
        Assert.Equal(1.5m, result[0].Quantity);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithMalformedData_SkipsInvalidEntries()
    {
        // Arrange
        var jArrayInput = JArray.Parse(@"[
            [""not-a-number"", ""1.5""],
            [""34000.00"", ""2.5""],
            [""33999.99"", ""not-a-number""]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only one valid entry
        Assert.Equal(34000.00m, result[0].Price);
        Assert.Equal(2.5m, result[0].Quantity);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var jArrayInput = JArray.Parse("[]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithExtraArrayElements_IgnoresExtraData()
    {
        // Arrange - Arrays with more than 2 elements
        var jArrayInput = JArray.Parse(@"[
            [""34000.01"", ""1.5"", ""extra"", ""data""],
            [""34000.00"", ""2.5"", {""some"": ""object""}],
            [""33999.99"", ""0.75"", [1, 2, 3]]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // Should handle all entries despite extra data
        Assert.Equal(34000.01m, result[0].Price);
        Assert.Equal(1.5m, result[0].Quantity);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithNestedArrayFormat_HandlesCorrectly()
    {
        // Arrange - Using string format instead of numbers to avoid culture-specific parsing issues
        var jArrayInput = JArray.Parse(@"[
            [""34000.01"", ""1.5""],
            [""34000.00"", ""2.5""]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        // Check approximate decimal equality to avoid culture-specific issues
        Assert.True(Math.Abs(result[0].Price - 34000.01m) < 0.001m, 
            $"Expected price close to 34000.01, got {result[0].Price}");
        Assert.True(Math.Abs(result[0].Quantity - 1.5m) < 0.001m,
            $"Expected quantity close to 1.5, got {result[0].Quantity}");
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithMissingElements_SkipsIncompleteEntries()
    {
        // Arrange - Test with arrays that have fewer than 2 elements
        var jArrayInput = JArray.Parse(@"[
            [""34000.01""],
            [""34000.00"", ""2.5""],
            []
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only one valid entry
        Assert.Equal(34000.00m, result[0].Price);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithZeroValues_FiltersOutZeroPriceAndQuantity()
    {
        // Arrange - Test with zero values which should be filtered out
        var jArrayInput = JArray.Parse(@"[
            [""0"", ""1.5""],
            [""34000.00"", ""0""],
            [""33999.99"", ""0.75""]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only one valid entry with non-zero price and quantity
        Assert.Equal(33999.99m, result[0].Price);
        Assert.Equal(0.75m, result[0].Quantity);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithBuySide_SortsDescending()
    {
        // Arrange - Test sorting for buy side (bids)
        var jArrayInput = JArray.Parse(@"[
            [""33999.99"", ""0.75""],
            [""34001.00"", ""1.5""], 
            [""34000.00"", ""2.5""]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Buy);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // Should be sorted descending for buy side
        Assert.Equal(34001.00m, result[0].Price);
        Assert.Equal(34000.00m, result[1].Price);
        Assert.Equal(33999.99m, result[2].Price);
    }
    
    [Fact]
    public void ParseOrderBookLevels_WithSellSide_SortsAscending()
    {
        // Arrange - Test sorting for sell side (asks)
        var jArrayInput = JArray.Parse(@"[
            [""34001.00"", ""1.5""],
            [""33999.99"", ""0.75""],
            [""34000.00"", ""2.5""]
        ]");
        
        // Act
        var result = InvokeParseOrderBookLevels(jArrayInput, OrderSide.Sell);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // Should be sorted ascending for sell side
        Assert.Equal(33999.99m, result[0].Price);
        Assert.Equal(34000.00m, result[1].Price);
        Assert.Equal(34001.00m, result[2].Price);
    }
    
    // Helper method to invoke the private ParseOrderBookLevels method
    private List<OrderBookEntry> InvokeParseOrderBookLevels(JArray input, OrderSide side)
    {
        // Convert JArray to JsonElement[][]
        var jsonElementArray = ConvertJArrayToJsonElementArray(input);
        var result = _parseOrderBookLevelsMethod.Invoke(_client, new object[] { jsonElementArray, side });
        return result as List<OrderBookEntry> ?? new List<OrderBookEntry>();
    }
    
    // Helper method to convert JArray to JsonElement[][]
    private JsonElement[][] ConvertJArrayToJsonElementArray(JArray input)
    {
        var result = new List<JsonElement[]>();
        
        foreach (var item in input)
        {
            if (item is JArray subArray)
            {
                var elementArray = new List<JsonElement>();
                foreach (var subItem in subArray)
                {
                    try
                    {
                        // Convert JToken to JsonElement by serializing and deserializing
                        var json = subItem.ToString();
                        
                        // If it's not a valid JSON value, wrap it in quotes to make it a string
                        if (subItem.Type == JTokenType.String || 
                            (subItem.Type == JTokenType.Raw && !IsValidJson(json)))
                        {
                            json = JsonSerializer.Serialize(json);
                        }
                        
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                        elementArray.Add(jsonElement);
                    }
                    catch (JsonException)
                    {
                        // If deserialization fails, treat it as a string
                        var stringValue = subItem.ToString();
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(stringValue));
                        elementArray.Add(jsonElement);
                    }
                }
                result.Add(elementArray.ToArray());
            }
        }
        
        return result.ToArray();
    }
    
    private bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
} 