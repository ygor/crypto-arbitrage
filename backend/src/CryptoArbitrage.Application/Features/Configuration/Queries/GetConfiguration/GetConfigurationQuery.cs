using MediatR;

namespace CryptoArbitrage.Application.Features.Configuration.Queries.GetConfiguration;

/// <summary>
/// Query to get application configuration.
/// </summary>
public record GetConfigurationQuery : IRequest<GetConfigurationResult>
{
    /// <summary>
    /// Whether to include risk profile in the response.
    /// </summary>
    public bool IncludeRiskProfile { get; init; } = true;

    /// <summary>
    /// Whether to include exchange configurations in the response.
    /// </summary>
    public bool IncludeExchangeConfigs { get; init; } = true;

    /// <summary>
    /// Whether to include notification configuration in the response.
    /// </summary>
    public bool IncludeNotificationConfig { get; init; } = true;

    /// <summary>
    /// Whether to include system status information.
    /// </summary>
    public bool IncludeSystemStatus { get; init; } = false;

    /// <summary>
    /// Whether to include sensitive information (API keys, tokens, etc.).
    /// </summary>
    public bool IncludeSensitiveData { get; init; } = false;

    /// <summary>
    /// Whether to force refresh from the underlying data store.
    /// </summary>
    public bool ForceRefresh { get; init; } = false;

    /// <summary>
    /// Configuration categories to include (empty means all).
    /// </summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
} 