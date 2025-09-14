using MediatR;
using CryptoArbitrage.Domain.Models;
using System.Text.Json.Serialization;

namespace CryptoArbitrage.Application.Features.Arbitrage.Commands.ExecuteArbitrageOpportunity;

/// <summary>
/// Command to detect and execute an arbitrage opportunity between exchanges.
/// </summary>
public record ExecuteArbitrageOpportunityCommand : IRequest<ExecuteArbitrageOpportunityResult>
{
    /// <summary>
    /// The trading pair to look for arbitrage opportunities.
    /// </summary>
    public required TradingPair TradingPair { get; init; }

    /// <summary>
    /// Source exchange ID where we will buy.
    /// </summary>
    public required string BuyExchangeId { get; init; }

    /// <summary>
    /// Target exchange ID where we will sell.
    /// </summary>
    public required string SellExchangeId { get; init; }

    /// <summary>
    /// Maximum amount to trade in base currency.
    /// </summary>
    public required decimal MaxTradeAmount { get; init; }

    /// <summary>
    /// Minimum profit percentage required to execute the trade.
    /// </summary>
    public decimal MinProfitPercentage { get; init; } = 0.5m;

    /// <summary>
    /// Maximum execution timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; init; } = 30000;

    /// <summary>
    /// Whether to execute the trade automatically or just analyze the opportunity.
    /// </summary>
    public bool AutoExecute { get; init; } = false;

    /// <summary>
    /// Whether to force execution even if validation warnings exist.
    /// </summary>
    public bool ForceExecution { get; init; } = false;
} 