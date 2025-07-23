using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Configuration.Commands.UpdateRiskProfile;

/// <summary>
/// Command to update the risk profile configuration.
/// </summary>
public record UpdateRiskProfileCommand : IRequest<UpdateRiskProfileResult>
{
    /// <summary>
    /// Updated risk profile.
    /// </summary>
    public required RiskProfile RiskProfile { get; init; }

    /// <summary>
    /// Whether to validate the risk profile before updating.
    /// </summary>
    public bool ValidateProfile { get; init; } = true;

    /// <summary>
    /// Whether to apply the changes immediately to running operations.
    /// </summary>
    public bool ApplyImmediately { get; init; } = true;

    /// <summary>
    /// Optional reason for the update (for audit purposes).
    /// </summary>
    public string? UpdateReason { get; init; }

    /// <summary>
    /// User or system making the update.
    /// </summary>
    public string? UpdatedBy { get; init; }
} 