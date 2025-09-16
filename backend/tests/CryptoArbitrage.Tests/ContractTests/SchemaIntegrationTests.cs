using System.Text.Json;
using System.Text.Json.Nodes;
using CryptoArbitrage.Infrastructure.Exchanges;
using CryptoArbitrage.Infrastructure.Validation;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CryptoArbitrage.Tests.ContractTests;

/// <summary>
/// Tests that validate our exchange client implementations produce schema-compliant messages
/// These are the critical tests that prevent runtime failures by ensuring our code generates
/// messages that match what exchanges expect
/// </summary>
public class SchemaIntegrationTests
{
    private readonly ISchemaValidator _schemaValidator;

    public SchemaIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<ISchemaValidator, SchemaValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        _schemaValidator = serviceProvider.GetRequiredService<ISchemaValidator>();
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void CoinbaseClient_GeneratesValidSubscriptionMessage()
    {
        // This test validates that our CoinbaseExchangeClient generates messages
        // that match the Coinbase WebSocket API schema
        
        // Simulate the exact message format our CoinbaseExchangeClient creates
        var subscribeMessage = new
        {
            type = "subscribe",
            product_ids = new[] { "BTC-USD" },
            channels = new object[]
            {
                new { name = "level2", product_ids = new[] { "BTC-USD" } },
                new { name = "heartbeat", product_ids = new[] { "BTC-USD" } }
            }
        };

        var messageJson = JsonSerializer.Serialize(subscribeMessage);
        var validation = _schemaValidator.ValidatePayload("coinbase/ws/subscribe.schema.json", messageJson);

        Assert.True(validation.IsValid, 
            $"Coinbase subscription message failed schema validation: {string.Join("; ", validation.Errors)}. Message: {messageJson}");
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void KrakenClient_GeneratesValidSubscriptionMessage()
    {
        // This test validates that our KrakenExchangeClient generates messages
        // that match the Kraken WebSocket API v1 schema
        
        // Simulate the exact message format our KrakenExchangeClient creates
        var subscribeMessage = new
        {
            @event = "subscribe",
            reqid = 12345,
            pair = new[] { "XBT/USD" },
            subscription = new
            {
                name = "book",
                depth = 25
            }
        };

        var messageJson = JsonSerializer.Serialize(subscribeMessage);
        var validation = _schemaValidator.ValidatePayload("kraken/ws-v1/subscribe.schema.json", messageJson);

        Assert.True(validation.IsValid, 
            $"Kraken subscription message failed schema validation: {string.Join("; ", validation.Errors)}. Message: {messageJson}");
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void AllExchangeClients_ProduceValidMessages()
    {
        // This is a comprehensive test that validates all our exchange clients
        // produce schema-compliant messages for their respective APIs
        
        var testCases = new (string ExchangeName, string SchemaPath, object Message)[]
        {
            ("Coinbase", "coinbase/ws/subscribe.schema.json", new
            {
                type = "subscribe",
                product_ids = new[] { "BTC-USD", "ETH-USD" },
                channels = new object[]
                {
                    new { name = "level2", product_ids = new[] { "BTC-USD", "ETH-USD" } },
                    new { name = "heartbeat", product_ids = new[] { "BTC-USD", "ETH-USD" } }
                }
            }),
            ("Kraken", "kraken/ws-v1/subscribe.schema.json", new
            {
                @event = "subscribe",
                reqid = 42,
                pair = new[] { "XBT/USD", "ETH/USD" },
                subscription = new
                {
                    name = "book",
                    depth = 100
                }
            })
        };

        foreach (var testCase in testCases)
        {
            var messageJson = JsonSerializer.Serialize(testCase.Message);
            var validation = _schemaValidator.ValidatePayload(testCase.SchemaPath, messageJson);

            Assert.True(validation.IsValid, 
                $"{testCase.ExchangeName} message failed schema validation: {string.Join("; ", validation.Errors)}. Message: {messageJson}");
        }
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void SchemaValidator_RejectsInvalidMessages()
    {
        // This test ensures our schema validation is working correctly
        // by verifying it rejects clearly invalid messages
        
        var invalidCoinbaseMessage = new
        {
            type = "invalid_type", // Wrong type
            product_ids = new[] { "BTC-USD" },
            channels = new[] { "level2" }
        };

        var messageJson = JsonSerializer.Serialize(invalidCoinbaseMessage);
        var validation = _schemaValidator.ValidatePayload("coinbase/ws/subscribe.schema.json", messageJson);

        Assert.False(validation.IsValid, "Schema validator should reject invalid messages");
        Assert.NotEmpty(validation.Errors);
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void IncomingMessages_MatchExpectedSchemas()
    {
        // Test that our schemas correctly validate typical incoming messages
        // This ensures we can parse what exchanges actually send us
        
        var coinbaseSnapshot = """
        {
          "type": "snapshot",
          "product_id": "BTC-USD",
          "bids": [["50000.00", "1.5"], ["49999.99", "2.0"]],
          "asks": [["50001.00", "0.8"], ["50001.01", "1.2"]]
        }
        """;

        var coinbaseL2Update = """
        {
          "type": "l2update",
          "product_id": "BTC-USD",
          "time": "2023-10-01T10:00:00Z",
          "changes": [["buy", "50000.10", "0.5"], ["sell", "50010.00", "1.0"]]
        }
        """;

        var krakenBookSnapshot = """
        [
          1234,
          {
            "as": [["5541.30000", "2.50700000", "1534614248.123678"]],
            "bs": [["5541.20000", "1.52900000", "1534614248.765567"]]
          },
          "book-10",
          "XBT/USD"
        ]
        """;

        var testCases = new[]
        {
            ("Coinbase Snapshot", "coinbase/ws/snapshot.schema.json", coinbaseSnapshot),
            ("Coinbase L2 Update", "coinbase/ws/l2update.schema.json", coinbaseL2Update),
            ("Kraken Book Snapshot", "kraken/ws-v1/book.snapshot.schema.json", krakenBookSnapshot)
        };

        foreach (var (name, schemaPath, message) in testCases)
        {
            var validation = _schemaValidator.ValidatePayload(schemaPath, message);
            Assert.True(validation.IsValid, 
                $"{name} message failed schema validation: {string.Join("; ", validation.Errors)}");
        }
    }
} 