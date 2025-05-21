using System;
using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents statistical data about arbitrage operations.
    /// </summary>
    public class ArbitrageStatistics
    {
        /// <summary>
        /// Gets or sets the start date of the statistical period.
        /// </summary>
        public string startDate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the end date of the statistical period.
        /// </summary>
        public string endDate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of detected arbitrage opportunities in the period.
        /// </summary>
        public int detectedOpportunities { get; set; }

        /// <summary>
        /// Gets or sets the number of executed trades in the period.
        /// </summary>
        public int executedTrades { get; set; }

        /// <summary>
        /// Gets or sets the number of successful trades in the period.
        /// </summary>
        public int successfulTrades { get; set; }

        /// <summary>
        /// Gets or sets the number of failed trades in the period.
        /// </summary>
        public int failedTrades { get; set; }

        /// <summary>
        /// Gets or sets the total profit amount across all trades in the period.
        /// </summary>
        public decimal totalProfitAmount { get; set; }

        /// <summary>
        /// Gets or sets the total profit percentage across all trades in the period.
        /// </summary>
        public decimal totalProfitPercentage { get; set; }

        /// <summary>
        /// Gets or sets the average profit per trade in the period.
        /// </summary>
        public decimal averageProfitPerTrade { get; set; }

        /// <summary>
        /// Gets or sets the maximum profit amount from a single trade in the period.
        /// </summary>
        public decimal maxProfitAmount { get; set; }

        /// <summary>
        /// Gets or sets the maximum profit percentage from a single trade in the period.
        /// </summary>
        public decimal maxProfitPercentage { get; set; }

        /// <summary>
        /// Gets or sets the total trading volume in the period.
        /// </summary>
        public decimal totalTradeVolume { get; set; }

        /// <summary>
        /// Gets or sets the total fees paid in the period.
        /// </summary>
        public decimal totalFees { get; set; }

        /// <summary>
        /// Gets or sets the average execution time of trades in milliseconds.
        /// </summary>
        public double averageExecutionTimeMs { get; set; }
    }
} 