# Database Migration Guide

This guide explains how to migrate from file-based storage to MongoDB and configure the crypto arbitrage system to use MongoDB.

## Overview

The crypto arbitrage system now supports two storage backends:
1. **File-based storage** (default) - Uses JSON files for data persistence
2. **MongoDB** - Uses MongoDB database for scalable data storage

## MongoDB Configuration

### 1. Enable MongoDB

To enable MongoDB, update your `appsettings.json` configuration:

```json
{
  "Database": {
    "UseMongoDb": true,
    "MigrateFromFiles": true
  },
  "ConnectionStrings": {
    "MongoDb": "mongodb://admin:password@mongodb:27017"
  },
  "MongoDb": {
    "DatabaseName": "CryptoArbitrage",
    "MaxConnectionPoolSize": 100,
    "ConnectionTimeoutMs": 30000,
    "SocketTimeoutMs": 30000,
    "ServerSelectionTimeoutMs": 30000,
    "UseSsl": false
  }
}
```

### 2. Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `Database:UseMongoDb` | Enable MongoDB instead of file storage | `false` |
| `Database:MigrateFromFiles` | Automatically migrate existing JSON files | `false` |
| `MongoDb:DatabaseName` | MongoDB database name | `CryptoArbitrage` |
| `MongoDb:MaxConnectionPoolSize` | Maximum connection pool size | `100` |
| `MongoDb:ConnectionTimeoutMs` | Connection timeout in milliseconds | `30000` |
| `MongoDb:SocketTimeoutMs` | Socket timeout in milliseconds | `30000` |
| `MongoDb:ServerSelectionTimeoutMs` | Server selection timeout | `30000` |
| `MongoDb:UseSsl` | Enable SSL/TLS connection | `false` |

## Migration Process

### Automatic Migration

1. Set `Database:UseMongoDb: true` and `Database:MigrateFromFiles: true`
2. Start the application
3. The system will automatically:
   - Create a backup of existing JSON files
   - Initialize MongoDB database and indexes
   - Migrate all data from JSON files to MongoDB collections
   - Log the migration results

### Manual Migration

You can also perform migration manually using the `DataMigrationService`:

```csharp
// In your application startup or migration tool
var migrationService = serviceProvider.GetRequiredService<DataMigrationService>();

// Create backup
await migrationService.CreateBackupAsync();

// Perform migration
var result = await migrationService.MigrateAllDataAsync();

if (result.Success)
{
    Console.WriteLine($"Migration completed: {result.Message}");
    Console.WriteLine($"Opportunities migrated: {result.OpportunitiesMigrated}");
    Console.WriteLine($"Trades migrated: {result.TradesMigrated}");
    Console.WriteLine($"Statistics migrated: {result.StatisticsMigrated}");
}
else
{
    Console.WriteLine($"Migration failed: {result.Message}");
}
```

## MongoDB Collections

The system creates the following MongoDB collections:

### 1. arbitrageOpportunities
Stores detected arbitrage opportunities with automatic 30-day retention.

**Indexes:**
- `{ detectedAt: -1, isExecuted: 1 }` - Query by time and execution status
- `{ opportunityId: 1 }` - Lookup by opportunity ID
- `{ tradingPair: 1, detectedAt: -1 }` - Trading pair queries
- `{ buyExchangeId: 1, sellExchangeId: 1, detectedAt: -1 }` - Exchange pair queries

### 2. tradeResults
Stores executed trade results with automatic 1-year retention.

**Indexes:**
- `{ timestamp: -1, isSuccess: 1 }` - Query by time and success status
- `{ tradeId: 1 }` - Lookup by trade ID
- `{ opportunityId: 1 }` - Link to opportunities
- `{ tradingPair: 1, buyExchangeId: 1, sellExchangeId: 1, timestamp: -1 }` - Analysis queries
- `{ profitAmount: -1, timestamp: -1 }` - Profit analysis

### 3. arbitrageStatistics
Stores aggregated statistics with automatic 2-year retention.

**Indexes:**
- `{ date: -1, periodType: 1 }` - Query by date and period
- `{ exchangePair: 1, date: -1 }` - Exchange pair analysis
- `{ tradingPair: 1, date: -1 }` - Trading pair analysis

### 4. systemConfiguration
Stores application settings and configurations.

## Health Checks

When MongoDB is enabled, the system automatically adds MongoDB health checks:

```
GET /health
```

Response includes MongoDB connectivity status and database statistics.

## Data Backup and Recovery

### Backup Strategy

1. **Automatic Backup**: Migration service creates timestamped backups
2. **MongoDB Dumps**: Use `mongodump` for regular database backups
3. **File System**: JSON files are preserved during migration

### Backup Locations

- **JSON File Backups**: `%APPDATA%/CryptoArbitrage/Data/backup_YYYYMMDD_HHMMSS/`
- **MongoDB Dumps**: Use standard MongoDB backup tools

### Recovery Process

1. **From JSON Files**: Set `UseMongoDb: false` to revert to file storage
2. **From MongoDB Backup**: Restore using `mongorestore`
3. **From File Backup**: Copy backup files to data directory

## Performance Considerations

### Indexing Strategy

The system automatically creates optimized indexes for:
- Time-based queries (most common)
- Trading pair analysis
- Exchange pair analysis
- Profit analysis
- Foreign key relationships

### TTL (Time To Live) Indexes

Automatic data retention policies:
- **Opportunities**: 30 days
- **Trades**: 1 year  
- **Statistics**: 2 years

### Connection Pooling

MongoDB driver uses connection pooling for optimal performance:
- Default pool size: 100 connections
- Configurable timeout settings
- Automatic connection management

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   - Check MongoDB server is running
   - Verify connection string
   - Increase timeout values in configuration

2. **Migration Failures**
   - Check disk space
   - Verify file permissions
   - Review migration logs for specific errors

3. **Index Creation Errors**
   - Ensure MongoDB user has appropriate permissions
   - Check for existing conflicting indexes

### Logging

Enable detailed logging for MongoDB operations:

```json
{
  "Logging": {
    "LogLevel": {
      "CryptoArbitrage.Infrastructure.Database": "Debug",
      "CryptoArbitrage.Infrastructure.Repositories.MongoDbArbitrageRepository": "Debug"
    }
  }
}
```

## Development vs Production

### Development Configuration

```json
{
  "Database": {
    "UseMongoDb": true,
    "MigrateFromFiles": true
  },
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  }
}
```

### Production Configuration

```json
{
  "Database": {
    "UseMongoDb": true,
    "MigrateFromFiles": false
  },
  "ConnectionStrings": {
    "MongoDb": "mongodb://username:password@prod-mongodb:27017"
  },
  "MongoDb": {
    "UseSsl": true,
    "MaxConnectionPoolSize": 200
  }
}
```

## Next Steps

After successful MongoDB migration:

1. Monitor application performance and database metrics
2. Set up regular MongoDB backups
3. Configure MongoDB monitoring and alerting
4. Consider MongoDB cluster setup for high availability
5. Plan for data archival strategies beyond TTL policies 