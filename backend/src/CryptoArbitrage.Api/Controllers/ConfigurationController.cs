using MediatR;
using Microsoft.AspNetCore.Mvc;
using CryptoArbitrage.Application.Features.Configuration.Commands.UpdateRiskProfile;
using CryptoArbitrage.Application.Features.Configuration.Queries.GetConfiguration;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Api.Controllers;

/// <summary>
/// Controller for configuration management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigurationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets the complete application configuration.
    /// </summary>
    /// <param name="includeRiskProfile">Whether to include risk profile in the response.</param>
    /// <param name="includeExchangeConfigs">Whether to include exchange configurations.</param>
    /// <param name="includeNotificationConfig">Whether to include notification configuration.</param>
    /// <param name="includeSystemStatus">Whether to include system status information.</param>
    /// <param name="includeSensitiveData">Whether to include sensitive information (requires admin access).</param>
    /// <param name="forceRefresh">Whether to force refresh from underlying data store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Application configuration data.</returns>
    [HttpGet]
    public async Task<ActionResult<GetConfigurationResult>> GetConfiguration(
        [FromQuery] bool includeRiskProfile = true,
        [FromQuery] bool includeExchangeConfigs = true,
        [FromQuery] bool includeNotificationConfig = true,
        [FromQuery] bool includeSystemStatus = false,
        [FromQuery] bool includeSensitiveData = false,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetConfigurationQuery
        {
            IncludeRiskProfile = includeRiskProfile,
            IncludeExchangeConfigs = includeExchangeConfigs,
            IncludeNotificationConfig = includeNotificationConfig,
            IncludeSystemStatus = includeSystemStatus,
            IncludeSensitiveData = includeSensitiveData,
            ForceRefresh = forceRefresh
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates the risk profile configuration.
    /// </summary>
    /// <param name="request">Risk profile update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the risk profile update.</returns>
    [HttpPut("risk-profile")]
    public async Task<ActionResult<UpdateRiskProfileResult>> UpdateRiskProfile(
        [FromBody] UpdateRiskProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request?.RiskProfile == null)
        {
            return BadRequest("Risk profile is required");
        }

        var command = new UpdateRiskProfileCommand
        {
            RiskProfile = request.RiskProfile,
            ValidateProfile = request.ValidateProfile,
            ApplyImmediately = request.ApplyImmediately,
            UpdateReason = request.UpdateReason,
            UpdatedBy = request.UpdatedBy ?? "API"
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the current risk profile configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current risk profile configuration.</returns>
    [HttpGet("risk-profile")]
    public async Task<ActionResult<GetConfigurationResult>> GetRiskProfile(
        CancellationToken cancellationToken = default)
    {
        var query = new GetConfigurationQuery
        {
            IncludeRiskProfile = true,
            IncludeExchangeConfigs = false,
            IncludeNotificationConfig = false,
            IncludeSystemStatus = false
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(new
        {
            result.IsSuccess,
            result.RiskProfile,
            result.Warnings,
            result.ExecutionTimeMs,
            result.RetrievedAt
        });
    }

    /// <summary>
    /// Gets exchange configurations.
    /// </summary>
    /// <param name="includeSensitiveData">Whether to include sensitive data like API keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exchange configurations.</returns>
    [HttpGet("exchanges")]
    public async Task<ActionResult<GetConfigurationResult>> GetExchangeConfigurations(
        [FromQuery] bool includeSensitiveData = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetConfigurationQuery
        {
            IncludeRiskProfile = false,
            IncludeExchangeConfigs = true,
            IncludeNotificationConfig = false,
            IncludeSystemStatus = false,
            IncludeSensitiveData = includeSensitiveData
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(new
        {
            result.IsSuccess,
            result.ExchangeConfigs,
            result.Warnings,
            result.ExecutionTimeMs,
            result.RetrievedAt
        });
    }

    /// <summary>
    /// Gets system status and health information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System status information.</returns>
    [HttpGet("system-status")]
    public async Task<ActionResult<GetConfigurationResult>> GetSystemStatus(
        CancellationToken cancellationToken = default)
    {
        var query = new GetConfigurationQuery
        {
            IncludeRiskProfile = false,
            IncludeExchangeConfigs = false,
            IncludeNotificationConfig = false,
            IncludeSystemStatus = true
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(new
        {
            result.IsSuccess,
            result.SystemStatus,
            result.ExecutionTimeMs,
            result.RetrievedAt
        });
    }

    /// <summary>
    /// Gets configuration overview with key metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration overview.</returns>
    [HttpGet("overview")]
    public async Task<ActionResult<GetConfigurationResult>> GetConfigurationOverview(
        CancellationToken cancellationToken = default)
    {
        var query = new GetConfigurationQuery
        {
            IncludeRiskProfile = true,
            IncludeExchangeConfigs = true,
            IncludeNotificationConfig = true,
            IncludeSystemStatus = true,
            IncludeSensitiveData = false // Never include sensitive data in overview
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        // Return summarized overview
        return Ok(new
        {
            result.IsSuccess,
            ArbitrageEnabled = result.ArbitrageConfig?.IsEnabled ?? false,
            PaperTradingEnabled = result.ArbitrageConfig?.PaperTradingEnabled ?? true,
            RiskProfileType = result.RiskProfile?.Type ?? "Unknown",
            EnabledExchanges = result.ExchangeConfigs.Count(e => e.IsEnabled),
            TotalExchanges = result.ExchangeConfigs.Count,
            NotificationChannels = new
            {
                Email = result.NotificationConfig?.EmailEnabled ?? false,
                Sms = result.NotificationConfig?.SmsEnabled ?? false,
                Webhook = result.NotificationConfig?.WebhookEnabled ?? false
            },
            SystemHealth = result.SystemStatus?.HealthStatus ?? "Unknown",
            IsRunning = result.SystemStatus?.IsRunning ?? false,
            result.Warnings,
            result.ExecutionTimeMs,
            result.RetrievedAt
        });
    }
}

/// <summary>
/// Request model for updating risk profile.
/// </summary>
public class UpdateRiskProfileRequest
{
    /// <summary>
    /// Updated risk profile.
    /// </summary>
    public required RiskProfile RiskProfile { get; set; }

    /// <summary>
    /// Whether to validate the risk profile before updating.
    /// </summary>
    public bool ValidateProfile { get; set; } = true;

    /// <summary>
    /// Whether to apply the changes immediately to running operations.
    /// </summary>
    public bool ApplyImmediately { get; set; } = true;

    /// <summary>
    /// Optional reason for the update (for audit purposes).
    /// </summary>
    public string? UpdateReason { get; set; }

    /// <summary>
    /// User or system making the update.
    /// </summary>
    public string? UpdatedBy { get; set; }
} 