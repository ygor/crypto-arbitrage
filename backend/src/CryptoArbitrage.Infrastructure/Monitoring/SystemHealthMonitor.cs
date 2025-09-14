using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Monitoring;

/// <summary>
/// System health monitoring service that tracks system performance metrics.
/// </summary>
public class SystemHealthMonitor : BackgroundService
{
    private readonly ILogger<SystemHealthMonitor> _logger;
    private readonly PrometheusMetrics _metrics;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly Process _currentProcess;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30);

    public SystemHealthMonitor(ILogger<SystemHealthMonitor> logger, PrometheusMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
        _currentProcess = Process.GetCurrentProcess();

        // Initialize CPU counter (Windows/Linux compatible)
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize CPU performance counter. CPU metrics will be estimated.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System health monitoring started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectSystemMetrics();
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting system health metrics");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("System health monitoring stopped");
    }

    private async Task CollectSystemMetrics()
    {
        try
        {
            // Collect CPU usage
            var cpuUsage = await GetCpuUsageAsync();
            
            // Collect memory usage
            var memoryUsage = GetMemoryUsage();
            
            // Collect thread count
            var threadCount = _currentProcess.Threads.Count;
            
            // Update Prometheus metrics
            _metrics.UpdateSystemHealth(cpuUsage, memoryUsage, threadCount);

            _logger.LogDebug("System metrics updated - CPU: {CpuUsage:F1}%, Memory: {MemoryMB:F1}MB, Threads: {ThreadCount}",
                cpuUsage, memoryUsage / 1024.0 / 1024.0, threadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
        }
    }

    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            if (_cpuCounter != null && OperatingSystem.IsWindows())
            {
                // Windows: Use performance counter
                return _cpuCounter.NextValue();
            }
            else
            {
                // Cross-platform: Use process-based estimation
                return await EstimateCpuUsageAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU usage, returning 0");
            return 0.0;
        }
    }

    private async Task<double> EstimateCpuUsageAsync()
    {
        var startTime = DateTime.UtcNow;
        var startCpuUsage = _currentProcess.TotalProcessorTime;
        
        await Task.Delay(100); // Short sampling period
        
        var endTime = DateTime.UtcNow;
        var endCpuUsage = _currentProcess.TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return Math.Min(100.0, Math.Max(0.0, cpuUsageTotal * 100));
    }

    private long GetMemoryUsage()
    {
        try
        {
            // Get working set memory (physical memory currently used by the process)
            return _currentProcess.WorkingSet64;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory usage, returning 0");
            return 0;
        }
    }

    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _currentProcess?.Dispose();
        base.Dispose();
    }
} 