using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

/// <summary>
/// Simple query to check if the bot is running (alias for IsArbitrageRunningQuery).
/// </summary>
public record IsRunningQuery() : IRequest<bool>; 