using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Infrastructure.Database.Documents;

/// <summary>
/// MongoDB document representation of arbitrage statistics.
/// </summary>
[BsonIgnoreExtraElements]
public class ArbitrageStatisticsDocument
{
    /// <summary>
    /// Gets or sets the MongoDB object identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the statistics date.
    /// </summary>
    [BsonElement("date")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the period type (Daily, Hourly, etc.).
    /// </summary>
    [BsonElement("periodType")]
    public string PeriodType { get; set; } = "Daily";

    /// <summary>
    /// Gets or sets the exchange pair identifier.
    /// </summary>
    [BsonElement("exchangePair")]
    public string? ExchangePair { get; set; }

    /// <summary>
    /// Gets or sets the trading pair.
    /// </summary>
    [BsonElement("tradingPair")]
    public string? TradingPair { get; set; }

    /// <summary>
    /// Gets or sets the total opportunities detected.
    /// </summary>
    [BsonElement("totalOpportunities")]
    public long TotalOpportunities { get; set; }

    /// <summary>
    /// Gets or sets the executed opportunities count.
    /// </summary>
    [BsonElement("executedOpportunities")]
    public long ExecutedOpportunities { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    [BsonElement("successRate")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the total profit amount.
    /// </summary>
    [BsonElement("totalProfit")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalProfit { get; set; }

    /// <summary>
    /// Gets or sets the average profit per trade.
    /// </summary>
    [BsonElement("averageProfit")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageProfit { get; set; }

    /// <summary>
    /// Gets or sets the best profit achieved.
    /// </summary>
    [BsonElement("bestProfit")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal BestProfit { get; set; }

    /// <summary>
    /// Gets or sets the worst profit (or loss).
    /// </summary>
    [BsonElement("worstProfit")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal WorstProfit { get; set; }

    /// <summary>
    /// Gets or sets the total volume traded.
    /// </summary>
    [BsonElement("totalVolume")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalVolume { get; set; }

    /// <summary>
    /// Gets or sets the total fees paid.
    /// </summary>
    [BsonElement("totalFees")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalFees { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    [BsonElement("averageExecutionTime")]
    public double AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the fastest execution time in milliseconds.
    /// </summary>
    [BsonElement("fastestExecutionTime")]
    public double FastestExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the slowest execution time in milliseconds.
    /// </summary>
    [BsonElement("slowestExecutionTime")]
    public double SlowestExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the average profit percentage.
    /// </summary>
    [BsonElement("averageProfitPercentage")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the best profit percentage.
    /// </summary>
    [BsonElement("bestProfitPercentage")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal BestProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the failed trades count.
    /// </summary>
    [BsonElement("failedTrades")]
    public long FailedTrades { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Gets or sets when this statistics record was created.
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this statistics record was last updated.
    /// </summary>
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Converts this document to a domain model.
    /// </summary>
    /// <returns>The domain model representation.</returns>
    public ArbitrageStatistics ToDomainModel()
    {
        return new ArbitrageStatistics
        {
            CreatedAt = CreatedAt,
            StartTime = Date,
            EndTime = Date.AddDays(1), // Assuming daily statistics
            TotalOpportunitiesCount = (int)TotalOpportunities,
            TotalTradesCount = (int)ExecutedOpportunities,
            SuccessfulTradesCount = (int)(ExecutedOpportunities - FailedTrades),
            FailedTradesCount = (int)FailedTrades,
            SuccessRate = SuccessRate,
            TotalProfitAmount = TotalProfit,
            AverageProfitAmount = AverageProfit,
            HighestProfitAmount = BestProfit,
            LowestProfit = WorstProfit,
            TotalVolume = TotalVolume,
            TotalFeesAmount = TotalFees,
            AverageProfitPercentage = AverageProfitPercentage,
            HighestProfitPercentage = BestProfitPercentage,
            AverageExecutionTimeMs = (decimal)AverageExecutionTime
        };
    }

    /// <summary>
    /// Creates a document from a domain model.
    /// </summary>
    /// <param name="statistics">The domain model.</param>
    /// <param name="date">The date for this statistics period.</param>
    /// <param name="periodType">The period type (Daily, Hourly, etc.).</param>
    /// <param name="exchangePair">Optional exchange pair filter.</param>
    /// <param name="tradingPair">Optional trading pair filter.</param>
    /// <returns>The document representation.</returns>
    public static ArbitrageStatisticsDocument FromDomainModel(
        ArbitrageStatistics statistics, 
        DateTime date,
        string periodType = "Daily",
        string? exchangePair = null,
        string? tradingPair = null)
    {
        return new ArbitrageStatisticsDocument
        {
            Date = date,
            PeriodType = periodType,
            ExchangePair = exchangePair,
            TradingPair = tradingPair,
            TotalOpportunities = statistics.TotalOpportunitiesCount,
            ExecutedOpportunities = statistics.TotalTradesCount,
            SuccessRate = statistics.SuccessRate,
            TotalProfit = statistics.TotalProfitAmount,
            AverageProfit = statistics.AverageProfitAmount,
            BestProfit = statistics.HighestProfitAmount,
            WorstProfit = statistics.LowestProfit,
            TotalVolume = statistics.TotalVolume,
            TotalFees = statistics.TotalFeesAmount,
            AverageExecutionTime = (double)statistics.AverageExecutionTimeMs,
            FastestExecutionTime = 0, // Not available in domain model
            SlowestExecutionTime = 0, // Not available in domain model
            AverageProfitPercentage = statistics.AverageProfitPercentage,
            BestProfitPercentage = statistics.HighestProfitPercentage,
            FailedTrades = statistics.FailedTradesCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
} 