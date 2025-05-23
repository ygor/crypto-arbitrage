using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Api.Models;

/// <summary>
/// Activity log entry for tracking system events and actions
/// </summary>
public class ActivityLogEntry
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    [Required]
    public string id { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the event occurred (ISO 8601 format)
    /// </summary>
    [Required]
    public string timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of activity (Info, Warning, Error, Success)
    /// </summary>
    [Required]
    public string type { get; set; } = string.Empty;
    
    /// <summary>
    /// Brief description of the activity
    /// </summary>
    [Required]
    public string message { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity related to this activity (e.g., "Exchange", "Trade", "System")
    /// </summary>
    public string? relatedEntityType { get; set; }
    
    /// <summary>
    /// Identifier of the specific entity related to this activity
    /// </summary>
    public string? relatedEntityId { get; set; }
    
    /// <summary>
    /// Additional details about the activity
    /// </summary>
    public string? details { get; set; }
} 