using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Configuration.Commands.UpdateRiskProfile;

/// <summary>
/// Result of updating the risk profile configuration.
/// </summary>
public record UpdateRiskProfileResult
{
    /// <summary>
    /// Whether the update was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Updated risk profile.
    /// </summary>
    public RiskProfile? UpdatedRiskProfile { get; init; }

    /// <summary>
    /// Previous risk profile before update.
    /// </summary>
    public RiskProfile? PreviousRiskProfile { get; init; }

    /// <summary>
    /// Error message if update failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Validation errors if any.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Warnings about the risk profile configuration.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Changes that were applied.
    /// </summary>
    public IReadOnlyList<string> AppliedChanges { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether the changes require restart of services.
    /// </summary>
    public bool RequiresRestart { get; init; }

    /// <summary>
    /// Duration of the update operation in milliseconds.
    /// </summary>
    public long UpdateDurationMs { get; init; }

    /// <summary>
    /// Timestamp when the update was completed.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static UpdateRiskProfileResult Success(
        RiskProfile updatedRiskProfile,
        RiskProfile? previousRiskProfile,
        long durationMs,
        IReadOnlyList<string>? appliedChanges = null,
        IReadOnlyList<string>? warnings = null,
        bool requiresRestart = false)
    {
        return new UpdateRiskProfileResult
        {
            IsSuccess = true,
            UpdatedRiskProfile = updatedRiskProfile,
            PreviousRiskProfile = previousRiskProfile,
            UpdateDurationMs = durationMs,
            AppliedChanges = appliedChanges ?? Array.Empty<string>(),
            Warnings = warnings ?? Array.Empty<string>(),
            RequiresRestart = requiresRestart
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static UpdateRiskProfileResult Failure(
        string errorMessage,
        long durationMs = 0,
        IReadOnlyList<string>? validationErrors = null)
    {
        return new UpdateRiskProfileResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            UpdateDurationMs = durationMs,
            ValidationErrors = validationErrors ?? Array.Empty<string>()
        };
    }
} 