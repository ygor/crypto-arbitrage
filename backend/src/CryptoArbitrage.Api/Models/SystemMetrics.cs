namespace CryptoArbitrage.Api.Models
{
    /// <summary>
    /// Represents system metrics for monitoring.
    /// </summary>
    public class SystemMetrics
    {
        /// <summary>
        /// Gets or sets the CPU usage percentage.
        /// </summary>
        public double cpuUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets the memory usage in MB.
        /// </summary>
        public double memoryUsageMb { get; set; }

        /// <summary>
        /// Gets or sets the available memory in MB.
        /// </summary>
        public double availableMemoryMb { get; set; }

        /// <summary>
        /// Gets or sets the disk usage percentage.
        /// </summary>
        public double diskUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets the number of active threads.
        /// </summary>
        public int activeThreads { get; set; }

        /// <summary>
        /// Gets or sets the number of requests per second.
        /// </summary>
        public double requestsPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the average response time in milliseconds.
        /// </summary>
        public double avgResponseTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the process metrics.
        /// </summary>
        public ProcessMetrics process { get; set; } = new ProcessMetrics();
        
        /// <summary>
        /// Gets or sets the application metrics.
        /// </summary>
        public ApplicationMetrics application { get; set; } = new ApplicationMetrics();
    }
    
    /// <summary>
    /// Represents metrics for the process.
    /// </summary>
    public class ProcessMetrics
    {
        /// <summary>
        /// Gets or sets the memory usage in MB.
        /// </summary>
        public int memoryUsageMB { get; set; }
        
        /// <summary>
        /// Gets or sets the total CPU time in seconds.
        /// </summary>
        public double totalCpuTime { get; set; }
        
        /// <summary>
        /// Gets or sets the user processor time in seconds.
        /// </summary>
        public double userProcessorTime { get; set; }
        
        /// <summary>
        /// Gets or sets the thread count.
        /// </summary>
        public int threadCount { get; set; }
        
        /// <summary>
        /// Gets or sets the handle count.
        /// </summary>
        public int handleCount { get; set; }
        
        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public string startTime { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the uptime in seconds.
        /// </summary>
        public double uptimeSeconds { get; set; }
    }
    
    /// <summary>
    /// Represents metrics for the application.
    /// </summary>
    public class ApplicationMetrics
    {
        /// <summary>
        /// Gets or sets a value indicating whether the crypto arbitrage bot is running.
        /// </summary>
        public bool cryptoArbitrageBotRunning { get; set; }
        
        /// <summary>
        /// Gets or sets the API requests per minute.
        /// </summary>
        public int apiRequestsPerMinute { get; set; }
        
        /// <summary>
        /// Gets or sets the active connections.
        /// </summary>
        public int activeConnections { get; set; }
        
        /// <summary>
        /// Gets or sets the error rate.
        /// </summary>
        public double errorRate { get; set; }
    }
} 