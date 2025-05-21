using CryptoArbitrage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CryptoArbitrage.Api.Controllers.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using ApiModels = CryptoArbitrage.Api.Models;
using DomainModels = CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/trades")]
public class TradesController : ControllerBase, ITradesController
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
    public async Task<ICollection<ApiModels.TradeResult>> GetRecentTradesAsync(int limit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} recent trades", limit);
        var trades = await _arbitrageRepository.GetRecentTradesAsync(limit);
        return trades.Select(MapToContractModel).ToList();
    }

    [HttpGet]
    public async Task<ICollection<ApiModels.TradeResult>> GetTradesByTimeRangeAsync(
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting trades between {Start} and {End}", start, end);
        
        // Use default values if start/end not provided
        start ??= DateTimeOffset.UtcNow.AddDays(-7);
        end ??= DateTimeOffset.UtcNow;
        
        var trades = await _arbitrageRepository.GetTradesByTimeRangeAsync(start.Value, end.Value);
        return trades.Select(MapToContractModel).ToList();
    }
    
    private ApiModels.TradeResult MapToContractModel(DomainModels.TradeResult trade)
    {
        return new ApiModels.TradeResult
        {
            id = trade.Id.ToString(),
            opportunityId = trade.OpportunityId.ToString(),
            tradingPair = new ApiModels.TradingPair
            {
                baseCurrency = trade.TradingPair.Split('/')[0],
                quoteCurrency = trade.TradingPair.Contains('/') ? trade.TradingPair.Split('/')[1] : string.Empty
            },
            buyExchangeId = trade.BuyExchangeId,
            sellExchangeId = trade.SellExchangeId,
            buyPrice = trade.BuyPrice,
            sellPrice = trade.SellPrice,
            quantity = trade.Quantity,
            timestamp = trade.Timestamp.ToString("o"),
            status = trade.Status.ToString(),
            profitAmount = trade.ProfitAmount,
            profitPercentage = trade.ProfitPercentage,
            fees = trade.Fees,
            executionTimeMs = trade.ExecutionTimeMs
        };
    }
} 