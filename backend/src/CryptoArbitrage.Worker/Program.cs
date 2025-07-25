using CryptoArbitrage.Application;
using CryptoArbitrage.Infrastructure;
using CryptoArbitrage.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Infrastructure.Repositories;
using CryptoArbitrage.Infrastructure.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cryptoarbitrage-worker-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting CryptoArbitrage Worker");

    var builder = Host.CreateApplicationBuilder(args);
    
    // Add additional configuration sources if needed
    builder.Configuration.AddEnvironmentVariables(prefix: "CryptoArbitrage_");
    
    // Configure Serilog
    builder.Services.AddSerilog(Log.Logger);
    
    // Configure services
    var configuration = builder.Configuration;

    // Add health checks
    builder.Services.AddHealthChecks();

    // Add application services using the newer version with business logic services
    CryptoArbitrage.Application.ServiceCollectionExtensions.AddApplicationServices(builder.Services);

    // Also register additional services from DependencyInjection class
    CryptoArbitrage.Application.DependencyInjection.AddApplicationServices(builder.Services);

    // Add infrastructure services
    builder.Services.AddInfrastructureServices();

    // Register the Worker service as a hosted service
    builder.Services.AddHostedService<Worker>();

    // Add services to the container.
    builder.Services.AddHostedService<PerformanceDiagnosticsService>();

    // Add configuration services - changed from Scoped to Singleton to match Infrastructure registration
    builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
    builder.Services.AddSingleton<ConfigurationLoader>();

    // Make sure IArbitrageRepository is registered
    builder.Services.AddSingleton<IArbitrageRepository, ArbitrageRepository>();
    
    var host = builder.Build();

    // Run the host
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "CryptoArbitrage Worker terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// A hosted service that reports performance metrics.
/// </summary>
public class PerformanceDiagnosticsService : BackgroundService
{
    private readonly PerformanceMetricsService _metricsService;
    private readonly ILogger<PerformanceDiagnosticsService> _logger;
    
    public PerformanceDiagnosticsService(
        PerformanceMetricsService metricsService,
        ILogger<PerformanceDiagnosticsService> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance diagnostics service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Report metrics every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                
                // Report on CPU and memory usage
                var process = Process.GetCurrentProcess();
                var cpuTime = process.TotalProcessorTime;
                var memory = process.WorkingSet64 / 1024 / 1024; // MB
                
                _logger.LogInformation(
                    "Performance: CPU Time: {CpuTime}ms, Memory: {MemoryMB}MB, Threads: {ThreadCount}", 
                    cpuTime.TotalMilliseconds,
                    memory,
                    process.Threads.Count);
                
                // Report on operation metrics
                foreach (var metric in _metricsService.OperationMetrics)
                {
                    if (metric.Value.ExecutionCount > 0)
                    {
                        _logger.LogDebug(
                            "Operation: {Operation}, Count: {Count}, Avg Time: {AvgTime}ms, Total: {TotalTime}ms",
                            metric.Key,
                            metric.Value.ExecutionCount,
                            metric.Value.AverageExecutionTimeMs,
                            metric.Value.TotalExecutionTimeMs);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance diagnostics service");
            }
        }
        
        _logger.LogInformation("Performance diagnostics service stopped");
    }
}
