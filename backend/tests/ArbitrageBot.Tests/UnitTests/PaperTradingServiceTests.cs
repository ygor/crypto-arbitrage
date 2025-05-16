using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using ArbitrageBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArbitrageBot.Tests.UnitTests;

public class PaperTradingServiceTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<IMarketDataService> _mockMarketDataService;
    private readonly Mock<ILogger<PaperTradingService>> _mockLogger;
    private readonly PaperTradingService _paperTradingService;
    private readonly TradingPair _btcUsdt = new TradingPair("BTC", "USDT");

    public PaperTradingServiceTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockMarketDataService = new Mock<IMarketDataService>();
        _mockLogger = new Mock<ILogger<PaperTradingService>>();
        
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
        
        _paperTradingService = new PaperTradingService(
            _mockConfigService.Object,
            _mockMarketDataService.Object,
            _mockLogger.Object);
    }
    
    [Fact]
    public async Task InitializeAsync_ShouldSetupDefaultBalances()
    {
        // Arrange & Act
        await _paperTradingService.InitializeAsync();
        
        // Assert
        var balances = await _paperTradingService.GetAllBalancesAsync();
        
        // Verify we have balances for all exchanges
        Assert.Equal(3, balances.Count); // binance, coinbase, kraken
        
        // Verify each exchange has the expected assets
        foreach (var exchange in balances.Keys)
        {
            var exchangeBalances = balances[exchange];
            
            // Check for BTC and USDT
            Assert.Contains(exchangeBalances, b => b.Currency == "BTC");
            Assert.Contains(exchangeBalances, b => b.Currency == "USDT");
            
            // Verify amounts
            var btcBalance = exchangeBalances.First(b => b.Currency == "BTC");
            var usdtBalance = exchangeBalances.First(b => b.Currency == "USDT");
            
            Assert.Equal(1m, btcBalance.Available);
            Assert.Equal(10000m, usdtBalance.Available);
        }
    }
    
    [Fact]
    public async Task SimulateMarketBuyOrderAsync_ShouldUpdateBalances()
    {
        // Arrange
        const string exchangeId = "binance";
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
        
        // BTC balance should increase by the quantity bought
        Assert.Equal(initialBtcBalance!.Value.Available + quantity, updatedBtcBalance!.Value.Available);
        
        // USDT balance should decrease by (price * quantity) + fees
        var fee = 0.001m; // 0.1% fee for Binance
        var cost = btcPrice * quantity;
        var costWithFees = cost * (1 + fee);
        Assert.Equal(initialUsdtBalance!.Value.Available - costWithFees, updatedUsdtBalance!.Value.Available, 6); // Compare with precision of 6 decimal places
    }
    
    [Fact]
    public async Task SimulateMarketSellOrderAsync_ShouldUpdateBalances()
    {
        // Arrange
        const string exchangeId = "binance";
        const decimal quantity = 0.1m; // Sell 0.1 BTC instead of 0.5 BTC
        const decimal btcPrice = 10000m; // BTC price at $10,000 instead of $50,000
        
        // Setup mock order book specifically for this test
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
        
        // BTC balance should decrease by the quantity sold
        Assert.Equal(initialBtcBalance!.Value.Available - quantity, updatedBtcBalance!.Value.Available);
        
        // USDT balance should increase by (price * quantity) - fees
        var fee = 0.001m; // 0.1% fee for Binance
        var proceeds = btcPrice * quantity;
        var netProceeds = proceeds * (1 - fee);
        Assert.Equal(initialUsdtBalance!.Value.Available + netProceeds, updatedUsdtBalance!.Value.Available, 6); // Compare with precision of 6 decimal places
    }
    
    [Fact]
    public async Task GetTradeHistoryAsync_ShouldReturnAllTrades()
    {
        // Arrange
        const string exchangeId = "binance";
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
        const string exchangeId = "binance";
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
} 