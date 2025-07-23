using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;

namespace CryptoArbitrage.Infrastructure.Database;

/// <summary>
/// Health check for MongoDB connectivity.
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbHealthCheck"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public MongoDbHealthCheck(IConfiguration configuration, ILogger<MongoDbHealthCheck> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks the health of the MongoDB connection.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("MongoDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy("MongoDB connection string is not configured");
            }

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(_configuration.GetValue<string>("MongoDb:DatabaseName") ?? "CryptoArbitrage");

            // Perform a simple ping to check connectivity
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);

            _logger.LogDebug("MongoDB health check passed");
            return HealthCheckResult.Healthy("MongoDB is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB health check failed", ex);
        }
    }
} 