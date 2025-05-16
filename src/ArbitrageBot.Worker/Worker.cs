using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Application.Services;
using ArbitrageBot.Domain.Models;
using ArbitrageBot.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArbitrageBot.Worker;

/// <summary>
/// Worker service for the arbitrage bot.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IArbitrageService _arbitrageService;
    private readonly OptimizedArbitrageDetectionService? _optimizedArbitrageService;
    private readonly PerformanceMetricsService _performanceMetrics;
    private readonly bool _highPerformanceMode;
    private RiskProfile _riskProfile = new RiskProfile();
    private CancellationTokenSource _reportingCts = new CancellationTokenSource();

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="arbitrageService">The arbitrage service.</param>
    /// <param name="performanceMetrics">The performance metrics service.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public Worker(
        ILogger<Worker> logger,
        IConfigurationService configurationService,
        IArbitrageService arbitrageService,
        PerformanceMetricsService performanceMetrics,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configurationService = configurationService;
        _arbitrageService = arbitrageService;
        _performanceMetrics = performanceMetrics;
        
        // Check if we're running in high-performance mode
        _highPerformanceMode = Environment.GetEnvironmentVariable("ARBITRAGEBOT_HIGH_PERFORMANCE") == "true" ||
                              Environment.CommandLine.Contains("--high-performance");
        
        // Try to resolve the optimized service (only registered in high-performance mode)
        if (_highPerformanceMode)
        {
            _optimizedArbitrageService = (OptimizedArbitrageDetectionService?)serviceProvider
                .GetService(typeof(OptimizedArbitrageDetectionService));
            
            if (_optimizedArbitrageService != null)
            {
                _logger.LogInformation("Using optimized arbitrage detection service");
            }
            else
            {
                _logger.LogWarning("High-performance mode enabled but optimized service not found");
            }
        }
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Arbitrage Bot Worker");
        
        using var performanceTimer = _performanceMetrics.StartTimer("Worker.StartAsync");
        
        try
        {
            _logger.LogInformation("Loading configuration...");
            var configuration = await _configurationService.LoadConfigurationAsync(cancellationToken);
            _riskProfile = configuration.RiskProfile;
            
            if (_optimizedArbitrageService != null)
            {
                await _optimizedArbitrageService.UpdateRiskProfileAsync(_riskProfile, cancellationToken);
            }
            
            _logger.LogInformation("Configuration loaded. Risk profile: Max trade amount: {MaxTradeAmount}, Min profit: {MinimumProfitPercentage}%",
                _riskProfile.MaxTradeAmount, _riskProfile.MinimumProfitPercentage);
            
            // Start the periodic reporting task
            _ = StartPeriodicReportingAsync(_reportingCts.Token);
            
            await base.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during worker startup");
            throw;
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Arbitrage Bot Worker running");
        
        try
        {
            if (_highPerformanceMode && _optimizedArbitrageService != null)
            {
                // Use optimized service
                await _optimizedArbitrageService.StartAsync(stoppingToken);
                
                // Wait for cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            else
            {
                // Use standard service
                await _arbitrageService.StartAsync(_riskProfile, stoppingToken);
                
                // Wait for cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal cancellation, don't treat as error
            _logger.LogInformation("Arbitrage service execution cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in arbitrage service");
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Arbitrage Bot Worker");
        
        using var performanceTimer = _performanceMetrics.StartTimer("Worker.StopAsync");
        
        try
        {
            // Cancel the reporting task
            _reportingCts.Cancel();
            
            if (_highPerformanceMode && _optimizedArbitrageService != null)
            {
                await _optimizedArbitrageService.StopAsync(cancellationToken);
            }
            else
            {
                await _arbitrageService.StopAsync(cancellationToken);
            }
            
            await base.StopAsync(cancellationToken);
            
            _logger.LogInformation("Arbitrage Bot Worker stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during worker shutdown");
            throw;
        }
    }
    
    private async Task StartPeriodicReportingAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                
                using var performanceTimer = _performanceMetrics.StartTimer("Worker.PeriodicReporting");
                
                if (_highPerformanceMode && _optimizedArbitrageService != null)
                {
                    // Report from optimized service
                    var opportunities = _optimizedArbitrageService.GetTopOpportunitiesCount();
                    _logger.LogInformation("Optimized arbitrage service currently tracking {Count} opportunities", 
                        opportunities);
                }
                else
                {
                    // Standard reporting
                    _logger.LogInformation("Arbitrage service running normally");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected cancellation
            _logger.LogDebug("Periodic reporting cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in periodic reporting");
        }
    }
}
