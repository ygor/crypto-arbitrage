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
[Route("api/arbitrage")]
public class ArbitrageController : ControllerBase, IArbitrageController
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
    public async Task<ICollection<ApiModels.ArbitrageOpportunity>> GetArbitrageOpportunitiesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} arbitrage opportunities", limit);
        var opportunities = await _arbitrageRepository.GetRecentOpportunitiesAsync(limit);
        return opportunities.Select(MapToContractModel).ToList();
    }

    [HttpGet("trades")]
    public async Task<ICollection<ApiModels.TradeResult>> GetArbitrageTradesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} arbitrage trades", limit);
        var trades = await _arbitrageRepository.GetRecentTradesAsync(limit);
        return trades.Select(MapToContractModel).ToList();
    }

    [HttpGet("statistics")]
    public async Task<ApiModels.ArbitrageStatistics> GetArbitrageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting arbitrage statistics");
        // Get statistics for the last 30 days
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var stats = await _arbitrageRepository.GetStatisticsAsync(thirtyDaysAgo, now, cancellationToken);
        return MapToContractModel(stats);
    }
    
    private ApiModels.ArbitrageOpportunity MapToContractModel(DomainModels.ArbitrageOpportunity opportunity)
    {
        return new ApiModels.ArbitrageOpportunity
        {
            id = opportunity.Id,
            tradingPair = new ApiModels.TradingPair
            {
                baseCurrency = opportunity.BaseCurrency,
                quoteCurrency = opportunity.QuoteCurrency
            },
            buyExchangeId = opportunity.BuyExchangeId,
            sellExchangeId = opportunity.SellExchangeId,
            buyPrice = opportunity.BuyPrice,
            sellPrice = opportunity.SellPrice,
            quantity = opportunity.EffectiveQuantity,
            timestamp = opportunity.Timestamp.ToString("o"),
            status = opportunity.Status.ToString(),
            potentialProfit = opportunity.EstimatedProfit,
            spreadPercentage = opportunity.SpreadPercentage,
            estimatedProfit = opportunity.EstimatedProfit,
            detectedAt = opportunity.DetectedAt.ToString("o"),
            spread = opportunity.Spread,
            effectiveQuantity = opportunity.EffectiveQuantity,
            isQualified = opportunity.IsQualified
        };
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
    
    private ApiModels.ArbitrageStatistics MapToContractModel(DomainModels.ArbitrageStatistics stats)
    {
        return new ApiModels.ArbitrageStatistics
        {
            startDate = stats.StartTime.ToString("o"),
            endDate = stats.EndTime.ToString("o"),
            detectedOpportunities = stats.TotalOpportunitiesCount,
            executedTrades = stats.TotalTradesCount,
            successfulTrades = stats.SuccessfulTradesCount,
            failedTrades = stats.FailedTradesCount,
            totalProfitAmount = stats.TotalProfitAmount,
            totalProfitPercentage = stats.AverageProfitPercentage,
            averageProfitPerTrade = stats.AverageProfitAmount,
            maxProfitAmount = stats.HighestProfitAmount,
            maxProfitPercentage = stats.HighestProfitPercentage,
            totalTradeVolume = stats.TotalVolume,
            totalFees = stats.TotalFeesAmount,
            averageExecutionTimeMs = (double)stats.AverageExecutionTimeMs
        };
    }
}

[ApiController]
[Route("api/opportunities")]
public class OpportunitiesController : ControllerBase, IOpportunitiesController
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
    public async Task<ICollection<ApiModels.ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} recent opportunities", limit);
        var opportunities = await _arbitrageRepository.GetRecentOpportunitiesAsync(limit);
        return opportunities.Select(MapToContractModel).ToList();
    }

    [HttpGet]
    public async Task<ICollection<ApiModels.ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting opportunities between {Start} and {End}", start, end);
        
        // Use default values if start/end not provided
        start ??= DateTimeOffset.UtcNow.AddDays(-7);
        end ??= DateTimeOffset.UtcNow;
        
        var opportunities = await _arbitrageRepository.GetOpportunitiesByTimeRangeAsync(start.Value, end.Value);
        return opportunities.Select(MapToContractModel).ToList();
    }
    
    private ApiModels.ArbitrageOpportunity MapToContractModel(DomainModels.ArbitrageOpportunity opportunity)
    {
        return new ApiModels.ArbitrageOpportunity
        {
            id = opportunity.Id,
            tradingPair = new ApiModels.TradingPair
            {
                baseCurrency = opportunity.BaseCurrency,
                quoteCurrency = opportunity.QuoteCurrency
            },
            buyExchangeId = opportunity.BuyExchangeId,
            sellExchangeId = opportunity.SellExchangeId,
            buyPrice = opportunity.BuyPrice,
            sellPrice = opportunity.SellPrice,
            quantity = opportunity.EffectiveQuantity,
            timestamp = opportunity.Timestamp.ToString("o"),
            status = opportunity.Status.ToString(),
            potentialProfit = opportunity.EstimatedProfit,
            spreadPercentage = opportunity.SpreadPercentage,
            estimatedProfit = opportunity.EstimatedProfit,
            detectedAt = opportunity.DetectedAt.ToString("o"),
            spread = opportunity.Spread,
            effectiveQuantity = opportunity.EffectiveQuantity,
            isQualified = opportunity.IsQualified
        };
    }
} 