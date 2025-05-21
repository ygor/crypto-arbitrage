using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents a trading pair used in cryptocurrency exchanges.
    /// </summary>
    public class TradingPair
    {
        /// <summary>
        /// Gets or sets the base currency in the trading pair (e.g., BTC in BTC/USDT).
        /// </summary>
        [Required]
        public string baseCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quote currency in the trading pair (e.g., USDT in BTC/USDT).
        /// </summary>
        [Required]
        public string quoteCurrency { get; set; } = string.Empty;
    }
} 