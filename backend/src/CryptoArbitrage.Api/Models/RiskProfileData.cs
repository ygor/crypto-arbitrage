namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents risk profile configuration data.
    /// </summary>
    public class RiskProfileData
    {
        /// <summary>
        /// Gets or sets the minimum profit percentage to consider a trade.
        /// </summary>
        public decimal minProfitPercent { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount to risk per trade.
        /// </summary>
        public decimal maxTradeAmount { get; set; }

        /// <summary>
        /// Gets or sets the maximum percentage of portfolio to allocate.
        /// </summary>
        public decimal maxPortfolioPercent { get; set; }

        /// <summary>
        /// Gets or sets the maximum simultaneous trades allowed.
        /// </summary>
        public int maxSimultaneousTrades { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to enable stop-loss.
        /// </summary>
        public bool enableStopLoss { get; set; }
        
        /// <summary>
        /// Gets or sets the stop loss percentage.
        /// </summary>
        public decimal stopLossPercent { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum concurrent trades.
        /// </summary>
        public int maxConcurrentTrades { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum daily trade volume.
        /// </summary>
        public decimal maxDailyTradeVolume { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum position percentage.
        /// </summary>
        public decimal maxPositionPercentage { get; set; }
        
        /// <summary>
        /// Gets or sets the trade volume unit.
        /// </summary>
        public string tradeVolumeUnit { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the cooldown period in milliseconds.
        /// </summary>
        public int cooldownPeriodMs { get; set; }
    }
} 