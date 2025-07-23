using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.TradeExecution.Queries.GetTradeHistory;

/// <summary>
/// Result of getting trade history.
/// </summary>
public record GetTradeHistoryResult
{
    /// <summary>
    /// The list of trade results.
    /// </summary>
    public IReadOnlyList<TradeResult> Trades { get; init; } = Array.Empty<TradeResult>();

    /// <summary>
    /// Total number of trades matching the filter criteria (for pagination).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Whether there are more results available.
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// Summary statistics for the filtered trades.
    /// </summary>
    public TradeHistorySummary Summary { get; init; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GetTradeHistoryResult Success(
        IReadOnlyList<TradeResult> trades,
        int totalCount,
        bool hasMore,
        TradeHistorySummary? summary = null)
    {
        return new GetTradeHistoryResult
        {
            Trades = trades,
            TotalCount = totalCount,
            HasMore = hasMore,
            Summary = summary ?? TradeHistorySummary.FromTrades(trades)
        };
    }
}

/// <summary>
/// Summary statistics for trade history.
/// </summary>
public record TradeHistorySummary
{
    /// <summary>
    /// Total number of successful trades.
    /// </summary>
    public int SuccessfulTradesCount { get; init; }

    /// <summary>
    /// Total number of failed trades.
    /// </summary>
    public int FailedTradesCount { get; init; }

    /// <summary>
    /// Total profit amount across all trades.
    /// </summary>
    public decimal TotalProfitAmount { get; init; }

    /// <summary>
    /// Average profit percentage across successful trades.
    /// </summary>
    public decimal AverageProfitPercentage { get; init; }

    /// <summary>
    /// Total fees paid across all trades.
    /// </summary>
    public decimal TotalFees { get; init; }

    /// <summary>
    /// Average execution time across all trades.
    /// </summary>
    public double AverageExecutionTimeMs { get; init; }

    /// <summary>
    /// Most profitable trade.
    /// </summary>
    public TradeResult? MostProfitableTrade { get; init; }

    /// <summary>
    /// Least profitable trade.
    /// </summary>
    public TradeResult? LeastProfitableTrade { get; init; }

    /// <summary>
    /// Creates summary statistics from a collection of trades.
    /// </summary>
    public static TradeHistorySummary FromTrades(IReadOnlyList<TradeResult> trades)
    {
        if (!trades.Any())
        {
            return new TradeHistorySummary();
        }

        var successfulTrades = trades.Where(t => t.IsSuccess).ToList();
        var failedTrades = trades.Where(t => !t.IsSuccess).ToList();

        return new TradeHistorySummary
        {
            SuccessfulTradesCount = successfulTrades.Count,
            FailedTradesCount = failedTrades.Count,
            TotalProfitAmount = trades.Sum(t => t.ProfitAmount),
            AverageProfitPercentage = successfulTrades.Any() 
                ? successfulTrades.Average(t => t.ProfitPercentage) 
                : 0,
            TotalFees = trades.Sum(t => t.Fees),
            AverageExecutionTimeMs = trades.Average(t => t.ExecutionTimeMs),
            MostProfitableTrade = trades
                .Where(t => t.IsSuccess)
                .OrderByDescending(t => t.ProfitAmount)
                .FirstOrDefault(),
            LeastProfitableTrade = trades
                .Where(t => t.IsSuccess)
                .OrderBy(t => t.ProfitAmount)
                .FirstOrDefault()
        };
    }
} 