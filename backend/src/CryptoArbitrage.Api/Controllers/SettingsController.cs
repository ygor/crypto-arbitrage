using CryptoArbitrage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using CryptoArbitrage.Api.Controllers.Interfaces;
using ApiModels = CryptoArbitrage.Api.Models;
using DomainModels = CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase, ISettingsController
{
    private readonly IArbitrageService _arbitrageService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IArbitrageService arbitrageService,
        ISettingsRepository settingsRepository,
        ILogger<SettingsController> logger)
    {
        _arbitrageService = arbitrageService;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    [HttpGet("exchanges")]
    public async Task<ICollection<ApiModels.ExchangeConfiguration>> GetExchangeConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exchangeConfigs = await _settingsRepository.GetExchangeConfigurationsAsync(cancellationToken);
            return exchangeConfigs.Select(config => new ApiModels.ExchangeConfiguration
            {
                id = config.ExchangeId,
                name = config.Name,
                isEnabled = config.IsEnabled,
                apiKey = config.ApiKey,
                apiSecret = config.ApiSecret,
                tradingFeePercentage = config.TakerFeePercentage,
                availableBalances = new Dictionary<string, decimal>(), // Initialize with empty dictionary
                supportedPairs = config.SupportedTradingPairs?.Select(p => 
                {
                    var parts = p.Split('/');
                    return new ApiModels.TradingPair
                    {
                        baseCurrency = parts.Length > 0 ? parts[0] : string.Empty,
                        quoteCurrency = parts.Length > 1 ? parts[1] : string.Empty
                    };
                }).ToList() ?? new List<ApiModels.TradingPair>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exchange configurations");
            throw;
        }
    }

    [HttpPost("exchanges")]
    public async Task<ApiModels.SaveResponse> SaveExchangeConfigurationsAsync([FromBody] ICollection<ApiModels.ExchangeConfiguration> exchangeConfigs, CancellationToken cancellationToken = default)
    {
        try
        {
            var domainConfigs = exchangeConfigs.Select(config => new DomainModels.ExchangeConfig
            {
                ExchangeId = config.id,
                Name = config.name,
                IsEnabled = config.isEnabled,
                ApiKey = config.apiKey,
                ApiSecret = config.apiSecret,
                TakerFeePercentage = config.tradingFeePercentage,
                MakerFeePercentage = config.tradingFeePercentage,
                SupportedTradingPairs = config.supportedPairs?.Select(p => 
                    $"{p.baseCurrency}/{p.quoteCurrency}"
                ).ToList() ?? new List<string>()
            }).ToList();

            await _settingsRepository.SaveExchangeConfigurationsAsync(domainConfigs, cancellationToken);
            await _arbitrageService.RefreshExchangeConfigurationsAsync(cancellationToken);
            
            return new ApiModels.SaveResponse { message = "Exchange configurations saved successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving exchange configurations");
            throw;
        }
    }

    [HttpGet("arbitrage")]
    public async Task<ApiModels.ArbitrageConfiguration> GetArbitrageConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var arbitrageConfig = await _settingsRepository.GetArbitrageConfigurationAsync(cancellationToken);
            return new ApiModels.ArbitrageConfiguration
            {
                minimumSpreadPercentage = arbitrageConfig.IsEnabled ? 0.5m : 0m, // Default value
                minimumTradeAmount = 10m, // Default value
                maximumTradeAmount = 1000m, // Default value
                tradingPairs = arbitrageConfig.EnabledTradingPairs?.Select(p => 
                {
                    var parts = p.Split('/');
                    return new ApiModels.TradingPair
                    {
                        baseCurrency = parts.Length > 0 ? parts[0] : string.Empty,
                        quoteCurrency = parts.Length > 1 ? parts[1] : string.Empty
                    };
                }).ToList() ?? new List<ApiModels.TradingPair>(),
                scanIntervalMs = arbitrageConfig.ScanIntervalMs,
                enabledExchanges = arbitrageConfig.EnabledExchanges,
                autoExecuteTrades = arbitrageConfig.AutoTradeEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage configuration");
            throw;
        }
    }

    [HttpPost("arbitrage")]
    public async Task<ApiModels.SaveResponse> SaveArbitrageConfigurationAsync([FromBody] ApiModels.ArbitrageConfiguration arbitrageConfig, CancellationToken cancellationToken = default)
    {
        try
        {
            var domainConfig = new DomainModels.ArbitrageConfig
            {
                IsEnabled = true,
                EnabledTradingPairs = arbitrageConfig.tradingPairs?.Select(p => 
                    $"{p.baseCurrency}/{p.quoteCurrency}"
                ).ToList() ?? new List<string>(),
                ScanIntervalMs = arbitrageConfig.scanIntervalMs,
                EnabledExchanges = arbitrageConfig.enabledExchanges?.ToList() ?? new List<string>(),
                AutoTradeEnabled = arbitrageConfig.autoExecuteTrades,
                PollingIntervalMs = arbitrageConfig.scanIntervalMs, // Use same value
                MaxConcurrentScans = 5 // Default value
            };

            await _settingsRepository.SaveArbitrageConfigurationAsync(domainConfig, cancellationToken);
            await _arbitrageService.RefreshArbitrageConfigurationAsync(cancellationToken);
            
            return new ApiModels.SaveResponse { message = "Arbitrage configuration saved successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving arbitrage configuration");
            throw;
        }
    }

    [HttpGet("risk-profile")]
    public async Task<ApiModels.RiskProfileData> GetRiskProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var riskProfile = await _settingsRepository.GetRiskProfileAsync(cancellationToken);
            return new ApiModels.RiskProfileData
            {
                maxConcurrentTrades = riskProfile.MaxConcurrentTrades,
                maxDailyTradeVolume = riskProfile.MaxTradeAmount * 10, // Approximate conversion
                maxPositionPercentage = riskProfile.MaxCapitalPerTradePercent,
                tradeVolumeUnit = "USD", // Default currency unit
                cooldownPeriodMs = riskProfile.CooldownPeriodMs,
                minProfitPercent = riskProfile.MinimumProfitPercentage,
                maxTradeAmount = riskProfile.MaxTradeAmount,
                maxPortfolioPercent = riskProfile.MaxTotalExposurePercentage,
                maxSimultaneousTrades = riskProfile.MaxConcurrentTrades,
                enableStopLoss = riskProfile.UsePriceProtection,
                stopLossPercent = riskProfile.StopLossPercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk profile");
            throw;
        }
    }

    [HttpPost("risk-profile")]
    public async Task<ApiModels.SaveResponse> SaveRiskProfileAsync([FromBody] ApiModels.RiskProfileData riskProfile, CancellationToken cancellationToken = default)
    {
        try
        {
            var domainProfile = new DomainModels.RiskProfile
            {
                MaxConcurrentTrades = riskProfile.maxConcurrentTrades,
                MaxTradeAmount = riskProfile.maxTradeAmount,
                MaxCapitalPerTradePercent = riskProfile.maxPositionPercentage,
                CooldownPeriodMs = riskProfile.cooldownPeriodMs,
                MinimumProfitPercentage = riskProfile.minProfitPercent,
                MaxTotalExposurePercentage = riskProfile.maxPortfolioPercent,
                UsePriceProtection = riskProfile.enableStopLoss,
                StopLossPercentage = riskProfile.stopLossPercent
            };

            await _settingsRepository.SaveRiskProfileAsync(domainProfile, cancellationToken);
            await _arbitrageService.RefreshRiskProfileAsync(cancellationToken);
            
            return new ApiModels.SaveResponse { message = "Risk profile saved successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving risk profile");
            throw;
        }
    }

    [HttpPost("bot/start")]
    public async Task<ApiModels.BotResponse> StartBotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _arbitrageService.StartAsync(cancellationToken);
            return ApiModels.BotResponse.Success("Arbitrage bot started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting arbitrage bot");
            throw;
        }
    }

    [HttpPost("bot/stop")]
    public async Task<ApiModels.BotResponse> StopBotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _arbitrageService.StopAsync(cancellationToken);
            return ApiModels.BotResponse.Success("Arbitrage bot stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping arbitrage bot");
            throw;
        }
    }

    [HttpGet("bot/status")]
    public async Task<ApiModels.BotStatus> GetBotStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isRunning = await _arbitrageService.IsRunningAsync(cancellationToken);
            return new ApiModels.BotStatus { 
                isRunning = isRunning,
                state = isRunning ? "Running" : "Stopped",
                startTime = isRunning ? DateTime.UtcNow.AddHours(-1).ToString("o") : DateTime.UtcNow.ToString("o"),
                uptimeSeconds = isRunning ? 3600 : 0,
                errorState = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bot status");
            throw;
        }
    }
} 