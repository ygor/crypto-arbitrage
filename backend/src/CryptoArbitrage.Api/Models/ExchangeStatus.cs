using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Api.Models;

/// <summary>
/// Exchange status information for monitoring exchange health
/// </summary>
public class ExchangeStatus
{
    /// <summary>
    /// Unique identifier for the exchange
    /// </summary>
    [Required]
    public string exchangeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the exchange
    /// </summary>
    [Required]
    public string exchangeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the exchange is currently up and responding
    /// </summary>
    [Required]
    public bool isUp { get; set; }
    
    /// <summary>
    /// Timestamp when the exchange was last checked (ISO 8601 format)
    /// </summary>
    [Required]
    public string lastChecked { get; set; } = string.Empty;
    
    /// <summary>
    /// Response time in milliseconds for the last health check
    /// </summary>
    [Required]
    public int responseTimeMs { get; set; }
    
    /// <summary>
    /// Additional information about the exchange status
    /// </summary>
    public string? additionalInfo { get; set; }
} 