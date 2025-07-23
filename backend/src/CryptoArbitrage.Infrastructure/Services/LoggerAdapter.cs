using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// Adapter to bridge different ILogger types.
/// </summary>
/// <typeparam name="T">The target logger category type.</typeparam>
public class LoggerAdapter<T> : ILogger<T>
{
    private readonly ILogger _logger;

    public LoggerAdapter(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
} 