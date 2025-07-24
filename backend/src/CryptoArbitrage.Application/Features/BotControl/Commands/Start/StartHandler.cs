using MediatR;
using Microsoft.Extensions.Logging;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.Start;

/// <summary>
/// Handler for the simple start command.
/// </summary>
public class StartHandler : IRequestHandler<StartCommand, StartResult>
{
    private readonly ILogger<StartHandler> _logger;

    public StartHandler(ILogger<StartHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<StartResult> Handle(StartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            IsRunningHandler.SetRunning(true);
            _logger.LogInformation("Bot started successfully");
            return Task.FromResult(new StartResult(true, "Bot started successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start bot");
            return Task.FromResult(new StartResult(false, $"Failed to start bot: {ex.Message}"));
        }
    }
} 