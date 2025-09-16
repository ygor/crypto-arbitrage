using System.Text.Json;
using CryptoArbitrage.Infrastructure.Validation;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Service for validating exchange WebSocket messages against schemas
/// This is the core component that prevents runtime failures by ensuring
/// our messages match what exchanges expect
/// </summary>
public class ExchangeMessageValidator
{
    private readonly ISchemaValidator _schemaValidator;
    private readonly ILogger<ExchangeMessageValidator> _logger;
    private readonly Dictionary<string, string> _subscribeSchemaMap;

    public ExchangeMessageValidator(ISchemaValidator schemaValidator, ILogger<ExchangeMessageValidator> logger)
    {
        _schemaValidator = schemaValidator;
        _logger = logger;
        
        // Map exchange IDs to their subscribe schema paths
        _subscribeSchemaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "coinbase", "coinbase/ws/subscribe.schema.json" },
            { "kraken", "kraken/ws-v1/subscribe.schema.json" }
        };
    }

    /// <summary>
    /// Validates a WebSocket subscription message before sending to an exchange
    /// </summary>
    /// <param name="exchangeId">The exchange identifier (e.g., "coinbase", "kraken")</param>
    /// <param name="subscriptionMessage">The subscription message object</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateSubscriptionMessage(string exchangeId, object subscriptionMessage)
    {
        try
        {
            if (!_subscribeSchemaMap.TryGetValue(exchangeId, out var schemaPath))
            {
                _logger.LogWarning("No schema validation available for exchange {ExchangeId}", exchangeId);
                return true; // Allow if no schema available
            }

            var json = JsonSerializer.Serialize(subscriptionMessage);
            var result = _schemaValidator.ValidatePayload(schemaPath, json);

            if (!result.IsValid)
            {
                _logger.LogError("WebSocket subscription message failed schema validation for {ExchangeId}: {Errors}. Message: {Message}", 
                    exchangeId, string.Join("; ", result.Errors), json);
                return false;
            }

            _logger.LogDebug("WebSocket subscription message validated successfully for {ExchangeId}", exchangeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription message for {ExchangeId}", exchangeId);
            return false;
        }
    }

    /// <summary>
    /// Validates an incoming WebSocket message from an exchange
    /// </summary>
    /// <param name="exchangeId">The exchange identifier</param>
    /// <param name="messageType">The message type (e.g., "snapshot", "l2update")</param>
    /// <param name="messageJson">The raw JSON message</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateIncomingMessage(string exchangeId, string messageType, string messageJson)
    {
        try
        {
            var schemaPath = GetIncomingMessageSchemaPath(exchangeId, messageType);
            if (schemaPath == null)
            {
                // No schema available for this message type - allow it
                return true;
            }

            var result = _schemaValidator.ValidatePayload(schemaPath, messageJson);

            if (!result.IsValid)
            {
                _logger.LogWarning("Incoming message failed schema validation for {ExchangeId} {MessageType}: {Errors}", 
                    exchangeId, messageType, string.Join("; ", result.Errors));
                // Don't fail hard on incoming messages - log and continue
                return false;
            }

            _logger.LogTrace("Incoming message validated successfully for {ExchangeId} {MessageType}", exchangeId, messageType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating incoming message for {ExchangeId} {MessageType}", exchangeId, messageType);
            return false;
        }
    }

    private string? GetIncomingMessageSchemaPath(string exchangeId, string messageType)
    {
        return exchangeId.ToLowerInvariant() switch
        {
            "coinbase" => messageType switch
            {
                "subscriptions" => "coinbase/ws/subscriptions.schema.json",
                "snapshot" => "coinbase/ws/snapshot.schema.json",
                "l2update" => "coinbase/ws/l2update.schema.json",
                "error" => "coinbase/ws/error.schema.json",
                _ => null
            },
            "kraken" => messageType switch
            {
                "book" => "kraken/ws-v1/book.snapshot.schema.json",
                "ticker" => "kraken/ws-v2/ticker.snapshot.schema.json",
                _ => null
            },
            _ => null
        };
    }
} 