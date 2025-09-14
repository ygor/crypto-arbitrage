using CryptoArbitrage.Application.Features.Arbitrage.Commands.ExecuteArbitrageOpportunity;
using CryptoArbitrage.Application.Features.Arbitrage.Queries.GetArbitrageOpportunities;
using CryptoArbitrage.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArbitrageController : ControllerBase
{
    private readonly IMediator _mediator;

    public ArbitrageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> ScanForOpportunities([FromBody] GetArbitrageOpportunitiesQuery request)
    {
        var result = await _mediator.Send(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("scan/{baseCurrency}/{quoteCurrency}")]
    public async Task<IActionResult> ScanSpecificPair(
        string baseCurrency,
        string quoteCurrency,
        [FromQuery] decimal? minProfitPercentage = null,
        [FromQuery] decimal? maxTradeAmount = null)
    {
        var tradingPairString = $"{baseCurrency}/{quoteCurrency}";
        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPairString },
            ExchangeIds = new[] { "coinbase", "kraken" }, // Default exchanges
            MinProfitPercentage = minProfitPercentage ?? 0.1m,
            MaxTradeAmount = maxTradeAmount ?? 1000m,
            MaxResults = 1
        };

        var result = await _mediator.Send(query);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        if (!result.Opportunities.Any())
        {
            return NotFound(new
            {
                message = $"No profitable arbitrage opportunities found for {baseCurrency}-{quoteCurrency} at the requested threshold"
            });
        }

        var top = result.Opportunities.First();
        return Ok(new
        {
            tradingPair = $"{top.TradingPair.BaseCurrency}-{top.TradingPair.QuoteCurrency}",
            opportunity = new
            {
                buyExchangeId = top.BuyExchangeId,
                sellExchangeId = top.SellExchangeId,
                profitPercentage = top.ProfitPercentage,
                effectiveQuantity = top.EffectiveQuantity
            },
            scanMetrics = new { scanTimeMs = result.ScanTimeMs }
        });
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteArbitrage([FromBody] ExecuteArbitrageOpportunityCommand request)
    {
        var result = await _mediator.Send(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeOpportunity([FromBody] ExecuteArbitrageOpportunityCommand request)
    {
        var result = await _mediator.Send(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("opportunities")]
    public async Task<IActionResult> GetOpportunities(
        [FromQuery] string? pairs = null,
        [FromQuery] string? exchanges = null,
        [FromQuery] decimal? minProfit = null,
        [FromQuery] int? limit = null)
    {
        var tradingPairs = pairs?.Split(',').Select(s => s.Replace('-', '/')).ToArray() ?? new[] { "BTC/USDT", "ETH/USDT" };
        var exchangeIds = exchanges?.Split(',') ?? new[] { "coinbase", "kraken" };

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = tradingPairs,
            ExchangeIds = exchangeIds,
            MinProfitPercentage = minProfit ?? 0.1m,
            MaxTradeAmount = 1000m,
            MaxResults = limit ?? 10
        };

        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}