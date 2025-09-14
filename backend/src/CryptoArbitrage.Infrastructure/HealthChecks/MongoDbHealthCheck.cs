using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using CryptoArbitrage.Infrastructure.Database;

namespace CryptoArbitrage.Infrastructure.HealthChecks;

/// <summary>
/// Health check for MongoDB connectivity and performance.
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly CryptoArbitrageDbContext _dbContext;
    private readonly ILogger<MongoDbHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The configuration.</param>
    public MongoDbHealthCheck(
        CryptoArbitrageDbContext dbContext,
        ILogger<MongoDbHealthCheck> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var timeout = _configuration.GetValue<int>("HealthChecks:MongoDB:TimeoutSeconds", 5);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Test basic connectivity
            var startTime = DateTime.UtcNow;
            await _dbContext.Database.RunCommandAsync<object>("{ ping: 1 }", cancellationToken: linkedCts.Token);
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Get database statistics
            var stats = await GetDatabaseStatsAsync(linkedCts.Token);

            // Prepare health check data
            var data = new Dictionary<string, object>
            {
                ["database_name"] = _dbContext.Database.DatabaseNamespace.DatabaseName,
                ["response_time_ms"] = Math.Round(responseTime, 2),
                ["server_status"] = "connected"
            };

            // Add database statistics if available
            if (stats != null)
            {
                data["collections_count"] = stats.CollectionCount;
                data["documents_count"] = stats.DocumentCount;
                data["data_size_mb"] = Math.Round(stats.DataSizeMB, 2);
                data["index_size_mb"] = Math.Round(stats.IndexSizeMB, 2);
            }

            // Determine health status based on response time
            var status = responseTime switch
            {
                < 100 => HealthStatus.Healthy,
                < 500 => HealthStatus.Degraded,
                _ => HealthStatus.Unhealthy
            };

            var message = $"MongoDB is {status.ToString().ToLower()} (response time: {responseTime:F2}ms)";

            _logger.LogInformation("MongoDB health check completed: {Status}, Response time: {ResponseTime}ms", 
                status, responseTime);

            return new HealthCheckResult(status, message, data: data);
        }
        catch (OperationCanceledException)
        {
            var message = "MongoDB health check timed out";
            _logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message, data: new Dictionary<string, object>
            {
                ["timeout_seconds"] = _configuration.GetValue<int>("HealthChecks:MongoDB:TimeoutSeconds", 5)
            });
        }
        catch (Exception ex)
        {
            var message = $"MongoDB health check failed: {ex.Message}";
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy(message, ex, data: new Dictionary<string, object>
            {
                ["error_type"] = ex.GetType().Name,
                ["connection_string"] = MaskConnectionString(_configuration.GetConnectionString("MongoDb"))
            });
        }
    }

    /// <summary>
    /// Gets database statistics for health monitoring.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Database statistics or null if unavailable.</returns>
    private async Task<DatabaseStats?> GetDatabaseStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var statsCommand = new BsonDocument("dbStats", 1);
            var result = await _dbContext.Database.RunCommandAsync<BsonDocument>(statsCommand, cancellationToken: cancellationToken);

            return new DatabaseStats
            {
                CollectionCount = result.GetValue("collections", 0).ToInt32(),
                DocumentCount = result.GetValue("objects", 0).ToInt64(),
                DataSizeMB = result.GetValue("dataSize", 0).ToDouble() / (1024 * 1024),
                IndexSizeMB = result.GetValue("indexSize", 0).ToDouble() / (1024 * 1024)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get MongoDB database statistics");
            return null;
        }
    }

    /// <summary>
    /// Masks sensitive information in the connection string for logging.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The masked connection string.</returns>
    private static string? MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Simple masking - replace password with asterisks
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"password=([^;&]+)",
            "password=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Database statistics for health monitoring.
    /// </summary>
    private class DatabaseStats
    {
        public int CollectionCount { get; set; }
        public long DocumentCount { get; set; }
        public double DataSizeMB { get; set; }
        public double IndexSizeMB { get; set; }
    }
} 