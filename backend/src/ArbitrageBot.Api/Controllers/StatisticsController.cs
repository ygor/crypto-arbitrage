using ArbitrageBot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArbitrageBot.Api.Controllers;

[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly IArbitrageRepository _arbitrageRepository;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IArbitrageRepository arbitrageRepository,
        ILogger<StatisticsController> logger)
    {
        _arbitrageRepository = arbitrageRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics(
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
            
            var statistics = await _arbitrageRepository.GetStatisticsAsync(startTime, endTime);
            
            return Ok(statistics);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid date format in request");
            return BadRequest("Invalid date format. Use ISO 8601 format (e.g. 2023-09-01T00:00:00Z)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage statistics");
            return StatusCode(500, "An error occurred while retrieving arbitrage statistics");
        }
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayStatistics()
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = new DateTimeOffset(end.Date, end.Offset); // Start of current day
            
            var statistics = await _arbitrageRepository.GetStatisticsAsync(start, end);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's arbitrage statistics");
            return StatusCode(500, "An error occurred while retrieving today's arbitrage statistics");
        }
    }

    [HttpGet("week")]
    public async Task<IActionResult> GetWeekStatistics()
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddDays(-7); // Last 7 days
            
            var statistics = await _arbitrageRepository.GetStatisticsAsync(start, end);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weekly arbitrage statistics");
            return StatusCode(500, "An error occurred while retrieving weekly arbitrage statistics");
        }
    }

    [HttpGet("month")]
    public async Task<IActionResult> GetMonthStatistics()
    {
        try
        {
            var end = DateTimeOffset.UtcNow;
            var start = end.AddDays(-30); // Last 30 days
            
            var statistics = await _arbitrageRepository.GetStatisticsAsync(start, end);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly arbitrage statistics");
            return StatusCode(500, "An error occurred while retrieving monthly arbitrage statistics");
        }
    }
} 