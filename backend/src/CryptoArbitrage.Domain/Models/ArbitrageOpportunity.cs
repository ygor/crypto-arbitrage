using System;
using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents an arbitrage opportunity between two exchanges.
/// </summary>
public class ArbitrageOpportunity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArbitrageOpportunity"/> class.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="buyExchangeId">The exchange identifier to buy from.</param>
    /// <param name="buyPrice">The price to buy at.</param>
    /// <param name="buyQuantity">The quantity available to buy.</param>
    /// <param name="sellExchangeId">The exchange identifier to sell to.</param>
    /// <param name="sellPrice">The price to sell at.</param>
    /// <param name="sellQuantity">The quantity available to sell.</param>
    public ArbitrageOpportunity(
        TradingPair tradingPair,
        string buyExchangeId,
        decimal buyPrice,
        decimal buyQuantity,
        string sellExchangeId,
        decimal sellPrice,
        decimal sellQuantity)
    {
        if (buyPrice <= 0) throw new ArgumentException("Buy price must be greater than zero", nameof(buyPrice));
        if (sellPrice <= 0) throw new ArgumentException("Sell price must be greater than zero", nameof(sellPrice));
        if (buyQuantity <= 0) throw new ArgumentException("Buy quantity must be greater than zero", nameof(buyQuantity));
        if (sellQuantity <= 0) throw new ArgumentException("Sell quantity must be greater than zero", nameof(sellQuantity));
        if (buyPrice >= sellPrice) throw new ArgumentException("Buy price must be less than sell price for an opportunity", nameof(buyPrice));

        TradingPair = tradingPair;
        BuyExchangeId = buyExchangeId ?? throw new ArgumentNullException(nameof(buyExchangeId));
        BuyPrice = buyPrice;
        BuyQuantity = buyQuantity;
        SellExchangeId = sellExchangeId ?? throw new ArgumentNullException(nameof(sellExchangeId));
        SellPrice = sellPrice;
        SellQuantity = sellQuantity;
        
        // Determine the effective quantity as the minimum of buy and sell quantities
        EffectiveQuantity = Math.Min(buyQuantity, sellQuantity);
        
        // Calculate the spread and profit
        Spread = sellPrice - buyPrice;
        SpreadPercentage = (Spread / buyPrice) * 100m;
        EstimatedProfit = Spread * EffectiveQuantity;
        
        // When created, the opportunity is always detected but not yet executed
        DetectedAt = DateTime.UtcNow;
        Status = ArbitrageOpportunityStatus.Detected;
    }

    /// <summary>
    /// Parameterless constructor for object initialization.
    /// </summary>
    public ArbitrageOpportunity()
    {
        TradingPair = new TradingPair("BTC", "USD");
        BuyExchangeId = string.Empty;
        SellExchangeId = string.Empty;
        DetectedAt = DateTime.UtcNow;
        Status = ArbitrageOpportunityStatus.Detected;
    }

    /// <summary>
    /// Gets the trading pair.
    /// </summary>
    public TradingPair TradingPair { get; set; }

    /// <summary>
    /// Gets the exchange identifier to buy from.
    /// </summary>
    public string BuyExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the price to buy at.
    /// </summary>
    public decimal BuyPrice { get; set; }

    /// <summary>
    /// Gets the quantity available to buy.
    /// </summary>
    public decimal BuyQuantity { get; set; }

    /// <summary>
    /// Gets the exchange identifier to sell to.
    /// </summary>
    public string SellExchangeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the price to sell at.
    /// </summary>
    public decimal SellPrice { get; set; }

    /// <summary>
    /// Gets the quantity available to sell.
    /// </summary>
    public decimal SellQuantity { get; set; }

    /// <summary>
    /// Gets the effective quantity for the arbitrage (minimum of buy and sell quantities).
    /// </summary>
    public decimal EffectiveQuantity { get; set; }

    /// <summary>
    /// Gets the maximum quantity that can be traded for this opportunity (alias for EffectiveQuantity).
    /// </summary>
    public decimal MaxQuantity => EffectiveQuantity;

    /// <summary>
    /// Gets the spread between sell and buy prices.
    /// </summary>
    public decimal Spread { get; set; }

    /// <summary>
    /// Gets the spread as a percentage of the buy price.
    /// </summary>
    public decimal SpreadPercentage { get; set; }

    /// <summary>
    /// Gets the return on investment percentage (alias for SpreadPercentage).
    /// </summary>
    public decimal ROIPercentage => SpreadPercentage;

    /// <summary>
    /// Gets the estimated profit before fees.
    /// </summary>
    public decimal EstimatedProfit { get; set; }

    /// <summary>
    /// Gets the time when the opportunity was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Gets the timestamp of this opportunity (alias for DetectedAt).
    /// </summary>
    public DateTime Timestamp => DetectedAt;

    /// <summary>
    /// Gets the status of the arbitrage opportunity.
    /// </summary>
    public ArbitrageOpportunityStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the time when the opportunity was executed, if applicable.
    /// </summary>
    public DateTime? ExecutedAt { get; private set; }

    /// <summary>
    /// Updates the status of the opportunity to executing.
    /// </summary>
    public void MarkAsExecuting()
    {
        Status = ArbitrageOpportunityStatus.Executing;
    }

    /// <summary>
    /// Updates the status of the opportunity to executed.
    /// </summary>
    public void MarkAsExecuted()
    {
        Status = ArbitrageOpportunityStatus.Executed;
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the status of the opportunity to failed.
    /// </summary>
    public void MarkAsFailed()
    {
        Status = ArbitrageOpportunityStatus.Failed;
    }

    /// <summary>
    /// Updates the status of the opportunity to missed.
    /// </summary>
    public void MarkAsMissed()
    {
        Status = ArbitrageOpportunityStatus.Missed;
    }

    public string Id { get; set; } = string.Empty;
    public string TradingPairString { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public decimal SpreadAmount => SellPrice - BuyPrice;
    public decimal ProfitAmount { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal EstimatedQuantity { get; set; }
    public decimal EstimatedTotalValue { get; set; }
    public decimal EstimatedFees { get; set; }
    public decimal NetProfitAmount => ProfitAmount - EstimatedFees;
    public decimal NetProfitPercentage => EstimatedTotalValue > 0 
        ? ProfitPercentage - (EstimatedFees / EstimatedTotalValue * 100) 
        : ProfitPercentage;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsQualified { get; set; }
    public string? DisqualificationReason { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum trade amount for this opportunity.
    /// </summary>
    public decimal MaxTradeAmount { get; set; }
    
    // Risk/reward metrics
    public decimal RiskScore { get; set; }
    public decimal RewardScore { get; set; }
    public decimal RiskToRewardRatio => RiskScore > 0 ? RewardScore / RiskScore : 0;
}

/// <summary>
/// Represents the status of an arbitrage opportunity.
/// </summary>
public enum ArbitrageOpportunityStatus
{
    /// <summary>
    /// The opportunity has been detected but not yet acted upon.
    /// </summary>
    Detected,
    
    /// <summary>
    /// The opportunity is currently being executed.
    /// </summary>
    Executing,
    
    /// <summary>
    /// The opportunity has been successfully executed.
    /// </summary>
    Executed,
    
    /// <summary>
    /// The opportunity execution has failed.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The opportunity could not be executed in time and is no longer available.
    /// </summary>
    Missed
} 