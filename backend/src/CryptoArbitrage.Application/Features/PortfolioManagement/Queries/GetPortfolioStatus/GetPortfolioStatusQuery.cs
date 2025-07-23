using MediatR;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Queries.GetPortfolioStatus;

/// <summary>
/// Query to get overall portfolio status and metrics.
/// </summary>
public record GetPortfolioStatusQuery : IRequest<GetPortfolioStatusResult>
{
    /// <summary>
    /// Whether to include detailed balance breakdown by exchange.
    /// </summary>
    public bool IncludeBalanceDetails { get; init; } = true;

    /// <summary>
    /// Whether to include risk metrics in the response.
    /// </summary>
    public bool IncludeRiskMetrics { get; init; } = true;

    /// <summary>
    /// Whether to include performance statistics.
    /// </summary>
    public bool IncludePerformanceMetrics { get; init; } = true;

    /// <summary>
    /// Base currency for portfolio valuation (default: USD).
    /// </summary>
    public string BaseCurrency { get; init; } = "USD";

    /// <summary>
    /// Whether to force refresh of data from external sources.
    /// </summary>
    public bool ForceRefresh { get; init; } = false;
} 