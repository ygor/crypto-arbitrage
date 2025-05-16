using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace ArbitrageBot.Api.Controllers;

[ApiController]
[Route("api/trades")]
public class TradesController : ControllerBase
{
    private readonly IArbitrageRepository _arbitrageRepository;
    private readonly ILogger<TradesController> _logger;

    public TradesController(
        IArbitrageRepository arbitrageRepository,
        ILogger<TradesController> logger)
    {
        _arbitrageRepository = arbitrageRepository;
        _logger = logger;
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentTrades([FromQuery] int limit = 20)
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddHours(-1); // Last hour by default for recent trades
            
            var trades = await _arbitrageRepository.GetTradeResultsAsync(start, end);
            
            // Convert the tuples to a flat structure for the frontend
            var tradeResults = trades
                .Take(limit)
                .Select(t => new
                {
                    Id = $"{t.Opportunity.BuyExchangeId}_{t.Opportunity.SellExchangeId}_{t.Timestamp:yyyyMMddHHmmssfff}",
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
            _logger.LogError(ex, "Error retrieving recent trade results");
            return StatusCode(500, "An error occurred while retrieving recent trade results");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTradesByTimeRange(
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
            
            var trades = await _arbitrageRepository.GetTradeResultsAsync(startTime, endTime);
            
            // Convert the tuples to a flat structure for the frontend
            var tradeResults = trades
                .Select(t => new
                {
                    Id = $"{t.Opportunity.BuyExchangeId}_{t.Opportunity.SellExchangeId}_{t.Timestamp:yyyyMMddHHmmssfff}",
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
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid date format in request");
            return BadRequest("Invalid date format. Use ISO 8601 format (e.g. 2023-09-01T00:00:00Z)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trade results by time range");
            return StatusCode(500, "An error occurred while retrieving trade results");
        }
    }
} 