namespace CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;

/// <summary>
/// Result of starting the arbitrage bot.
/// </summary>
public record StartArbitrageResult(
    bool Success,
    string Message,
    object? Data = null
); 