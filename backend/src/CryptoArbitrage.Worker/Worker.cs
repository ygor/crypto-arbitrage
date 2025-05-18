using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoArbitrage.Worker;

/// <summary>
/// Worker service for the arbitrage bot.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IArbitrageService _arbitrageService;
    private readonly IConfigurationService _configurationService;
    private readonly IHostApplicationLifetime _appLifetime;
    private bool _isShuttingDown = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="arbitrageService">The arbitrage service.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="appLifetime">The application lifetime.</param>
    public Worker(
        ILogger<Worker> logger,
        IArbitrageService arbitrageService,
        IConfigurationService configurationService,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _arbitrageService = arbitrageService;
        _configurationService = configurationService;
        _appLifetime = appLifetime;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("CryptoArbitrage Worker starting at: {time}", DateTimeOffset.Now);
            
            // Register shutdown handler
            _appLifetime.ApplicationStopping.Register(OnShutdown);

            // Initialize configurations
            await _configurationService.LoadConfigurationAsync(stoppingToken);
            _logger.LogInformation("Configuration loaded successfully");

            // Check if auto-start is enabled
            var config = await _configurationService.GetConfigurationAsync(stoppingToken);
            
            if (config != null && config.IsEnabled)
            {
                _logger.LogInformation("Auto-start is enabled, starting arbitrage bot...");
                await _arbitrageService.StartAsync(config.TradingPairs, stoppingToken);
            }
            else
            {
                _logger.LogInformation("Auto-start is disabled, waiting for manual start");
            }

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested && !_isShuttingDown)
            {
                // Check if the bot is still running
                bool isRunning = await _arbitrageService.IsRunningAsync();
                
                if (isRunning)
                {
                    _logger.LogInformation("CryptoArbitrage bot is running. Status: OK");
                }
                else
                {
                    _logger.LogInformation("CryptoArbitrage bot is not running. Waiting for manual start.");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
            _logger.LogInformation("CryptoArbitrage Worker is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred in the worker");
            throw;
        }
    }

    private void OnShutdown()
    {
        _isShuttingDown = true;
        _logger.LogInformation("Application is shutting down...");
        
        try
        {
            // Ensure the arbitrage service is stopped
            _arbitrageService.StopAsync().Wait();
            _logger.LogInformation("Arbitrage service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping arbitrage service during shutdown");
        }
    }
    
    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CryptoArbitrage Worker is stopping");
        
        try
        {
            // Make sure arbitrage service is stopped
            if (await _arbitrageService.IsRunningAsync())
            {
                _logger.LogInformation("Stopping arbitrage service...");
                await _arbitrageService.StopAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping arbitrage service");
        }
        
        await base.StopAsync(cancellationToken);
    }
}
