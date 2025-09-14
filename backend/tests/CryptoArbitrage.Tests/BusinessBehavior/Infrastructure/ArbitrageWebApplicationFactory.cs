using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CryptoArbitrage.Api;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;
using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using System.Threading.Tasks;
using DomainArbitrageOpportunity = CryptoArbitrage.Domain.Models.ArbitrageOpportunity;

namespace CryptoArbitrage.Tests.BusinessBehavior.Infrastructure;

/// <summary>
/// Web application factory for arbitrage API integration testing.
/// Provides controlled environment for testing API endpoints with realistic market data.
/// </summary>
public class ArbitrageWebApplicationFactory : WebApplicationFactory<Program>
{
    private ArbitrageTestFixture.TestMarketDataProvider? _marketDataProvider;
    private ArbitrageTestFixture.TestExchangeFactory? _exchangeFactory;
    private TestConfigurationService? _configurationService;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ARBITRAGE_TEST_MODE", "1");
        builder.ConfigureServices(services =>
        {
            // Remove existing services that we want to replace
            RemoveService<IMarketDataAggregator>(services);
            RemoveService<IExchangeFactory>(services);
            RemoveService<IConfigurationService>(services);
            RemoveService<IPaperTradingService>(services);
            RemoveService<INotificationService>(services);

            // Register test infrastructure
            _marketDataProvider = new ArbitrageTestFixture.TestMarketDataProvider();
            _exchangeFactory = new ArbitrageTestFixture.TestExchangeFactory(_marketDataProvider);
            _configurationService = new TestConfigurationService();

            services.AddSingleton<IMarketDataAggregator>(_marketDataProvider);
            services.AddSingleton<IExchangeFactory>(_exchangeFactory);
            services.AddSingleton<IConfigurationService>(_configurationService);

            // Paper trading mock
            var paperMock = new Mock<IPaperTradingService>();
            paperMock.SetupGet(x => x.IsPaperTradingEnabled).Returns(true);
            paperMock.Setup(x => x.ExecuteTradeAsync(It.IsAny<string>(), It.IsAny<TradingPair>(), It.IsAny<OrderSide>(), It.IsAny<decimal>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync((string exchangeId, TradingPair tradingPair, OrderSide side, decimal qty, System.Threading.CancellationToken ct) =>
                {
                    var price = 0m;
                    var ob = _marketDataProvider!.GetOrderBook(exchangeId, tradingPair);
                    price = side == OrderSide.Buy ? ob?.Asks.FirstOrDefault().Price ?? 0m : ob?.Bids.FirstOrDefault().Price ?? 0m;
                    return new TradeResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.NewGuid().ToString(),
                        ExchangeId = exchangeId,
                        TradingPair = tradingPair.ToString(),
                        TradeType = side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                        Side = side,
                        ExecutedPrice = price,
                        ExecutedQuantity = qty,
                        TotalValue = price * qty,
                        Fee = 0,
                        FeeCurrency = tradingPair.QuoteCurrency,
                        Timestamp = DateTime.UtcNow,
                        Status = TradeStatus.Completed
                    };
                });
            services.AddSingleton<IPaperTradingService>(paperMock.Object);

            // Notification service mock
            var notifMock = new Mock<INotificationService>();
            notifMock.Setup(x => x.NotifyOpportunityDetectedAsync(It.IsAny<DomainArbitrageOpportunity>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            notifMock.Setup(x => x.NotifyArbitrageCompletedAsync(It.IsAny<DomainArbitrageOpportunity>(), It.IsAny<TradeResult>(), It.IsAny<TradeResult>(), It.IsAny<decimal>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            notifMock.Setup(x => x.NotifyArbitrageFailedAsync(It.IsAny<DomainArbitrageOpportunity>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            notifMock.Setup(x => x.NotifySystemErrorAsync(It.IsAny<Exception>(), It.IsAny<ErrorSeverity>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            notifMock.Setup(x => x.NotifySystemErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ErrorSeverity>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            notifMock.Setup(x => x.NotifyDailyStatisticsAsync(It.IsAny<ArbitrageStatistics>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            notifMock.Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton<INotificationService>(notifMock.Object);

            // Configure logging for testing
            services.AddLogging(builder => 
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Sets up market conditions for a specific exchange and trading pair.
    /// </summary>
    public void SetupMarketCondition(string exchangeId, TradingPair tradingPair, decimal askPrice, decimal bidPrice, decimal volume = 1.0m)
    {
        if (_marketDataProvider == null)
            throw new InvalidOperationException("Market data provider not initialized");

        var asks = new List<OrderBookEntry> { new OrderBookEntry(askPrice, volume) };
        var bids = new List<OrderBookEntry> { new OrderBookEntry(bidPrice, volume) };
        
        _marketDataProvider.SetOrderBook(exchangeId, tradingPair, asks, bids);
    }

    /// <summary>
    /// Sets up detailed order book depth for realistic quantity calculations.
    /// </summary>
    public void SetupOrderBookDepth(string exchangeId, TradingPair tradingPair, 
        OrderBookEntry[] asks, OrderBookEntry[] bids)
    {
        if (_marketDataProvider == null)
            throw new InvalidOperationException("Market data provider not initialized");

        _marketDataProvider.SetOrderBook(exchangeId, tradingPair, asks, bids);
    }

    /// <summary>
    /// Enables paper trading mode for safe test execution.
    /// </summary>
    public void EnablePaperTrading()
    {
        if (_configurationService == null)
            throw new InvalidOperationException("Configuration service not initialized");

        _configurationService.SetLiveTradingEnabled(false);
    }

    /// <summary>
    /// Simulates exchange connectivity failure.
    /// </summary>
    public void SimulateExchangeFailure(string exchangeId)
    {
        if (_exchangeFactory == null)
            throw new InvalidOperationException("Exchange factory not initialized");

        _exchangeFactory.SimulateFailure(exchangeId);
    }

    /// <summary>
    /// Updates market prices dynamically during tests.
    /// </summary>
    public void UpdateMarketPrice(string exchangeId, TradingPair tradingPair, decimal askPrice, decimal bidPrice)
    {
        SetupMarketCondition(exchangeId, tradingPair, askPrice, bidPrice);
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
} 