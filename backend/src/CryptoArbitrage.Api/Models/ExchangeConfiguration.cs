using System.Collections.Generic;

namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents the configuration for an exchange.
    /// </summary>
    public class ExchangeConfiguration
    {
        /// <summary>
        /// Gets or sets the exchange identifier.
        /// </summary>
        public string id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the exchange name.
        /// </summary>
        public string name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets a value indicating whether this exchange is enabled.
        /// </summary>
        public bool isEnabled { get; set; }
        
        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string apiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the API secret.
        /// </summary>
        public string apiSecret { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the trading fee percentage.
        /// </summary>
        public decimal tradingFeePercentage { get; set; }
        
        /// <summary>
        /// Gets or sets the available balances by currency.
        /// </summary>
        public Dictionary<string, decimal> availableBalances { get; set; } = new Dictionary<string, decimal>();
        
        /// <summary>
        /// Gets or sets the supported trading pairs.
        /// </summary>
        public List<TradingPair> supportedPairs { get; set; } = new List<TradingPair>();
    }
} 