using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.Mocks;
// Use fully qualified names to avoid ambiguous references
using InfrastructurePaperTradingService = CryptoArbitrage.Infrastructure.Services.PaperTradingService;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CryptoArbitrage.Tests.UnitTests;

public class PaperTradingServiceTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<IMarketDataService> _mockMarketDataService;
    private readonly Mock<ILogger<InfrastructurePaperTradingService>> _mockLogger;
    private readonly InfrastructurePaperTradingService _paperTradingService;
    private readonly TradingPair _btcUsdt = new TradingPair("BTC", "USDT");
    private readonly MockArbitrageRepository _mockArbitrageRepository;
    private readonly MockExchangeFactory _mockExchangeFactory;

    public PaperTradingServiceTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockMarketDataService = new Mock<IMarketDataService>();
        _mockLogger = new Mock<ILogger<InfrastructurePaperTradingService>>();
        _mockArbitrageRepository = new MockArbitrageRepository();
        _mockExchangeFactory = new MockExchangeFactory();
        
        // Setup configuration service to return paper trading enabled
        var config = new ArbitrageConfiguration
        {
            PaperTradingEnabled = true,
            TradingPairs = new List<TradingPair> { _btcUsdt }
        };
        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        
        // Set up a default mock response for GetLatestOrderBook to avoid null reference issues
        var defaultOrderBook = new OrderBook(
            "binance",
            _btcUsdt,
            DateTime.UtcNow,
            new List<OrderBookEntry> { new OrderBookEntry(50000m, 1m) }, // Default bid
            new List<OrderBookEntry> { new OrderBookEntry(50100m, 1m) }  // Default ask
        );
        
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(It.IsAny<string>(), It.IsAny<TradingPair>()))
            .Returns(defaultOrderBook);
        
        _paperTradingService = new InfrastructurePaperTradingService(
            _mockConfigService.Object,
            _mockMarketDataService.Object,
            _mockLogger.Object);
    }
    
    [Fact]
    public async Task InitializeAsync_ShouldSetupDefaultBalances()
    {
        // Arrange
        
        // Act
        await _paperTradingService.InitializeAsync();
        
        // Assert
        var balances = await _paperTradingService.GetAllBalancesAsync();
        
        // Check if default balances were setup for major exchanges
        Assert.Contains("coinbase", balances.Keys);
        Assert.Contains("kraken", balances.Keys);
        
        var coinbaseBalances = balances["coinbase"];
        var krakenBalances = balances["kraken"];
        
        // Check if specific currencies exist
        Assert.Contains(coinbaseBalances, b => b.Currency == "USDT");
        Assert.Contains(coinbaseBalances, b => b.Currency == "BTC");
        Assert.Contains(krakenBalances, b => b.Currency == "USDT");
        Assert.Contains(krakenBalances, b => b.Currency == "BTC");
        
        // Verify that default balances have expected values
        var coinbaseUsdt = coinbaseBalances.First(b => b.Currency == "USDT");
        var coinbaseBtc = coinbaseBalances.First(b => b.Currency == "BTC");
        
        Assert.Equal(10000m, coinbaseUsdt.Total);
        Assert.Equal(1m, coinbaseBtc.Total);
    }
    
    [Fact]
    public async Task SimulateMarketBuyOrderAsync_ShouldUpdateBalances()
    {
        // Arrange
        const string exchangeId = "coinbase";
        const decimal quantity = 0.1m; // Buy 0.1 BTC instead of 0.5 BTC
        const decimal btcPrice = 10000m; // BTC price at $10,000 instead of $50,000
        
        // Setup mock order book specifically for this test
        var asks = new List<OrderBookEntry> { new OrderBookEntry(btcPrice, 1m) };
        var bids = new List<OrderBookEntry> { new OrderBookEntry(btcPrice - 100, 1m) };
        var orderBook = new OrderBook(
            exchangeId,
            _btcUsdt,
            DateTime.UtcNow,
            bids,
            asks
        );
        
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(exchangeId, _btcUsdt))
            .Returns(orderBook);
        
        // Initialize with default balances
        await _paperTradingService.InitializeAsync();
        
        // Get initial balances
        var initialBtcBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "BTC");
        var initialUsdtBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "USDT");
        
        Assert.NotNull(initialBtcBalance);
        Assert.NotNull(initialUsdtBalance);
        
        decimal initialBtcAmount = initialBtcBalance.Value.Total;
        decimal initialUsdtAmount = initialUsdtBalance.Value.Total;
        
        // Act
        var tradeResult = await _paperTradingService.SimulateMarketBuyOrderAsync(exchangeId, _btcUsdt, quantity);
        
        // Assert
        Assert.True(tradeResult.IsSuccess, $"Trade failed with error: {tradeResult.ErrorMessage}");
        Assert.Equal(quantity, tradeResult.ExecutedQuantity);
        Assert.Equal(btcPrice, tradeResult.ExecutedPrice);
        
        // Check that balances were updated correctly
        var updatedBtcBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "BTC");
        var updatedUsdtBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "USDT");
        
        Assert.NotNull(updatedBtcBalance);
        Assert.NotNull(updatedUsdtBalance);
        
        decimal updatedBtcAmount = updatedBtcBalance.Value.Total;
        decimal updatedUsdtAmount = updatedUsdtBalance.Value.Total;
        
        // BTC balance should increase by the quantity bought
        Assert.Equal(initialBtcAmount + quantity, updatedBtcAmount);
        
        // USDT balance should decrease by the quantity * price + fees
        decimal expectedUsdtSpent = quantity * btcPrice * (1 + tradeResult.Fee / tradeResult.TotalValue);
        Assert.Equal(initialUsdtAmount - expectedUsdtSpent, updatedUsdtAmount, 4); // Allow some rounding error
    }
    
    [Fact]
    public async Task SimulateMarketSellOrderAsync_ShouldUpdateBalances()
    {
        // Arrange
        const string exchangeId = "coinbase";
        const decimal quantity = 0.1m; // Sell 0.1 BTC instead of 0.5 BTC
        const decimal btcPrice = 10000m; // BTC price at $10,000 instead of $50,000
        
        // Setup mock order book
        var asks = new List<OrderBookEntry> { new OrderBookEntry(btcPrice + 100, 1m) };
        var bids = new List<OrderBookEntry> { new OrderBookEntry(btcPrice, 1m) };
        var orderBook = new OrderBook(
            exchangeId,
            _btcUsdt,
            DateTime.UtcNow,
            bids,
            asks
        );
        
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(exchangeId, _btcUsdt))
            .Returns(orderBook);
        
        // Initialize with default balances
        await _paperTradingService.InitializeAsync();
        
        // Get initial balances
        var initialBtcBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "BTC");
        var initialUsdtBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "USDT");
        
        Assert.NotNull(initialBtcBalance);
        Assert.NotNull(initialUsdtBalance);
        
        decimal initialBtcAmount = initialBtcBalance.Value.Total;
        decimal initialUsdtAmount = initialUsdtBalance.Value.Total;
        
        // Act
        var tradeResult = await _paperTradingService.SimulateMarketSellOrderAsync(exchangeId, _btcUsdt, quantity);
        
        // Assert
        Assert.True(tradeResult.IsSuccess, $"Trade failed with error: {tradeResult.ErrorMessage}");
        Assert.Equal(quantity, tradeResult.ExecutedQuantity);
        Assert.Equal(btcPrice, tradeResult.ExecutedPrice);
        
        // Check that balances were updated correctly
        var updatedBtcBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "BTC");
        var updatedUsdtBalance = await _paperTradingService.GetBalanceAsync(exchangeId, "USDT");
        
        Assert.NotNull(updatedBtcBalance);
        Assert.NotNull(updatedUsdtBalance);
        
        decimal updatedBtcAmount = updatedBtcBalance.Value.Total;
        decimal updatedUsdtAmount = updatedUsdtBalance.Value.Total;
        
        // BTC balance should decrease by the quantity sold
        Assert.Equal(initialBtcAmount - quantity, updatedBtcAmount);
        
        // USDT balance should increase by the quantity * price - fees
        decimal expectedUsdtReceived = quantity * btcPrice * (1 - tradeResult.Fee / tradeResult.TotalValue);
        Assert.Equal(initialUsdtAmount + expectedUsdtReceived, updatedUsdtAmount, 4); // Allow some rounding error
    }
    
    [Fact]
    public async Task GetTradeHistoryAsync_ShouldReturnAllTrades()
    {
        // Arrange
        const string exchangeId = "coinbase";
        const decimal quantity = 0.1m; // Use 0.1 BTC instead of 0.5 BTC
        
        // Setup mock order books for buy and sell
        var askPrice = 9000m; // Lower price to stay within balance limits
        var bidPrice = 8900m; // Lower price to stay within balance limits
        
        // Order book for buy
        var buyOrderBook = new OrderBook(
            exchangeId,
            _btcUsdt,
            DateTime.UtcNow,
            new List<OrderBookEntry> { new OrderBookEntry(bidPrice, 1m) }, 
            new List<OrderBookEntry> { new OrderBookEntry(askPrice, 1m) }
        );
        
        // Order book for sell
        var sellOrderBook = new OrderBook(
            exchangeId,
            _btcUsdt,
            DateTime.UtcNow,
            new List<OrderBookEntry> { new OrderBookEntry(bidPrice, 1m) },
            new List<OrderBookEntry> { new OrderBookEntry(askPrice, 1m) }
        );
        
        // First setup for buy
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(exchangeId, _btcUsdt))
            .Returns(buyOrderBook);
        
        // Initialize with default balances
        await _paperTradingService.InitializeAsync();
        
        // Buy some BTC
        var buyResult = await _paperTradingService.SimulateMarketBuyOrderAsync(exchangeId, _btcUsdt, quantity);
        Assert.True(buyResult.IsSuccess, $"Buy trade failed with error: {buyResult.ErrorMessage}");
        
        // Update setup for sell
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(exchangeId, _btcUsdt))
            .Returns(sellOrderBook);
        
        // Sell some BTC
        var sellResult = await _paperTradingService.SimulateMarketSellOrderAsync(exchangeId, _btcUsdt, quantity / 2);
        Assert.True(sellResult.IsSuccess, $"Sell trade failed with error: {sellResult.ErrorMessage}");
        
        // Act
        var tradeHistory = await _paperTradingService.GetTradeHistoryAsync();
        
        // Assert
        Assert.Equal(2, tradeHistory.Count);
        Assert.Contains(tradeHistory, t => t.TradeType == TradeType.Buy && t.ExecutedQuantity == quantity);
        Assert.Contains(tradeHistory, t => t.TradeType == TradeType.Sell && t.ExecutedQuantity == quantity / 2);
    }
    
    [Fact]
    public async Task ResetAsync_ShouldClearAllData()
    {
        // Arrange
        const string exchangeId = "coinbase";
        const decimal quantity = 0.1m; // Use 0.1 BTC instead of 0.5 BTC
        
        // Setup mock order book
        var orderBook = new OrderBook(
            exchangeId,
            _btcUsdt,
            DateTime.UtcNow,
            new List<OrderBookEntry> { new OrderBookEntry(8000m, 1m) }, // Lower price
            new List<OrderBookEntry> { new OrderBookEntry(8100m, 1m) }  // Lower price
        );
        
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(exchangeId, _btcUsdt))
            .Returns(orderBook);
        
        // Initialize with default balances
        await _paperTradingService.InitializeAsync();
        
        // Execute a trade to populate trade history
        var tradeResult = await _paperTradingService.SimulateMarketBuyOrderAsync(exchangeId, _btcUsdt, quantity);
        Assert.True(tradeResult.IsSuccess, $"Trade failed with error: {tradeResult.ErrorMessage}");
        
        // Verify we have data before reset
        var balancesBeforeReset = await _paperTradingService.GetAllBalancesAsync();
        var historyBeforeReset = await _paperTradingService.GetTradeHistoryAsync();
        
        Assert.NotEmpty(balancesBeforeReset);
        Assert.Single(historyBeforeReset);
        
        // Act
        await _paperTradingService.ResetAsync();
        
        // Assert
        var balancesAfterReset = await _paperTradingService.GetAllBalancesAsync();
        var historyAfterReset = await _paperTradingService.GetTradeHistoryAsync();
        
        Assert.Empty(balancesAfterReset);
        Assert.Empty(historyAfterReset);
    }
    
    [Fact]
    public async Task SimulateMarketBuyOrderAsync_WithInsufficientBalance_ShouldFail()
    {
        // Arrange
        const string exchangeId = "coinbase";
        const decimal quantity = 100m; // Very large quantity that exceeds default balance
        
        // Setup mock order book with high price to ensure balance is insufficient
        var orderBook = new OrderBook(
            exchangeId,
            _btcUsdt,
            DateTime.UtcNow,
            new List<OrderBookEntry> { new OrderBookEntry(50000m, 1m) },
            new List<OrderBookEntry> { new OrderBookEntry(50100m, 1m) }
        );
        
        _mockMarketDataService.Setup(x => x.GetLatestOrderBook(exchangeId, _btcUsdt))
            .Returns(orderBook);
        
        // Initialize with default balances
        await _paperTradingService.InitializeAsync();
        
        // Act
        var tradeResult = await _paperTradingService.SimulateMarketBuyOrderAsync(exchangeId, _btcUsdt, quantity);
        
        // Assert
        Assert.False(tradeResult.IsSuccess);
        Assert.Contains("insufficient usdt balance", tradeResult.ErrorMessage?.ToLower() ?? "");
    }
} 