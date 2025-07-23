using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.ExecuteTrade;

/// <summary>
/// Command to execute a trade for an arbitrage opportunity.
/// </summary>
public record ExecuteTradeCommand : IRequest<ExecuteTradeResult>
{
    /// <summary>
    /// The arbitrage opportunity to execute.
    /// </summary>
    public required ArbitrageOpportunity Opportunity { get; init; }

    /// <summary>
    /// Optional quantity override. If null, uses the opportunity's effective quantity.
    /// </summary>
    public decimal? Quantity { get; init; }

    /// <summary>
    /// Whether to force execution even if validation warnings exist.
    /// </summary>
    public bool ForceExecution { get; init; } = false;

    /// <summary>
    /// Optional execution timeout in milliseconds.
    /// </summary>
    public int? TimeoutMs { get; init; }
} 