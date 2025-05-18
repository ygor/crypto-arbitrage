using System.Collections.Concurrent;
using System.Diagnostics;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// Represents metrics for a specific operation.
/// </summary>
public class OperationMetrics
{
    private readonly int _sampleSize;
    private readonly Queue<long> _executionTimes = new();
    private long _totalExecutionTime;
    private readonly object _lock = new();
    
    /// <summary>
    /// Gets the average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs
    {
        get
        {
            lock (_lock)
            {
                return _executionTimes.Count > 0 
                    ? (double)_totalExecutionTime / _executionTimes.Count 
                    : 0;
            }
        }
    }
    
    /// <summary>
    /// Gets the number of recorded executions.
    /// </summary>
    public int ExecutionCount
    {
        get
        {
            lock (_lock)
            {
                return _executionTimes.Count;
            }
        }
    }
    
    /// <summary>
    /// Gets the total execution time in milliseconds.
    /// </summary>
    public long TotalExecutionTimeMs
    {
        get
        {
            lock (_lock)
            {
                return _totalExecutionTime;
            }
        }
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationMetrics"/> class.
    /// </summary>
    /// <param name="sampleSize">The number of samples to keep for moving averages.</param>
    public OperationMetrics(int sampleSize = 100)
    {
        _sampleSize = sampleSize;
    }
    
    /// <summary>
    /// Records an execution of the operation.
    /// </summary>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    public void RecordExecution(long executionTimeMs)
    {
        lock (_lock)
        {
            _executionTimes.Enqueue(executionTimeMs);
            _totalExecutionTime += executionTimeMs;
            
            // Keep only the most recent samples for moving average
            if (_executionTimes.Count > _sampleSize)
            {
                _totalExecutionTime -= _executionTimes.Dequeue();
            }
        }
    }
    
    /// <summary>
    /// Resets the metrics.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _executionTimes.Clear();
            _totalExecutionTime = 0;
        }
    }
}

/// <summary>
/// A service for tracking performance metrics throughout the application.
/// </summary>
public class PerformanceMetricsService
{
    private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics = new();
    
    /// <summary>
    /// Gets all recorded operation metrics.
    /// </summary>
    public IReadOnlyDictionary<string, OperationMetrics> OperationMetrics => _operationMetrics;
    
    /// <summary>
    /// Starts timing an operation and returns a disposable timer that will record the duration when disposed.
    /// </summary>
    /// <param name="operationName">The name of the operation to time.</param>
    /// <returns>A disposable timer that will record the duration when disposed.</returns>
    public IDisposable StartTimer(string operationName)
    {
        return new OperationTimer(this, operationName);
    }
    
    /// <summary>
    /// Records the execution time of an operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    public void RecordOperation(string operationName, long executionTimeMs)
    {
        var metrics = _operationMetrics.GetOrAdd(operationName, _ => new OperationMetrics());
        metrics.RecordExecution(executionTimeMs);
    }
    
    /// <summary>
    /// Creates a timer that will automatically record the execution time when disposed.
    /// </summary>
    /// <param name="operationName">The name of the operation to time.</param>
    /// <returns>An IDisposable that should be disposed when the operation completes.</returns>
    public IDisposable TimeOperation(string operationName)
    {
        return new OperationTimer(this, operationName);
    }
    
    /// <summary>
    /// Gets metrics for a specific operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>The metrics for the operation, or null if no metrics have been recorded.</returns>
    public OperationMetrics? GetMetrics(string operationName)
    {
        _operationMetrics.TryGetValue(operationName, out var metrics);
        return metrics;
    }
    
    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void ResetAllMetrics()
    {
        foreach (var metrics in _operationMetrics.Values)
        {
            metrics.Reset();
        }
    }
    
    /// <summary>
    /// Timer class that automatically records the execution time when disposed.
    /// </summary>
    private class OperationTimer : IDisposable
    {
        private readonly PerformanceMetricsService _service;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        
        public OperationTimer(PerformanceMetricsService service, string operationName)
        {
            _service = service;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _service.RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
} 