using MediatR;

namespace CryptoArbitrage.Application.Features.TradeExecution.Queries.GetTradeHistory;

/// <summary>
/// Query to get trade history with optional filtering.
/// </summary>
public record GetTradeHistoryQuery : IRequest<GetTradeHistoryResult>
{
    /// <summary>
    /// Number of trades to return (default: 10, max: 100).
    /// </summary>
    public int Limit { get; init; } = 10;

    /// <summary>
    /// Number of trades to skip for pagination.
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Start date for filtering trades (optional).
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// End date for filtering trades (optional).
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// Filter by trading pair (optional).
    /// </summary>
    public string? TradingPair { get; init; }

    /// <summary>
    /// Filter by buy exchange ID (optional).
    /// </summary>
    public string? BuyExchangeId { get; init; }

    /// <summary>
    /// Filter by sell exchange ID (optional).
    /// </summary>
    public string? SellExchangeId { get; init; }

    /// <summary>
    /// Filter by trade status (optional).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Filter by minimum profit amount (optional).
    /// </summary>
    public decimal? MinProfit { get; init; }

    /// <summary>
    /// Filter by maximum profit amount (optional).
    /// </summary>
    public decimal? MaxProfit { get; init; }

    /// <summary>
    /// Filter by minimum profit percentage (optional).
    /// </summary>
    public decimal? MinProfitPercentage { get; init; }

    /// <summary>
    /// Filter by maximum profit percentage (optional).
    /// </summary>
    public decimal? MaxProfitPercentage { get; init; }

    /// <summary>
    /// Whether to include only successful trades.
    /// </summary>
    public bool? SuccessfulOnly { get; init; }

    /// <summary>
    /// Sort order: "newest" (default), "oldest", "profit_desc", "profit_asc".
    /// </summary>
    public string SortBy { get; init; } = "newest";
} 