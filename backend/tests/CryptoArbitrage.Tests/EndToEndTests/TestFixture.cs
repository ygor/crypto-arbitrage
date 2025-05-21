using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Exchanges;
using CryptoArbitrage.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Services;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace CryptoArbitrage.Tests.EndToEndTests;

/// <summary>
/// Test fixture that sets up the dependency injection container for end-to-end tests.
/// </summary>
public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    
    // Mocks for external dependencies
    public Mock<IExchangeFactory> MockExchangeFactory { get; }
    public Mock<IConfigurationService> MockConfigurationService { get; }
    public Mock<IArbitrageRepository> MockArbitrageRepository { get; }
    public Mock<IArbitrageDetectionService> MockArbitrageDetectionService { get; }
    public Mock<IPaperTradingService> MockPaperTradingService { get; }
    
    // Dictionary to store mock exchange clients
    public Dictionary<string, Mock<IExchangeClient>> MockExchangeClients { get; }
    
    // Test data
    public List<TradingPair> TestTradingPairs { get; }
    public RiskProfile TestRiskProfile { get; }
    public ArbitrageConfiguration TestArbitrageConfiguration { get; }
    
    // Create a channel to handle trade results in tests
    public Channel<ArbitrageTradeResult> TradeResultsChannel { get; } = Channel.CreateUnbounded<ArbitrageTradeResult>();
    
    public TestFixture()
    {
        // Initialize mocks
        MockExchangeFactory = new Mock<IExchangeFactory>();
        MockConfigurationService = new Mock<IConfigurationService>();
        MockArbitrageRepository = new Mock<IArbitrageRepository>();
        MockArbitrageDetectionService = new Mock<IArbitrageDetectionService>();
        MockPaperTradingService = new Mock<IPaperTradingService>();
        MockExchangeClients = new Dictionary<string, Mock<IExchangeClient>>();
        
        // Set up test data first
        TestTradingPairs = new List<TradingPair>
        {
            TradingPair.BTCUSDT,
            TradingPair.ETHUSDT
        };
        
        TestRiskProfile = SetupTestRiskProfile();
        TestArbitrageConfiguration = SetupTestArbitrageConfiguration();
        
        // Now set up mock exchange clients
        SetupMockExchangeClients();
        
        // Set up configuration service mock
        SetupConfigurationServiceMock();
        
        // Set up exchange factory mock
        SetupExchangeFactoryMock();
        
        // Set up arbitrage repository mock
        SetupArbitrageRepositoryMock();
        
        // Set up arbitrage detection service mock
        SetupArbitrageDetectionServiceMock();
        
        // Set up paper trading service mock
        SetupPaperTradingServiceMock();
        
        // Set up dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }
    
    private void SetupMockExchangeClients()
    {
        var exchangeIds = new[] { "binance", "coinbase", "kraken" };
        
        foreach (var exchangeId in exchangeIds)
        {
            var mockClient = new Mock<IExchangeClient>();
            mockClient.Setup(c => c.ExchangeId).Returns(exchangeId);
            mockClient.Setup(c => c.SupportsStreaming).Returns(true);
            mockClient.Setup(c => c.IsConnected).Returns(true);
            mockClient.Setup(c => c.IsAuthenticated).Returns(true);
            
            // Connect/Disconnect methods
            mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockClient.Setup(c => c.DisconnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Set up mock to return stub fee schedule
            mockClient.Setup(c => c.GetFeeScheduleAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeeSchedule(exchangeId, 0.001m, 0.001m));
            
            // Set up default order book for all trading pairs
            foreach (var tradingPair in TestTradingPairs)
            {
                var orderBook = TestHelpers.CreateOrderBook(
                    exchangeId,
                    tradingPair,
                    askStartPrice: 30000m, // Default price, will be overridden in specific tests
                    bidStartPrice: 29950m,
                    quantity: 1.0m);
                    
                mockClient.Setup(c => c.GetOrderBookSnapshotAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(orderBook);
            }
                
            MockExchangeClients[exchangeId] = mockClient;
        }
    }
    
    private RiskProfile SetupTestRiskProfile()
    {
        var riskProfile = new RiskProfile
        {
            MinimumProfitPercentage = 0.5m,
            MaxTradeAmount = 100m,
            MaxConcurrentTrades = 3,
            CooldownPeriodMs = 0 // No cooldown for testing
        };

        return riskProfile;
    }
    
    private ArbitrageConfiguration SetupTestArbitrageConfiguration()
    {
        return new ArbitrageConfiguration
        {
            IsEnabled = true,
            PollingIntervalMs = 100,
            MaxConcurrentExecutions = 2,
            MaxConcurrentArbitrageOperations = 3,
            MaxTradeAmount = 1000m,
            AutoExecuteTrades = true,
            AutoTradeEnabled = true,
            MinimumProfitPercentage = 0.5m,
            MaxExecutionTimeMs = 3000,
            RiskProfile = TestRiskProfile,
            TradingPairs = new List<TradingPair>(TestTradingPairs)
        };
    }
    
    private void SetupConfigurationServiceMock()
    {
        // Setup GetRiskProfileAsync
        MockConfigurationService.Setup(c => c.GetRiskProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestRiskProfile);
            
        // Setup GetConfigurationAsync
        MockConfigurationService.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestArbitrageConfiguration);
            
        // Setup exchange configurations
        foreach (var exchangeId in MockExchangeClients.Keys)
        {
            var config = new ExchangeConfiguration
            {
                ExchangeId = exchangeId,
                IsEnabled = true,
                ApiKey = "test_api_key",
                ApiSecret = "test_api_secret",
                MaxRequestsPerSecond = 10,
                ApiTimeoutMs = 5000,
                WebSocketReconnectIntervalMs = 5000
            };
            
            MockConfigurationService.Setup(c => c.GetExchangeConfigurationAsync(exchangeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
        }
        
        // Setup GetAllExchangeConfigurationsAsync
        var allConfigs = MockExchangeClients.Keys.Select(id => new ExchangeConfiguration
        {
            ExchangeId = id,
            IsEnabled = true,
            ApiKey = "test_api_key",
            ApiSecret = "test_api_secret"
        }).ToList();
        
        MockConfigurationService.Setup(c => c.GetAllExchangeConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allConfigs);
    }
    
    private void SetupExchangeFactoryMock()
    {
        // Setup CreateClient to return mock clients
        MockExchangeFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns<string>(id => 
            {
                if (MockExchangeClients.TryGetValue(id, out var client))
                {
                    return client.Object;
                }
                
                throw new InvalidOperationException($"No mock client available for exchange {id}");
            });
            
        // Setup GetSupportedExchanges
        MockExchangeFactory.Setup(f => f.GetSupportedExchanges())
            .Returns(MockExchangeClients.Keys.ToList());
    }
    
    private void SetupArbitrageRepositoryMock()
    {
        MockArbitrageRepository.Setup(r => r.SaveOpportunityAsync(
                It.IsAny<ArbitrageOpportunity>(), 
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        MockArbitrageRepository.Setup(r => r.SaveTradeResultAsync(
                It.IsAny<ArbitrageOpportunity>(),
                It.IsAny<TradeResult>(),
                It.IsAny<TradeResult>(),
                It.IsAny<decimal>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        MockArbitrageRepository.Setup(r => r.SaveStatisticsAsync(
                It.IsAny<ArbitrageStatistics>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
    
    private void SetupArbitrageDetectionServiceMock()
    {
        // Setup IsRunning
        MockArbitrageDetectionService.Setup(d => d.IsRunning)
            .Returns(true);

        // Setup RiskProfile
        MockArbitrageDetectionService.Setup(d => d.RiskProfile)
            .Returns(TestRiskProfile);

        // Setup GetOpportunitiesAsync
        MockArbitrageDetectionService
            .Setup(d => d.GetOpportunitiesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => GetEmptyOpportunitySequence(ct));

        // Setup GetTradeResultsAsync
        MockArbitrageDetectionService
            .Setup(d => d.GetTradeResultsAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => GetTradeResultsFromChannel(ct));

        // Setup PublishTradeResultAsync
        MockArbitrageDetectionService
            .Setup(d => d.PublishTradeResultAsync(It.IsAny<ArbitrageTradeResult>(), It.IsAny<CancellationToken>()))
            .Returns((ArbitrageTradeResult result, CancellationToken ct) => 
            {
                Console.WriteLine($"Publishing trade result: {result.IsSuccess}, BuyTradeId: {result.BuyResult?.OrderId}, SellTradeId: {result.SellResult?.OrderId}");
                return TradeResultsChannel.Writer.WriteAsync(result, ct).AsTask();
            });

        // Setup UpdateRiskProfile
        MockArbitrageDetectionService.Setup(d => d.UpdateRiskProfile(It.IsAny<RiskProfile>()));

        // Setup StartAsync
        MockArbitrageDetectionService.Setup(d => d.StartAsync(It.IsAny<IEnumerable<TradingPair>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup StopAsync
        MockArbitrageDetectionService.Setup(d => d.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup AddTradingPairAsync
        MockArbitrageDetectionService.Setup(d => d.AddTradingPairAsync(It.IsAny<TradingPair>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup RemoveTradingPairAsync
        MockArbitrageDetectionService.Setup(d => d.RemoveTradingPairAsync(It.IsAny<TradingPair>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup GetActiveTradingPairs
        MockArbitrageDetectionService.Setup(d => d.GetActiveTradingPairs())
            .Returns(TestTradingPairs);
    }

    // Helper methods to create empty IAsyncEnumerable sequences
    private static async IAsyncEnumerable<ArbitrageOpportunity> GetEmptyOpportunitySequence([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        await Task.CompletedTask;
        yield break;
    }

    // Helper method to create an IAsyncEnumerable from the channel
    private async IAsyncEnumerable<ArbitrageTradeResult> GetTradeResultsFromChannel([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var result in TradeResultsChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }
    }

    private void SetupPaperTradingServiceMock()
    {
        // Setup IsPaperTradingEnabled
        MockPaperTradingService.Setup(p => p.IsPaperTradingEnabled)
            .Returns(false);

        // Setup InitializeAsync
        MockPaperTradingService.Setup(p => p.InitializeAsync(
                It.IsAny<Dictionary<string, Dictionary<string, decimal>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup SimulateMarketBuyOrderAsync
        MockPaperTradingService.Setup(p => p.SimulateMarketBuyOrderAsync(
                It.IsAny<string>(),
                It.IsAny<TradingPair>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken ct) =>
            {
                return new TradeResult
                {
                    IsSuccess = true,
                    OrderId = Guid.NewGuid().ToString(),
                    ClientOrderId = Guid.NewGuid().ToString(),
                    TradingPair = tradingPair.ToString(),
                    TradeType = TradeType.Buy,
                    ExecutedPrice = 50000m, // Mock price
                    RequestedPrice = 50000m,
                    ExecutedQuantity = quantity,
                    RequestedQuantity = quantity,
                    TotalValue = 50000m * quantity,
                    Fee = 50000m * quantity * 0.001m,
                    FeeCurrency = tradingPair.QuoteCurrency,
                    Timestamp = DateTime.UtcNow,
                    ExecutionTimeMs = 50
                };
            });

        // Setup SimulateMarketSellOrderAsync
        MockPaperTradingService.Setup(p => p.SimulateMarketSellOrderAsync(
                It.IsAny<string>(),
                It.IsAny<TradingPair>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken ct) =>
            {
                return new TradeResult
                {
                    IsSuccess = true,
                    OrderId = Guid.NewGuid().ToString(),
                    ClientOrderId = Guid.NewGuid().ToString(),
                    TradingPair = tradingPair.ToString(),
                    TradeType = TradeType.Sell,
                    ExecutedPrice = 50000m, // Mock price
                    RequestedPrice = 50000m,
                    ExecutedQuantity = quantity,
                    RequestedQuantity = quantity,
                    TotalValue = 50000m * quantity,
                    Fee = 50000m * quantity * 0.001m,
                    FeeCurrency = tradingPair.QuoteCurrency,
                    Timestamp = DateTime.UtcNow,
                    ExecutionTimeMs = 50
                };
            });

        // Setup GetBalanceAsync
        MockPaperTradingService.Setup(p => p.GetBalanceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string exchangeId, string asset, CancellationToken ct) =>
            {
                return new Balance(
                    exchangeId,
                    asset,
                    1000m, // Total
                    1000m, // Available
                    0m     // Reserved
                );
            });

        // Setup GetAllBalancesAsync
        MockPaperTradingService.Setup(p => p.GetAllBalancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var result = new Dictionary<string, IReadOnlyCollection<Balance>>();
                
                foreach (var exchangeId in MockExchangeClients.Keys)
                {
                    var balances = new List<Balance>
                    {
                        new Balance(exchangeId, "BTC", 1.0m, 1.0m, 0),
                        new Balance(exchangeId, "ETH", 10.0m, 10.0m, 0),
                        new Balance(exchangeId, "USDT", 10000m, 10000m, 0),
                        new Balance(exchangeId, "USD", 10000m, 10000m, 0)
                    };
                    
                    result[exchangeId] = balances;
                }
                
                return result;
            });

        // Setup GetTradeHistoryAsync
        MockPaperTradingService.Setup(p => p.GetTradeHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TradeResult>());

        // Setup ResetAsync
        MockPaperTradingService.Setup(p => p.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Register mocks
        services.AddSingleton(MockExchangeFactory.Object);
        services.AddSingleton(MockConfigurationService.Object);
        services.AddSingleton(MockArbitrageRepository.Object);
        services.AddSingleton(MockArbitrageDetectionService.Object);
        services.AddSingleton(MockPaperTradingService.Object);
        
        // Register real services
        services.AddSingleton<IMarketDataService, MarketDataService>();
        services.AddSingleton<ITradingService, TradingService>();
        services.AddSingleton<IArbitrageService, ArbitrageService>();
        services.AddSingleton<INotificationService, NotificationService>();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
} 