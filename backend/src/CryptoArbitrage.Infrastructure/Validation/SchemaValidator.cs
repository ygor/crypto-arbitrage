using System.Text.Json.Nodes;
using Json.Schema;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Validation;

/// <summary>
/// Implementation of schema validator using JsonSchema.Net
/// </summary>
public class SchemaValidator : ISchemaValidator
{
    private readonly ILogger<SchemaValidator> _logger;
    private readonly Dictionary<string, JsonSchema> _schemaCache = new();
    private readonly string _specsBasePath;

    public SchemaValidator(ILogger<SchemaValidator> logger)
    {
        _logger = logger;
        _specsBasePath = Path.Combine(AppContext.BaseDirectory, "specs");
    }

    public ValidationResult ValidatePayload(string schemaPath, JsonNode payload)
    {
        try
        {
            var schema = LoadSchema(schemaPath);
            var evaluation = schema.Evaluate(payload, new EvaluationOptions 
            { 
                OutputFormat = OutputFormat.Hierarchical 
            });

            if (evaluation.IsValid)
            {
                return ValidationResult.Success();
            }

            var errors = ExtractErrors(evaluation);
            _logger.LogWarning("Schema validation failed for {SchemaPath}: {Errors}", 
                schemaPath, string.Join("; ", errors));
            
            return ValidationResult.Failure(errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payload against schema {SchemaPath}", schemaPath);
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    public ValidationResult ValidatePayload(string schemaPath, string payloadJson)
    {
        try
        {
            var payload = JsonNode.Parse(payloadJson);
            if (payload == null)
            {
                return ValidationResult.Failure("Invalid JSON payload");
            }
            
            return ValidatePayload(schemaPath, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON payload for schema {SchemaPath}", schemaPath);
            return ValidationResult.Failure($"JSON parsing error: {ex.Message}");
        }
    }

    private JsonSchema LoadSchema(string schemaPath)
    {
        if (_schemaCache.TryGetValue(schemaPath, out var cachedSchema))
        {
            return cachedSchema;
        }

        var fullPath = Path.Combine(_specsBasePath, schemaPath.Replace('/', Path.DirectorySeparatorChar));
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Schema file not found: {fullPath}");
        }

        var schemaText = File.ReadAllText(fullPath);
        var schema = JsonSchema.FromText(schemaText);
        
        _schemaCache[schemaPath] = schema;
        return schema;
    }

    private static string[] ExtractErrors(EvaluationResults evaluation)
    {
        var errors = new List<string>();
        
        if (evaluation.Errors != null)
        {
            errors.AddRange(evaluation.Errors.Values);
        }

        if (evaluation.Details != null)
        {
            foreach (var detail in evaluation.Details)
            {
                if (detail.Errors != null)
                {
                    errors.AddRange(detail.Errors.Values);
                }
            }
        }

        return errors.Count > 0 ? errors.ToArray() : new[] { "Schema validation failed" };
    }
} 