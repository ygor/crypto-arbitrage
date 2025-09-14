using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Monitoring;

/// <summary>
/// Prometheus metrics for crypto arbitrage system monitoring.
/// Tracks business KPIs and technical performance metrics.
/// </summary>
public class PrometheusMetrics : IHostedService
{
    private readonly ILogger<PrometheusMetrics> _logger;
    private readonly Meter _meter;

    // Business Metrics
    public Counter<long> OpportunitiesDetected { get; }
    public Counter<long> TradesExecuted { get; }
    public Counter<decimal> TotalProfit { get; }
    public Histogram<double> ProfitPercentage { get; }
    public Histogram<double> OpportunityLatency { get; }
    public Gauge<int> ActiveOpportunities { get; }

    // Technical Metrics  
    public Counter<long> ApiRequests { get; }
    public Counter<long> ApiErrors { get; }
    public Histogram<double> ApiResponseTime { get; }
    public Histogram<double> DatabaseQueryTime { get; }
    public Gauge<int> ActiveConnections { get; }
    public Counter<long> WebSocketReconnections { get; }

    // Exchange Metrics
    public Histogram<double> ExchangeLatency { get; }
    public Counter<long> ExchangeErrors { get; }
    public Gauge<decimal> ExchangeSpread { get; }
    public Counter<long> OrderBookUpdates { get; }

    // System Health
    public Gauge<double> CpuUsage { get; }
    public Gauge<long> MemoryUsage { get; }
    public Gauge<int> ThreadCount { get; }
    public Counter<long> Exceptions { get; }

    public PrometheusMetrics(ILogger<PrometheusMetrics> logger)
    {
        _logger = logger;
        _meter = new Meter("CryptoArbitrage", "1.0.0");

        // Business Metrics
        OpportunitiesDetected = _meter.CreateCounter<long>(
            "arbitrage_opportunities_detected_total",
            description: "Total number of arbitrage opportunities detected");

        TradesExecuted = _meter.CreateCounter<long>(
            "arbitrage_trades_executed_total", 
            description: "Total number of arbitrage trades executed");

        TotalProfit = _meter.CreateCounter<decimal>(
            "arbitrage_profit_total",
            unit: "USD",
            description: "Total profit from arbitrage trades");

        ProfitPercentage = _meter.CreateHistogram<double>(
            "arbitrage_profit_percentage",
            unit: "%",
            description: "Distribution of profit percentages");

        OpportunityLatency = _meter.CreateHistogram<double>(
            "arbitrage_opportunity_latency_seconds",
            unit: "s", 
            description: "Time from price update to opportunity detection");

        ActiveOpportunities = _meter.CreateGauge<int>(
            "arbitrage_active_opportunities",
            description: "Number of currently active arbitrage opportunities");

        // Technical Metrics
        ApiRequests = _meter.CreateCounter<long>(
            "exchange_api_requests_total",
            description: "Total number of exchange API requests");

        ApiErrors = _meter.CreateCounter<long>(
            "exchange_api_errors_total",
            description: "Total number of exchange API errors");

        ApiResponseTime = _meter.CreateHistogram<double>(
            "exchange_api_response_time_seconds",
            unit: "s",
            description: "Exchange API response time distribution");

        DatabaseQueryTime = _meter.CreateHistogram<double>(
            "database_query_time_seconds",
            unit: "s",
            description: "Database query execution time");

        ActiveConnections = _meter.CreateGauge<int>(
            "database_active_connections",
            description: "Number of active database connections");

        WebSocketReconnections = _meter.CreateCounter<long>(
            "websocket_reconnections_total",
            description: "Total number of WebSocket reconnections");

        // Exchange Metrics
        ExchangeLatency = _meter.CreateHistogram<double>(
            "exchange_latency_seconds",
            unit: "s",
            description: "Exchange-specific latency distribution");

        ExchangeErrors = _meter.CreateCounter<long>(
            "exchange_errors_total", 
            description: "Exchange-specific error count");

        ExchangeSpread = _meter.CreateGauge<decimal>(
            "exchange_spread_percentage",
            unit: "%",
            description: "Current spread percentage by exchange and pair");

        OrderBookUpdates = _meter.CreateCounter<long>(
            "order_book_updates_total",
            description: "Total number of order book updates received");

        // System Health
        CpuUsage = _meter.CreateGauge<double>(
            "system_cpu_usage_percentage",
            unit: "%",
            description: "Current CPU usage percentage");

        MemoryUsage = _meter.CreateGauge<long>(
            "system_memory_usage_bytes",
            unit: "bytes",
            description: "Current memory usage in bytes");

        ThreadCount = _meter.CreateGauge<int>(
            "system_thread_count",
            description: "Current number of threads");

        Exceptions = _meter.CreateCounter<long>(
            "system_exceptions_total",
            description: "Total number of unhandled exceptions");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started Prometheus metrics collection");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _meter.Dispose();
        _logger.LogInformation("Stopped Prometheus metrics collection");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records an arbitrage opportunity detection event.
    /// </summary>
    public void RecordOpportunityDetected(string exchange1, string exchange2, string tradingPair, decimal profitPercentage)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("exchange1", exchange1),
            new KeyValuePair<string, object?>("exchange2", exchange2),
            new KeyValuePair<string, object?>("trading_pair", tradingPair)
        };
        
        OpportunitiesDetected.Add(1, tags);
        ProfitPercentage.Record((double)profitPercentage, tags);
    }

    /// <summary>
    /// Records a trade execution event.
    /// </summary>
    public void RecordTradeExecuted(string exchangeId, string tradingPair, decimal profitAmount, bool successful)
    {
        var tradeTags = new[]
        {
            new KeyValuePair<string, object?>("exchange", exchangeId),
            new KeyValuePair<string, object?>("trading_pair", tradingPair),
            new KeyValuePair<string, object?>("status", successful ? "success" : "failure")
        };

        TradesExecuted.Add(1, tradeTags);

        if (successful)
        {
            var profitTags = new[]
            {
                new KeyValuePair<string, object?>("exchange", exchangeId),
                new KeyValuePair<string, object?>("trading_pair", tradingPair)
            };
            TotalProfit.Add(profitAmount, profitTags);
        }
    }

    /// <summary>
    /// Records API request metrics.
    /// </summary>
    public void RecordApiRequest(string exchangeId, double responseTimeSeconds, bool successful)
    {
        var requestTags = new[]
        {
            new KeyValuePair<string, object?>("exchange", exchangeId),
            new KeyValuePair<string, object?>("status", successful ? "success" : "error")
        };

        ApiRequests.Add(1, requestTags);

        if (!successful)
        {
            var errorTags = new[]
            {
                new KeyValuePair<string, object?>("exchange", exchangeId)
            };
            ApiErrors.Add(1, errorTags);
        }

        var latencyTags = new[]
        {
            new KeyValuePair<string, object?>("exchange", exchangeId)
        };
        ApiResponseTime.Record(responseTimeSeconds, latencyTags);
    }

    /// <summary>
    /// Records database operation metrics.
    /// </summary>
    public void RecordDatabaseOperation(string operation, double executionTimeSeconds, bool successful)
    {
        var dbTags = new[]
        {
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", successful ? "success" : "error")
        };
        
        DatabaseQueryTime.Record(executionTimeSeconds, dbTags);
    }

    /// <summary>
    /// Updates system health metrics.
    /// </summary>
    public void UpdateSystemHealth(double cpuPercentage, long memoryBytes, int threadCount)
    {
        CpuUsage.Record(cpuPercentage);
        MemoryUsage.Record(memoryBytes);
        ThreadCount.Record(threadCount);
    }

    /// <summary>
    /// Records an exception occurrence.
    /// </summary>
    public void RecordException(string exceptionType, string source)
    {
        var exceptionTags = new[]
        {
            new KeyValuePair<string, object?>("exception_type", exceptionType),
            new KeyValuePair<string, object?>("source", source)
        };
        
        Exceptions.Add(1, exceptionTags);
    }
} 