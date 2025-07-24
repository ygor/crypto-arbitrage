using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CryptoArbitrage.Application.Features.BotControl.Commands.Stop;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

namespace CryptoArbitrage.Application.Tests.Features.BotControl.Commands.Stop;

/// <summary>
/// Unit tests for StopHandler - complements StartHandler testing.
/// </summary>
public class StopHandlerTests
{
    private readonly Mock<ILogger<StopHandler>> _mockLogger;
    private readonly StopHandler _handler;

    public StopHandlerTests()
    {
        _mockLogger = new Mock<ILogger<StopHandler>>();
        _handler = new StopHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var command = new StopCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Bot stopped successfully", result.Message);
        
        // Verify the bot is now stopped using the query
        var isRunningHandler = new IsRunningHandler();
        var isRunning = await isRunningHandler.Handle(new IsRunningQuery(), cancellationToken);
        Assert.False(isRunning);
    }

    [Fact]
    public async Task Handle_ValidRequest_LogsInformation()
    {
        // Arrange
        var command = new StopCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bot stopped successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleStopRequests_AllSucceed()
    {
        // Arrange
        var command = new StopCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        var result1 = await _handler.Handle(command, cancellationToken);
        var result2 = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        
        // Should log twice
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bot stopped successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StopHandler(null!));
    }
} 