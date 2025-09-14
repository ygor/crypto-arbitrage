using MediatR;

namespace CryptoArbitrage.Application.Features.Arbitrage.Queries.GetArbitrageOpportunities;

/// <summary>
/// Query to get arbitrage opportunities.
/// </summary>
public class GetArbitrageOpportunitiesQuery : IRequest<GetArbitrageOpportunitiesResult>
{
    /// <summary>
    /// Trading pairs to scan for opportunities.
    /// </summary>
    public string[] TradingPairs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Exchange IDs to include in the scan.
    /// </summary>
    public string[] ExchangeIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Minimum profit percentage threshold.
    /// </summary>
    public decimal MinProfitPercentage { get; set; } = 0.1m;

    /// <summary>
    /// Maximum trade amount to consider.
    /// </summary>
    public decimal MaxTradeAmount { get; set; } = 1000m;

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Whether to include detailed market data.
    /// </summary>
    public bool IncludeMarketData { get; set; } = false;

    /// <summary>
    /// Whether to filter out opportunities with insufficient liquidity.
    /// </summary>
    public bool FilterByLiquidity { get; set; } = true;

    /// <summary>
    /// Whether to sort results by profitability (highest first).
    /// </summary>
    public bool SortByProfitability { get; set; } = true;
} 