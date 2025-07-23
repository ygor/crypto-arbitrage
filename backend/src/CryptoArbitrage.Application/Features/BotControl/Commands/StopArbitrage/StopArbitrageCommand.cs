using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.StopArbitrage;

/// <summary>
/// Command to stop the arbitrage bot.
/// </summary>
public record StopArbitrageCommand() : IRequest<StopArbitrageResult>; 