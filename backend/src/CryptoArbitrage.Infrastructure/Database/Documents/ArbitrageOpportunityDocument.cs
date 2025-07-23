using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Infrastructure.Database.Documents;

/// <summary>
/// MongoDB document representation of an arbitrage opportunity.
/// </summary>
[BsonIgnoreExtraElements]
public class ArbitrageOpportunityDocument
{
    /// <summary>
    /// Gets or sets the MongoDB object identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the opportunity identifier.
    /// </summary>
    [BsonElement("opportunityId")]
    public string OpportunityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trading pair.
    /// </summary>
    [BsonElement("tradingPair")]
    public string TradingPair { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buy exchange identifier.
    /// </summary>
    [BsonElement("buyExchangeId")]
    public string BuyExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sell exchange identifier.
    /// </summary>
    [BsonElement("sellExchangeId")]
    public string SellExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buy price.
    /// </summary>
    [BsonElement("buyPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal BuyPrice { get; set; }

    /// <summary>
    /// Gets or sets the sell price.
    /// </summary>
    [BsonElement("sellPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal SellPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [BsonElement("quantity")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the effective quantity after considering constraints.
    /// </summary>
    [BsonElement("effectiveQuantity")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal EffectiveQuantity { get; set; }

    /// <summary>
    /// Gets or sets the profit amount.
    /// </summary>
    [BsonElement("profitAmount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal ProfitAmount { get; set; }

    /// <summary>
    /// Gets or sets the profit percentage.
    /// </summary>
    [BsonElement("profitPercentage")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal ProfitPercentage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the opportunity was detected.
    /// </summary>
    [BsonElement("detectedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the opportunity has been executed.
    /// </summary>
    [BsonElement("isExecuted")]
    public bool IsExecuted { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp if executed.
    /// </summary>
    [BsonElement("executedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the buy order book level.
    /// </summary>
    [BsonElement("buyOrderBookLevel")]
    public OrderBookLevelDocument? BuyOrderBookLevel { get; set; }

    /// <summary>
    /// Gets or sets the sell order book level.
    /// </summary>
    [BsonElement("sellOrderBookLevel")]
    public OrderBookLevelDocument? SellOrderBookLevel { get; set; }

    /// <summary>
    /// Gets or sets the fees for the buy exchange.
    /// </summary>
    [BsonElement("buyFees")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal BuyFees { get; set; }

    /// <summary>
    /// Gets or sets the fees for the sell exchange.
    /// </summary>
    [BsonElement("sellFees")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal SellFees { get; set; }

    /// <summary>
    /// Gets or sets the status of the opportunity.
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "Detected";

    /// <summary>
    /// Gets or sets additional metadata about the opportunity.
    /// </summary>
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Converts this document to a domain model.
    /// </summary>
    /// <returns>The domain model representation.</returns>
    public ArbitrageOpportunity ToDomainModel()
    {
        var tradingPair = Domain.Models.TradingPair.Parse(TradingPair);
        
        // Create the opportunity using the required constructor
        var opportunity = new ArbitrageOpportunity(
            tradingPair,
            BuyExchangeId,
            BuyPrice,
            Quantity, // Buy quantity
            SellExchangeId,
            SellPrice,
            Quantity  // Sell quantity - using same quantity for both
        );

        // Set the mutable properties
        opportunity.Id = OpportunityId;
        opportunity.ProfitAmount = ProfitAmount;
        opportunity.ProfitPercentage = ProfitPercentage;

        // Mark as executed if needed
        if (IsExecuted && ExecutedAt.HasValue)
        {
            opportunity.MarkAsExecuted();
        }

        return opportunity;
    }

    /// <summary>
    /// Creates a document from a domain model.
    /// </summary>
    /// <param name="opportunity">The domain model.</param>
    /// <returns>The document representation.</returns>
    public static ArbitrageOpportunityDocument FromDomainModel(ArbitrageOpportunity opportunity)
    {
        return new ArbitrageOpportunityDocument
        {
            OpportunityId = opportunity.Id,
            TradingPair = opportunity.TradingPair.ToString(),
            BuyExchangeId = opportunity.BuyExchangeId,
            SellExchangeId = opportunity.SellExchangeId,
            BuyPrice = opportunity.BuyPrice,
            SellPrice = opportunity.SellPrice,
            Quantity = opportunity.EffectiveQuantity,
            EffectiveQuantity = opportunity.EffectiveQuantity,
            ProfitAmount = opportunity.ProfitAmount,
            ProfitPercentage = opportunity.ProfitPercentage,
            DetectedAt = opportunity.DetectedAt,
            IsExecuted = opportunity.Status == ArbitrageOpportunityStatus.Executed,
            ExecutedAt = opportunity.ExecutedAt,
            BuyOrderBookLevel = null, // Not available in current domain model
            SellOrderBookLevel = null, // Not available in current domain model
            BuyFees = 0, // Not available in current domain model
            SellFees = 0, // Not available in current domain model
            Status = opportunity.Status == ArbitrageOpportunityStatus.Executed ? "Executed" : "Detected"
        };
    }
}

/// <summary>
/// MongoDB document representation of an order book level.
/// </summary>
[BsonIgnoreExtraElements]
public class OrderBookLevelDocument
{
    /// <summary>
    /// Gets or sets the price.
    /// </summary>
    [BsonElement("price")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [BsonElement("quantity")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Converts this document to a domain model.
    /// </summary>
    /// <returns>The domain model representation.</returns>
    public OrderBookLevel ToDomainModel()
    {
        return new OrderBookLevel(Price, Quantity);
    }

    /// <summary>
    /// Creates a document from a domain model.
    /// </summary>
    /// <param name="level">The domain model.</param>
    /// <returns>The document representation.</returns>
    public static OrderBookLevelDocument FromDomainModel(OrderBookLevel level)
    {
        return new OrderBookLevelDocument
        {
            Price = level.Price,
            Quantity = level.Quantity
        };
    }
} 