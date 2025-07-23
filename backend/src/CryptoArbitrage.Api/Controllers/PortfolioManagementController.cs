using MediatR;
using Microsoft.AspNetCore.Mvc;
using CryptoArbitrage.Application.Features.PortfolioManagement.Commands.UpdateBalance;
using CryptoArbitrage.Application.Features.PortfolioManagement.Queries.GetPortfolioStatus;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Api.Controllers;

/// <summary>
/// Controller for portfolio management operations using vertical slice architecture.
/// </summary>
[ApiController]
[Route("api/portfolio-management")]
public class PortfolioManagementController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PortfolioManagementController> _logger;

    public PortfolioManagementController(
        IMediator mediator,
        ILogger<PortfolioManagementController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Update portfolio balances from exchanges.
    /// </summary>
    /// <param name="exchangeId">Specific exchange ID to update (optional).</param>
    /// <param name="currency">Specific currency to update (optional).</param>
    /// <param name="forceRefresh">Whether to force refresh from exchange APIs.</param>
    /// <param name="maxCacheAgeMinutes">Maximum age of cached data in minutes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The balance update result.</returns>
    [HttpPost("update-balances")]
    public async Task<ActionResult<UpdateBalanceResult>> UpdateBalancesAsync(
        [FromQuery] string? exchangeId = null,
        [FromQuery] string? currency = null,
        [FromQuery] bool forceRefresh = false,
        [FromQuery] int maxCacheAgeMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating portfolio balances - ExchangeId: {ExchangeId}, Currency: {Currency}", 
            exchangeId ?? "All", currency ?? "All");

        try
        {
            var command = new UpdateBalanceCommand
            {
                ExchangeId = exchangeId,
                Currency = currency,
                ForceRefresh = forceRefresh,
                MaxCacheAgeMinutes = maxCacheAgeMinutes
            };

            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio balances");
            return StatusCode(500, new { error = "Internal server error during balance update" });
        }
    }

    /// <summary>
    /// Get comprehensive portfolio status and metrics.
    /// </summary>
    /// <param name="includeBalanceDetails">Whether to include detailed balance breakdown.</param>
    /// <param name="includeRiskMetrics">Whether to include risk metrics.</param>
    /// <param name="includePerformanceMetrics">Whether to include performance statistics.</param>
    /// <param name="baseCurrency">Base currency for portfolio valuation.</param>
    /// <param name="forceRefresh">Whether to force refresh of data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The portfolio status result.</returns>
    [HttpGet("status")]
    public async Task<ActionResult<GetPortfolioStatusResult>> GetPortfolioStatusAsync(
        [FromQuery] bool includeBalanceDetails = true,
        [FromQuery] bool includeRiskMetrics = true,
        [FromQuery] bool includePerformanceMetrics = true,
        [FromQuery] string baseCurrency = "USD",
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting portfolio status with baseCurrency={BaseCurrency}", baseCurrency);

        try
        {
            var query = new GetPortfolioStatusQuery
            {
                IncludeBalanceDetails = includeBalanceDetails,
                IncludeRiskMetrics = includeRiskMetrics,
                IncludePerformanceMetrics = includePerformanceMetrics,
                BaseCurrency = baseCurrency,
                ForceRefresh = forceRefresh
            };

            var result = await _mediator.Send(query, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio status");
            return StatusCode(500, new { error = "Internal server error retrieving portfolio status" });
        }
    }

    /// <summary>
    /// Get portfolio overview summary (lightweight version).
    /// </summary>
    /// <param name="baseCurrency">Base currency for portfolio valuation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The portfolio overview.</returns>
    [HttpGet("overview")]
    public async Task<ActionResult<GetPortfolioStatusResult>> GetPortfolioOverviewAsync(
        [FromQuery] string baseCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting portfolio overview with baseCurrency={BaseCurrency}", baseCurrency);

        try
        {
            var query = new GetPortfolioStatusQuery
            {
                IncludeBalanceDetails = false,
                IncludeRiskMetrics = false,
                IncludePerformanceMetrics = false,
                BaseCurrency = baseCurrency,
                ForceRefresh = false
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio overview");
            return StatusCode(500, new { error = "Internal server error retrieving portfolio overview" });
        }
    }

    /// <summary>
    /// Get current risk metrics for the portfolio.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The risk metrics.</returns>
    [HttpGet("risk-metrics")]
    public async Task<ActionResult<GetPortfolioStatusResult>> GetRiskMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting portfolio risk metrics");

        try
        {
            var query = new GetPortfolioStatusQuery
            {
                IncludeBalanceDetails = false,
                IncludeRiskMetrics = true,
                IncludePerformanceMetrics = false,
                ForceRefresh = false
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio risk metrics");
            return StatusCode(500, new { error = "Internal server error retrieving risk metrics" });
        }
    }

    /// <summary>
    /// Get performance metrics for the portfolio.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The performance metrics.</returns>
    [HttpGet("performance-metrics")]
    public async Task<ActionResult<GetPortfolioStatusResult>> GetPerformanceMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting portfolio performance metrics");

        try
        {
            var query = new GetPortfolioStatusQuery
            {
                IncludeBalanceDetails = false,
                IncludeRiskMetrics = false,
                IncludePerformanceMetrics = true,
                ForceRefresh = false
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio performance metrics");
            return StatusCode(500, new { error = "Internal server error retrieving performance metrics" });
        }
    }
} 