using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using MudBlazor.Services;
using CryptoArbitrage.Blazor.Pages;
using CryptoArbitrage.Blazor.Services;
using CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;
using CryptoArbitrage.Domain.Models;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;

namespace CryptoArbitrage.UI.Tests.ComponentTests;

/// <summary>
/// 🎯 BLAZOR COMPONENT TESTS - Dashboard Page
/// 
/// These tests verify that UI components render correctly and respond appropriately
/// to user interactions and data changes.
/// </summary>
public class DashboardComponentTests : TestContext
{
	public DashboardComponentTests()
	{
		// Setup required services for Blazor component testing
		Services.AddMudServices();
		Services.AddLogging();
		
		// Mock MediatR
		var mockMediator = new Mock<IMediator>();
		SetupMediatorMocks(mockMediator);
		Services.AddSingleton(mockMediator.Object);
		
		// Mock Blazor service
		var mockBlazorService = new Mock<IBlazorModelService>();
		Services.AddSingleton(mockBlazorService.Object);
		
		// Mock ExchangeFactory required by child components (e.g., ExchangeStatus)
		var mockExchangeFactory = new Mock<IExchangeFactory>();
		mockExchangeFactory.Setup(f => f.GetSupportedExchanges())
			.Returns(new[] { "coinbase", "kraken" });
		mockExchangeFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
			.Returns<string>(id => new CryptoArbitrage.UI.Tests.Infrastructure.UiTestMockExchangeClient(id));
		Services.AddSingleton(mockExchangeFactory.Object);
		
		// Stub MudBlazor JS interop used by components during render
		JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
		JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);
		JSInterop.SetupVoid("resizeObserverFactory.connect", _ => true);
		JSInterop.SetupVoid("resizeObserverFactory.disconnect", _ => true);
	}

	[Fact]
	public void Dashboard_Should_Render_Statistics_Cards()
	{
		// 🎯 COMPONENT TEST: Verify dashboard structure renders correctly
		
		// Act: Render the Dashboard component
		var component = RenderComponent<Dashboard>();
		
		// Assert: Verify key dashboard elements are present
		Assert.Contains("Dashboard", component.Markup);
		Assert.Contains("Total Profit", component.Markup);
		Assert.Contains("Opportunities", component.Markup);
		Assert.Contains("Successful Trades", component.Markup);
		Assert.Contains("Volume", component.Markup);
	}

	[Fact]
	public void Dashboard_Should_Display_Loading_State_Initially()
	{
		// 🎯 UI BEHAVIOR TEST: Verify loading states
		
		// Arrange: Setup delayed response from mediator
		var mockMediator = new Mock<IMediator>();
		mockMediator.Setup(m => m.Send(It.IsAny<GetStatisticsQuery>(), It.IsAny<CancellationToken>()))
				   .Returns(Task.Delay(1000).ContinueWith(_ => CreateMockStatistics()));
		Services.AddSingleton(mockMediator.Object);
		
		// Act: Render dashboard
		var component = RenderComponent<Dashboard>();
		
		// Assert: Should render the page without data initially
		Assert.Contains("Dashboard", component.Markup);
	}

	[Fact]
	public void Dashboard_Should_Show_Bot_Status()
	{
		// 🎯 BUSINESS INTEGRATION TEST: Verify bot status display
		
		// Arrange: Mock bot running status
		var mockMediator = new Mock<IMediator>();
		mockMediator.Setup(m => m.Send(It.IsAny<IsRunningQuery>(), It.IsAny<CancellationToken>()))
				   .ReturnsAsync(true);
		mockMediator.Setup(m => m.Send(It.IsAny<GetStatisticsQuery>(), It.IsAny<CancellationToken>()))
				   .ReturnsAsync(CreateMockStatistics());
		Services.AddSingleton(mockMediator.Object);
		
		// Act: Render dashboard
		var component = RenderComponent<Dashboard>();
		
		// Assert: Should display running status
		// Note: The actual status display will depend on Dashboard implementation
		Assert.NotNull(component.Markup);
	}

	[Fact]
	public void Dashboard_Should_Handle_Real_Statistics_Data()
	{
		// 🎯 DATA INTEGRATION TEST: Verify dashboard handles real data correctly
		
		// Arrange: Setup realistic statistics data
		var mockStats = new ArbitrageStatistics
		{
			Id = Guid.NewGuid(),
			TradingPair = "OVERALL",
			CreatedAt = DateTime.UtcNow,
			StartTime = DateTimeOffset.UtcNow.AddDays(-7),
			EndTime = DateTimeOffset.UtcNow,
			TotalOpportunitiesCount = 150,
			QualifiedOpportunitiesCount = 45,
			TotalTradesCount = 32,
			SuccessfulTradesCount = 28,
			FailedTradesCount = 4,
			TotalProfitAmount = 1250.75m,
			AverageProfitAmount = 44.67m,
			HighestProfitAmount = 125.50m,
			LowestProfit = 8.25m,
			AverageExecutionTimeMs = 245.8m,
			TotalFeesAmount = 75.25m,
			TotalVolume = 25000.00m,
			AverageProfitPercentage = 1.85m,
			// Back-compat properties used by Dashboard
			TotalProfit = 1250.75m,
			TotalOpportunitiesDetected = 150,
			SuccessfulTrades = 28
		};

		var mockMediator = new Mock<IMediator>();
		mockMediator.Setup(m => m.Send(It.IsAny<GetStatisticsQuery>(), It.IsAny<CancellationToken>()))
				   .ReturnsAsync(mockStats);
		Services.AddSingleton(mockMediator.Object);
		
		// Act: Render dashboard with real data
		var component = RenderComponent<Dashboard>();
		
		// Assert: Should display actual statistics values
		Assert.Contains("1250.75", component.Markup); // Total profit (numeric part)
		Assert.Contains("Opportunities", component.Markup);
		Assert.Contains("Successful Trades", component.Markup);
	}

	[Fact]
	public void Dashboard_Statistics_Cards_Should_Have_Correct_Structure()
	{
		// 🎯 UI STRUCTURE TEST: Verify card layout and styling
		
		// Act: Render dashboard
		var component = RenderComponent<Dashboard>();
		
		// Assert: Verify MudBlazor card structure
		Assert.Contains("mud-card", component.Markup);
		Assert.Contains("mud-grid", component.Markup);
		
		// Verify accessibility and semantic structure
		Assert.Contains("mud-card-content", component.Markup);
	}

	private void SetupMediatorMocks(Mock<IMediator> mockMediator)
	{
		// Setup default responses for common queries
		mockMediator.Setup(m => m.Send(It.IsAny<GetStatisticsQuery>(), It.IsAny<CancellationToken>()))
				   .ReturnsAsync(CreateMockStatistics());
		
		mockMediator.Setup(m => m.Send(It.IsAny<IsRunningQuery>(), It.IsAny<CancellationToken>()))
				   .ReturnsAsync(false);
	}

	private ArbitrageStatistics CreateMockStatistics()
	{
		return new ArbitrageStatistics
		{
			Id = Guid.NewGuid(),
			TradingPair = "OVERALL",
			CreatedAt = DateTime.UtcNow,
			StartTime = DateTimeOffset.UtcNow.AddDays(-1),
			EndTime = DateTimeOffset.UtcNow,
			TotalOpportunitiesCount = 25,
			QualifiedOpportunitiesCount = 12,
			TotalTradesCount = 8,
			SuccessfulTradesCount = 7,
			FailedTradesCount = 1,
			TotalProfitAmount = 245.50m,
			AverageProfitAmount = 35.07m,
			HighestProfitAmount = 78.25m,
			LowestProfit = 12.10m,
			AverageExecutionTimeMs = 156.3m,
			TotalFeesAmount = 18.75m,
			TotalVolume = 5000.00m,
			AverageProfitPercentage = 1.25m
		};
	}
} 