using System;
using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents an arbitrage opportunity between two cryptocurrency exchanges.
    /// </summary>
    public class ArbitrageOpportunity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the opportunity.
        /// </summary>
        public string id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the trading pair involved in the opportunity.
        /// </summary>
        public TradingPair tradingPair { get; set; } = new TradingPair();
        
        /// <summary>
        /// Gets or sets the identifier of the exchange to buy from.
        /// </summary>
        public string buyExchangeId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the identifier of the exchange to sell to.
        /// </summary>
        public string sellExchangeId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the price to buy at.
        /// </summary>
        public decimal buyPrice { get; set; }
        
        /// <summary>
        /// Gets or sets the price to sell at.
        /// </summary>
        public decimal sellPrice { get; set; }
        
        /// <summary>
        /// Gets or sets the quantity to trade.
        /// </summary>
        public decimal quantity { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the opportunity.
        /// </summary>
        public string timestamp { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the status of the opportunity.
        /// </summary>
        public string status { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the potential profit from the opportunity.
        /// </summary>
        public decimal potentialProfit { get; set; }
        
        /// <summary>
        /// Gets or sets the spread percentage between buy and sell prices.
        /// </summary>
        public decimal spreadPercentage { get; set; }
        
        /// <summary>
        /// Gets or sets the estimated profit.
        /// </summary>
        public decimal estimatedProfit { get; set; }
        
        /// <summary>
        /// Gets or sets when the opportunity was detected.
        /// </summary>
        public string detectedAt { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the absolute spread between buy and sell prices.
        /// </summary>
        public decimal spread { get; set; }
        
        /// <summary>
        /// Gets or sets the effective quantity available for the trade.
        /// </summary>
        public decimal effectiveQuantity { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this opportunity is qualified for trading.
        /// </summary>
        public bool isQualified { get; set; }
    }
} 