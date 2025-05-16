using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Infrastructure.Exchanges;

/// <summary>
/// Utility methods for exchange operations.
/// </summary>
public static class ExchangeUtils
{
    /// <summary>
    /// Normalizes a trading pair symbol for a specific exchange.
    /// </summary>
    /// <param name="tradingPair">The trading pair to normalize.</param>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>The normalized trading pair symbol for the exchange.</returns>
    public static string NormalizeSymbol(TradingPair tradingPair, string exchangeId)
    {
        return exchangeId.ToLowerInvariant() switch
        {
            "coinbase" => $"{tradingPair.BaseCurrency}-{tradingPair.QuoteCurrency}", // Coinbase uses BTC-USD format
            "kraken" => $"{tradingPair.BaseCurrency}{tradingPair.QuoteCurrency}",    // Kraken uses BTCUSD format
            "binance" => $"{tradingPair.BaseCurrency}{tradingPair.QuoteCurrency}",   // Binance uses BTCUSDT format
            _ => $"{tradingPair.BaseCurrency}/{tradingPair.QuoteCurrency}"           // Default format
        };
    }
    
    /// <summary>
    /// Gets the normalized trading pair for a specific exchange.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>A tuple containing the base currency, quote currency, and symbol.</returns>
    public static (string baseCurrency, string quoteCurrency, string symbol) GetNativeTradingPair(TradingPair tradingPair, string exchangeId)
    {
        var baseCurrency = tradingPair.BaseCurrency;
        var quoteCurrency = tradingPair.QuoteCurrency;
        
        string symbol = exchangeId.ToLowerInvariant() switch
        {
            "binance" => $"{baseCurrency}{quoteCurrency}",
            "coinbase" => $"{baseCurrency}-{quoteCurrency}",
            "kraken" => GetKrakenPairSymbol(baseCurrency, quoteCurrency),
            "kucoin" => $"{baseCurrency}-{quoteCurrency}",
            "okx" => $"{baseCurrency}-{quoteCurrency}",
            _ => $"{baseCurrency}{quoteCurrency}"
        };
        
        return (baseCurrency, quoteCurrency, symbol);
    }
    
    /// <summary>
    /// Formats the order side for a specific exchange.
    /// </summary>
    /// <param name="side">The order side.</param>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>The formatted order side string.</returns>
    public static string FormatOrderSide(OrderSide side, string exchangeId)
    {
        return exchangeId.ToLowerInvariant() switch
        {
            "binance" => side == OrderSide.Buy ? "BUY" : "SELL",
            "coinbase" => side == OrderSide.Buy ? "buy" : "sell",
            "kraken" => side == OrderSide.Buy ? "buy" : "sell",
            "kucoin" => side == OrderSide.Buy ? "buy" : "sell",
            "okx" => side == OrderSide.Buy ? "buy" : "sell",
            _ => side.ToString().ToLowerInvariant()
        };
    }
    
    /// <summary>
    /// Formats the order type for a specific exchange.
    /// </summary>
    /// <param name="orderType">The order type.</param>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>The formatted order type string.</returns>
    public static string FormatOrderType(OrderType orderType, string exchangeId)
    {
        return exchangeId.ToLowerInvariant() switch
        {
            "binance" => orderType switch
            {
                OrderType.Market => "MARKET",
                OrderType.Limit => "LIMIT",
                OrderType.FillOrKill => "FOK",
                OrderType.ImmediateOrCancel => "IOC",
                _ => orderType.ToString().ToUpperInvariant()
            },
            "coinbase" => orderType switch
            {
                OrderType.Market => "market",
                OrderType.Limit => "limit",
                OrderType.FillOrKill => "limit",
                OrderType.ImmediateOrCancel => "limit",
                _ => orderType.ToString().ToLowerInvariant()
            },
            "kraken" => orderType switch
            {
                OrderType.Market => "market",
                OrderType.Limit => "limit",
                OrderType.FillOrKill => "limit",
                OrderType.ImmediateOrCancel => "limit",
                _ => orderType.ToString().ToLowerInvariant()
            },
            _ => orderType.ToString().ToLowerInvariant()
        };
    }
    
    /// <summary>
    /// Generates a client order ID for a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>The client order ID.</returns>
    public static string GenerateClientOrderId(string exchangeId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(10000, 99999);
        var prefix = exchangeId.ToUpperInvariant().Substring(0, Math.Min(3, exchangeId.Length));
        
        return $"{prefix}{timestamp}{random}";
    }
    
    /// <summary>
    /// Gets the Kraken pair symbol.
    /// </summary>
    /// <param name="baseCurrency">The base currency.</param>
    /// <param name="quoteCurrency">The quote currency.</param>
    /// <returns>The Kraken pair symbol.</returns>
    private static string GetKrakenPairSymbol(string baseCurrency, string quoteCurrency)
    {
        // Kraken uses special pair naming for some assets
        var baseAsset = baseCurrency.ToUpperInvariant() switch
        {
            "BTC" => "XBT",
            _ => baseCurrency
        };
        
        var quoteAsset = quoteCurrency.ToUpperInvariant() switch
        {
            "BTC" => "XBT",
            _ => quoteCurrency
        };
        
        return $"{baseAsset}{quoteAsset}";
    }
} 