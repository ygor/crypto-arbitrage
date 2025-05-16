using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

public interface ISettingsRepository
{
    // Exchange configurations
    Task<List<ExchangeConfig>> GetExchangeConfigurationsAsync();
    Task<ExchangeConfig?> GetExchangeConfigurationAsync(string exchangeId);
    Task SaveExchangeConfigurationsAsync(List<ExchangeConfig> configurations);
    Task SaveExchangeConfigurationAsync(ExchangeConfig configuration);
    
    // Arbitrage configurations
    Task<ArbitrageConfig> GetArbitrageConfigurationAsync();
    Task SaveArbitrageConfigurationAsync(ArbitrageConfig configuration);
    
    // Risk profile
    Task<RiskProfile> GetRiskProfileAsync();
    Task SaveRiskProfileAsync(RiskProfile riskProfile);
    
    // Application settings
    Task<T> GetSettingAsync<T>(string key, T defaultValue);
    Task SaveSettingAsync<T>(string key, T value);
} 