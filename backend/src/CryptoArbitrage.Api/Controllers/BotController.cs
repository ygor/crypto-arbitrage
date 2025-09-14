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
using CryptoArbitrage.Application.Interfaces;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/settings/bot")]
public class BotController : ControllerBase
{
	private readonly ILogger<BotController> _logger;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IArbitrageRepository _arbitrageRepository;

	public BotController(ILogger<BotController> logger, IHttpClientFactory httpClientFactory, IArbitrageRepository arbitrageRepository)
	{
		_logger = logger;
		_httpClientFactory = httpClientFactory;
		_arbitrageRepository = arbitrageRepository;
	}

	/// <summary>
	/// Gets the activity log entries.
	/// </summary>
	/// <returns>The activity log entries.</returns>
	[HttpGet("activity")]
	public async Task<ActionResult<IEnumerable<ApiModels.ActivityLogEntry>>> GetActivityLogAsync([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
	{
		try
		{
			if (limit <= 0 || limit > 200)
			{
				limit = 20;
			}

			var entries = new List<ApiModels.ActivityLogEntry>();

			// Recent trades as activity
			var trades = await _arbitrageRepository.GetRecentTradesAsync(limit, null);
			foreach (var trade in trades)
			{
				var type = trade.IsSuccess ? "Success" : "Error";
				var message = $"Trade executed {trade.TradingPair} | Buy {trade.BuyExchangeId} @{trade.BuyPrice} → Sell {trade.SellExchangeId} @{trade.SellPrice} | Profit {trade.ProfitAmount:N2}";
				entries.Add(new ApiModels.ActivityLogEntry
				{
					id = trade.Id.ToString(),
					timestamp = trade.Timestamp.ToUniversalTime().ToString("O"),
					type = type,
					message = message,
					relatedEntityType = "Trade",
					relatedEntityId = trade.Id.ToString()
				});
			}

			// Recent opportunities as activity
			var opportunities = await _arbitrageRepository.GetRecentOpportunitiesAsync(limit, null);
			foreach (var opp in opportunities)
			{
				var message = $"Opportunity {opp.TradingPair} | Buy {opp.BuyExchangeId} @{opp.BuyPrice} → Sell {opp.SellExchangeId} @{opp.SellPrice} | Spread {opp.SpreadPercentage:N2}%";
				entries.Add(new ApiModels.ActivityLogEntry
				{
					id = opp.Id,
					timestamp = opp.DetectedAt.ToUniversalTime().ToString("O"),
					type = "Info",
					message = message,
					relatedEntityType = "Opportunity",
					relatedEntityId = opp.Id
				});
			}

			var ordered = entries
				.OrderByDescending(e => DateTime.Parse(e.timestamp))
				.Take(limit)
				.ToList();

			return Ok(ordered);
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