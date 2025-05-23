using CryptoArbitrage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CryptoArbitrage.Api.Controllers.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using ApiModels = CryptoArbitrage.Api.Models;
using DomainModels = CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase, IStatisticsController
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

    [HttpGet("current-day")]
    public async Task<ApiModels.ArbitrageStatistics> GetCurrentDayStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting current day statistics");
        var domainStats = await _arbitrageRepository.GetCurrentDayStatisticsAsync();
        return MapToContractModel(domainStats);
    }

    [HttpGet("previous-day")]
    public async Task<ApiModels.ArbitrageStatistics> GetPreviousDayStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting previous day statistics");
        var domainStats = await _arbitrageRepository.GetLastDayStatisticsAsync();
        return MapToContractModel(domainStats);
    }

    [HttpGet("current-week")]
    public async Task<ApiModels.ArbitrageStatistics> GetCurrentWeekStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting current week statistics");
        // There's no specific current week method, so we'll need to calculate the date range
        var now = DateTime.UtcNow;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        var domainStats = await _arbitrageRepository.GetStatisticsAsync(startOfWeek, endOfWeek, cancellationToken);
        return MapToContractModel(domainStats);
    }

    [HttpGet("previous-week")]
    public async Task<ApiModels.ArbitrageStatistics> GetPreviousWeekStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting previous week statistics");
        var domainStats = await _arbitrageRepository.GetLastWeekStatisticsAsync();
        return MapToContractModel(domainStats);
    }

    [HttpGet("current-month")]
    public async Task<ApiModels.ArbitrageStatistics> GetCurrentMonthStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting current month statistics");
        // There's no specific current month method, so we'll need to calculate the date range
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);
        var domainStats = await _arbitrageRepository.GetStatisticsAsync(startOfMonth, endOfMonth, cancellationToken);
        return MapToContractModel(domainStats);
    }

    [HttpGet("previous-month")]
    public async Task<ApiModels.ArbitrageStatistics> GetPreviousMonthStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting previous month statistics");
        var domainStats = await _arbitrageRepository.GetLastMonthStatisticsAsync();
        return MapToContractModel(domainStats);
    }

    [HttpGet]
    public async Task<ApiModels.ArbitrageStatistics> GetStatistics(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting general statistics");
        // Return current day statistics as the default/general statistics
        var domainStats = await _arbitrageRepository.GetCurrentDayStatisticsAsync();
        return MapToContractModel(domainStats);
    }
    
    private ApiModels.ArbitrageStatistics MapToContractModel(DomainModels.ArbitrageStatistics domainStats)
    {
        return new ApiModels.ArbitrageStatistics
        {
            startDate = domainStats.StartTime.ToString("o"),
            endDate = domainStats.EndTime.ToString("o"),
            detectedOpportunities = domainStats.TotalOpportunitiesCount,
            executedTrades = domainStats.TotalTradesCount,
            successfulTrades = domainStats.SuccessfulTradesCount,
            failedTrades = domainStats.FailedTradesCount,
            totalProfitAmount = domainStats.TotalProfitAmount,
            totalProfitPercentage = domainStats.AverageProfitPercentage,
            averageProfitPerTrade = domainStats.AverageProfitAmount,
            maxProfitAmount = domainStats.HighestProfitAmount,
            maxProfitPercentage = domainStats.HighestProfitPercentage,
            totalTradeVolume = domainStats.TotalVolume,
            totalFees = domainStats.TotalFeesAmount,
            averageExecutionTimeMs = (double)domainStats.AverageExecutionTimeMs
        };
    }
} 