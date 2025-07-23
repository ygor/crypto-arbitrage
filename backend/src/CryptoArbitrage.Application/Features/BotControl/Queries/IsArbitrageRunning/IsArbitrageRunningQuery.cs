using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.IsArbitrageRunning;

/// <summary>
/// Query to check if the arbitrage bot is currently running.
/// </summary>
public record IsArbitrageRunningQuery() : IRequest<bool>; 