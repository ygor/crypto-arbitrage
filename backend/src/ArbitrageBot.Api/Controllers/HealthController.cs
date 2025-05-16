using ArbitrageBot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace ArbitrageBot.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
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
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            var cpuUsage = process.TotalProcessorTime.TotalMilliseconds / 
                           (Environment.ProcessorCount * (DateTime.UtcNow - process.StartTime).TotalMilliseconds) * 100;
            var isRunning = await _arbitrageService.IsRunningAsync();
            
            var health = new
            {
                Status = "Healthy",
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                Uptime = (DateTime.UtcNow - _startTime).ToString(),
                MemoryUsageMB = memoryMB,
                CpuUsagePercent = Math.Round(cpuUsage, 2),
                ArbitrageBotRunning = isRunning,
                Timestamp = DateTime.UtcNow
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");
            return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var isRunning = await _arbitrageService.IsRunningAsync();
            
            var metrics = new
            {
                Process = new
                {
                    MemoryUsageMB = process.WorkingSet64 / 1024 / 1024,
                    TotalCpuTime = process.TotalProcessorTime.TotalSeconds,
                    UserProcessorTime = process.UserProcessorTime.TotalSeconds,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    StartTime = process.StartTime.ToUniversalTime(),
                    UptimeSeconds = (DateTime.UtcNow - process.StartTime).TotalSeconds
                },
                Application = new
                {
                    ArbitrageBotRunning = isRunning,
                    ApiRequestsPerMinute = 0, // Placeholder for actual metrics
                    ActiveConnections = 0,    // Placeholder for actual metrics
                    ErrorRate = 0            // Placeholder for actual metrics
                }
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
} 