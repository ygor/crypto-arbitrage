using System.Collections.Concurrent;
using System.Text.Json;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ArbitrageBot.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly ILogger<SettingsRepository> _logger;
    private readonly string _settingsFilePath;
    private readonly ConcurrentDictionary<string, object> _cache = new();
    
    // Default settings
    private static readonly RiskProfile _defaultRiskProfile = RiskProfile.CreateBalanced();
    private static readonly ArbitrageConfig _defaultArbitrageConfig = new();
    private static readonly List<ExchangeConfig> _defaultExchangeConfigs = new()
    {
        new ExchangeConfig { ExchangeId = "binance", Name = "Binance", IsEnabled = true },
        new ExchangeConfig { ExchangeId = "coinbase", Name = "Coinbase", IsEnabled = true },
        new ExchangeConfig { ExchangeId = "kraken", Name = "Kraken", IsEnabled = true },
        new ExchangeConfig { ExchangeId = "kucoin", Name = "KuCoin", IsEnabled = false },
        new ExchangeConfig { ExchangeId = "okx", Name = "OKX", IsEnabled = false }
    };

    public SettingsRepository(ILogger<SettingsRepository> logger)
    {
        _logger = logger;
        
        // Create settings directory if it doesn't exist
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArbitrageBot");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
        
        // Load settings from file if exists
        LoadSettingsFromFile();
    }

    public async Task<List<ExchangeConfig>> GetExchangeConfigurationsAsync()
    {
        if (_cache.TryGetValue("exchangeConfigs", out var cached))
        {
            return (List<ExchangeConfig>)cached;
        }
        
        var result = _defaultExchangeConfigs;
        _cache["exchangeConfigs"] = result;
        return result;
    }

    public async Task<ExchangeConfig?> GetExchangeConfigurationAsync(string exchangeId)
    {
        var configs = await GetExchangeConfigurationsAsync();
        return configs.FirstOrDefault(c => c.ExchangeId.Equals(exchangeId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveExchangeConfigurationsAsync(List<ExchangeConfig> configurations)
    {
        _cache["exchangeConfigs"] = configurations;
        await SaveSettingsToFileAsync();
    }

    public async Task SaveExchangeConfigurationAsync(ExchangeConfig configuration)
    {
        var configs = await GetExchangeConfigurationsAsync();
        var existingIndex = configs.FindIndex(c => c.ExchangeId.Equals(configuration.ExchangeId, StringComparison.OrdinalIgnoreCase));
        
        if (existingIndex >= 0)
        {
            configs[existingIndex] = configuration;
        }
        else
        {
            configs.Add(configuration);
        }
        
        await SaveExchangeConfigurationsAsync(configs);
    }

    public async Task<ArbitrageConfig> GetArbitrageConfigurationAsync()
    {
        if (_cache.TryGetValue("arbitrageConfig", out var cached))
        {
            return (ArbitrageConfig)cached;
        }
        
        var result = _defaultArbitrageConfig;
        _cache["arbitrageConfig"] = result;
        return result;
    }

    public async Task SaveArbitrageConfigurationAsync(ArbitrageConfig configuration)
    {
        _cache["arbitrageConfig"] = configuration;
        await SaveSettingsToFileAsync();
    }

    public async Task<RiskProfile> GetRiskProfileAsync()
    {
        if (_cache.TryGetValue("riskProfile", out var cached))
        {
            return (RiskProfile)cached;
        }
        
        var result = _defaultRiskProfile;
        _cache["riskProfile"] = result;
        return result;
    }

    public async Task SaveRiskProfileAsync(RiskProfile riskProfile)
    {
        _cache["riskProfile"] = riskProfile;
        await SaveSettingsToFileAsync();
    }

    public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached is T typedValue)
            {
                return typedValue;
            }
        }
        
        return defaultValue;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        _cache[key] = value!;
        await SaveSettingsToFileAsync();
    }
    
    private void LoadSettingsFromFile()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (settings != null)
                {
                    foreach (var setting in settings)
                    {
                        if (setting.Key == "exchangeConfigs")
                        {
                            var configs = setting.Value.Deserialize<List<ExchangeConfig>>();
                            if (configs != null)
                            {
                                _cache[setting.Key] = configs;
                            }
                        }
                        else if (setting.Key == "arbitrageConfig")
                        {
                            var config = setting.Value.Deserialize<ArbitrageConfig>();
                            if (config != null)
                            {
                                _cache[setting.Key] = config;
                            }
                        }
                        else if (setting.Key == "riskProfile")
                        {
                            var profile = setting.Value.Deserialize<RiskProfile>();
                            if (profile != null)
                            {
                                _cache[setting.Key] = profile;
                            }
                        }
                        else
                        {
                            // For generic settings, store as JsonElement until requested with type
                            _cache[setting.Key] = setting.Value;
                        }
                    }
                }
                
                _logger.LogInformation("Settings loaded from {FilePath}", _settingsFilePath);
            }
            else
            {
                _logger.LogInformation("Settings file not found. Using default settings.");
                // Initialize with defaults
                _cache["exchangeConfigs"] = _defaultExchangeConfigs;
                _cache["arbitrageConfig"] = _defaultArbitrageConfig;
                _cache["riskProfile"] = _defaultRiskProfile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings from file");
            // Initialize with defaults on error
            _cache["exchangeConfigs"] = _defaultExchangeConfigs;
            _cache["arbitrageConfig"] = _defaultArbitrageConfig;
            _cache["riskProfile"] = _defaultRiskProfile;
        }
    }
    
    private async Task SaveSettingsToFileAsync()
    {
        try
        {
            var settings = new Dictionary<string, object>();
            
            foreach (var item in _cache)
            {
                settings[item.Key] = item.Value;
            }
            
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger.LogInformation("Settings saved to {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to file");
        }
    }
} 