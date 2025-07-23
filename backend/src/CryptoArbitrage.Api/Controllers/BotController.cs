using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiModels = CryptoArbitrage.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/settings/bot")]
public class BotController : ControllerBase
{
    private readonly ILogger<BotController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public BotController(ILogger<BotController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets the activity log entries.
    /// </summary>
    /// <returns>The activity log entries.</returns>
    [HttpGet("activity")]
    public async Task<ActionResult<IEnumerable<ApiModels.ActivityLogEntry>>> GetActivityLogAsync()
    {
        try
        {
            // Return sample activity data for now
            var sampleLogs = new List<ApiModels.ActivityLogEntry>
            {
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-5).ToString("O"),
                    type = "Info",
                    message = "Arbitrage service started",
                    relatedEntityType = "System",
                    relatedEntityId = "arbitrage-service"
                },
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-3).ToString("O"),
                    type = "Info",
                    message = "Market data subscription established for BTC-USD",
                    relatedEntityType = "Exchange",
                    relatedEntityId = "coinbase"
                },
                new ApiModels.ActivityLogEntry
                {
                    id = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.AddMinutes(-1).ToString("O"),
                    type = "Warning",
                    message = "High latency detected on Kraken connection",
                    relatedEntityType = "Exchange",
                    relatedEntityId = "kraken"
                }
            };

            return Ok(sampleLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity log");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("exchange-status")]
    public async Task<ActionResult<IEnumerable<ApiModels.ExchangeStatus>>> GetExchangeStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting exchange status");

            var exchanges = new List<ApiModels.ExchangeStatus>();
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Test Coinbase (real implementation exists)
            var coinbaseStatus = await TestExchangeStatus(
                httpClient, 
                "coinbase", 
                "Coinbase Advanced Trade", 
                "https://api.coinbase.com/v2/time",
                cancellationToken);
            exchanges.Add(coinbaseStatus);

            // Test Kraken (real implementation exists)
            var krakenStatus = await TestExchangeStatus(
                httpClient, 
                "kraken", 
                "Kraken", 
                "https://api.kraken.com/0/public/SystemStatus",
                cancellationToken);
            exchanges.Add(krakenStatus);

            // Note: Binance removed - no real BinanceExchangeClient implementation exists

            return Ok(exchanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange status");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<ApiModels.ExchangeStatus> TestExchangeStatus(
        HttpClient httpClient,
        string exchangeId,
        string exchangeName,
        string healthEndpoint,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await httpClient.GetAsync(healthEndpoint, cancellationToken);
            stopwatch.Stop();
            
            var isUp = response.IsSuccessStatusCode;
            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            
            var additionalInfo = isUp ? "All services operational" : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            
            return new ApiModels.ExchangeStatus
            {
                exchangeId = exchangeId,
                exchangeName = exchangeName,
                isUp = isUp,
                lastChecked = DateTime.UtcNow.ToString("O"),
                responseTimeMs = responseTimeMs,
                additionalInfo = additionalInfo
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogWarning(ex, "Failed to check status for {ExchangeId}", exchangeId);
            
            return new ApiModels.ExchangeStatus
            {
                exchangeId = exchangeId,
                exchangeName = exchangeName,
                isUp = false,
                lastChecked = DateTime.UtcNow.ToString("O"),
                responseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                additionalInfo = $"Connection error: {ex.Message}"
            };
        }
    }
} 