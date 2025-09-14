using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Arbitrage.Commands.ExecuteArbitrageOpportunity;

/// <summary>
/// Result of executing an arbitrage opportunity command.
/// </summary>
public record ExecuteArbitrageOpportunityResult
{
    /// <summary>
    /// Whether the arbitrage execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The arbitrage opportunity that was analyzed or executed.
    /// </summary>
    public ArbitrageOpportunity? Opportunity { get; init; }

    /// <summary>
    /// Buy trade result if executed.
    /// </summary>
    public TradeResult? BuyTradeResult { get; init; }

    /// <summary>
    /// Sell trade result if executed.
    /// </summary>
    public TradeResult? SellTradeResult { get; init; }

    /// <summary>
    /// Total profit realized from the arbitrage (if executed).
    /// </summary>
    public decimal RealizedProfit { get; init; }

    /// <summary>
    /// Profit percentage achieved.
    /// </summary>
    public decimal ProfitPercentage { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Validation warnings that were considered during execution.
    /// </summary>
    public IReadOnlyList<string> ValidationWarnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Whether the opportunity was executed or just analyzed.
    /// </summary>
    public bool WasExecuted { get; init; }

    /// <summary>
    /// Creates a successful result for executed arbitrage.
    /// </summary>
    public static ExecuteArbitrageOpportunityResult SuccessExecuted(
        ArbitrageOpportunity opportunity,
        TradeResult buyTrade,
        TradeResult sellTrade,
        decimal realizedProfit,
        decimal profitPercentage,
        long executionTimeMs,
        IReadOnlyList<string>? warnings = null)
    {
        return new ExecuteArbitrageOpportunityResult
        {
            IsSuccess = true,
            Opportunity = opportunity,
            BuyTradeResult = buyTrade,
            SellTradeResult = sellTrade,
            RealizedProfit = realizedProfit,
            ProfitPercentage = profitPercentage,
            ExecutionTimeMs = executionTimeMs > 0 ? executionTimeMs : 1,
            WasExecuted = true,
            ValidationWarnings = warnings ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a successful result for analyzed opportunity (not executed).
    /// </summary>
    public static ExecuteArbitrageOpportunityResult SuccessAnalyzed(
        ArbitrageOpportunity opportunity,
        long executionTimeMs,
        IReadOnlyList<string>? warnings = null)
    {
        return new ExecuteArbitrageOpportunityResult
        {
            IsSuccess = true,
            Opportunity = opportunity,
            ProfitPercentage = opportunity.ProfitPercentage,
            ExecutionTimeMs = executionTimeMs > 0 ? executionTimeMs : 1,
            WasExecuted = false,
            ValidationWarnings = warnings ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ExecuteArbitrageOpportunityResult Failure(
        string errorMessage,
        ArbitrageOpportunity? opportunity = null,
        long executionTimeMs = 0)
    {
        return new ExecuteArbitrageOpportunityResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Opportunity = opportunity,
            ExecutionTimeMs = executionTimeMs,
            WasExecuted = false
        };
    }
} 