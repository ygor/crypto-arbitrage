using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Features.Configuration.Queries.GetConfiguration;

/// <summary>
/// Result containing application configuration data.
/// </summary>
public record GetConfigurationResult
{
    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Main arbitrage configuration.
    /// </summary>
    public ArbitrageConfiguration? ArbitrageConfig { get; init; }

    /// <summary>
    /// Risk profile configuration.
    /// </summary>
    public RiskProfile? RiskProfile { get; init; }

    /// <summary>
    /// Exchange configurations.
    /// </summary>
    public IReadOnlyList<ExchangeConfiguration> ExchangeConfigs { get; init; } = Array.Empty<ExchangeConfiguration>();

    /// <summary>
    /// Notification configuration.
    /// </summary>
    public NotificationConfiguration? NotificationConfig { get; init; }

    /// <summary>
    /// System status information.
    /// </summary>
    public SystemStatus? SystemStatus { get; init; }

    /// <summary>
    /// Configuration metadata.
    /// </summary>
    public ConfigurationMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Configuration warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when the configuration was retrieved.
    /// </summary>
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GetConfigurationResult Success(
        ArbitrageConfiguration? arbitrageConfig,
        long executionTimeMs,
        RiskProfile? riskProfile = null,
        IReadOnlyList<ExchangeConfiguration>? exchangeConfigs = null,
        NotificationConfiguration? notificationConfig = null,
        SystemStatus? systemStatus = null,
        IReadOnlyList<string>? warnings = null)
    {
        return new GetConfigurationResult
        {
            IsSuccess = true,
            ArbitrageConfig = arbitrageConfig,
            RiskProfile = riskProfile,
            ExchangeConfigs = exchangeConfigs ?? Array.Empty<ExchangeConfiguration>(),
            NotificationConfig = notificationConfig,
            SystemStatus = systemStatus,
            ExecutionTimeMs = executionTimeMs,
            Warnings = warnings ?? Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GetConfigurationResult Failure(string errorMessage, long executionTimeMs = 0)
    {
        return new GetConfigurationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExecutionTimeMs = executionTimeMs
        };
    }
}

/// <summary>
/// System status information.
/// </summary>
public record SystemStatus
{
    /// <summary>
    /// Whether the arbitrage system is currently running.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Current system mode.
    /// </summary>
    public string Mode { get; init; } = "Unknown";

    /// <summary>
    /// Number of active arbitrage operations.
    /// </summary>
    public int ActiveOperations { get; init; }

    /// <summary>
    /// Number of connected exchanges.
    /// </summary>
    public int ConnectedExchanges { get; init; }

    /// <summary>
    /// System uptime.
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// Last configuration update timestamp.
    /// </summary>
    public DateTime? LastConfigUpdate { get; init; }

    /// <summary>
    /// System health status.
    /// </summary>
    public string HealthStatus { get; init; } = "Unknown";

    /// <summary>
    /// Current errors or issues.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Configuration metadata.
/// </summary>
public record ConfigurationMetadata
{
    /// <summary>
    /// Configuration version.
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Environment name.
    /// </summary>
    public string Environment { get; init; } = "Development";

    /// <summary>
    /// Configuration source.
    /// </summary>
    public string Source { get; init; } = "InMemory";

    /// <summary>
    /// Whether configuration is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? LastModified { get; init; }

    /// <summary>
    /// User who last modified the configuration.
    /// </summary>
    public string? LastModifiedBy { get; init; }

    /// <summary>
    /// Configuration backup availability.
    /// </summary>
    public bool HasBackup { get; init; }

    /// <summary>
    /// Configuration validation status.
    /// </summary>
    public string ValidationStatus { get; init; } = "Valid";
} 