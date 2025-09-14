using Xunit;
using CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;
using CryptoArbitrage.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Tests.Features.BotControl.Queries.GetStatistics;

/// <summary>
/// Unit tests for GetStatisticsHandler - demonstrates testing query handlers.
/// </summary>
public class GetStatisticsHandlerTests
{
	private readonly GetStatisticsHandler _handler;
	private readonly Mock<IArbitrageRepository> _repoMock;
	private readonly Mock<ILogger<GetStatisticsHandler>> _loggerMock;

	public GetStatisticsHandlerTests()
	{
		_repoMock = new Mock<IArbitrageRepository>();
		_loggerMock = new Mock<ILogger<GetStatisticsHandler>>();
		
		// Default repository behavior returns an empty statistics object with expected defaults
		_repoMock
			.Setup(r => r.GetStatisticsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((DateTimeOffset start, DateTimeOffset end, CancellationToken ct) => new ArbitrageStatistics
			{
				Id = Guid.NewGuid(),
				TradingPair = "OVERALL",
				CreatedAt = DateTime.UtcNow,
				StartTime = start,
				EndTime = end,
				TotalOpportunitiesCount = 0,
				TotalTradesCount = 0,
				TotalProfitAmount = 0m
			});
		
		_handler = new GetStatisticsHandler(_repoMock.Object, _loggerMock.Object);
	}

	[Fact]
	public async Task Handle_ValidRequest_ReturnsStatistics()
	{
		// Arrange
		var query = new GetStatisticsQuery();
		var cancellationToken = CancellationToken.None;

		// Act
		var result = await _handler.Handle(query, cancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotEqual(Guid.Empty, result.Id);
		Assert.Equal("OVERALL", result.TradingPair);
		Assert.True(result.CreatedAt <= DateTime.UtcNow);
		
		// Verify default values for mock implementation
		Assert.Equal(0, result.TotalOpportunitiesCount);
		Assert.Equal(0, result.TotalTradesCount);
		Assert.Equal(0m, result.TotalProfitAmount);
	}

	[Fact]
	public async Task Handle_MultipleRequests_ReturnsUniqueIds()
	{
		// Arrange
		var query = new GetStatisticsQuery();
		var cancellationToken = CancellationToken.None;

		// Act
		var result1 = await _handler.Handle(query, cancellationToken);
		var result2 = await _handler.Handle(query, cancellationToken);

		// Assert
		Assert.NotEqual(result1.Id, result2.Id);
		Assert.Equal(result1.TradingPair, result2.TradingPair);
	}

	[Fact]
	public async Task Handle_ValidRequest_HasCorrectDateRange()
	{
		// Arrange
		var query = new GetStatisticsQuery();
		var cancellationToken = CancellationToken.None;

		// Act
		var result = await _handler.Handle(query, cancellationToken);

		// Assert
		Assert.True(result.StartTime < result.EndTime);
		Assert.True(result.EndTime <= DateTimeOffset.UtcNow);
		
		// Should be approximately 30 days range
		var daysDifference = (result.EndTime - result.StartTime).TotalDays;
		Assert.True(daysDifference >= 29 && daysDifference <= 31);
	}
} 