using MediatR;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Arbitrage.Events;

/// <summary>
/// Event published when an arbitrage execution is successful.
/// </summary>
public record ArbitrageExecutionSuccessEvent(
    ArbitrageOpportunity Opportunity,
    TradeResult BuyTradeResult,
    TradeResult SellTradeResult,
    decimal RealizedProfit
) : INotification
{
    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event identifier for tracking.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Profit percentage achieved.
    /// </summary>
    public decimal ProfitPercentage => BuyTradeResult.TotalValue > 0 
        ? (RealizedProfit / BuyTradeResult.TotalValue) * 100 
        : 0;
} 