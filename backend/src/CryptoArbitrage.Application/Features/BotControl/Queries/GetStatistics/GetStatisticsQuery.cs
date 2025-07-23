using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;

/// <summary>
/// Query to get arbitrage bot statistics.
/// </summary>
public record GetStatisticsQuery() : IRequest<ArbitrageStatistics>; 