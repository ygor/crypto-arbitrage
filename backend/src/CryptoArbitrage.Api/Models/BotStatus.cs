namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents the current status of the trading bot.
    /// </summary>
    public class BotStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the bot is currently running.
        /// </summary>
        public bool isRunning { get; set; }

        /// <summary>
        /// Gets or sets the current state of the bot.
        /// </summary>
        public required string state { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the bot was last started.
        /// </summary>
        public required string startTime { get; set; }

        /// <summary>
        /// Gets or sets the uptime in seconds.
        /// </summary>
        public long uptimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the number of opportunities detected during the current session.
        /// </summary>
        public int opportunitiesDetected { get; set; }

        /// <summary>
        /// Gets or sets the number of trades executed during the current session.
        /// </summary>
        public int tradesExecuted { get; set; }

        /// <summary>
        /// Gets or sets the total profit during the current session.
        /// </summary>
        public decimal currentSessionProfit { get; set; }

        /// <summary>
        /// Gets or sets the current error state, if any.
        /// </summary>
        public string? errorState { get; set; }
    }
} 