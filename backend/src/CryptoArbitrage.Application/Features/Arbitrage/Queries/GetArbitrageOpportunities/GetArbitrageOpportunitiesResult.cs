using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Arbitrage.Queries.GetArbitrageOpportunities;

/// <summary>
/// Result containing arbitrage opportunities.
/// </summary>
public class GetArbitrageOpportunitiesResult
{
    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of detected arbitrage opportunities.
    /// </summary>
    public List<ArbitrageOpportunity> Opportunities { get; set; } = new();

    /// <summary>
    /// Number of exchanges scanned.
    /// </summary>
    public int ExchangesScanned { get; set; }

    /// <summary>
    /// Number of trading pairs scanned.
    /// </summary>
    public int TradingPairsScanned { get; set; }

    /// <summary>
    /// Total scan time in milliseconds.
    /// </summary>
    public long ScanTimeMs { get; set; }

    /// <summary>
    /// Timestamp when the scan was performed.
    /// </summary>
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata about the scan.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Total number of opportunities found (alias for Opportunities.Count).
    /// </summary>
    public int TotalOpportunities => Opportunities.Count;

    /// <summary>
    /// Timestamp when the scan was performed (alias for ScannedAt).
    /// </summary>
    public DateTime Timestamp => ScannedAt;

    /// <summary>
    /// Warnings generated during the scan.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GetArbitrageOpportunitiesResult Success(
        List<ArbitrageOpportunity> opportunities,
        int exchangesScanned,
        int tradingPairsScanned,
        long scanTimeMs = 0)
    {
        return new GetArbitrageOpportunitiesResult
        {
            IsSuccess = true,
            Opportunities = opportunities,
            ExchangesScanned = exchangesScanned,
            TradingPairsScanned = tradingPairsScanned,
            ScanTimeMs = scanTimeMs
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GetArbitrageOpportunitiesResult Failure(string errorMessage)
    {
        return new GetArbitrageOpportunitiesResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Opportunities = new List<ArbitrageOpportunity>()
        };
    }
} 