using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiModels = CryptoArbitrage.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/settings/bot")]
public class BotController : ControllerBase
{
    private readonly ILogger<BotController> _logger;

    public BotController(ILogger<BotController> logger)
    {
        _logger = logger;
    }

    [HttpGet("activity-logs")]
    public async Task<ActionResult<IEnumerable<ApiModels.ActivityLogEntry>>> GetActivityLogs(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting activity logs with limit: {Limit}", limit);

            // For now, return sample data
            // TODO: Implement proper activity log repository
            var sampleLogs = new List<ApiModels.ActivityLogEntry>
            {
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-5).ToString("O"),
                    type = "Info",
                    message = "Arbitrage service started",
                    relatedEntityType = "System",
                    relatedEntityId = "arbitrage-service",
                    details = "Service successfully initialized and began monitoring for opportunities"
                },
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-3).ToString("O"),
                    type = "Success",
                    message = "Connected to Binance exchange",
                    relatedEntityType = "Exchange",
                    relatedEntityId = "binance",
                    details = "WebSocket connection established successfully"
                },
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-2).ToString("O"),
                    type = "Success",
                    message = "Connected to Coinbase exchange",
                    relatedEntityType = "Exchange",
                    relatedEntityId = "coinbase",
                    details = "WebSocket connection established successfully"
                },
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-1).ToString("O"),
                    type = "Info",
                    message = "Market data streaming initiated",
                    relatedEntityType = "System",
                    relatedEntityId = "market-data-service",
                    details = "Started receiving real-time price data from all configured exchanges"
                }
            };

            return Ok(sampleLogs.Take(limit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity logs");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("exchange-status")]
    public async Task<ActionResult<IEnumerable<ApiModels.ExchangeStatus>>> GetExchangeStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting exchange status");

            // For now, return sample data
            // TODO: Implement proper exchange status monitoring
            var sampleExchanges = new List<ApiModels.ExchangeStatus>
            {
                new ApiModels.ExchangeStatus
                {
                    exchangeId = "binance",
                    exchangeName = "Binance",
                    isUp = true,
                    lastChecked = DateTime.UtcNow.AddSeconds(-30).ToString("O"),
                    responseTimeMs = 125,
                    additionalInfo = "All services operational"
                },
                new ApiModels.ExchangeStatus
                {
                    exchangeId = "coinbase",
                    exchangeName = "Coinbase Pro",
                    isUp = true,
                    lastChecked = DateTime.UtcNow.AddSeconds(-45).ToString("O"),
                    responseTimeMs = 89,
                    additionalInfo = "All services operational"
                },
                new ApiModels.ExchangeStatus
                {
                    exchangeId = "kraken",
                    exchangeName = "Kraken",
                    isUp = false,
                    lastChecked = DateTime.UtcNow.AddMinutes(-2).ToString("O"),
                    responseTimeMs = 0,
                    additionalInfo = "Connection timeout - investigating"
                }
            };

            return Ok(sampleExchanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange status");
            return StatusCode(500, "Internal server error");
        }
    }
} 