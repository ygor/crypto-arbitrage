using MediatR;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;

/// <summary>
/// Handler for getting arbitrage bot statistics.
/// </summary>
public class GetStatisticsHandler : IRequestHandler<GetStatisticsQuery, ArbitrageStatistics>
{
	private readonly IArbitrageRepository _arbitrageRepository;
	private readonly ILogger<GetStatisticsHandler> _logger;

	public GetStatisticsHandler(IArbitrageRepository arbitrageRepository, ILogger<GetStatisticsHandler> logger)
	{
		_arbitrageRepository = arbitrageRepository;
		_logger = logger;
	}

	public async Task<ArbitrageStatistics> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
	{
		try
		{
			// Return real statistics aggregated from repository for the last 30 days
			var end = DateTimeOffset.UtcNow;
			var start = end.AddDays(-30);
			var stats = await _arbitrageRepository.GetStatisticsAsync(start, end, cancellationToken);
			return stats;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get arbitrage statistics");
			// Return empty stats object to avoid throwing to UI callers
			return new ArbitrageStatistics
			{
				Id = Guid.NewGuid(),
				TradingPair = "OVERALL",
				CreatedAt = DateTime.UtcNow,
				StartTime = DateTimeOffset.UtcNow.AddDays(-30),
				EndTime = DateTimeOffset.UtcNow
			};
		}
	}
} 