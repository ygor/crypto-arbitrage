namespace CryptoArbitrage.Application.Features.BotControl.Commands.StopArbitrage;

/// <summary>
/// Result of stopping the arbitrage bot.
/// </summary>
public record StopArbitrageResult(
    bool Success,
    string Message,
    object? Data = null
); 