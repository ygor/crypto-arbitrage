using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArbitrageController : ControllerBase
{
    private readonly IArbitrageService _arbitrageService;
    private readonly IArbitrageRepository _arbitrageRepository;
    private readonly ILogger<ArbitrageController> _logger;

    public ArbitrageController(
        IArbitrageService arbitrageService,
        IArbitrageRepository arbitrageRepository,
        ILogger<ArbitrageController> logger)
    {
        _arbitrageService = arbitrageService;
        _arbitrageRepository = arbitrageRepository;
        _logger = logger;
    }

    [HttpGet("opportunities")]
    public async Task<IActionResult> GetOpportunities([FromQuery] int limit = 100)
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddDays(-1); // Last 24 hours by default
            
            var opportunities = await _arbitrageRepository.GetOpportunitiesAsync(start, end);
            
            return Ok(opportunities.Take(limit).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage opportunities");
            return StatusCode(500, "An error occurred while retrieving arbitrage opportunities");
        }
    }

    [HttpGet("trades")]
    public async Task<IActionResult> GetTrades([FromQuery] int limit = 100)
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddDays(-1); // Last 24 hours by default
            
            var trades = await _arbitrageRepository.GetTradeResultsAsync(start, end);
            
            // Convert the tuples to a flat structure for the frontend
            var tradeResults = trades
                .Take(limit)
                .Select(t => new
                {
                    OpportunityId = t.Opportunity.DetectedAt.ToString("yyyyMMddHHmmssfff"),
                    TradingPair = t.Opportunity.TradingPair,
                    BuyExchangeId = t.Opportunity.BuyExchangeId,
                    SellExchangeId = t.Opportunity.SellExchangeId,
                    BuyPrice = t.Opportunity.BuyPrice,
                    SellPrice = t.Opportunity.SellPrice,
                    Quantity = t.Opportunity.EffectiveQuantity,
                    Timestamp = t.Timestamp,
                    Status = t.BuyResult?.IsSuccess == true && t.SellResult?.IsSuccess == true 
                        ? TradeStatus.Completed 
                        : TradeStatus.Failed,
                    ProfitAmount = t.Profit,
                    ProfitPercentage = t.Opportunity.SpreadPercentage,
                    Fees = (t.BuyResult?.Fee ?? 0) + (t.SellResult?.Fee ?? 0),
                    ExecutionTimeMs = t.BuyResult != null && t.SellResult != null 
                        ? (t.BuyResult.ExecutionTimeMs + t.SellResult.ExecutionTimeMs) / 2 
                        : 0
                })
                .ToList();
            
            return Ok(tradeResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trade results");
            return StatusCode(500, "An error occurred while retrieving trade results");
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddDays(-1); // Last 24 hours by default
            
            var statistics = await _arbitrageRepository.GetStatisticsAsync(start, end);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage statistics");
            return StatusCode(500, "An error occurred while retrieving arbitrage statistics");
        }
    }
}

[ApiController]
[Route("api/opportunities")]
public class OpportunitiesController : ControllerBase
{
    private readonly IArbitrageRepository _arbitrageRepository;
    private readonly ILogger<OpportunitiesController> _logger;

    public OpportunitiesController(
        IArbitrageRepository arbitrageRepository,
        ILogger<OpportunitiesController> logger)
    {
        _arbitrageRepository = arbitrageRepository;
        _logger = logger;
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentOpportunities([FromQuery] int limit = 20)
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddHours(-1); // Last hour by default for recent opportunities
            
            var opportunities = await _arbitrageRepository.GetOpportunitiesAsync(start, end);
            
            return Ok(opportunities.Take(limit).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent arbitrage opportunities");
            return StatusCode(500, "An error occurred while retrieving recent arbitrage opportunities");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOpportunitiesByTimeRange(
        [FromQuery] string? start = null,
        [FromQuery] string? end = null)
    {
        try
        {
            var endTime = string.IsNullOrEmpty(end) 
                ? DateTimeOffset.UtcNow 
                : DateTimeOffset.Parse(end);
            
            var startTime = string.IsNullOrEmpty(start) 
                ? endTime.AddDays(-1) 
                : DateTimeOffset.Parse(start);
            
            var opportunities = await _arbitrageRepository.GetOpportunitiesAsync(startTime, endTime);
            
            return Ok(opportunities.ToList());
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid date format in request");
            return BadRequest("Invalid date format. Use ISO 8601 format (e.g. 2023-09-01T00:00:00Z)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage opportunities by time range");
            return StatusCode(500, "An error occurred while retrieving arbitrage opportunities");
        }
    }
} 