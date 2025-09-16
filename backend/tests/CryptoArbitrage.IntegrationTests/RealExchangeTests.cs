using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CryptoArbitrage.Infrastructure.Validation;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CryptoArbitrage.IntegrationTests;

/// <summary>
/// Integration tests that connect to real exchange public APIs
/// These tests verify our message schemas against actual exchange responses
/// </summary>
public class RealExchangeTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<RealExchangeTests> _logger;
    private readonly ISchemaValidator _schemaValidator;
    private readonly ServiceProvider _serviceProvider;

    public RealExchangeTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<ISchemaValidator, SchemaValidator>();
        
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<RealExchangeTests>>();
        _schemaValidator = _serviceProvider.GetRequiredService<ISchemaValidator>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("External", "Coinbase")]
    public async Task Coinbase_WebSocket_Subscribe_Message_IsAccepted()
    {
        // Skip if running in CI without network access
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            _output.WriteLine("Skipping real exchange test in CI environment");
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var ws = new ClientWebSocket();

        try
        {
            // Connect to Coinbase WebSocket
            var uri = new Uri("wss://ws-feed.exchange.coinbase.com");
            await ws.ConnectAsync(uri, cts.Token);
            _output.WriteLine("Connected to Coinbase WebSocket");

            // Test our subscribe message format
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

            // Validate against our schema first
            var validation = _schemaValidator.ValidatePayload("coinbase/ws/subscribe.schema.json", 
                JsonSerializer.Serialize(subscribeMessage));
            Assert.True(validation.IsValid, $"Schema validation failed: {string.Join("; ", validation.Errors)}");

            // Send to real Coinbase
            var messageJson = JsonSerializer.Serialize(subscribeMessage);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cts.Token);
            _output.WriteLine($"Sent: {messageJson}");

            // Wait for response
            var buffer = new byte[4096];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
            _output.WriteLine($"Received: {response}");

            // Parse response and check it's not an error
            var responseJson = JsonNode.Parse(response);
            Assert.NotNull(responseJson);
            
            var messageType = responseJson["type"]?.GetValue<string>();
            
            // Should receive subscriptions confirmation, not error
            if (messageType == "error")
            {
                var errorMessage = responseJson["message"]?.GetValue<string>() ?? "Unknown error";
                var errorReason = responseJson["reason"]?.GetValue<string>() ?? "No reason";
                Assert.Fail($"Coinbase rejected our subscription: {errorMessage} - {errorReason}");
            }

            Assert.Equal("subscriptions", messageType);
            _output.WriteLine("✓ Coinbase accepted our subscription format");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test failed: {ex.Message}");
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("External", "Kraken")]
    public async Task Kraken_WebSocket_Subscribe_Message_IsAccepted()
    {
        // Skip if running in CI without network access
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            _output.WriteLine("Skipping real exchange test in CI environment");
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var ws = new ClientWebSocket();

        try
        {
            // Connect to Kraken WebSocket
            var uri = new Uri("wss://ws.kraken.com");
            await ws.ConnectAsync(uri, cts.Token);
            _output.WriteLine("Connected to Kraken WebSocket");

            // Test our subscribe message format
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

            // Validate against our schema first
            var validation = _schemaValidator.ValidatePayload("kraken/ws-v1/subscribe.schema.json", 
                JsonSerializer.Serialize(subscribeMessage));
            Assert.True(validation.IsValid, $"Schema validation failed: {string.Join("; ", validation.Errors)}");

            // Send to real Kraken
            var messageJson = JsonSerializer.Serialize(subscribeMessage);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cts.Token);
            _output.WriteLine($"Sent: {messageJson}");

            // Wait for response
            var buffer = new byte[4096];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
            _output.WriteLine($"Received: {response}");

            // Parse response and check it's not an error
            var responseJson = JsonNode.Parse(response);
            Assert.NotNull(responseJson);
            
            // Kraken responses can be arrays or objects
            if (responseJson is JsonObject obj)
            {
                var errorMsg = obj["errorMessage"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Assert.Fail($"Kraken rejected our subscription: {errorMsg}");
                }
                
                var status = obj["status"]?.GetValue<string>();
                Assert.Equal("subscribed", status);
            }
            
            _output.WriteLine("✓ Kraken accepted our subscription format");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test failed: {ex.Message}");
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Coinbase_Live_Messages_Match_Schemas()
    {
        // Skip if running in CI without network access
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            _output.WriteLine("Skipping real exchange test in CI environment");
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var ws = new ClientWebSocket();

        try
        {
            var uri = new Uri("wss://ws-feed.exchange.coinbase.com");
            await ws.ConnectAsync(uri, cts.Token);

            // Subscribe to level2 updates
            var subscribeMessage = new
            {
                type = "subscribe",
                product_ids = new[] { "BTC-USD" },
                channels = new object[]
                {
                    new { name = "level2", product_ids = new[] { "BTC-USD" } }
                }
            };

            var messageJson = JsonSerializer.Serialize(subscribeMessage);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cts.Token);

            var messagesReceived = 0;
            var snapshotReceived = false;
            var updateReceived = false;

            while (!cts.Token.IsCancellationRequested && messagesReceived < 10)
            {
                var buffer = new byte[8192];
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                var responseJson = JsonNode.Parse(response);
                var messageType = responseJson?["type"]?.GetValue<string>();

                _output.WriteLine($"Received {messageType}: {response.Substring(0, Math.Min(200, response.Length))}...");

                switch (messageType)
                {
                    case "subscriptions":
                        // Validate subscriptions message
                        var subscriptionsValidation = _schemaValidator.ValidatePayload("coinbase/ws/subscriptions.schema.json", response);
                        Assert.True(subscriptionsValidation.IsValid, $"Subscriptions schema validation failed: {string.Join("; ", subscriptionsValidation.Errors)}");
                        break;
                        
                    case "snapshot":
                        // Validate snapshot message
                        var snapshotValidation = _schemaValidator.ValidatePayload("coinbase/ws/snapshot.schema.json", response);
                        Assert.True(snapshotValidation.IsValid, $"Snapshot schema validation failed: {string.Join("; ", snapshotValidation.Errors)}");
                        snapshotReceived = true;
                        break;
                        
                    case "l2update":
                        // Validate l2update message
                        var updateValidation = _schemaValidator.ValidatePayload("coinbase/ws/l2update.schema.json", response);
                        Assert.True(updateValidation.IsValid, $"L2 update schema validation failed: {string.Join("; ", updateValidation.Errors)}");
                        updateReceived = true;
                        break;
                }

                messagesReceived++;
                
                if (snapshotReceived && updateReceived)
                {
                    break; // We've validated the key message types
                }
            }

            Assert.True(snapshotReceived, "Should have received at least one snapshot message");
            Assert.True(updateReceived, "Should have received at least one l2update message");
            _output.WriteLine("✓ All received Coinbase messages matched their schemas");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Live message validation failed: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

[CollectionDefinition("RealExchange")]
public class RealExchangeCollection : ICollectionFixture<RealExchangeTests>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
} 