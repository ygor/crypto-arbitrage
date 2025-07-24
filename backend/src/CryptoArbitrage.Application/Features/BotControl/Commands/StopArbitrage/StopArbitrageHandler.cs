using MediatR;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.StopArbitrage;

/// <summary>
/// Handler for stopping the arbitrage bot.
/// </summary>
public class StopArbitrageHandler : IRequestHandler<StopArbitrageCommand, StopArbitrageResult>
{
    private readonly ILogger<StopArbitrageHandler> _logger;

    public StopArbitrageHandler(ILogger<StopArbitrageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<StopArbitrageResult> Handle(StopArbitrageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Arbitrage bot stopped successfully");
            return Task.FromResult(new StopArbitrageResult(true, "Arbitrage bot stopped successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop arbitrage bot");
            return Task.FromResult(new StopArbitrageResult(false, $"Failed to stop arbitrage bot: {ex.Message}"));
        }
    }
} 