using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;
using System.Reflection;

namespace CryptoArbitrage.Application.Tests.Features.BotControl.Commands.StartArbitrage;

/// <summary>
/// Unit tests for StartArbitrageHandler - demonstrates testing handlers with business logic.
/// </summary>
public class StartArbitrageHandlerTests
{
    private readonly Mock<ILogger<StartArbitrageHandler>> _mockLogger;
    private readonly StartArbitrageHandler _handler;

    public StartArbitrageHandlerTests()
    {
        _mockLogger = new Mock<ILogger<StartArbitrageHandler>>();
        _handler = new StartArbitrageHandler(_mockLogger.Object);
        
        // Reset static state before each test
        ResetStaticState();
    }

    private static void ResetStaticState()
    {
        // Use reflection to reset the static _isRunning field to false
        var type = typeof(StartArbitrageHandler);
        var field = type.GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, false);
    }

    [Fact]
    public async Task Handle_WhenNotRunning_StartsSuccessfully()
    {
        // Arrange
        var command = new StartArbitrageCommand();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Arbitrage bot started successfully", result.Message);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Arbitrage bot started successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyRunning_ReturnsFailure()
    {
        // Arrange
        var command = new StartArbitrageCommand();
        var cancellationToken = CancellationToken.None;

        // Act - Start twice with same handler instance
        var firstResult = await _handler.Handle(command, cancellationToken);
        var secondResult = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(firstResult.Success, "First start should succeed");
        
        Assert.False(secondResult.Success, "Second start should fail");
        Assert.Equal("Arbitrage bot is already running", secondResult.Message);
    }

    [Fact]
    public async Task Handle_WithTradingPairs_StartsSuccessfully()
    {
        // Arrange
        var tradingPairs = new List<CryptoArbitrage.Domain.Models.TradingPair>
        {
            new("BTC", "USD"),
            new("ETH", "USD")
        };
        var command = new StartArbitrageCommand(tradingPairs);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Arbitrage bot started successfully", result.Message);
    }

    [Fact]
    public async Task Handle_MultipleStartAttempts_OnlyFirstSucceeds()
    {
        // Arrange
        var command = new StartArbitrageCommand();
        var cancellationToken = CancellationToken.None;

        // Act - Multiple attempts with same handler instance
        var result1 = await _handler.Handle(command, cancellationToken);
        var result2 = await _handler.Handle(command, cancellationToken);
        var result3 = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        Assert.False(result3.Success);
        
        Assert.Equal("Arbitrage bot started successfully", result1.Message);
        Assert.Equal("Arbitrage bot is already running", result2.Message);
        Assert.Equal("Arbitrage bot is already running", result3.Message);

        // Should only log success once
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Arbitrage bot started successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyTradingPairsList_StartsSuccessfully()
    {
        // Arrange
        var emptyTradingPairs = new List<CryptoArbitrage.Domain.Models.TradingPair>();
        var command = new StartArbitrageCommand(emptyTradingPairs);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StartArbitrageHandler(null!));
    }
} 