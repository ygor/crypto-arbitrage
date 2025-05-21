using CryptoArbitrage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using CryptoArbitrage.Api.Controllers.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using ApiModels = CryptoArbitrage.Api.Models;

namespace CryptoArbitrage.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase, IHealthController
{
    private readonly IArbitrageService _arbitrageService;
    private readonly ILogger<HealthController> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthController(
        IArbitrageService arbitrageService,
        ILogger<HealthController> logger)
    {
        _arbitrageService = arbitrageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ApiModels.HealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            var cpuUsage = process.TotalProcessorTime.TotalMilliseconds / 
                           (Environment.ProcessorCount * (DateTime.UtcNow - process.StartTime).TotalMilliseconds) * 100;
            var isRunning = _arbitrageService.IsRunning;
            
            return new ApiModels.HealthStatus
            {
                status = "Healthy",
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                uptime = (DateTime.UtcNow - _startTime).ToString(),
                memoryUsageMB = (int)memoryMB,
                cpuUsagePercent = Math.Round(cpuUsage, 2),
                cryptoArbitrageBotRunning = isRunning,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");
            throw;
        }
    }

    [HttpGet("metrics")]
    public async Task<ApiModels.SystemMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var isRunning = _arbitrageService.IsRunning;
            
            return new ApiModels.SystemMetrics
            {
                process = new ApiModels.ProcessMetrics
                {
                    memoryUsageMB = (int)(process.WorkingSet64 / 1024 / 1024),
                    totalCpuTime = process.TotalProcessorTime.TotalSeconds,
                    userProcessorTime = process.UserProcessorTime.TotalSeconds,
                    threadCount = process.Threads.Count,
                    handleCount = process.HandleCount,
                    startTime = process.StartTime.ToUniversalTime().ToString("o"),
                    uptimeSeconds = (DateTime.UtcNow - process.StartTime).TotalSeconds
                },
                application = new ApiModels.ApplicationMetrics
                {
                    cryptoArbitrageBotRunning = isRunning,
                    apiRequestsPerMinute = 0, // Placeholder for actual metrics
                    activeConnections = 0,    // Placeholder for actual metrics
                    errorRate = 0            // Placeholder for actual metrics
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            throw;
        }
    }
} 