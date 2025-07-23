using MediatR;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;

/// <summary>
/// Handler for starting the arbitrage bot.
/// </summary>
public class StartArbitrageHandler : IRequestHandler<StartArbitrageCommand, StartArbitrageResult>
{
    private readonly ILogger<StartArbitrageHandler> _logger;
    private static bool _isRunning = false;

    public StartArbitrageHandler(ILogger<StartArbitrageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StartArbitrageResult> Handle(StartArbitrageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (_isRunning)
            {
                return new StartArbitrageResult(false, "Arbitrage bot is already running");
            }

            _isRunning = true;
            _logger.LogInformation("Arbitrage bot started successfully");
            
            return new StartArbitrageResult(true, "Arbitrage bot started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start arbitrage bot");
            return new StartArbitrageResult(false, $"Failed to start arbitrage bot: {ex.Message}");
        }
    }
} 