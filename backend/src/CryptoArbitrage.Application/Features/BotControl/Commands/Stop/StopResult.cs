namespace CryptoArbitrage.Application.Features.BotControl.Commands.Stop;

/// <summary>
/// Result of the simple stop command.
/// </summary>
public record StopResult(
    bool Success,
    string Message
); 