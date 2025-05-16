using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace ArbitrageBot.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
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
    public async Task<IActionResult> GetExchangeConfigurations()
    {
        try
        {
            var exchangeConfigs = await _settingsRepository.GetExchangeConfigurationsAsync();
            return Ok(exchangeConfigs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exchange configurations");
            return StatusCode(500, "An error occurred while retrieving exchange configurations");
        }
    }

    [HttpPost("exchanges")]
    public async Task<IActionResult> SaveExchangeConfigurations([FromBody] List<ExchangeConfig> exchangeConfigs)
    {
        try
        {
            await _settingsRepository.SaveExchangeConfigurationsAsync(exchangeConfigs);
            await _arbitrageService.RefreshExchangeConfigurationsAsync();
            return Ok(new { message = "Exchange configurations saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving exchange configurations");
            return StatusCode(500, "An error occurred while saving exchange configurations");
        }
    }

    [HttpGet("arbitrage")]
    public async Task<IActionResult> GetArbitrageConfiguration()
    {
        try
        {
            var arbitrageConfig = await _settingsRepository.GetArbitrageConfigurationAsync();
            return Ok(arbitrageConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage configuration");
            return StatusCode(500, "An error occurred while retrieving arbitrage configuration");
        }
    }

    [HttpPost("arbitrage")]
    public async Task<IActionResult> SaveArbitrageConfiguration([FromBody] ArbitrageConfig arbitrageConfig)
    {
        try
        {
            await _settingsRepository.SaveArbitrageConfigurationAsync(arbitrageConfig);
            await _arbitrageService.RefreshArbitrageConfigurationAsync();
            return Ok(new { message = "Arbitrage configuration saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving arbitrage configuration");
            return StatusCode(500, "An error occurred while saving arbitrage configuration");
        }
    }

    [HttpGet("risk-profile")]
    public async Task<IActionResult> GetRiskProfile()
    {
        try
        {
            var riskProfile = await _settingsRepository.GetRiskProfileAsync();
            return Ok(riskProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk profile");
            return StatusCode(500, "An error occurred while retrieving risk profile");
        }
    }

    [HttpPost("risk-profile")]
    public async Task<IActionResult> SaveRiskProfile([FromBody] RiskProfile riskProfile)
    {
        try
        {
            await _settingsRepository.SaveRiskProfileAsync(riskProfile);
            await _arbitrageService.RefreshRiskProfileAsync();
            return Ok(new { message = "Risk profile saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving risk profile");
            return StatusCode(500, "An error occurred while saving risk profile");
        }
    }

    [HttpPost("bot/start")]
    public async Task<IActionResult> StartBot()
    {
        try
        {
            await _arbitrageService.StartAsync();
            return Ok(new { message = "Arbitrage bot started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting arbitrage bot");
            return StatusCode(500, "An error occurred while starting arbitrage bot");
        }
    }

    [HttpPost("bot/stop")]
    public async Task<IActionResult> StopBot()
    {
        try
        {
            await _arbitrageService.StopAsync();
            return Ok(new { message = "Arbitrage bot stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping arbitrage bot");
            return StatusCode(500, "An error occurred while stopping arbitrage bot");
        }
    }

    [HttpGet("bot/status")]
    public async Task<IActionResult> GetBotStatus()
    {
        try
        {
            var isRunning = await _arbitrageService.IsRunningAsync();
            return Ok(new { isRunning });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bot status");
            return StatusCode(500, "An error occurred while retrieving bot status");
        }
    }
} 