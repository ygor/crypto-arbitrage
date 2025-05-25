using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CryptoArbitrage.Api;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Tests.Mocks;
using CryptoArbitrage.Tests.TestInfrastructure;
using Moq;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Tests.TestInfrastructure;

/// <summary>
/// Custom WebApplicationFactory that replaces real services with mocked ones
/// to prevent actual API calls during integration tests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real services
            services.RemoveAll<IMarketDataService>();
            services.RemoveAll<IExchangeFactory>();
            services.RemoveAll<IArbitrageDetectionService>();
            services.RemoveAll<ITradingService>();
            services.RemoveAll<IPaperTradingService>();

            // Add mock services
            var mockMarketDataService = new Mock<IMarketDataService>();
            var mockExchangeFactory = new Mock<IExchangeFactory>();
            var mockArbitrageDetectionService = new Mock<IArbitrageDetectionService>();
            var mockTradingService = new Mock<ITradingService>();
            var mockPaperTradingService = new Mock<IPaperTradingService>();
            var mockConfigurationService = new Mock<IConfigurationService>();

            // Create mock loggers for stub clients
            var mockBinanceLogger = new Mock<ILogger<StubExchangeClient>>();
            var mockCoinbaseLogger = new Mock<ILogger<StubExchangeClient>>();
            var mockKrakenLogger = new Mock<ILogger<StubExchangeClient>>();

            // Setup mock exchange factory to return stub clients
            var stubBinanceClient = new StubExchangeClient("binance", mockConfigurationService.Object, mockBinanceLogger.Object);
            var stubCoinbaseClient = new StubExchangeClient("coinbase", mockConfigurationService.Object, mockCoinbaseLogger.Object);
            var stubKrakenClient = new StubExchangeClient("kraken", mockConfigurationService.Object, mockKrakenLogger.Object);

            mockExchangeFactory.Setup(f => f.CreateExchangeClientAsync("binance"))
                .ReturnsAsync(stubBinanceClient);
            mockExchangeFactory.Setup(f => f.CreateExchangeClientAsync("coinbase"))
                .ReturnsAsync(stubCoinbaseClient);
            mockExchangeFactory.Setup(f => f.CreateExchangeClientAsync("kraken"))
                .ReturnsAsync(stubKrakenClient);

            // Setup market data service to work with stub clients
            mockMarketDataService.Setup(m => m.SubscribeToOrderBookAsync(
                It.IsAny<string>(), It.IsAny<Domain.Models.TradingPair>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockMarketDataService.Setup(m => m.UnsubscribeFromOrderBookAsync(
                It.IsAny<string>(), It.IsAny<Domain.Models.TradingPair>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Setup arbitrage detection service
            mockArbitrageDetectionService.Setup(d => d.StartAsync(
                It.IsAny<IEnumerable<Domain.Models.TradingPair>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockArbitrageDetectionService.Setup(d => d.StopAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Register mock services
            services.AddSingleton(mockMarketDataService.Object);
            services.AddSingleton(mockExchangeFactory.Object);
            services.AddSingleton(mockArbitrageDetectionService.Object);
            services.AddSingleton(mockTradingService.Object);
            services.AddSingleton(mockPaperTradingService.Object);
        });
    }
} 