using System.Collections.Generic;

namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents arbitrage configuration settings.
    /// </summary>
    public class ArbitrageConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum spread percentage to consider for arbitrage.
        /// </summary>
        public decimal minimumSpreadPercentage { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum trade amount.
        /// </summary>
        public decimal minimumTradeAmount { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum trade amount.
        /// </summary>
        public decimal maximumTradeAmount { get; set; }
        
        /// <summary>
        /// Gets or sets the trading pairs to monitor for arbitrage opportunities.
        /// </summary>
        public List<TradingPair> tradingPairs { get; set; } = new List<TradingPair>();
        
        /// <summary>
        /// Gets or sets the scan interval in milliseconds.
        /// </summary>
        public int scanIntervalMs { get; set; }
        
        /// <summary>
        /// Gets or sets the list of enabled exchanges.
        /// </summary>
        public List<string> enabledExchanges { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets a value indicating whether to automatically execute trades.
        /// </summary>
        public bool autoExecuteTrades { get; set; }
    }
} 