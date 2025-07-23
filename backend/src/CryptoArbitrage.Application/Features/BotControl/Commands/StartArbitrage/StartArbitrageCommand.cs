using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;

/// <summary>
/// Command to start the arbitrage bot.
/// </summary>
public record StartArbitrageCommand(
    IList<TradingPair>? TradingPairs = null
) : IRequest<StartArbitrageResult>; 