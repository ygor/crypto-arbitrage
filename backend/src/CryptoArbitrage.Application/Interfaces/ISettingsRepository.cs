using CryptoArbitrage.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryptoArbitrage.Application.Interfaces;

public interface ISettingsRepository
{
    // Exchange configurations
    Task<List<ExchangeConfig>> GetExchangeConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<ExchangeConfig?> GetExchangeConfigurationAsync(string exchangeId, CancellationToken cancellationToken = default);
    Task SaveExchangeConfigurationsAsync(List<ExchangeConfig> configurations, CancellationToken cancellationToken = default);
    Task SaveExchangeConfigurationAsync(ExchangeConfig configuration, CancellationToken cancellationToken = default);
    
    // Arbitrage configurations
    Task<ArbitrageConfig> GetArbitrageConfigurationAsync(CancellationToken cancellationToken = default);
    Task SaveArbitrageConfigurationAsync(ArbitrageConfig configuration, CancellationToken cancellationToken = default);
    
    // Risk profile
    Task<RiskProfile> GetRiskProfileAsync(CancellationToken cancellationToken = default);
    Task SaveRiskProfileAsync(RiskProfile riskProfile, CancellationToken cancellationToken = default);
    
    // Application settings
    Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);
    Task SaveSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default);
} 