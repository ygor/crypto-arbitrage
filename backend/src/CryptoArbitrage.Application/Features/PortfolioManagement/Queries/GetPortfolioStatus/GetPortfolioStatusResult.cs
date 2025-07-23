using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Queries.GetPortfolioStatus;

/// <summary>
/// Result containing overall portfolio status and metrics.
/// </summary>
public record GetPortfolioStatusResult
{
    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Portfolio overview summary.
    /// </summary>
    public PortfolioOverview Overview { get; init; } = new();

    /// <summary>
    /// Detailed balance breakdown by exchange.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyCollection<Balance>> BalanceDetails { get; init; } 
        = new Dictionary<string, IReadOnlyCollection<Balance>>();

    /// <summary>
    /// Risk metrics for the portfolio.
    /// </summary>
    public PortfolioRiskMetrics? RiskMetrics { get; init; }

    /// <summary>
    /// Performance metrics for the portfolio.
    /// </summary>
    public PortfolioPerformanceMetrics? PerformanceMetrics { get; init; }

    /// <summary>
    /// Timestamp when the status was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GetPortfolioStatusResult Success(
        PortfolioOverview overview,
        IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>? balanceDetails = null,
        PortfolioRiskMetrics? riskMetrics = null,
        PortfolioPerformanceMetrics? performanceMetrics = null)
    {
        return new GetPortfolioStatusResult
        {
            IsSuccess = true,
            Overview = overview,
            BalanceDetails = balanceDetails ?? new Dictionary<string, IReadOnlyCollection<Balance>>(),
            RiskMetrics = riskMetrics,
            PerformanceMetrics = performanceMetrics
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GetPortfolioStatusResult Failure(string errorMessage)
    {
        return new GetPortfolioStatusResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Portfolio overview summary.
/// </summary>
public record PortfolioOverview
{
    /// <summary>
    /// Total portfolio value in base currency.
    /// </summary>
    public decimal TotalValue { get; init; }

    /// <summary>
    /// Number of active exchanges.
    /// </summary>
    public int ActiveExchanges { get; init; }

    /// <summary>
    /// Number of different currencies held.
    /// </summary>
    public int CurrenciesCount { get; init; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Base currency for valuation.
    /// </summary>
    public string BaseCurrency { get; init; } = "USD";

    /// <summary>
    /// Top currency allocations.
    /// </summary>
    public IReadOnlyList<CurrencyAllocation> TopAllocations { get; init; } = Array.Empty<CurrencyAllocation>();
}

/// <summary>
/// Currency allocation in the portfolio.
/// </summary>
public record CurrencyAllocation
{
    /// <summary>
    /// Currency symbol.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Total amount across all exchanges.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Value in base currency.
    /// </summary>
    public decimal ValueInBaseCurrency { get; init; }

    /// <summary>
    /// Percentage of total portfolio.
    /// </summary>
    public decimal PercentageOfPortfolio { get; init; }
}

/// <summary>
/// Portfolio risk metrics.
/// </summary>
public record PortfolioRiskMetrics
{
    /// <summary>
    /// Overall risk score (0-100).
    /// </summary>
    public decimal RiskScore { get; init; }

    /// <summary>
    /// Current risk level.
    /// </summary>
    public string RiskLevel { get; init; } = "Low";

    /// <summary>
    /// Maximum single asset exposure percentage.
    /// </summary>
    public decimal MaxAssetExposure { get; init; }

    /// <summary>
    /// Current diversification score.
    /// </summary>
    public decimal DiversificationScore { get; init; }

    /// <summary>
    /// Exchange concentration risk.
    /// </summary>
    public decimal ExchangeConcentrationRisk { get; init; }

    /// <summary>
    /// Risk warnings.
    /// </summary>
    public IReadOnlyList<string> RiskWarnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Portfolio performance metrics.
/// </summary>
public record PortfolioPerformanceMetrics
{
    /// <summary>
    /// Total profit/loss in base currency.
    /// </summary>
    public decimal TotalProfitLoss { get; init; }

    /// <summary>
    /// Total return percentage.
    /// </summary>
    public decimal TotalReturnPercentage { get; init; }

    /// <summary>
    /// Number of completed trades.
    /// </summary>
    public int CompletedTrades { get; init; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public decimal SuccessRate { get; init; }

    /// <summary>
    /// Average trade profit.
    /// </summary>
    public decimal AverageTradeProfit { get; init; }

    /// <summary>
    /// Total fees paid.
    /// </summary>
    public decimal TotalFees { get; init; }
} 