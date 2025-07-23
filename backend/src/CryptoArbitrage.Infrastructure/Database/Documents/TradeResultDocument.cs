using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Infrastructure.Database.Documents;

/// <summary>
/// MongoDB document representation of a trade result.
/// </summary>
[BsonIgnoreExtraElements]
public class TradeResultDocument
{
    /// <summary>
    /// Gets or sets the MongoDB object identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the trade result identifier.
    /// </summary>
    [BsonElement("tradeId")]
    public string TradeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related opportunity identifier.
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
    /// Gets or sets the quantity traded.
    /// </summary>
    [BsonElement("quantity")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the trade was executed.
    /// </summary>
    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the trade status.
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the trade was successful.
    /// </summary>
    [BsonElement("isSuccess")]
    public bool IsSuccess { get; set; }

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
    /// Gets or sets the total fees paid.
    /// </summary>
    [BsonElement("fees")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Fees { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    [BsonElement("executionTimeMs")]
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the buy trade result.
    /// </summary>
    [BsonElement("buyResult")]
    public TradeSubResultDocument? BuyResult { get; set; }

    /// <summary>
    /// Gets or sets the sell trade result.
    /// </summary>
    [BsonElement("sellResult")]
    public TradeSubResultDocument? SellResult { get; set; }

    /// <summary>
    /// Gets or sets any error message if the trade failed.
    /// </summary>
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the trade.
    /// </summary>
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Converts this document to a domain model.
    /// </summary>
    /// <returns>The domain model representation.</returns>
    public TradeResult ToDomainModel()
    {
        return new TradeResult
        {
            Id = Guid.Parse(TradeId),
            OpportunityId = Guid.Parse(OpportunityId),
            TradingPair = TradingPair,
            BuyExchangeId = BuyExchangeId,
            SellExchangeId = SellExchangeId,
            BuyPrice = BuyPrice,
            SellPrice = SellPrice,
            Quantity = Quantity,
            Timestamp = Timestamp,
            Status = Enum.Parse<TradeStatus>(Status),
            IsSuccess = IsSuccess,
            ProfitAmount = ProfitAmount,
            ProfitPercentage = ProfitPercentage,
            Fees = Fees,
            ExecutionTimeMs = (long)ExecutionTimeMs,
            BuyResult = BuyResult?.ToDomainModel(),
            SellResult = SellResult?.ToDomainModel(),
            ErrorMessage = ErrorMessage
        };
    }

    /// <summary>
    /// Creates a document from a domain model.
    /// </summary>
    /// <param name="tradeResult">The domain model.</param>
    /// <returns>The document representation.</returns>
    public static TradeResultDocument FromDomainModel(TradeResult tradeResult)
    {
        return new TradeResultDocument
        {
            TradeId = tradeResult.Id.ToString(),
            OpportunityId = tradeResult.OpportunityId.ToString(),
            TradingPair = tradeResult.TradingPair,
            BuyExchangeId = tradeResult.BuyExchangeId,
            SellExchangeId = tradeResult.SellExchangeId,
            BuyPrice = tradeResult.BuyPrice,
            SellPrice = tradeResult.SellPrice,
            Quantity = tradeResult.Quantity,
            Timestamp = tradeResult.Timestamp,
            Status = tradeResult.Status.ToString(),
            IsSuccess = tradeResult.IsSuccess,
            ProfitAmount = tradeResult.ProfitAmount,
            ProfitPercentage = tradeResult.ProfitPercentage,
            Fees = tradeResult.Fees,
            ExecutionTimeMs = tradeResult.ExecutionTimeMs,
            BuyResult = tradeResult.BuyResult != null
                ? TradeSubResultDocument.FromDomainModel(tradeResult.BuyResult)
                : null,
            SellResult = tradeResult.SellResult != null
                ? TradeSubResultDocument.FromDomainModel(tradeResult.SellResult)
                : null,
            ErrorMessage = tradeResult.ErrorMessage
        };
    }
}

/// <summary>
/// MongoDB document representation of a trade sub-result.
/// </summary>
[BsonIgnoreExtraElements]
public class TradeSubResultDocument
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    [BsonElement("orderId")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the client order identifier.
    /// </summary>
    [BsonElement("clientOrderId")]
    public string? ClientOrderId { get; set; }

    /// <summary>
    /// Gets or sets the order side (Buy/Sell).
    /// </summary>
    [BsonElement("side")]
    public string Side { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requested quantity.
    /// </summary>
    [BsonElement("quantity")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the requested price.
    /// </summary>
    [BsonElement("price")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the filled quantity.
    /// </summary>
    [BsonElement("filledQuantity")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Gets or sets the average fill price.
    /// </summary>
    [BsonElement("averageFillPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageFillPrice { get; set; }

    /// <summary>
    /// Gets or sets the fee amount.
    /// </summary>
    [BsonElement("feeAmount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal FeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the fee currency.
    /// </summary>
    [BsonElement("feeCurrency")]
    public string? FeeCurrency { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Converts this document to a domain model.
    /// </summary>
    /// <returns>The domain model representation.</returns>
    public TradeSubResult ToDomainModel()
    {
        return new TradeSubResult
        {
            OrderId = OrderId,
            ClientOrderId = ClientOrderId,
            Side = Enum.Parse<OrderSide>(Side),
            Quantity = Quantity,
            Price = Price,
            FilledQuantity = FilledQuantity,
            AverageFillPrice = AverageFillPrice,
            FeeAmount = FeeAmount,
            FeeCurrency = FeeCurrency,
            Status = Enum.Parse<OrderStatus>(Status),
            Timestamp = Timestamp,
            ErrorMessage = ErrorMessage
        };
    }

    /// <summary>
    /// Creates a document from a domain model.
    /// </summary>
    /// <param name="subResult">The domain model.</param>
    /// <returns>The document representation.</returns>
    public static TradeSubResultDocument FromDomainModel(TradeSubResult subResult)
    {
        return new TradeSubResultDocument
        {
            OrderId = subResult.OrderId,
            ClientOrderId = subResult.ClientOrderId,
            Side = subResult.Side.ToString(),
            Quantity = subResult.Quantity,
            Price = subResult.Price,
            FilledQuantity = subResult.FilledQuantity,
            AverageFillPrice = subResult.AverageFillPrice,
            FeeAmount = subResult.FeeAmount,
            FeeCurrency = subResult.FeeCurrency,
            Status = subResult.Status.ToString(),
            Timestamp = subResult.Timestamp,
            ErrorMessage = subResult.ErrorMessage
        };
    }
} 