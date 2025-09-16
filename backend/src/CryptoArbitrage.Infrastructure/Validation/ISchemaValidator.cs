using System.Text.Json.Nodes;

namespace CryptoArbitrage.Infrastructure.Validation;

/// <summary>
/// Interface for validating JSON messages against schemas
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates a JSON payload against a schema
    /// </summary>
    /// <param name="schemaPath">Path to the schema file relative to specs directory</param>
    /// <param name="payload">JSON payload to validate</param>
    /// <returns>Validation result with success status and error details</returns>
    ValidationResult ValidatePayload(string schemaPath, JsonNode payload);
    
    /// <summary>
    /// Validates a JSON string against a schema
    /// </summary>
    /// <param name="schemaPath">Path to the schema file relative to specs directory</param>
    /// <param name="payloadJson">JSON string to validate</param>
    /// <returns>Validation result with success status and error details</returns>
    ValidationResult ValidatePayload(string schemaPath, string payloadJson);
}

/// <summary>
/// Result of schema validation
/// </summary>
public record ValidationResult(
    bool IsValid,
    string[] Errors
)
{
    public static ValidationResult Success() => new(true, Array.Empty<string>());
    public static ValidationResult Failure(params string[] errors) => new(false, errors);
} 