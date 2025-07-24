using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
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
            Id = Guid.NewGuid(),
            RiskProfile = new RiskProfile
            {
                MaxTradeAmount = 1000m,           // $1000 max per trade
                DailyLossLimitPercent = 5.0m,     // 5% daily loss limit
                MinProfitThresholdPercent = 0.5m, // 0.5% minimum profit
                MaxPositionSizePercent = 10.0m    // 10% max position size
            },
            TradingPairs = new[]
            {
                TradingPair.Parse("BTC/USD"),
                TradingPair.Parse("ETH/USD"),
                TradingPair.Parse("ADA/USD")
            },
            EnabledExchanges = new[] { "coinbase", "kraken", "binance" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow
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
    public Task LoadConfigurationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    public Task<ArbitrageConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult<ArbitrageConfiguration?>(_configuration);
    
    public Task<RiskProfile> GetRiskProfileAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult(_configuration.RiskProfile ?? new RiskProfile());
    
    public Task<ExchangeConfiguration?> GetExchangeConfigurationAsync(string exchangeId, CancellationToken cancellationToken = default) => 
        Task.FromResult<ExchangeConfiguration?>(new ExchangeConfiguration { ExchangeId = exchangeId });
    
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
    
    public Task<ArbitrageConfiguration?> GetArbitrageConfigurationAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult<ArbitrageConfiguration?>(_configuration);
    
    public Task UpdateArbitrageConfigurationAsync(ArbitrageConfiguration configuration, CancellationToken cancellationToken = default) => 
        Task.CompletedTask;
} 