using MongoDB.Driver;
using MongoDB.Bson;
using CryptoArbitrage.Infrastructure.Database.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoArbitrage.Infrastructure.Database;

/// <summary>
/// MongoDB database context for the crypto arbitrage application.
/// </summary>
public class CryptoArbitrageDbContext : IDisposable
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly ILogger<CryptoArbitrageDbContext> _logger;
    private readonly MongoDbConfiguration _configuration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoArbitrageDbContext"/> class.
    /// </summary>
    /// <param name="configuration">The MongoDB configuration.</param>
    /// <param name="logger">The logger.</param>
    public CryptoArbitrageDbContext(IOptions<MongoDbConfiguration> configuration, ILogger<CryptoArbitrageDbContext> logger)
    {
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            // Create MongoDB client with configuration
            var clientSettings = _configuration.ToMongoClientSettings();
            _mongoClient = new MongoClient(clientSettings);
            
            // Get database
            _database = _mongoClient.GetDatabase(_configuration.DatabaseName);
            
            _logger.LogInformation("Connected to MongoDB database: {DatabaseName}", _configuration.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MongoDB database");
            throw;
        }
    }

    /// <summary>
    /// Gets the arbitrage opportunities collection.
    /// </summary>
    public IMongoCollection<ArbitrageOpportunityDocument> ArbitrageOpportunities =>
        _database.GetCollection<ArbitrageOpportunityDocument>("arbitrageOpportunities");

    /// <summary>
    /// Gets the trade results collection.
    /// </summary>
    public IMongoCollection<TradeResultDocument> TradeResults =>
        _database.GetCollection<TradeResultDocument>("tradeResults");

    /// <summary>
    /// Gets the arbitrage statistics collection.
    /// </summary>
    public IMongoCollection<ArbitrageStatisticsDocument> ArbitrageStatistics =>
        _database.GetCollection<ArbitrageStatisticsDocument>("arbitrageStatistics");

    /// <summary>
    /// Gets the system configuration collection.
    /// </summary>
    public IMongoCollection<BsonDocument> SystemConfiguration =>
        _database.GetCollection<BsonDocument>("systemConfiguration");

    /// <summary>
    /// Gets the MongoDB database instance.
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Gets the MongoDB client instance.
    /// </summary>
    public IMongoClient Client => _mongoClient;

    /// <summary>
    /// Initializes the database by creating indexes and ensuring collections exist.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing MongoDB database and indexes");

            await CreateIndexesAsync(cancellationToken);
            await SetupTtlIndexesAsync(cancellationToken);

            _logger.LogInformation("MongoDB database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB database");
            throw;
        }
    }

    /// <summary>
    /// Checks if the database connection is healthy.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the database is healthy, false otherwise.</returns>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return false;
        }
    }

    /// <summary>
    /// Gets database statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Database statistics as a BSON document.</returns>
    public async Task<BsonDocument> GetDatabaseStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocument("dbStats", 1);
            return await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database statistics");
            throw;
        }
    }

    /// <summary>
    /// Creates performance indexes for all collections.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        // ArbitrageOpportunities indexes
        var opportunityIndexes = new List<CreateIndexModel<ArbitrageOpportunityDocument>>
        {
            // Compound index for queries by detection time and execution status
            new(Builders<ArbitrageOpportunityDocument>.IndexKeys
                .Descending(x => x.DetectedAt)
                .Ascending(x => x.IsExecuted)),
            
            // Index for opportunity lookup by ID
            new(Builders<ArbitrageOpportunityDocument>.IndexKeys
                .Ascending(x => x.OpportunityId)),
            
            // Index for trading pair queries
            new(Builders<ArbitrageOpportunityDocument>.IndexKeys
                .Ascending(x => x.TradingPair)
                .Descending(x => x.DetectedAt)),
            
            // Index for exchange pair queries
            new(Builders<ArbitrageOpportunityDocument>.IndexKeys
                .Ascending(x => x.BuyExchangeId)
                .Ascending(x => x.SellExchangeId)
                .Descending(x => x.DetectedAt))
        };

        await ArbitrageOpportunities.Indexes.CreateManyAsync(opportunityIndexes, cancellationToken: cancellationToken);

        // TradeResults indexes
        var tradeIndexes = new List<CreateIndexModel<TradeResultDocument>>
        {
            // Compound index for queries by timestamp and success status
            new(Builders<TradeResultDocument>.IndexKeys
                .Descending(x => x.Timestamp)
                .Ascending(x => x.IsSuccess)),
            
            // Index for trade lookup by ID
            new(Builders<TradeResultDocument>.IndexKeys
                .Ascending(x => x.TradeId)),
            
            // Index for opportunity relationship
            new(Builders<TradeResultDocument>.IndexKeys
                .Ascending(x => x.OpportunityId)),
            
            // Index for trading pair and exchange analysis
            new(Builders<TradeResultDocument>.IndexKeys
                .Ascending(x => x.TradingPair)
                .Ascending(x => x.BuyExchangeId)
                .Ascending(x => x.SellExchangeId)
                .Descending(x => x.Timestamp)),
            
            // Index for profit analysis
            new(Builders<TradeResultDocument>.IndexKeys
                .Descending(x => x.ProfitAmount)
                .Descending(x => x.Timestamp))
        };

        await TradeResults.Indexes.CreateManyAsync(tradeIndexes, cancellationToken: cancellationToken);

        // ArbitrageStatistics indexes
        var statisticsIndexes = new List<CreateIndexModel<ArbitrageStatisticsDocument>>
        {
            // Compound index for date and period queries
            new(Builders<ArbitrageStatisticsDocument>.IndexKeys
                .Descending(x => x.Date)
                .Ascending(x => x.PeriodType)),
            
            // Index for exchange pair analysis
            new(Builders<ArbitrageStatisticsDocument>.IndexKeys
                .Ascending(x => x.ExchangePair)
                .Descending(x => x.Date)),
            
            // Index for trading pair analysis
            new(Builders<ArbitrageStatisticsDocument>.IndexKeys
                .Ascending(x => x.TradingPair)
                .Descending(x => x.Date))
        };

        await ArbitrageStatistics.Indexes.CreateManyAsync(statisticsIndexes, cancellationToken: cancellationToken);

        _logger.LogInformation("Database indexes created successfully");
    }

    /// <summary>
    /// Sets up TTL (Time To Live) indexes for automatic data retention.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SetupTtlIndexesAsync(CancellationToken cancellationToken = default)
    {
        // TTL index for opportunities (30 days retention)
        var opportunityTtlIndex = new CreateIndexModel<ArbitrageOpportunityDocument>(
            Builders<ArbitrageOpportunityDocument>.IndexKeys.Ascending(x => x.DetectedAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30) });

        await ArbitrageOpportunities.Indexes.CreateOneAsync(opportunityTtlIndex, cancellationToken: cancellationToken);

        // TTL index for trade results (1 year retention)
        var tradeTtlIndex = new CreateIndexModel<TradeResultDocument>(
            Builders<TradeResultDocument>.IndexKeys.Ascending(x => x.Timestamp),
            new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(365) });

        await TradeResults.Indexes.CreateOneAsync(tradeTtlIndex, cancellationToken: cancellationToken);

        // TTL index for statistics (2 years retention)
        var statisticsTtlIndex = new CreateIndexModel<ArbitrageStatisticsDocument>(
            Builders<ArbitrageStatisticsDocument>.IndexKeys.Ascending(x => x.CreatedAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(730) });

        await ArbitrageStatistics.Indexes.CreateOneAsync(statisticsTtlIndex, cancellationToken: cancellationToken);

        _logger.LogInformation("TTL indexes for data retention configured successfully");
    }

    /// <summary>
    /// Performs cleanup operations and disposes of resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing MongoDB database context");
            _disposed = true;
        }
    }
} 