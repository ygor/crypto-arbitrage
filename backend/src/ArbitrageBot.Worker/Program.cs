using ArbitrageBot.Application;
using ArbitrageBot.Application.Services;
using ArbitrageBot.Infrastructure;
using ArbitrageBot.Infrastructure.Services;
using ArbitrageBot.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting ArbitrageBot Worker with performance optimizations");

    // Check if running in high-performance mode
    bool highPerformanceMode = args.Contains("--high-performance") || 
                              Environment.GetEnvironmentVariable("ARBITRAGEBOT_HIGH_PERFORMANCE") == "true";
    
    if (highPerformanceMode)
    {
        Log.Information("Running in HIGH PERFORMANCE mode");
    }

    IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            // Add additional configuration sources if needed
            config.AddEnvironmentVariables(prefix: "ArbitrageBot_");
        })
        .UseSerilog((hostContext, loggerConfiguration) => 
        {
            loggerConfiguration
                .ReadFrom.Configuration(hostContext.Configuration)
                .Enrich.FromLogContext();
        })
        .ConfigureServices((hostContext, services) =>
        {
            // Register application services
            services.AddApplicationServices();
            
            // Register infrastructure services based on performance mode
            if (highPerformanceMode)
            {
                services.AddHighPerformanceServices();
                
                // Register optimized services
                services.AddSingleton<OptimizedArbitrageDetectionService>();
                
                // Increase thread pool size for high performance scenarios
                ThreadPool.SetMinThreads(32, 32);
            }
            else
            {
                services.AddInfrastructureServices();
            }
            
            // Register the worker
            services.AddHostedService<Worker>();
            
            // Register additional optimization services
            services.AddSingleton<PerformanceMetricsService>();
            
            // Add performance diagnostics
            services.AddHostedService<PerformanceDiagnosticsService>();
        })
        .Build();

    await host.RunAsync();
    
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
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
