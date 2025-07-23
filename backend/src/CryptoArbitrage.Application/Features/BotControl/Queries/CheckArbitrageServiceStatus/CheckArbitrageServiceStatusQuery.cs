using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.CheckArbitrageServiceStatus;

/// <summary>
/// Query to check the arbitrage service status.
/// </summary>
public record CheckArbitrageServiceStatusQuery() : IRequest<bool>; 