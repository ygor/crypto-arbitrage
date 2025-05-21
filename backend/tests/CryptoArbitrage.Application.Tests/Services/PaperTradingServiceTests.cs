using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Domain.Models.Events;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CryptoArbitrage.Application.Tests.Services;

public class PaperTradingServiceTests
{
    private readonly Mock<ILogger<PaperTradingService>> _loggerMock;
    private readonly PaperTradingService _service;

    public PaperTradingServiceTests()
    {
        _loggerMock = new Mock<ILogger<PaperTradingService>>();
        _service = new PaperTradingService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PaperTradingService(null!));
    }

    [Fact]
    public async Task GetBalanceAsync_NewExchangeAndCurrency_ReturnsZero()
    {
        // Arrange
        var exchangeId = "Binance";
        var currency = "USDT";

        // Act
        var balance = await _service.GetBalanceAsync(exchangeId, currency);

        // Assert
        Assert.Equal(0m, balance);
    }

    [Fact]
    public async Task UpdateBalanceAsync_AddAmount_UpdatesBalanceCorrectly()
    {
        // Arrange
        var exchangeId = "Binance";
        var currency = "USDT";
        var amount = 1000m;

        // Act
        await _service.UpdateBalanceAsync(exchangeId, currency, amount);
        var balance = await _service.GetBalanceAsync(exchangeId, currency);

        // Assert
        Assert.Equal(amount, balance);
    }

    [Fact]
    public async Task UpdateBalanceAsync_SubtractAmount_UpdatesBalanceCorrectly()
    {
        // Arrange
        var exchangeId = "Binance";
        var currency = "USDT";
        var initialAmount = 1000m;
        var subtractAmount = -500m;

        // Act
        await _service.UpdateBalanceAsync(exchangeId, currency, initialAmount);
        await _service.UpdateBalanceAsync(exchangeId, currency, subtractAmount);
        var balance = await _service.GetBalanceAsync(exchangeId, currency);

        // Assert
        Assert.Equal(500m, balance);
    }

    [Fact]
    public async Task SimulateTradeAsync_BuyOrderWithSufficientBalance_ExecutesSuccessfully()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "BTCUSDT";
        var price = 50000m;
        var quantity = 0.1m;
        var totalValue = price * quantity;
        var fees = Math.Round(totalValue * 0.001m, 8); // 0.1% fee

        // Add sufficient USDT balance
        await _service.UpdateBalanceAsync(exchangeId, "USDT", totalValue + fees);

        // Act
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Buy,
            price,
            quantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TradeStatus.Completed, result.Status);
        Assert.Equal(price, result.ExecutedPrice);
        Assert.Equal(quantity, result.ExecutedQuantity);
        Assert.Equal(fees, result.Fees);
        Assert.Equal("USDT", result.FeeCurrency);

        // Verify balances
        var usdtBalance = await _service.GetBalanceAsync(exchangeId, "USDT");
        var btcBalance = await _service.GetBalanceAsync(exchangeId, "BTC");
        Assert.Equal(0m, usdtBalance); // All USDT spent
        Assert.Equal(quantity, btcBalance); // Received BTC
    }

    [Fact]
    public async Task SimulateTradeAsync_SellOrderWithSufficientBalance_ExecutesSuccessfully()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "BTCUSDT";
        var price = 50000m;
        var quantity = 0.1m;
        var totalValue = price * quantity;
        var fees = Math.Round(totalValue * 0.001m, 8); // 0.1% fee

        // Add sufficient BTC balance
        await _service.UpdateBalanceAsync(exchangeId, "BTC", quantity);

        // Act
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Sell,
            price,
            quantity);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TradeStatus.Completed, result.Status);
        Assert.Equal(price, result.ExecutedPrice);
        Assert.Equal(quantity, result.ExecutedQuantity);
        Assert.Equal(fees, result.Fees);
        Assert.Equal("USDT", result.FeeCurrency);

        // Verify balances
        var usdtBalance = await _service.GetBalanceAsync(exchangeId, "USDT");
        var btcBalance = await _service.GetBalanceAsync(exchangeId, "BTC");
        Assert.Equal(totalValue - fees, usdtBalance); // Received USDT minus fees
        Assert.Equal(0m, btcBalance); // All BTC sold
    }

    [Fact]
    public async Task SimulateTradeAsync_BuyOrderWithInsufficientBalance_ReturnsFailedResult()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "BTCUSDT";
        var price = 50000m;
        var quantity = 0.1m;
        var totalValue = price * quantity;
        var fees = Math.Round(totalValue * 0.001m, 8); // 0.1% fee

        // Add insufficient USDT balance
        await _service.UpdateBalanceAsync(exchangeId, "USDT", totalValue + fees - 1);

        // Act
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Buy,
            price,
            quantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TradeStatus.Failed, result.Status);
        Assert.Contains("Insufficient USDT balance", result.ErrorMessage);
        Assert.Equal(0m, result.ExecutedQuantity);
        Assert.Equal(0m, result.Fees);

        // Verify balances unchanged
        var usdtBalance = await _service.GetBalanceAsync(exchangeId, "USDT");
        var btcBalance = await _service.GetBalanceAsync(exchangeId, "BTC");
        Assert.Equal(totalValue + fees - 1, usdtBalance);
        Assert.Equal(0m, btcBalance);
    }

    [Fact]
    public async Task SimulateTradeAsync_SellOrderWithInsufficientBalance_ReturnsFailedResult()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "BTCUSDT";
        var price = 50000m;
        var quantity = 0.1m;

        // Add insufficient BTC balance
        await _service.UpdateBalanceAsync(exchangeId, "BTC", quantity - 0.01m);

        // Act
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Sell,
            price,
            quantity);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TradeStatus.Failed, result.Status);
        Assert.Contains("Insufficient BTC balance", result.ErrorMessage);
        Assert.Equal(0m, result.ExecutedQuantity);
        Assert.Equal(0m, result.Fees);

        // Verify balances unchanged
        var usdtBalance = await _service.GetBalanceAsync(exchangeId, "USDT");
        var btcBalance = await _service.GetBalanceAsync(exchangeId, "BTC");
        Assert.Equal(0m, usdtBalance);
        Assert.Equal(quantity - 0.01m, btcBalance);
    }

    [Fact]
    public async Task SimulateTradeAsync_InvalidTradingPair_ThrowsException()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "INVALID"; // Invalid trading pair format

        // Act & Assert
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Buy,
            100m,
            1m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TradeStatus.Failed, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task SimulateTradeAsync_ZeroPrice_ThrowsException()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "BTCUSDT";

        // Act & Assert
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Buy,
            0m,
            1m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TradeStatus.Failed, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task SimulateTradeAsync_ZeroQuantity_ThrowsException()
    {
        // Arrange
        var exchangeId = "Binance";
        var tradingPair = "BTCUSDT";

        // Act & Assert
        var result = await _service.SimulateTradeAsync(
            exchangeId,
            tradingPair,
            OrderSide.Buy,
            100m,
            0m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TradeStatus.Failed, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }
} 