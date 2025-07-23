using MediatR;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Features.Configuration.Events;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CryptoArbitrage.Application.Features.Configuration.Commands.UpdateRiskProfile;

/// <summary>
/// Handler for updating risk profile configuration.
/// </summary>
public class UpdateRiskProfileHandler : IRequestHandler<UpdateRiskProfileCommand, UpdateRiskProfileResult>
{
    private readonly IConfigurationService _configurationService;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateRiskProfileHandler> _logger;

    public UpdateRiskProfileHandler(
        IConfigurationService configurationService,
        IMediator mediator,
        ILogger<UpdateRiskProfileHandler> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateRiskProfileResult> Handle(UpdateRiskProfileCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        var appliedChanges = new List<string>();

        _logger.LogInformation(
            "Updating risk profile - Type: {RiskType}, UpdatedBy: {UpdatedBy}, Reason: {Reason}",
            request.RiskProfile.Type, request.UpdatedBy ?? "System", request.UpdateReason ?? "Manual update");

        try
        {
            // Get current risk profile for comparison
            var previousProfile = await _configurationService.GetRiskProfileAsync(cancellationToken);

            // Validate the risk profile if requested
            if (request.ValidateProfile)
            {
                var validationResult = ValidateRiskProfile(request.RiskProfile);
                if (!validationResult.IsValid)
                {
                    stopwatch.Stop();
                    return UpdateRiskProfileResult.Failure(
                        "Risk profile validation failed",
                        stopwatch.ElapsedMilliseconds,
                        validationResult.Errors);
                }

                warnings.AddRange(validationResult.Warnings);
            }

            // Compare with previous profile and track changes
            var changes = CompareRiskProfiles(previousProfile, request.RiskProfile);
            appliedChanges.AddRange(changes.Select(c => c.Description ?? $"{c.Property}: {c.PreviousValue} → {c.NewValue}"));

            // Determine if restart is required
            bool requiresRestart = DetermineIfRestartRequired(changes);
            if (requiresRestart)
            {
                warnings.Add("Some changes require system restart to take full effect");
            }

            // Update the risk profile
            await _configurationService.UpdateRiskProfileAsync(request.RiskProfile, cancellationToken);

            stopwatch.Stop();

            // Publish configuration updated event
            await PublishConfigurationEvent(previousProfile, request.RiskProfile, changes, request, cancellationToken);

            _logger.LogInformation(
                "Risk profile updated successfully in {ElapsedMs}ms - Applied {ChangeCount} changes",
                stopwatch.ElapsedMilliseconds, appliedChanges.Count);

            return UpdateRiskProfileResult.Success(
                request.RiskProfile,
                previousProfile,
                stopwatch.ElapsedMilliseconds,
                appliedChanges,
                warnings,
                requiresRestart);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error updating risk profile");
            return UpdateRiskProfileResult.Failure(
                $"Failed to update risk profile: {ex.Message}",
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static RiskProfileValidationResult ValidateRiskProfile(RiskProfile riskProfile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate profit thresholds
        if (riskProfile.MinProfitPercentage < 0)
        {
            errors.Add("Minimum profit percentage cannot be negative");
        }
        else if (riskProfile.MinProfitPercentage < 0.1m)
        {
            warnings.Add("Very low minimum profit percentage may result in unprofitable trades due to fees");
        }

        // Validate risk tolerance
        if (riskProfile.RiskTolerance < 0 || riskProfile.RiskTolerance > 1)
        {
            errors.Add("Risk tolerance must be between 0 and 1");
        }

        // Validate capital allocation
        if (riskProfile.MaxCapitalPerTradePercent <= 0 || riskProfile.MaxCapitalPerTradePercent > 100)
        {
            errors.Add("Max capital per trade percentage must be between 0 and 100");
        }
        else if (riskProfile.MaxCapitalPerTradePercent > 25)
        {
            warnings.Add("High capital per trade percentage increases risk exposure");
        }

        if (riskProfile.MaxCapitalPerAssetPercent <= 0 || riskProfile.MaxCapitalPerAssetPercent > 100)
        {
            errors.Add("Max capital per asset percentage must be between 0 and 100");
        }

        // Validate slippage and stop loss
        if (riskProfile.MaxSlippagePercentage < 0)
        {
            errors.Add("Max slippage percentage cannot be negative");
        }
        else if (riskProfile.MaxSlippagePercentage > 5)
        {
            warnings.Add("High slippage tolerance may result in poor trade execution");
        }

        if (riskProfile.StopLossPercentage < 0)
        {
            errors.Add("Stop loss percentage cannot be negative");
        }

        // Validate retry attempts
        if (riskProfile.MaxRetryAttempts < 0)
        {
            errors.Add("Max retry attempts cannot be negative");
        }
        else if (riskProfile.MaxRetryAttempts > 10)
        {
            warnings.Add("High retry attempts may delay trade execution");
        }

        return new RiskProfileValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings
        };
    }

    private static List<ConfigurationChange> CompareRiskProfiles(RiskProfile? previous, RiskProfile current)
    {
        var changes = new List<ConfigurationChange>();

        if (previous == null)
        {
            changes.Add(new ConfigurationChange
            {
                Property = "RiskProfile",
                PreviousValue = null,
                NewValue = "Created",
                ChangeType = "Create",
                Description = "Risk profile created"
            });
            return changes;
        }

        // Compare all relevant properties
        if (previous.Type != current.Type)
        {
            changes.Add(new ConfigurationChange
            {
                Property = nameof(RiskProfile.Type),
                PreviousValue = previous.Type,
                NewValue = current.Type,
                Description = $"Risk type changed from {previous.Type} to {current.Type}"
            });
        }

        if (previous.MinProfitPercentage != current.MinProfitPercentage)
        {
            changes.Add(new ConfigurationChange
            {
                Property = nameof(RiskProfile.MinProfitPercentage),
                PreviousValue = previous.MinProfitPercentage.ToString(),
                NewValue = current.MinProfitPercentage.ToString(),
                Description = $"Min profit percentage changed from {previous.MinProfitPercentage}% to {current.MinProfitPercentage}%"
            });
        }

        if (previous.MaxCapitalPerTradePercent != current.MaxCapitalPerTradePercent)
        {
            changes.Add(new ConfigurationChange
            {
                Property = nameof(RiskProfile.MaxCapitalPerTradePercent),
                PreviousValue = previous.MaxCapitalPerTradePercent.ToString(),
                NewValue = current.MaxCapitalPerTradePercent.ToString(),
                Description = $"Max capital per trade changed from {previous.MaxCapitalPerTradePercent}% to {current.MaxCapitalPerTradePercent}%"
            });
        }

        if (previous.RiskTolerance != current.RiskTolerance)
        {
            changes.Add(new ConfigurationChange
            {
                Property = nameof(RiskProfile.RiskTolerance),
                PreviousValue = previous.RiskTolerance.ToString(),
                NewValue = current.RiskTolerance.ToString(),
                Description = $"Risk tolerance changed from {previous.RiskTolerance} to {current.RiskTolerance}"
            });
        }

        return changes;
    }

    private static bool DetermineIfRestartRequired(List<ConfigurationChange> changes)
    {
        // Some changes might require restart in a real system
        // For now, assume no restart required for risk profile changes
        return false;
    }

    private async Task PublishConfigurationEvent(
        RiskProfile? previousProfile,
        RiskProfile updatedProfile,
        List<ConfigurationChange> changes,
        UpdateRiskProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Publish(new ConfigurationUpdatedEvent
            {
                ConfigurationType = "RiskProfile",
                PreviousConfiguration = previousProfile != null ? JsonSerializer.Serialize(previousProfile) : null,
                UpdatedConfiguration = JsonSerializer.Serialize(updatedProfile),
                Changes = changes,
                UpdatedBy = request.UpdatedBy,
                UpdateReason = request.UpdateReason,
                RequiresRestart = false,
                AutoApplied = request.ApplyImmediately,
                Severity = DetermineSeverity(changes)
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish configuration updated event");
        }
    }

    private static ConfigurationChangeSeverity DetermineSeverity(List<ConfigurationChange> changes)
    {
        if (changes.Any(c => c.Property == nameof(RiskProfile.Type)))
            return ConfigurationChangeSeverity.High;

        if (changes.Any(c => c.Property == nameof(RiskProfile.RiskTolerance) || 
                            c.Property == nameof(RiskProfile.MaxCapitalPerTradePercent)))
            return ConfigurationChangeSeverity.Medium;

        return ConfigurationChangeSeverity.Low;
    }

    private record RiskProfileValidationResult
    {
        public bool IsValid { get; init; }
        public List<string> Errors { get; init; } = new();
        public List<string> Warnings { get; init; } = new();
    }
} 