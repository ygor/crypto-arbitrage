using MediatR;

namespace CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

/// <summary>
/// Handler for the simple IsRunningQuery.
/// </summary>
public class IsRunningHandler : IRequestHandler<IsRunningQuery, bool>
{
    private static bool _isRunning = false;

    public async Task<bool> Handle(IsRunningQuery request, CancellationToken cancellationToken)
    {
        return _isRunning;
    }

    /// <summary>
    /// Sets the running state (used by start/stop handlers).
    /// </summary>
    public static void SetRunning(bool isRunning) => _isRunning = isRunning;
} 