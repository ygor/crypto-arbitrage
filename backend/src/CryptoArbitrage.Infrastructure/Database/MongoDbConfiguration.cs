using MongoDB.Driver;

namespace CryptoArbitrage.Infrastructure.Database;

/// <summary>
/// Configuration options for MongoDB connection and database settings.
/// </summary>
public class MongoDbConfiguration
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = "CryptoArbitrage";

    /// <summary>
    /// Gets or sets the maximum connection pool size.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the socket timeout in milliseconds.
    /// </summary>
    public int SocketTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the server selection timeout in milliseconds.
    /// </summary>
    public int ServerSelectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets whether to use SSL/TLS connection.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Gets or sets the read preference.
    /// </summary>
    public ReadPreference ReadPreference { get; set; } = ReadPreference.Primary;

    /// <summary>
    /// Gets or sets the write concern.
    /// </summary>
    public WriteConcern WriteConcern { get; set; } = WriteConcern.Acknowledged;

    /// <summary>
    /// Gets or sets the read concern.
    /// </summary>
    public ReadConcern ReadConcern { get; set; } = ReadConcern.Local;

    /// <summary>
    /// Creates MongoDB client settings from this configuration.
    /// </summary>
    /// <returns>The MongoDB client settings.</returns>
    public MongoClientSettings ToMongoClientSettings()
    {
        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        
        settings.MaxConnectionPoolSize = MaxConnectionPoolSize;
        settings.ConnectTimeout = TimeSpan.FromMilliseconds(ConnectionTimeoutMs);
        settings.SocketTimeout = TimeSpan.FromMilliseconds(SocketTimeoutMs);
        settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(ServerSelectionTimeoutMs);
        settings.ReadPreference = ReadPreference;
        settings.WriteConcern = WriteConcern;
        settings.ReadConcern = ReadConcern;

        if (UseSsl)
        {
            settings.UseTls = true;
        }

        return settings;
    }
} 