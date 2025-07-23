using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;

/// <summary>
/// Handler for getting arbitrage bot statistics.
/// </summary>
public class GetStatisticsHandler : IRequestHandler<GetStatisticsQuery, ArbitrageStatistics>
{
    public async Task<ArbitrageStatistics> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Return mock statistics for now - in a real implementation, 
        // this would aggregate data from various repositories
        return new ArbitrageStatistics
        {
            Id = Guid.NewGuid(),
            TradingPair = "OVERALL",
            CreatedAt = DateTime.UtcNow,
            StartTime = DateTimeOffset.UtcNow.AddDays(-30),
            EndTime = DateTimeOffset.UtcNow,
            TotalOpportunitiesCount = 0,
            QualifiedOpportunitiesCount = 0,
            TotalTradesCount = 0,
            SuccessfulTradesCount = 0,
            FailedTradesCount = 0,
            TotalProfitAmount = 0m,
            AverageProfitAmount = 0m,
            HighestProfitAmount = 0m,
            LowestProfit = 0m,
            AverageExecutionTimeMs = 0m,
            TotalFeesAmount = 0m,
            TotalVolume = 0m,
            AverageProfitPercentage = 0m
        };
    }
} 