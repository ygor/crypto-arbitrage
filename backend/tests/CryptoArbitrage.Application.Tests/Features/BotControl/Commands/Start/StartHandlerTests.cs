using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CryptoArbitrage.Application.Features.BotControl.Commands.Start;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

namespace CryptoArbitrage.Application.Tests.Features.BotControl.Commands.Start;

/// <summary>
/// Unit tests for StartHandler - demonstrates vertical slice handler testing pattern.
/// </summary>
public class StartHandlerTests
{
    private readonly Mock<ILogger<StartHandler>> _mockLogger;
    private readonly StartHandler _handler;

    public StartHandlerTests()
    {
        _mockLogger = new Mock<ILogger<StartHandler>>();
        _handler = new StartHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var command = new StartCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Bot started successfully", result.Message);
        
        // Verify the bot is now running using the query
        var isRunningHandler = new IsRunningHandler();
        var isRunning = await isRunningHandler.Handle(new IsRunningQuery(), cancellationToken);
        Assert.True(isRunning);
    }

    [Fact]
    public async Task Handle_ValidRequest_LogsInformation()
    {
        // Arrange
        var command = new StartCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bot started successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleRequests_AllSucceed()
    {
        // Arrange
        var command = new StartCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        var result1 = await _handler.Handle(command, cancellationToken);
        var result2 = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        
        // Both should succeed since StartHandler doesn't check current state
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bot started successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StartHandler(null!));
    }
} 