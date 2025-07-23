using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.Start;

/// <summary>
/// Simple command to start the bot (alias for StartArbitrageCommand).
/// </summary>
public record StartCommand() : IRequest<StartResult>; 