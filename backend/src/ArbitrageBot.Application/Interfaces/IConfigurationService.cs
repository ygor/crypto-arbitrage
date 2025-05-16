using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Interface for a service that provides configuration settings for the arbitrage system.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads all configuration from the source and initializes the service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded arbitrage configuration.</returns>
    Task<ArbitrageConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the arbitrage configuration.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The arbitrage configuration.</returns>
    Task<ArbitrageConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the risk profile.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The risk profile.</returns>
    Task<RiskProfile> GetRiskProfileAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the exchange configuration for a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exchange configuration if found; otherwise, null.</returns>
    Task<ExchangeConfiguration?> GetExchangeConfigurationAsync(string exchangeId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all exchange configurations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of exchange configurations.</returns>
    Task<IReadOnlyCollection<ExchangeConfiguration>> GetAllExchangeConfigurationsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the notification configuration.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The notification configuration.</returns>
    Task<NotificationConfiguration> GetNotificationConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the risk profile.
    /// </summary>
    /// <param name="riskProfile">The new risk profile.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateRiskProfileAsync(RiskProfile riskProfile, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the arbitrage configuration.
    /// </summary>
    /// <param name="configuration">The new arbitrage configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateConfigurationAsync(ArbitrageConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an exchange configuration.
    /// </summary>
    /// <param name="configuration">The new exchange configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateExchangeConfigurationAsync(ExchangeConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the notification configuration.
    /// </summary>
    /// <param name="configuration">The new notification configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateNotificationConfigurationAsync(NotificationConfiguration configuration, CancellationToken cancellationToken = default);
} 