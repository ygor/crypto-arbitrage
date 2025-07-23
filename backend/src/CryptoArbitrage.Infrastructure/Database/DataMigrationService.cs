using System.Text.Json;
using MongoDB.Driver;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Database.Documents;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Database;

/// <summary>
/// Service for migrating data from file-based storage to MongoDB.
/// </summary>
public class DataMigrationService
{
    private readonly CryptoArbitrageDbContext _dbContext;
    private readonly ILogger<DataMigrationService> _logger;
    private readonly string _dataDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMigrationService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    public DataMigrationService(
        CryptoArbitrageDbContext dbContext,
        ILogger<DataMigrationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Use the same data directory as the file-based repository
        _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CryptoArbitrage", "Data");
    }

    /// <summary>
    /// Performs a complete migration from file-based storage to MongoDB.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The migration result.</returns>
    public async Task<MigrationResult> MigrateAllDataAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();
        
        try
        {
            _logger.LogInformation("Starting data migration from file-based storage to MongoDB");

            // Initialize database and create indexes
            await _dbContext.InitializeAsync(cancellationToken);

            // Check if data directory exists
            if (!Directory.Exists(_dataDirectory))
            {
                _logger.LogWarning("Data directory does not exist: {DataDirectory}", _dataDirectory);
                result.Success = true;
                result.Message = "No data to migrate - data directory does not exist";
                return result;
            }

            // Migrate opportunities
            var opportunityResult = await MigrateOpportunitiesAsync(cancellationToken);
            result.OpportunitiesMigrated = opportunityResult.RecordsMigrated;
            result.OpportunityErrors = opportunityResult.Errors;

            // Migrate trades
            var tradeResult = await MigrateTradesAsync(cancellationToken);
            result.TradesMigrated = tradeResult.RecordsMigrated;
            result.TradeErrors = tradeResult.Errors;

            // Migrate statistics
            var statisticsResult = await MigrateStatisticsAsync(cancellationToken);
            result.StatisticsMigrated = statisticsResult.RecordsMigrated;
            result.StatisticsErrors = statisticsResult.Errors;

            result.Success = true;
            result.Message = $"Migration completed: {result.TotalRecordsMigrated} records migrated with {result.TotalErrors} errors";

            _logger.LogInformation("Data migration completed successfully: {Message}", result.Message);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Migration failed: {ex.Message}";
            _logger.LogError(ex, "Data migration failed");
        }

        return result;
    }

    /// <summary>
    /// Migrates arbitrage opportunities from JSON file to MongoDB.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The migration result for opportunities.</returns>
    private async Task<MigrationItemResult> MigrateOpportunitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationItemResult();
        var filePath = Path.Combine(_dataDirectory, "opportunities.json");

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogInformation("Opportunities file does not exist: {FilePath}", filePath);
                return result;
            }

            _logger.LogInformation("Migrating opportunities from {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogInformation("Opportunities file is empty");
                return result;
            }

            var opportunities = JsonSerializer.Deserialize<Dictionary<string, ArbitrageOpportunity>>(jsonContent);
            if (opportunities == null || !opportunities.Any())
            {
                _logger.LogInformation("No opportunities found in file");
                return result;
            }

            var documents = new List<ArbitrageOpportunityDocument>();
            foreach (var kvp in opportunities)
            {
                try
                {
                    var document = ArbitrageOpportunityDocument.FromDomainModel(kvp.Value);
                    documents.Add(document);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert opportunity {OpportunityId} to document", kvp.Key);
                    result.Errors.Add($"Failed to convert opportunity {kvp.Key}: {ex.Message}");
                }
            }

            if (documents.Any())
            {
                // Use bulk operations for better performance
                var bulkOps = documents.Select(doc => 
                    new ReplaceOneModel<ArbitrageOpportunityDocument>(
                        Builders<ArbitrageOpportunityDocument>.Filter.Eq(x => x.OpportunityId, doc.OpportunityId),
                        doc)
                    {
                        IsUpsert = true
                    }).ToList();

                var bulkResult = await _dbContext.ArbitrageOpportunities.BulkWriteAsync(bulkOps, 
                    new BulkWriteOptions { IsOrdered = false }, cancellationToken);
                
                result.RecordsMigrated = (int)(bulkResult.InsertedCount + bulkResult.ModifiedCount);
                _logger.LogInformation("Migrated {Count} opportunities to MongoDB", result.RecordsMigrated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating opportunities from {FilePath}", filePath);
            result.Errors.Add($"Failed to migrate opportunities: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Migrates trade results from JSON file to MongoDB.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The migration result for trades.</returns>
    private async Task<MigrationItemResult> MigrateTradesAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationItemResult();
        var filePath = Path.Combine(_dataDirectory, "trades.json");

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogInformation("Trades file does not exist: {FilePath}", filePath);
                return result;
            }

            _logger.LogInformation("Migrating trades from {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogInformation("Trades file is empty");
                return result;
            }

            var trades = JsonSerializer.Deserialize<Dictionary<string, TradeResult>>(jsonContent);
            if (trades == null || !trades.Any())
            {
                _logger.LogInformation("No trades found in file");
                return result;
            }

            var documents = new List<TradeResultDocument>();
            foreach (var kvp in trades)
            {
                try
                {
                    var document = TradeResultDocument.FromDomainModel(kvp.Value);
                    documents.Add(document);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert trade {TradeId} to document", kvp.Key);
                    result.Errors.Add($"Failed to convert trade {kvp.Key}: {ex.Message}");
                }
            }

            if (documents.Any())
            {
                // Use bulk operations for better performance
                var bulkOps = documents.Select(doc => 
                    new ReplaceOneModel<TradeResultDocument>(
                        Builders<TradeResultDocument>.Filter.Eq(x => x.TradeId, doc.TradeId),
                        doc)
                    {
                        IsUpsert = true
                    }).ToList();

                var bulkResult = await _dbContext.TradeResults.BulkWriteAsync(bulkOps, 
                    new BulkWriteOptions { IsOrdered = false }, cancellationToken);
                
                result.RecordsMigrated = (int)(bulkResult.InsertedCount + bulkResult.ModifiedCount);
                _logger.LogInformation("Migrated {Count} trades to MongoDB", result.RecordsMigrated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating trades from {FilePath}", filePath);
            result.Errors.Add($"Failed to migrate trades: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Migrates statistics from JSON file to MongoDB.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The migration result for statistics.</returns>
    private async Task<MigrationItemResult> MigrateStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationItemResult();
        var filePath = Path.Combine(_dataDirectory, "statistics.json");

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogInformation("Statistics file does not exist: {FilePath}", filePath);
                return result;
            }

            _logger.LogInformation("Migrating statistics from {FilePath}", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogInformation("Statistics file is empty");
                return result;
            }

            var statistics = JsonSerializer.Deserialize<Dictionary<string, ArbitrageStatistics>>(jsonContent);
            if (statistics == null || !statistics.Any())
            {
                _logger.LogInformation("No statistics found in file");
                return result;
            }

            var documents = new List<ArbitrageStatisticsDocument>();
            foreach (var kvp in statistics)
            {
                try
                {
                    // Parse the date from the key (assuming it's a DateTimeOffset string)
                    if (DateTimeOffset.TryParse(kvp.Key, out var date))
                    {
                        var document = ArbitrageStatisticsDocument.FromDomainModel(kvp.Value, date.DateTime);
                        documents.Add(document);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse date from statistics key: {Key}", kvp.Key);
                        result.Errors.Add($"Failed to parse date from statistics key: {kvp.Key}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert statistics {StatisticsKey} to document", kvp.Key);
                    result.Errors.Add($"Failed to convert statistics {kvp.Key}: {ex.Message}");
                }
            }

            if (documents.Any())
            {
                // Use bulk operations for better performance
                var bulkOps = documents.Select(doc => 
                    new ReplaceOneModel<ArbitrageStatisticsDocument>(
                        Builders<ArbitrageStatisticsDocument>.Filter.And(
                            Builders<ArbitrageStatisticsDocument>.Filter.Eq(x => x.Date, doc.Date),
                            Builders<ArbitrageStatisticsDocument>.Filter.Eq(x => x.PeriodType, doc.PeriodType)
                        ),
                        doc)
                    {
                        IsUpsert = true
                    }).ToList();

                var bulkResult = await _dbContext.ArbitrageStatistics.BulkWriteAsync(bulkOps, 
                    new BulkWriteOptions { IsOrdered = false }, cancellationToken);
                
                result.RecordsMigrated = (int)(bulkResult.InsertedCount + bulkResult.ModifiedCount);
                _logger.LogInformation("Migrated {Count} statistics records to MongoDB", result.RecordsMigrated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating statistics from {FilePath}", filePath);
            result.Errors.Add($"Failed to migrate statistics: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Creates a backup of existing JSON files before migration.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<bool> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_dataDirectory))
            {
                _logger.LogInformation("No data directory exists, no backup needed");
                return true;
            }

            var backupDirectory = Path.Combine(_dataDirectory, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(backupDirectory);

            var files = new[] { "opportunities.json", "trades.json", "statistics.json" };
            foreach (var file in files)
            {
                var sourcePath = Path.Combine(_dataDirectory, file);
                var backupPath = Path.Combine(backupDirectory, file);
                
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, backupPath, overwrite: true);
                    _logger.LogInformation("Backed up {FileName} to {BackupPath}", file, backupPath);
                }
            }

            _logger.LogInformation("Created backup in directory: {BackupDirectory}", backupDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return false;
        }
    }
}

/// <summary>
/// Represents the result of a data migration operation.
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Gets or sets whether the migration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the migration message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of opportunities migrated.
    /// </summary>
    public int OpportunitiesMigrated { get; set; }

    /// <summary>
    /// Gets or sets the number of trades migrated.
    /// </summary>
    public int TradesMigrated { get; set; }

    /// <summary>
    /// Gets or sets the number of statistics records migrated.
    /// </summary>
    public int StatisticsMigrated { get; set; }

    /// <summary>
    /// Gets the total number of records migrated.
    /// </summary>
    public int TotalRecordsMigrated => OpportunitiesMigrated + TradesMigrated + StatisticsMigrated;

    /// <summary>
    /// Gets or sets the opportunity migration errors.
    /// </summary>
    public List<string> OpportunityErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the trade migration errors.
    /// </summary>
    public List<string> TradeErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the statistics migration errors.
    /// </summary>
    public List<string> StatisticsErrors { get; set; } = new();

    /// <summary>
    /// Gets the total number of errors.
    /// </summary>
    public int TotalErrors => OpportunityErrors.Count + TradeErrors.Count + StatisticsErrors.Count;
}

/// <summary>
/// Represents the result of migrating a specific type of data.
/// </summary>
internal class MigrationItemResult
{
    /// <summary>
    /// Gets or sets the number of records migrated.
    /// </summary>
    public int RecordsMigrated { get; set; }

    /// <summary>
    /// Gets or sets the migration errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
} 