namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents the health status of the application.
    /// </summary>
    public class HealthStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the application is healthy.
        /// </summary>
        public bool healthy { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the uptime in seconds.
        /// </summary>
        public long uptimeSeconds { get; set; }
        
        /// <summary>
        /// Gets or sets the status of the application.
        /// </summary>
        public string status { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the uptime as a formatted string.
        /// </summary>
        public string uptime { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the memory usage in megabytes.
        /// </summary>
        public int memoryUsageMB { get; set; }
        
        /// <summary>
        /// Gets or sets the CPU usage percentage.
        /// </summary>
        public double cpuUsagePercent { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the crypto arbitrage bot is running.
        /// </summary>
        public bool cryptoArbitrageBotRunning { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the health status was checked.
        /// </summary>
        public string timestamp { get; set; } = string.Empty;
    }
} 