using System;
using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents the result of an arbitrage trade.
    /// </summary>
    public class TradeResult
    {
        /// <summary>
        /// Gets or sets the unique identifier of the trade.
        /// </summary>
        public string id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the identifier of the opportunity that led to this trade.
        /// </summary>
        public string opportunityId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the trading pair involved in the trade.
        /// </summary>
        public TradingPair tradingPair { get; set; } = new TradingPair();
        
        /// <summary>
        /// Gets or sets the identifier of the exchange where the buy occurred.
        /// </summary>
        public string buyExchangeId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the identifier of the exchange where the sell occurred.
        /// </summary>
        public string sellExchangeId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the price at which the buy was executed.
        /// </summary>
        public decimal buyPrice { get; set; }
        
        /// <summary>
        /// Gets or sets the price at which the sell was executed.
        /// </summary>
        public decimal sellPrice { get; set; }
        
        /// <summary>
        /// Gets or sets the quantity that was traded.
        /// </summary>
        public decimal quantity { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of when the trade occurred.
        /// </summary>
        public string timestamp { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the status of the trade.
        /// </summary>
        public string status { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the amount of profit realized from the trade.
        /// </summary>
        public decimal profitAmount { get; set; }
        
        /// <summary>
        /// Gets or sets the percentage of profit realized from the trade.
        /// </summary>
        public decimal profitPercentage { get; set; }
        
        /// <summary>
        /// Gets or sets the total fees paid for the trade.
        /// </summary>
        public decimal fees { get; set; }
        
        /// <summary>
        /// Gets or sets the execution time of the trade in milliseconds.
        /// </summary>
        public double executionTimeMs { get; set; }
    }
    
    /// <summary>
    /// Enumeration of possible trade result statuses.
    /// </summary>
    public enum TradeResultStatus
    {
        /// <summary>
        /// The trade is pending execution.
        /// </summary>
        Pending,
        
        /// <summary>
        /// The trade is currently being executed.
        /// </summary>
        Executing,
        
        /// <summary>
        /// The trade has been completed successfully.
        /// </summary>
        Completed,
        
        /// <summary>
        /// The trade failed to execute.
        /// </summary>
        Failed
    }
} 