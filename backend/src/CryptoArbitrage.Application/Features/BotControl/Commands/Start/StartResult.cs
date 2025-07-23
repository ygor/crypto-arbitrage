namespace CryptoArbitrage.Application.Features.BotControl.Commands.Start;

/// <summary>
/// Result of the simple start command.
/// </summary>
public record StartResult(
    bool Success,
    string Message
); 