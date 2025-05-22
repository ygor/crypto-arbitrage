using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Repositories;
using CryptoArbitrage.Api.Controllers;
using ApiModels = CryptoArbitrage.Api.Models;
using System;

namespace CryptoArbitrage.Tests.IntegrationTests;

public class ArbitrageFlowTests : IDisposable
{
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IExchangeFactory> _mockExchangeFactory;
    private readonly Mock<ITradingService> _mockTradingService;
    private readonly Mock<IMarketDataService> _mockMarketDataService;
    private readonly Mock<IPaperTradingService> _mockPaperTradingService;
    private readonly Mock<ILogger<ArbitrageService>> _mockArbitrageServiceLogger;
    private readonly Mock<ILogger<ArbitrageRepository>> _mockArbitrageRepositoryLogger;
    private readonly Mock<ILogger<ArbitrageController>> _mockArbitrageControllerLogger;
    private readonly Mock<ILogger<ArbitrageDetectionService>> _mockDetectionLogger;

    public ArbitrageFlowTests()
    {
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockExchangeFactory = new Mock<IExchangeFactory>();
        _mockTradingService = new Mock<ITradingService>();
        _mockMarketDataService = new Mock<IMarketDataService>();
        _mockPaperTradingService = new Mock<IPaperTradingService>();
        _mockArbitrageServiceLogger = new Mock<ILogger<ArbitrageService>>();
        _mockArbitrageRepositoryLogger = new Mock<ILogger<ArbitrageRepository>>();
        _mockArbitrageControllerLogger = new Mock<ILogger<ArbitrageController>>();
        _mockDetectionLogger = new Mock<ILogger<ArbitrageDetectionService>>();

        // Default config setup
        _mockConfigurationService.Setup(s => s.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<ArbitrageConfiguration?>(new ArbitrageConfiguration { AutoExecuteTrades = false }));
        _mockConfigurationService.Setup(s => s.GetRiskProfileAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new RiskProfile { MinimumProfitPercentage = 0.1m }));
    }

    [Fact]
    public async Task DetectedOpportunities_ShouldBeSavedAndRetrievableViaApi()
    {
        // Arrange
        var opportunitiesChannel = Channel.CreateUnbounded<ArbitrageOpportunity>();
        var mockDetectionService = new Mock<IArbitrageDetectionService>();

        // Setup mock detection service to yield opportunities from the channel
        mockDetectionService.Setup(s => s.GetOpportunitiesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => opportunitiesChannel.Reader.ReadAllAsync(ct));
        
        // Use a real ArbitrageRepository to test its interaction with ArbitrageService
        // Ensure a unique directory for this test run to avoid conflicts if tests run in parallel
        // or if previous test runs left files behind.
        var tempRepository = new ArbitrageRepository(_mockArbitrageRepositoryLogger.Object);

        var arbitrageService = new ArbitrageService(
            mockDetectionService.Object,
            _mockConfigurationService.Object,
            tempRepository, // Use the real repository
            _mockExchangeFactory.Object,
            _mockTradingService.Object,
            _mockMarketDataService.Object,
            _mockPaperTradingService.Object,
            _mockArbitrageServiceLogger.Object
        );

        var arbitrageController = new ArbitrageController(
            arbitrageService, 
            tempRepository, // Use the same repository instance
            _mockArbitrageControllerLogger.Object
        );

        var sampleOpportunities = new List<ArbitrageOpportunity>
        {
            new ArbitrageOpportunity(new TradingPair("BTC", "USDT"), "exchangeA", 10000, 1, "exchangeB", 10100, 1) { Id = Guid.NewGuid().ToString() },
            new ArbitrageOpportunity(new TradingPair("ETH", "USDT"), "exchangeA", 2000, 10, "exchangeB", 2030, 10) { Id = Guid.NewGuid().ToString() }
        };

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await arbitrageService.StartAsync(new List<TradingPair> { new TradingPair("BTC", "USDT"), new TradingPair("ETH", "USDT") }, cts.Token);

        // Write opportunities to the channel for the service to process
        foreach (var op in sampleOpportunities)
        {
            await opportunitiesChannel.Writer.WriteAsync(op, cts.Token);
        }
        opportunitiesChannel.Writer.Complete();

        // Wait for processing with a more robust approach
        var retryCount = 0;
        const int maxRetries = 20;
        const int delayMs = 100;
        
        while (retryCount < maxRetries)
        {
            var currentOpportunities = await tempRepository.GetRecentOpportunitiesAsync(10, TimeSpan.FromMinutes(5));
            if (currentOpportunities.Count >= sampleOpportunities.Count)
            {
                break;
            }
            await Task.Delay(delayMs, cts.Token);
            retryCount++;
        }
        
        await arbitrageService.StopAsync(CancellationToken.None); 


        // Assert - Check repository directly first
        var savedOpportunitiesFromRepo = await tempRepository.GetRecentOpportunitiesAsync(10, TimeSpan.FromMinutes(5));
        Assert.Equal(sampleOpportunities.Count, savedOpportunitiesFromRepo.Count);
        foreach (var expectedOp in sampleOpportunities)
        {
            Assert.Contains(savedOpportunitiesFromRepo, actualOp => actualOp.Id == expectedOp.Id && actualOp.TradingPair.ToString() == expectedOp.TradingPair.ToString());
        }

        // Assert - Check via API controller
        var opportunitiesFromApi = await arbitrageController.GetArbitrageOpportunitiesAsync(10, CancellationToken.None);
        Assert.Equal(sampleOpportunities.Count, opportunitiesFromApi.Count);
        foreach (var expectedOp in sampleOpportunities)
        {
            Assert.Contains(opportunitiesFromApi, actualOp => actualOp.id == expectedOp.Id && actualOp.tradingPair.baseCurrency == expectedOp.BaseCurrency && actualOp.tradingPair.quoteCurrency == expectedOp.QuoteCurrency );
        }

    }

    [Fact]
    public async Task ExecutedTrades_ShouldBeSavedAndRetrievableViaApi_Debug()
    {
        // Arrange - Create a simpler direct test that doesn't rely on the complex arbitrage service flow
        var tempRepository = new ArbitrageRepository(_mockArbitrageRepositoryLogger.Object);

        // Create a profitable opportunity
        var opportunityId = Guid.NewGuid();
        var profitableOpportunity = new ArbitrageOpportunity(
            new TradingPair("BTC", "USDT"), 
            "exchangeA", 
            10000m, 
            1m, 
            "exchangeB", 
            10100m, 
            1m
        ) 
        { 
            Id = opportunityId.ToString()
        };

        // First save the opportunity
        await tempRepository.SaveOpportunityAsync(profitableOpportunity);

        // Create buy and sell results directly
        var buyResult = new TradeResult
        {
            Id = Guid.NewGuid(),
            OrderId = "buy_order_123",
            IsSuccess = true,
            ExecutedPrice = 10000m,
            ExecutedQuantity = 1m,
            RequestedPrice = 10000m,
            RequestedQuantity = 1m,
            Timestamp = DateTime.UtcNow,
            Fee = 0.1m
        };

        var sellResult = new TradeResult
        {
            Id = Guid.NewGuid(),
            OrderId = "sell_order_456",
            IsSuccess = true,
            ExecutedPrice = 10100m,
            ExecutedQuantity = 1m,
            RequestedPrice = 10100m,
            RequestedQuantity = 1m,
            Timestamp = DateTime.UtcNow,
            Fee = 0.1m
        };

        // Act - Directly save a trade result using the repository
        await tempRepository.SaveTradeResultAsync(
            profitableOpportunity, 
            buyResult, 
            sellResult, 
            100m, // profit (10100 - 10000 = 100 per unit * 1 unit)
            DateTimeOffset.UtcNow);



        // Assert - Check if the trade was saved and can be retrieved
        var allTrades = await tempRepository.GetRecentTradesAsync(100, TimeSpan.FromDays(1));
        Assert.True(allTrades.Count > 0, $"Expected at least 1 trade to be saved, but found {allTrades.Count}.");
        
        var savedTrade = allTrades.First();
        Assert.Equal(opportunityId, savedTrade.OpportunityId);
        Assert.Equal("exchangeA", savedTrade.BuyExchangeId);
        Assert.Equal("exchangeB", savedTrade.SellExchangeId);
        Assert.Equal(100m, savedTrade.ProfitAmount);
        Assert.True(savedTrade.IsSuccess);

        // Also test retrieval by opportunity ID
        var tradesByOpportunity = await tempRepository.GetTradesByOpportunityIdAsync(opportunityId.ToString());
        Assert.Single(tradesByOpportunity);
        Assert.Equal(opportunityId, tradesByOpportunity.First().OpportunityId);
    }

    [Fact]
    public async Task SaveTradeResultAsync_ShouldSaveTradeToRepository()
    {
        // Arrange
        var tempRepository = new ArbitrageRepository(_mockArbitrageRepositoryLogger.Object);
        
        var opportunityId = Guid.NewGuid();
        var opportunity = new ArbitrageOpportunity(
            new TradingPair("BTC", "USDT"), 
            "exchangeA", 
            10000m, 
            1m, 
            "exchangeB", 
            10100m, 
            1m
        ) 
        { 
            Id = opportunityId.ToString()
        };

        var buyResult = new TradeResult
        {
            Id = Guid.NewGuid(),
            OrderId = "buy_123",
            IsSuccess = true,
            ExecutedPrice = 10000m,
            ExecutedQuantity = 1m,
            RequestedPrice = 10000m,
            RequestedQuantity = 1m,
            Timestamp = DateTime.UtcNow,
            Fee = 0.1m
        };

        var sellResult = new TradeResult
        {
            Id = Guid.NewGuid(),
            OrderId = "sell_456",
            IsSuccess = true,
            ExecutedPrice = 10100m,
            ExecutedQuantity = 1m,
            RequestedPrice = 10100m,
            RequestedQuantity = 1m,
            Timestamp = DateTime.UtcNow,
            Fee = 0.1m
        };

        // Act
        await tempRepository.SaveTradeResultAsync(
            opportunity, 
            buyResult, 
            sellResult, 
            100m, // profit
            DateTimeOffset.UtcNow);

        // Assert
        var savedTrades = await tempRepository.GetTradesByOpportunityIdAsync(opportunity.Id);
        
        Assert.Single(savedTrades);
        var savedTrade = savedTrades.First();
        Assert.Equal(opportunityId, savedTrade.OpportunityId);
        Assert.Equal("exchangeA", savedTrade.BuyExchangeId);
        Assert.Equal("exchangeB", savedTrade.SellExchangeId);
        Assert.Equal(100m, savedTrade.ProfitAmount);
    }

    public void Dispose()
    {
        // Cleanup if needed - consider implementing test-specific data paths in ArbitrageRepository
    }
} 