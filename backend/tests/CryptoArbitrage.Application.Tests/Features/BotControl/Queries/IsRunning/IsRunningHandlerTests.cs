using Xunit;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;

namespace CryptoArbitrage.Application.Tests.Features.BotControl.Queries.IsRunning;

/// <summary>
/// Unit tests for IsRunningHandler - demonstrates simple query handler testing.
/// </summary>
public class IsRunningHandlerTests
{
    private readonly IsRunningHandler _handler;

    public IsRunningHandlerTests()
    {
        _handler = new IsRunningHandler();
    }

    [Fact]
    public async Task Handle_WhenBotIsRunning_ReturnsTrue()
    {
        // Arrange
        var query = new IsRunningQuery();
        var cancellationToken = CancellationToken.None;
        
        // Set bot to running state
        IsRunningHandler.SetRunning(true);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Handle_WhenBotIsStopped_ReturnsFalse()
    {
        // Arrange
        var query = new IsRunningQuery();
        var cancellationToken = CancellationToken.None;
        
        // Set bot to stopped state
        IsRunningHandler.SetRunning(false);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_StateChanges_ReflectsNewState()
    {
        // Arrange
        var query = new IsRunningQuery();
        var cancellationToken = CancellationToken.None;

        // Act & Assert - Test state transitions
        IsRunningHandler.SetRunning(false);
        var stoppedResult = await _handler.Handle(query, cancellationToken);
        Assert.False(stoppedResult);

        IsRunningHandler.SetRunning(true);
        var runningResult = await _handler.Handle(query, cancellationToken);
        Assert.True(runningResult);

        IsRunningHandler.SetRunning(false);
        var stoppedAgainResult = await _handler.Handle(query, cancellationToken);
        Assert.False(stoppedAgainResult);
    }

    [Fact]
    public async Task Handle_MultipleQueries_ReturnConsistentState()
    {
        // Arrange
        var query = new IsRunningQuery();
        var cancellationToken = CancellationToken.None;
        
        // Set initial state
        IsRunningHandler.SetRunning(true);

        // Act
        var result1 = await _handler.Handle(query, cancellationToken);
        var result2 = await _handler.Handle(query, cancellationToken);
        var result3 = await _handler.Handle(query, cancellationToken);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetRunning_UpdatesStateCorrectly(bool expectedState)
    {
        // Arrange
        var query = new IsRunningQuery();
        var cancellationToken = CancellationToken.None;

        // Act
        IsRunningHandler.SetRunning(expectedState);
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        Assert.Equal(expectedState, result);
    }
} 