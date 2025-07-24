using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;

/// <summary>
/// Test configuration service that provides realistic business configuration.
/// </summary>
public class TestConfigurationService : IConfigurationService
{
    private ArbitrageConfiguration _configuration;
    
    public TestConfigurationService()
    {
        // Setup realistic test configuration
        _configuration = new ArbitrageConfiguration
        {
            RiskProfile = new RiskProfile
            {
                MaxTradeAmount = 1000m,           // $1000 max per trade
                DailyLossLimitPercent = 5.0m,     // 5% daily loss limit
                MinProfitThresholdPercent = 0.5m, // 0.5% minimum profit
                MaxCapitalPerTradePercent = 10.0m // 10% max position size
            },
            TradingPairs = new List<TradingPair>
            {
                TradingPair.Parse("BTC/USD"),
                TradingPair.Parse("ETH/USD"),
                TradingPair.Parse("ADA/USD")
            },
            EnabledExchanges = new List<string> { "coinbase", "kraken", "binance" },
            IsEnabled = true
        };
    }
    
    public Task<ArbitrageConfiguration> GetConfigurationAsync()
    {
        return Task.FromResult(_configuration);
    }
    
    public Task SaveConfigurationAsync(ArbitrageConfiguration configuration)
    {
        _configuration = configuration;
        return Task.CompletedTask;
    }

    // Complete implementation of all IConfigurationService methods
    public Task<ArbitrageConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult(_configuration);
    
    public Task<ArbitrageConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ArbitrageConfiguration>(_configuration);
    
    public Task<RiskProfile> GetRiskProfileAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult(_configuration.RiskProfile ?? new RiskProfile());
    
    public Task<ExchangeConfiguration> GetExchangeConfigurationAsync(string exchangeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ExchangeConfiguration>(new ExchangeConfiguration
        {
            ExchangeId = exchangeId,
            ApiKey = "test_api_key",
            // ... existing code ...
        });
    
    public Task<IReadOnlyCollection<ExchangeConfiguration>> GetAllExchangeConfigurationsAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult<IReadOnlyCollection<ExchangeConfiguration>>(new List<ExchangeConfiguration>());
    
    public Task<NotificationConfiguration> GetNotificationConfigurationAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult(new NotificationConfiguration());
    
    public Task UpdateRiskProfileAsync(RiskProfile riskProfile, CancellationToken cancellationToken = default) => 
        Task.CompletedTask;
    
    public Task UpdateConfigurationAsync(ArbitrageConfiguration configuration, CancellationToken cancellationToken = default) => 
        Task.CompletedTask;
    
    public Task UpdateExchangeConfigurationAsync(ExchangeConfiguration exchangeConfiguration, CancellationToken cancellationToken = default) => 
        Task.CompletedTask;
    
    public Task UpdateNotificationConfigurationAsync(NotificationConfiguration notificationConfiguration, CancellationToken cancellationToken = default) => 
        Task.CompletedTask;
    
    public Task<ArbitrageConfiguration> GetArbitrageConfigurationAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult<ArbitrageConfiguration>(_configuration);
    
    public Task UpdateArbitrageConfigurationAsync(ArbitrageConfiguration configuration, CancellationToken cancellationToken = default) => 
        Task.CompletedTask;
} 