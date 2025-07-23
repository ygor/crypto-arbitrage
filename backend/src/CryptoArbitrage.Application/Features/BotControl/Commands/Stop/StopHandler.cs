using MediatR;
using Microsoft.Extensions.Logging;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.Stop;

/// <summary>
/// Handler for the simple stop command.
/// </summary>
public class StopHandler : IRequestHandler<StopCommand, StopResult>
{
    private readonly ILogger<StopHandler> _logger;

    public StopHandler(ILogger<StopHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StopResult> Handle(StopCommand request, CancellationToken cancellationToken)
    {
        try
        {
            IsRunningHandler.SetRunning(false);
            _logger.LogInformation("Bot stopped successfully");
            return new StopResult(true, "Bot stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop bot");
            return new StopResult(false, $"Failed to stop bot: {ex.Message}");
        }
    }
} 