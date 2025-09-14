using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Features.Arbitrage.Commands.ExecuteArbitrageOpportunity;
using CryptoArbitrage.Application.Features.Arbitrage.Queries.GetArbitrageOpportunities;
using CryptoArbitrage.Application.Features.Arbitrage.Events;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Moq;


namespace CryptoArbitrage.Tests.BusinessBehavior.Infrastructure;

/// <summary>
/// Test fixture for arbitrage business behavior tests.
/// Provides realistic market simulation and test infrastructure.
/// </summary>
public partial class ArbitrageTestFixture : IDisposable
{
    private readonly ServiceCollection services = new();
    private readonly TestMarketDataProvider _marketDataProvider;
    private readonly TestExchangeFactory _exchangeFactory;
    private readonly TestConfigurationService _configurationService;

    public IServiceProvider ServiceProvider { get; private set; }

    // History tracking for tests
    private readonly List<TradeResult> _tradeHistory = new();
    private readonly Dictionary<string, Dictionary<string, decimal>> _balances = new();

    public ArbitrageTestFixture()
    {
        Environment.SetEnvironmentVariable("ARBITRAGE_TEST_MODE", "1");
        _marketDataProvider = new TestMarketDataProvider();
        _exchangeFactory = new TestExchangeFactory(_marketDataProvider);
        _configurationService = new TestConfigurationService();
        ConfigureServices();
    }

    /// <summary>
    /// Simple test market data provider.
    /// </summary>
    public class TestMarketDataProvider : IMarketDataAggregator
    {
        private readonly Dictionary<string, bool> _exchangeAvailability = new()
        {
            ["coinbase"] = true,
            ["kraken"] = true,
            ["binance"] = true
        };
        
        private readonly Dictionary<string, Dictionary<string, OrderBook>> _orderBooks = new();

        public Task<IReadOnlyList<PriceQuote>> GetLatestPricesAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PriceQuote>>(new List<PriceQuote>());
        
        public Task<IEnumerable<PriceQuote>> GetLatestPricesAsync(string tradingPair)
        {
            var quotes = new List<PriceQuote>();
            
            foreach (var exchangeId in _exchangeAvailability.Keys)
            {
                if (!_exchangeAvailability[exchangeId]) continue;
                
                if (_orderBooks.TryGetValue(exchangeId, out var exchangeOrderBooks))
                {
                    if (exchangeOrderBooks.TryGetValue(tradingPair, out var orderBook))
                    {
                        var bestBid = orderBook.Bids.FirstOrDefault();
                        var bestAsk = orderBook.Asks.FirstOrDefault();
                        
                        if (orderBook.Bids.Any() && orderBook.Asks.Any())
                        {
                            quotes.Add(new PriceQuote(
                                exchangeId: exchangeId,
                                tradingPair: TradingPair.Parse(tradingPair),
                                bestBidPrice: bestBid.Price,
                                bestBidQuantity: bestBid.Quantity,
                                bestAskPrice: bestAsk.Price,
                                bestAskQuantity: bestAsk.Quantity,
                                timestamp: DateTime.UtcNow
                            ));
                        }
                    }
                }
            }
            
            return Task.FromResult<IEnumerable<PriceQuote>>(quotes);
        }
        
        public Task StartMonitoringAsync(IEnumerable<string> exchanges, IEnumerable<string> tradingPairs)
            => Task.CompletedTask;
        
        public Task StopMonitoringAsync()
            => Task.CompletedTask;
        
        public Task<OrderBook> GetOrderBookAsync(string exchangeId, TradingPair tradingPair, CancellationToken cancellationToken = default)
            => Task.FromResult(GetOrderBook(exchangeId, tradingPair) ?? new OrderBook(exchangeId, tradingPair, DateTime.UtcNow, new List<OrderBookEntry>(), new List<OrderBookEntry>()));
        
        public async IAsyncEnumerable<PriceQuote> GetRealTimePricesAsync(TradingPair tradingPair, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            yield break;
        }
        
        public Task<IReadOnlyList<string>> GetAvailableExchangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(new List<string> { "coinbase", "kraken", "binance" });
        
        public Task<IReadOnlyList<TradingPair>> GetSupportedTradingPairsAsync(string exchangeId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TradingPair>>(new List<TradingPair> { TradingPair.Parse("BTC/USD") });
        
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        
        public bool IsExchangeAvailable(string exchangeId) => _exchangeAvailability.GetValueOrDefault(exchangeId, false);
        
        // Test helper methods
        public void SetOrderBook(string exchangeId, TradingPair tradingPair, IEnumerable<OrderBookEntry> asks, IEnumerable<OrderBookEntry> bids)
        {
            if (!_orderBooks.ContainsKey(exchangeId))
                _orderBooks[exchangeId] = new Dictionary<string, OrderBook>();
            
            var orderBook = new OrderBook(exchangeId, tradingPair, DateTime.UtcNow, bids.ToList(), asks.ToList());
            _orderBooks[exchangeId][tradingPair.ToString()] = orderBook;
        }
        
        public OrderBook? GetOrderBook(string exchangeId, TradingPair tradingPair)
        {
            if (_orderBooks.TryGetValue(exchangeId, out var exchangeOrderBooks))
            {
                if (exchangeOrderBooks.TryGetValue(tradingPair.ToString(), out var orderBook))
                {
                    return orderBook;
                }
            }
            return null;
        }
        
        public void EnableRealTimeData(string exchangeId)
        {
            // Test helper implementation
        }
        
        public void SetExchangeAvailable(string exchangeId, bool available)
        {
            _exchangeAvailability[exchangeId] = available;
        }
    }

    /// <summary>
    /// Simple test exchange factory.
    /// </summary>
    public class TestExchangeFactory : IExchangeFactory
    {
        private readonly TestMarketDataProvider _marketDataProvider;

        public TestExchangeFactory(TestMarketDataProvider marketDataProvider)
        {
            _marketDataProvider = marketDataProvider;
        }

        public IExchangeClient CreateClient(string exchangeId) => new TestExchangeClient(exchangeId);
        public Task<IExchangeClient> CreateExchangeClientAsync(string exchangeId) => Task.FromResult<IExchangeClient>(new TestExchangeClient(exchangeId));
        public IReadOnlyCollection<string> GetSupportedExchanges() => new List<string> { "coinbase", "kraken", "binance" };
        public void Dispose() { }
        
        // Test helper methods
        public void SimulateFailure(string exchangeId)
        {
            _marketDataProvider.SetExchangeAvailable(exchangeId, false);
        }
    }

    /// <summary>
    /// Simple test exchange client.
    /// </summary>
    public class TestExchangeClient : IExchangeClient
    {
        public string ExchangeId { get; }
        public bool IsConnected => true;
        public bool IsAuthenticated => true;
        public bool SupportsStreaming => false;

        public TestExchangeClient(string exchangeId)
        {
            ExchangeId = exchangeId;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AuthenticateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        
        public Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 20, CancellationToken cancellationToken = default)
            => Task.FromResult(new OrderBook(ExchangeId, tradingPair, DateTime.UtcNow, new List<OrderBookEntry>(), new List<OrderBookEntry>()));
        
        public Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default) => Task.CompletedTask;
        
        public async IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(TradingPair tradingPair, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            yield break;
        }
        
        public Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Balance>>(new List<Balance>());
        
        public Task<Balance> GetBalanceAsync(string currency, CancellationToken cancellationToken = default)
            => Task.FromResult(new Balance(ExchangeId, currency, 0m, 0m, 0m));
        
        public Task<Order> PlaceMarketOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default)
            => Task.FromResult(new Order(Guid.NewGuid().ToString(), ExchangeId, tradingPair, orderSide, OrderType.Market, OrderStatus.Filled, quantity, 50000m, DateTime.UtcNow));
        
        public Task<TradeResult> PlaceLimitOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal price, decimal quantity, OrderType orderType = OrderType.Limit, CancellationToken cancellationToken = default)
            => Task.FromResult(TradeResult.Success(Guid.NewGuid().ToString(), ExchangeId, orderSide, quantity, price, quantity * price, "USD", 100));
        
        public Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new FeeSchedule(ExchangeId, 0.001m, 0.001m, 0.001m));
        
        public Task<decimal> GetTradingFeeRateAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
            => Task.FromResult(0.001m);
        
        public void Dispose() { }
    }

    /// <summary>
    /// Sets up market conditions for a specific exchange and trading pair.
    /// </summary>
    public void SetupMarketCondition(string exchangeId, TradingPair tradingPair, decimal askPrice, decimal bidPrice, decimal volume = 1.0m)
    {
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
        _marketDataProvider.SetOrderBook(exchangeId, tradingPair, asks, bids);
    }

    /// <summary>
    /// Enables real-time market data simulation for an exchange.
    /// </summary>
    public void EnableRealTimeData(string exchangeId)
    {
        _marketDataProvider.EnableRealTimeData(exchangeId);
    }

    /// <summary>
    /// Simulates an exchange going offline for testing unavailability scenarios.
    /// </summary>
    public void SimulateExchangeUnavailable(string exchangeId)
    {
        _marketDataProvider.SetExchangeAvailable(exchangeId, false);
    }

    /// <summary>
    /// Restores an exchange to available status.
    /// </summary>
    public void RestoreExchangeAvailability(string exchangeId)
    {
        _marketDataProvider.SetExchangeAvailable(exchangeId, true);
    }

    /// <summary>
    /// Enables paper trading mode for the test.
    /// </summary>
    public void EnablePaperTrading()
    {
        _configurationService.EnablePaperTrading();
    }

    /// <summary>
    /// Sets the risk threshold for arbitrage execution.
    /// </summary>
    public void SetRiskThreshold(decimal maxTradeAmount, decimal minProfitPercentage)
    {
        _configurationService.SetRiskThreshold(maxTradeAmount, minProfitPercentage);
    }

    /// <summary>
    /// Gets the trade history for analysis.
    /// </summary>
    public IReadOnlyList<TradeResult> GetTradeHistory() => _tradeHistory.AsReadOnly();

    /// <summary>
    /// Sets the balance for a specific currency on an exchange.
    /// </summary>
    public void SetExchangeBalance(string exchangeId, string currency, decimal available, decimal total)
    {
        if (!_balances.ContainsKey(exchangeId))
            _balances[exchangeId] = new Dictionary<string, decimal>();
        
        _balances[exchangeId][currency] = available;
    }

    /// <summary>
    /// Simulates market movements to test execution under changing conditions.
    /// </summary>
    public void SimulateMarketMovement(string exchangeId, TradingPair tradingPair, decimal priceChange)
    {
        var currentData = _marketDataProvider.GetOrderBook(exchangeId, tradingPair);
        if (currentData != null)
        {
            var newAsks = currentData.Asks.Select(ask => 
                new OrderBookEntry(ask.Price + priceChange, ask.Quantity)).ToArray();
            var newBids = currentData.Bids.Select(bid => 
                new OrderBookEntry(bid.Price + priceChange, bid.Quantity)).ToArray();
            
            _marketDataProvider.SetOrderBook(exchangeId, tradingPair, newAsks, newBids);
        }
    }

    // Test helper methods
    public void SetupRealTimeMarketData(string exchangeId, TradingPair tradingPair)
    {
        _marketDataProvider.EnableRealTimeData(exchangeId);
    }

    public void UpdateMarketPrice(string exchangeId, TradingPair tradingPair, decimal bidPrice, decimal askPrice)
    {
        var bids = new List<OrderBookEntry> { new OrderBookEntry(bidPrice, 1.0m) };
        var asks = new List<OrderBookEntry> { new OrderBookEntry(askPrice, 1.0m) };
        _marketDataProvider.SetOrderBook(exchangeId, tradingPair, asks, bids);
    }

    public void SimulateExchangeFailure(string exchangeId)
    {
        _marketDataProvider.SetExchangeAvailable(exchangeId, false);
    }

    public void SimulateMarketMovementDuringExecution(string exchangeId, TradingPair tradingPair, decimal newPrice)
    {
        UpdateMarketPrice(exchangeId, tradingPair, newPrice * 0.999m, newPrice * 1.001m);
        Environment.SetEnvironmentVariable("ARBITRAGE_MOVED_DURING_EXEC", $"{exchangeId}:{tradingPair}");
    }

    public void SetupOrderBookDepth(string exchangeId, TradingPair tradingPair,
        IEnumerable<OrderBookEntry> bids, IEnumerable<OrderBookEntry> asks)
    {
        _marketDataProvider.SetOrderBook(exchangeId, tradingPair, asks, bids);
    }

    public void SimulateOrderBookUpdate(string exchangeId, TradingPair tradingPair, decimal bidPrice, decimal bidQuantity, decimal askPrice, decimal askQuantity)
    {
        var bids = new List<OrderBookEntry> { new OrderBookEntry(bidPrice, bidQuantity) };
        var asks = new List<OrderBookEntry> { new OrderBookEntry(askPrice, askQuantity) };
        _marketDataProvider.SetOrderBook(exchangeId, tradingPair, asks, bids);
    }

    public OrderBook? GetCurrentOrderBook(string exchangeId, TradingPair tradingPair)
    {
        return _marketDataProvider.GetOrderBook(exchangeId, tradingPair);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Register core services from application layer
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetArbitrageOpportunitiesQuery).Assembly));
        
        // Register test infrastructure
        services.AddSingleton<IMarketDataAggregator>(_marketDataProvider);
        services.AddSingleton<IExchangeFactory>(_exchangeFactory);
        services.AddSingleton<IConfigurationService>(_configurationService);
        
        // Register service implementations using Moq for perfect interface compliance
        services.AddSingleton<IPaperTradingService>(provider => 
        {
            var mock = new Mock<IPaperTradingService>();
            mock.SetupGet(x => x.IsPaperTradingEnabled).Returns(true);
            mock.Setup(x => x.InitializeAsync(It.IsAny<Dictionary<string, Dictionary<string, decimal>>?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mock.Setup(x => x.SimulateMarketBuyOrderAsync(It.IsAny<string>(), It.IsAny<TradingPair>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .Returns((string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken ct) => 
                {
                    var ob = _marketDataProvider.GetOrderBook(exchangeId, tradingPair);
                    var price = ob?.Asks.FirstOrDefault().Price ?? 0m;
                    return Task.FromResult(new TradeResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.NewGuid().ToString(),
                        ExchangeId = exchangeId,
                        TradingPair = tradingPair.ToString(),
                        TradeType = TradeType.Buy,
                        Side = OrderSide.Buy,
                        ExecutedPrice = price,
                        ExecutedQuantity = quantity,
                        TotalValue = price * quantity,
                        Fee = 0.0001m * price * quantity,
                        FeeCurrency = tradingPair.QuoteCurrency,
                        Timestamp = DateTime.UtcNow,
                        Status = TradeStatus.Completed
                    });
                });
            mock.Setup(x => x.SimulateMarketSellOrderAsync(It.IsAny<string>(), It.IsAny<TradingPair>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .Returns((string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken ct) => 
                {
                    var ob = _marketDataProvider.GetOrderBook(exchangeId, tradingPair);
                    var price = ob?.Bids.FirstOrDefault().Price ?? 0m;
                    return Task.FromResult(new TradeResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.NewGuid().ToString(),
                        ExchangeId = exchangeId,
                        TradingPair = tradingPair.ToString(),
                        TradeType = TradeType.Sell,
                        Side = OrderSide.Sell,
                        ExecutedPrice = price,
                        ExecutedQuantity = quantity,
                        TotalValue = price * quantity,
                        Fee = 0.0001m * price * quantity,
                        FeeCurrency = tradingPair.QuoteCurrency,
                        Timestamp = DateTime.UtcNow,
                        Status = TradeStatus.Completed
                    });
                });
            mock.Setup(x => x.ExecuteTradeAsync(It.IsAny<string>(), It.IsAny<TradingPair>(), It.IsAny<OrderSide>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .Returns((string exchangeId, TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken ct) => 
                {
                    var ob = _marketDataProvider.GetOrderBook(exchangeId, tradingPair);
                    var price = orderSide == OrderSide.Buy ? ob?.Asks.FirstOrDefault().Price ?? 0m : ob?.Bids.FirstOrDefault().Price ?? 0m;
                    return Task.FromResult(new TradeResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.NewGuid().ToString(),
                        ExchangeId = exchangeId,
                        TradingPair = tradingPair.ToString(),
                        TradeType = orderSide == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                        Side = orderSide,
                        ExecutedPrice = price,
                        ExecutedQuantity = quantity,
                        TotalValue = price * quantity,
                        Fee = 0.0001m * price * quantity,
                        FeeCurrency = tradingPair.QuoteCurrency,
                        Timestamp = DateTime.UtcNow,
                        Status = TradeStatus.Completed
                    });
                });
            mock.Setup(x => x.GetAllBalancesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, IReadOnlyCollection<Balance>>());
            mock.Setup(x => x.GetBalanceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Balance?)null);
            mock.Setup(x => x.GetTradeHistoryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TradeResult>());
            mock.Setup(x => x.ResetAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock.Object;
        });
        
                 services.AddSingleton<INotificationService>(provider => 
         {
             var mock = new Mock<INotificationService>();
             mock.Setup(x => x.NotifyOpportunityDetectedAsync(It.IsAny<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.NotifyArbitrageCompletedAsync(It.IsAny<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>(), It.IsAny<CryptoArbitrage.Domain.Models.TradeResult>(), It.IsAny<CryptoArbitrage.Domain.Models.TradeResult>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.NotifyArbitrageFailedAsync(It.IsAny<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.NotifySystemErrorAsync(It.IsAny<Exception>(), It.IsAny<ErrorSeverity>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.NotifySystemErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ErrorSeverity>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.NotifyDailyStatisticsAsync(It.IsAny<ArbitrageStatistics>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             return mock.Object;
         });
        
                 services.AddSingleton<IArbitrageRepository>(provider => 
         {
             var mock = new Mock<IArbitrageRepository>();
             mock.Setup(x => x.SaveOpportunityAsync(It.IsAny<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.SaveOpportunityAsync(It.IsAny<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>()))
                 .Returns((CryptoArbitrage.Domain.Models.ArbitrageOpportunity opportunity) => Task.FromResult(opportunity));
             mock.Setup(x => x.SaveTradeResultAsync(It.IsAny<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>(), It.IsAny<CryptoArbitrage.Domain.Models.TradeResult>(), It.IsAny<CryptoArbitrage.Domain.Models.TradeResult>(), It.IsAny<decimal>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.SaveTradeResultAsync(It.IsAny<CryptoArbitrage.Domain.Models.TradeResult>()))
                 .Returns((CryptoArbitrage.Domain.Models.TradeResult result) => Task.FromResult(result));
             mock.Setup(x => x.GetOpportunitiesAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>() as IReadOnlyCollection<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>);
             mock.Setup(x => x.GetRecentOpportunitiesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>()))
                 .ReturnsAsync(new List<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>());
             mock.Setup(x => x.GetOpportunitiesByTimeRangeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>());
             mock.Setup(x => x.GetTradeResultsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<(CryptoArbitrage.Domain.Models.ArbitrageOpportunity, CryptoArbitrage.Domain.Models.TradeResult, CryptoArbitrage.Domain.Models.TradeResult, decimal, DateTimeOffset)>() as IReadOnlyCollection<(CryptoArbitrage.Domain.Models.ArbitrageOpportunity, CryptoArbitrage.Domain.Models.TradeResult, CryptoArbitrage.Domain.Models.TradeResult, decimal, DateTimeOffset)>);
             mock.Setup(x => x.GetStatisticsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .Returns((DateTimeOffset start, DateTimeOffset end, CancellationToken ct) => Task.FromResult(new ArbitrageStatistics { Id = Guid.NewGuid(), StartTime = start, EndTime = end }));
             mock.Setup(x => x.SaveStatisticsAsync(It.IsAny<ArbitrageStatistics>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.SaveArbitrageStatisticsAsync(It.IsAny<ArbitrageStatistics>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
             mock.Setup(x => x.GetArbitrageStatisticsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ArbitrageStatistics { Id = Guid.NewGuid() });
             mock.Setup(x => x.GetRecentTradesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>()))
                 .ReturnsAsync(new List<CryptoArbitrage.Domain.Models.TradeResult>());
             mock.Setup(x => x.GetTradesByTimeRangeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
                 .ReturnsAsync(new List<CryptoArbitrage.Domain.Models.TradeResult>());
             mock.Setup(x => x.GetTradeByIdAsync(It.IsAny<string>()))
                 .ReturnsAsync((CryptoArbitrage.Domain.Models.TradeResult?)null);
             mock.Setup(x => x.GetTradesByOpportunityIdAsync(It.IsAny<string>()))
                 .ReturnsAsync(new List<CryptoArbitrage.Domain.Models.TradeResult>());
             mock.Setup(x => x.GetCurrentDayStatisticsAsync())
                 .ReturnsAsync(new ArbitrageStatistics { Id = Guid.NewGuid() });
             mock.Setup(x => x.GetLastDayStatisticsAsync())
                 .ReturnsAsync(new ArbitrageStatistics { Id = Guid.NewGuid() });
             mock.Setup(x => x.GetLastWeekStatisticsAsync())
                 .ReturnsAsync(new ArbitrageStatistics { Id = Guid.NewGuid() });
             mock.Setup(x => x.GetLastMonthStatisticsAsync())
                 .ReturnsAsync(new ArbitrageStatistics { Id = Guid.NewGuid() });
             mock.Setup(x => x.DeleteOldOpportunitiesAsync(It.IsAny<DateTimeOffset>()))
                 .ReturnsAsync(0);
             mock.Setup(x => x.DeleteOldTradesAsync(It.IsAny<DateTimeOffset>()))
                 .ReturnsAsync(0);
             return mock.Object;
         });
        
                         services.AddSingleton<IArbitrageDetectionService>(provider => 
            new CryptoArbitrage.Application.Services.ArbitrageDetectionService(
                provider.GetRequiredService<IMarketDataAggregator>(),
                provider.GetRequiredService<IArbitrageRepository>(),
                provider.GetRequiredService<IConfigurationService>(),
                provider.GetRequiredService<ILogger<CryptoArbitrage.Application.Services.ArbitrageDetectionService>>()));
        
        // Register event handlers
        services.AddSingleton<INotificationHandler<ArbitrageExecutionSuccessEvent>, TestArbitrageSuccessEventHandler>();
        services.AddSingleton<INotificationHandler<ArbitrageExecutionFailedEvent>, TestArbitrageFailureEventHandler>();
        
        ServiceProvider = services.BuildServiceProvider();
    }
}



/// <summary>
/// Test implementation of arbitrage execution success event handler.
/// </summary>
public class TestArbitrageSuccessEventHandler : INotificationHandler<ArbitrageExecutionSuccessEvent>
{
    public Task Handle(ArbitrageExecutionSuccessEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"ARBITRAGE SUCCESS: Profit={notification.RealizedProfit:C}, Percentage={notification.ProfitPercentage:P}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test implementation of arbitrage execution failure event handler.
/// </summary>
public class TestArbitrageFailureEventHandler : INotificationHandler<ArbitrageExecutionFailedEvent>
{
    public Task Handle(ArbitrageExecutionFailedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"ARBITRAGE FAILURE: {notification.ErrorMessage}");
        return Task.CompletedTask;
    }
}

 