using System.Globalization;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Exchanges;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CryptoArbitrage.Tests.UnitTests;

public class ExchangeUtilsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ExchangeUtilsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void GetNativeTradingPair_ForCoinbase_ConvertsUSDTtoUSD()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "coinbase", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC", baseCurrency);
        Assert.Equal("USD", quoteCurrency);
        Assert.Equal("BTC-USD", symbol);
    }
    
    [Fact]
    public void GetNativeTradingPair_ForCoinbase_DoesNotConvertNonUSDT()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "EUR");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "coinbase", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC", baseCurrency);
        Assert.Equal("EUR", quoteCurrency);
        Assert.Equal("BTC-EUR", symbol);
    }

    [Theory]
    [InlineData("coinbase", "BTC", "USDT", "BTC-USD")]
    [InlineData("coinbase", "ETH", "BTC", "ETH-BTC")]
    [InlineData("binance", "BTC", "USDT", "BTCUSDT")]
    [InlineData("kraken", "BTC", "USD", "XBTUSD")]
    [InlineData("kraken", "BTC", "USDT", "XBTUSDT")]
    [InlineData("kucoin", "BTC", "USDT", "BTC-USDT")]
    [InlineData("unknown", "BTC", "USDT", "BTCUSDT")]
    public void GetNativeTradingPair_ReturnsCorrectSymbol(
        string exchangeId, string baseCurrency, string quoteCurrency, string expectedSymbol)
    {
        // Arrange
        var tradingPair = new TradingPair(baseCurrency, quoteCurrency);
        
        // Act
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, exchangeId, _mockLogger.Object);
        
        // Assert
        Assert.Equal(expectedSymbol, symbol);
    }
    
    [Theory]
    [InlineData("coinbase", OrderSide.Buy, "buy")]
    [InlineData("coinbase", OrderSide.Sell, "sell")]
    [InlineData("binance", OrderSide.Buy, "BUY")]
    [InlineData("binance", OrderSide.Sell, "SELL")]
    [InlineData("kraken", OrderSide.Buy, "buy")]
    [InlineData("kraken", OrderSide.Sell, "sell")]
    [InlineData("unknown", OrderSide.Buy, "buy")]
    [InlineData("unknown", OrderSide.Sell, "sell")]
    public void FormatOrderSide_ReturnsCorrectFormat(
        string exchangeId, OrderSide orderSide, string expectedFormat)
    {
        // Act
        var result = ExchangeUtils.FormatOrderSide(orderSide, exchangeId);
        
        // Assert
        Assert.Equal(expectedFormat, result);
    }
    
    [Theory]
    [InlineData("coinbase", OrderType.Market, "market")]
    [InlineData("coinbase", OrderType.Limit, "limit")]
    [InlineData("coinbase", OrderType.FillOrKill, "limit")]
    [InlineData("binance", OrderType.Market, "MARKET")]
    [InlineData("binance", OrderType.Limit, "LIMIT")]
    [InlineData("binance", OrderType.FillOrKill, "FOK")]
    [InlineData("unknown", OrderType.Market, "market")]
    public void FormatOrderType_ReturnsCorrectFormat(
        string exchangeId, OrderType orderType, string expectedFormat)
    {
        // Act
        var result = ExchangeUtils.FormatOrderType(orderType, exchangeId);
        
        // Assert
        Assert.Equal(expectedFormat, result);
    }
    
    [Fact]
    public void NormalizeSymbol_ForCoinbase_ReturnsHyphenatedFormat()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act
        var symbol = ExchangeUtils.NormalizeSymbol(tradingPair, "coinbase");
        
        // Assert
        Assert.Equal("BTC-USDT", symbol);
    }
    
    [Fact]
    public void NormalizeSymbol_ForBinance_ReturnsConcatenatedFormat()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act
        var symbol = ExchangeUtils.NormalizeSymbol(tradingPair, "binance");
        
        // Assert
        Assert.Equal("BTCUSDT", symbol);
    }
    
    [Fact]
    public void GenerateClientOrderId_IncludesExchangePrefix()
    {
        // Act
        var orderId = ExchangeUtils.GenerateClientOrderId("coinbase");
        
        // Assert
        Assert.StartsWith("COI", orderId);
        Assert.True(orderId.Length > 10); // Should include timestamp and random number
    }
    
    [Fact]
    public void GetKrakenPairSymbol_ConvertsBTCtoXBT()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USD");
        
        // Act
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(tradingPair, "kraken", _mockLogger.Object);
        
        // Assert
        Assert.Equal("XBTUSD", symbol);
    }
} 