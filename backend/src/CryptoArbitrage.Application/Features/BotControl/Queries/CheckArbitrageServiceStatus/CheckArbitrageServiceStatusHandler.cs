using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.CheckArbitrageServiceStatus;

/// <summary>
/// Handler for checking arbitrage service status.
/// </summary>
public class CheckArbitrageServiceStatusHandler : IRequestHandler<CheckArbitrageServiceStatusQuery, bool>
{
    private static bool _isRunning = false;

    public Task<bool> Handle(CheckArbitrageServiceStatusQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_isRunning);
    }

    /// <summary>
    /// Sets the running state (used by start/stop handlers).
    /// </summary>
    public static void SetRunning(bool isRunning) => _isRunning = isRunning;
} 