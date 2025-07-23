using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Events;

/// <summary>
/// Event published when portfolio risk thresholds are exceeded.
/// </summary>
public record RiskThresholdExceededEvent : INotification
{
    /// <summary>
    /// Type of risk threshold that was exceeded.
    /// </summary>
    public required string RiskType { get; init; }

    /// <summary>
    /// Current risk value.
    /// </summary>
    public decimal CurrentValue { get; init; }

    /// <summary>
    /// Risk threshold that was exceeded.
    /// </summary>
    public decimal ThresholdValue { get; init; }

    /// <summary>
    /// Risk severity level.
    /// </summary>
    public RiskSeverity Severity { get; init; } = RiskSeverity.Medium;

    /// <summary>
    /// Detailed risk message.
    /// </summary>
    public required string RiskMessage { get; init; }

    /// <summary>
    /// Risk profile that was violated.
    /// </summary>
    public RiskProfile? RiskProfile { get; init; }

    /// <summary>
    /// Affected assets or exchanges.
    /// </summary>
    public IReadOnlyList<string> AffectedAssets { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Recommended actions to mitigate risk.
    /// </summary>
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when the risk threshold was exceeded.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this risk breach requires immediate action.
    /// </summary>
    public bool RequiresImmediateAction { get; init; }

    /// <summary>
    /// Portfolio value when risk was detected.
    /// </summary>
    public decimal PortfolioValueAtDetection { get; init; }
}

/// <summary>
/// Risk severity levels.
/// </summary>
public enum RiskSeverity
{
    /// <summary>
    /// Low risk - informational only.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk - should be monitored.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk - requires attention.
    /// </summary>
    High,

    /// <summary>
    /// Critical risk - requires immediate action.
    /// </summary>
    Critical
} 