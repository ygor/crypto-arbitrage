using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Configuration.Events;

/// <summary>
/// Event published when configuration is updated.
/// </summary>
public record ConfigurationUpdatedEvent : INotification
{
    /// <summary>
    /// Type of configuration that was updated.
    /// </summary>
    public required string ConfigurationType { get; init; }

    /// <summary>
    /// Identifier for the specific configuration (e.g., exchange ID).
    /// </summary>
    public string? ConfigurationId { get; init; }

    /// <summary>
    /// Previous configuration value (serialized as JSON).
    /// </summary>
    public string? PreviousConfiguration { get; init; }

    /// <summary>
    /// Updated configuration value (serialized as JSON).
    /// </summary>
    public required string UpdatedConfiguration { get; init; }

    /// <summary>
    /// Changes that were applied.
    /// </summary>
    public IReadOnlyList<ConfigurationChange> Changes { get; init; } = Array.Empty<ConfigurationChange>();

    /// <summary>
    /// User or system that made the update.
    /// </summary>
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Reason for the update.
    /// </summary>
    public string? UpdateReason { get; init; }

    /// <summary>
    /// Timestamp when the configuration was updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the change requires system restart.
    /// </summary>
    public bool RequiresRestart { get; init; }

    /// <summary>
    /// Whether the change was applied automatically.
    /// </summary>
    public bool AutoApplied { get; init; }

    /// <summary>
    /// Severity of the configuration change.
    /// </summary>
    public ConfigurationChangeSeverity Severity { get; init; } = ConfigurationChangeSeverity.Medium;
}

/// <summary>
/// Represents a specific configuration change.
/// </summary>
public record ConfigurationChange
{
    /// <summary>
    /// Property or field that was changed.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Previous value.
    /// </summary>
    public string? PreviousValue { get; init; }

    /// <summary>
    /// New value.
    /// </summary>
    public required string NewValue { get; init; }

    /// <summary>
    /// Type of change operation.
    /// </summary>
    public string ChangeType { get; init; } = "Update";

    /// <summary>
    /// Description of the change impact.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Configuration change severity levels.
/// </summary>
public enum ConfigurationChangeSeverity
{
    /// <summary>
    /// Low impact - cosmetic or minor changes.
    /// </summary>
    Low,

    /// <summary>
    /// Medium impact - operational changes.
    /// </summary>
    Medium,

    /// <summary>
    /// High impact - significant behavioral changes.
    /// </summary>
    High,

    /// <summary>
    /// Critical impact - requires immediate attention.
    /// </summary>
    Critical
} 