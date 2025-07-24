using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.IsArbitrageRunning;

/// <summary>
/// Handler for checking if the arbitrage bot is running.
/// </summary>
public class IsArbitrageRunningHandler : IRequestHandler<IsArbitrageRunningQuery, bool>
{
    private static bool _isRunning = false;

    public Task<bool> Handle(IsArbitrageRunningQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_isRunning);
    }

    /// <summary>
    /// Sets the running state (used by start/stop handlers).
    /// </summary>
    public static void SetRunning(bool isRunning) => _isRunning = isRunning;
} 