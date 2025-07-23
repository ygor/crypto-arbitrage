using MediatR;
using Microsoft.AspNetCore.Mvc;
using CryptoArbitrage.Application.Features.TradeExecution.Commands.ExecuteTrade;
using CryptoArbitrage.Application.Features.TradeExecution.Queries.GetTradeHistory;
using Microsoft.Extensions.Logging;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Api.Controllers;

/// <summary>
/// Controller for trade execution operations using vertical slice architecture.
/// </summary>
[ApiController]
[Route("api/trade-execution")]
public class TradeExecutionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TradeExecutionController> _logger;

    public TradeExecutionController(
        IMediator mediator,
        ILogger<TradeExecutionController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute a trade for an arbitrage opportunity.
    /// </summary>
    /// <param name="command">The trade execution command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The trade execution result.</returns>
    [HttpPost("execute")]
    public async Task<ActionResult<ExecuteTradeResult>> ExecuteTradeAsync(
        [FromBody] ExecuteTradeCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing trade for opportunity {OpportunityId}", command.Opportunity?.Id);

        try
        {
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
            _logger.LogError(ex, "Error executing trade for opportunity {OpportunityId}", command.Opportunity?.Id);
            return StatusCode(500, new { error = "Internal server error during trade execution" });
        }
    }

    /// <summary>
    /// Get trade history with optional filtering and pagination.
    /// </summary>
    /// <param name="limit">Number of trades to return (default: 10, max: 100).</param>
    /// <param name="skip">Number of trades to skip for pagination.</param>
    /// <param name="startDate">Start date for filtering trades.</param>
    /// <param name="endDate">End date for filtering trades.</param>
    /// <param name="tradingPair">Filter by trading pair.</param>
    /// <param name="buyExchangeId">Filter by buy exchange ID.</param>
    /// <param name="sellExchangeId">Filter by sell exchange ID.</param>
    /// <param name="status">Filter by trade status.</param>
    /// <param name="minProfit">Filter by minimum profit amount.</param>
    /// <param name="maxProfit">Filter by maximum profit amount.</param>
    /// <param name="minProfitPercentage">Filter by minimum profit percentage.</param>
    /// <param name="maxProfitPercentage">Filter by maximum profit percentage.</param>
    /// <param name="successfulOnly">Whether to include only successful trades.</param>
    /// <param name="sortBy">Sort order: "newest" (default), "oldest", "profit_desc", "profit_asc".</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The trade history result.</returns>
    [HttpGet("history")]
    public async Task<ActionResult<GetTradeHistoryResult>> GetTradeHistoryAsync(
        [FromQuery] int limit = 10,
        [FromQuery] int skip = 0,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? tradingPair = null,
        [FromQuery] string? buyExchangeId = null,
        [FromQuery] string? sellExchangeId = null,
        [FromQuery] string? status = null,
        [FromQuery] decimal? minProfit = null,
        [FromQuery] decimal? maxProfit = null,
        [FromQuery] decimal? minProfitPercentage = null,
        [FromQuery] decimal? maxProfitPercentage = null,
        [FromQuery] bool? successfulOnly = null,
        [FromQuery] string sortBy = "newest",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting trade history with limit={Limit}, skip={Skip}", limit, skip);

        try
        {
            var query = new GetTradeHistoryQuery
            {
                Limit = limit,
                Skip = skip,
                StartDate = startDate,
                EndDate = endDate,
                TradingPair = tradingPair,
                BuyExchangeId = buyExchangeId,
                SellExchangeId = sellExchangeId,
                Status = status,
                MinProfit = minProfit,
                MaxProfit = maxProfit,
                MinProfitPercentage = minProfitPercentage,
                MaxProfitPercentage = maxProfitPercentage,
                SuccessfulOnly = successfulOnly,
                SortBy = sortBy
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trade history");
            return StatusCode(500, new { error = "Internal server error retrieving trade history" });
        }
    }

    /// <summary>
    /// Get recent trades (convenience endpoint).
    /// </summary>
    /// <param name="limit">Number of recent trades to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The recent trades.</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<GetTradeHistoryResult>> GetRecentTradesAsync(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} recent trades", limit);

        try
        {
            var query = new GetTradeHistoryQuery
            {
                Limit = limit,
                Skip = 0,
                SortBy = "newest"
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent trades");
            return StatusCode(500, new { error = "Internal server error retrieving recent trades" });
        }
    }
} 