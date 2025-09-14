using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;
using System.Reflection;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Tests.Features.BotControl.Commands.StartArbitrage;

/// <summary>
/// Unit tests for StartArbitrageHandler - demonstrates testing handlers with business logic.
/// </summary>
public class StartArbitrageHandlerTests
{
	private readonly Mock<ILogger<StartArbitrageHandler>> _mockLogger;
	private readonly Mock<IArbitrageDetectionService> _mockDetectionService;
	private readonly Mock<IConfigurationService> _mockConfigService;
	private readonly StartArbitrageHandler _handler;

	public StartArbitrageHandlerTests()
	{
		_mockLogger = new Mock<ILogger<StartArbitrageHandler>>();
		_mockDetectionService = new Mock<IArbitrageDetectionService>();
		_mockConfigService = new Mock<IConfigurationService>();
		
		// Default IsRunning false
		_mockDetectionService.SetupGet(x => x.IsRunning).Returns(false);
		// StartDetectionAsync completes successfully
		_mockDetectionService.Setup(x => x.StartDetectionAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
			.Returns(Task.CompletedTask);
		
		// Provide a minimal viable configuration
		_mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ArbitrageConfiguration
			{
				EnabledExchanges = new List<string> { "coinbase", "kraken" },
				TradingPairs = new List<TradingPair> { new("BTC", "USD"), new("ETH", "USD") },
				RiskProfile = new RiskProfile { MaxTradeAmount = 1m, MinProfitThresholdPercent = 0.01m }
			});
		
		_handler = new StartArbitrageHandler(_mockDetectionService.Object, _mockConfigService.Object, _mockLogger.Object);
		
		// Reset static state before each test (kept for backward compatibility if needed)
		ResetStaticState();
	}

	private static void ResetStaticState()
	{
		// Left intentionally blank - handler no longer uses static state.
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

		// Verify orchestration
		_mockConfigService.Verify(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Once);
		_mockDetectionService.Verify(x => x.StartDetectionAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenAlreadyRunning_ReturnsFailure()
	{
		// Arrange
		_mockDetectionService.SetupGet(x => x.IsRunning).Returns(true);
		var command = new StartArbitrageCommand();
		var cancellationToken = CancellationToken.None;

		// Act
		var result = await _handler.Handle(command, cancellationToken);

		// Assert
		Assert.False(result.Success);
		Assert.Equal("Arbitrage bot is already running", result.Message);
	}

	[Fact]
	public async Task Handle_WithTradingPairs_StartsSuccessfully()
	{
		// Arrange
		var tradingPairs = new List<TradingPair>
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

		// Act
		var result1 = await _handler.Handle(command, cancellationToken);
		_mockDetectionService.SetupGet(x => x.IsRunning).Returns(true);
		var result2 = await _handler.Handle(command, cancellationToken);
		var result3 = await _handler.Handle(command, cancellationToken);

		// Assert
		Assert.True(result1.Success);
		Assert.False(result2.Success);
		Assert.False(result3.Success);
		
		Assert.Equal("Arbitrage bot started successfully", result1.Message);
		Assert.Equal("Arbitrage bot is already running", result2.Message);
		Assert.Equal("Arbitrage bot is already running", result3.Message);
	}

	[Fact]
	public async Task Handle_EmptyTradingPairsList_StartsSuccessfully()
	{
		// Arrange
		var emptyTradingPairs = new List<TradingPair>();
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
		Assert.Throws<ArgumentNullException>(() => new StartArbitrageHandler(_mockDetectionService.Object, _mockConfigService.Object, null!));
	}
} 