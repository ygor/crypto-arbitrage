using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.Stop;

/// <summary>
/// Simple command to stop the bot.
/// </summary>
public record StopCommand() : IRequest<StopResult>; 