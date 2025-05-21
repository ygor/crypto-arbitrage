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
    public void GetNativeTradingPair_ForCoinbaseWithUSDT_ConvertsToUSD()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "coinbase", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC", baseCurrency);
        Assert.Equal("USD", quoteCurrency); // USDT should be converted to USD
        Assert.Equal("BTC-USD", symbol);
    }
    
    [Fact]
    public void GetNativeTradingPair_ForCoinbaseWithOtherQuote_PreservesQuoteCurrency()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "EUR");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "coinbase", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC", baseCurrency);
        Assert.Equal("EUR", quoteCurrency); // Should remain the same
        Assert.Equal("BTC-EUR", symbol);
    }
    
    [Fact]
    public void GetNativeTradingPair_ForBinance_UsesCorrectFormat()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "binance", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC", baseCurrency);
        Assert.Equal("USDT", quoteCurrency); // Should remain the same for Binance
        Assert.Equal("BTCUSDT", symbol); // No separator for Binance
    }
    
    [Fact]
    public void GetNativeTradingPair_ForCoinbase_UsesHyphenSeparator()
    {
        // Arrange
        var tradingPair = new TradingPair("ETH", "BTC");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "coinbase", _mockLogger.Object);
        
        // Assert
        Assert.Equal("ETH", baseCurrency);
        Assert.Equal("BTC", quoteCurrency);
        Assert.Equal("ETH-BTC", symbol); // Should use hyphen separator
    }
    
    [Fact]
    public void GetNativeTradingPair_ForKraken_HandlesBTCSpecialCase()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USD");
        
        // Act
        var (baseCurrency, quoteCurrency, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "kraken", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC", baseCurrency);
        Assert.Equal("USD", quoteCurrency);
        Assert.Equal("XBTUSD", symbol); // BTC should be converted to XBT for Kraken
    }
    
    [Fact]
    public void GetNativeTradingPair_WithExchangeIdCaseInsensitive_WorksCorrectly()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act - Test with mixed case
        var (_, _, symbol) = ExchangeUtils.GetNativeTradingPair(
            tradingPair, "Coinbase", _mockLogger.Object);
        
        // Assert
        Assert.Equal("BTC-USD", symbol); // Should still convert correctly
    }
    
    [Fact]
    public void FormatOrderSide_ReturnsCorrectFormatForExchanges()
    {
        // Arrange & Act
        var binanceBuy = ExchangeUtils.FormatOrderSide(OrderSide.Buy, "binance");
        var binanceSell = ExchangeUtils.FormatOrderSide(OrderSide.Sell, "binance");
        var coinbaseBuy = ExchangeUtils.FormatOrderSide(OrderSide.Buy, "coinbase");
        var coinbaseSell = ExchangeUtils.FormatOrderSide(OrderSide.Sell, "coinbase");
        
        // Assert
        Assert.Equal("BUY", binanceBuy);
        Assert.Equal("SELL", binanceSell);
        Assert.Equal("buy", coinbaseBuy); // Coinbase uses lowercase
        Assert.Equal("sell", coinbaseSell);
    }
    
    [Fact]
    public void NormalizeSymbol_ReturnsCorrectSymbolFormat()
    {
        // Arrange
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Act
        var binanceSymbol = ExchangeUtils.NormalizeSymbol(tradingPair, "binance");
        var coinbaseSymbol = ExchangeUtils.NormalizeSymbol(tradingPair, "coinbase");
        var krakenSymbol = ExchangeUtils.NormalizeSymbol(tradingPair, "kraken");
        var unknownSymbol = ExchangeUtils.NormalizeSymbol(tradingPair, "unknown");
        
        // Assert
        Assert.Equal("BTCUSDT", binanceSymbol);
        Assert.Equal("BTC-USDT", coinbaseSymbol); // No USD conversion in NormalizeSymbol
        Assert.Equal("BTCUSDT", krakenSymbol);
        Assert.Equal("BTC/USDT", unknownSymbol); // Default format
    }
} 