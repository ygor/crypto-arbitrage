namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents a trading pair in a cryptocurrency exchange.
/// </summary>
public readonly record struct TradingPair
{
    /// <summary>
    /// Gets the base currency of the trading pair.
    /// </summary>
    public string BaseCurrency { get; }
    
    /// <summary>
    /// Gets the quote currency of the trading pair.
    /// </summary>
    public string QuoteCurrency { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TradingPair"/> struct.
    /// </summary>
    /// <param name="baseCurrency">The base currency of the trading pair.</param>
    /// <param name="quoteCurrency">The quote currency of the trading pair.</param>
    /// <exception cref="ArgumentException">Thrown when either the base or quote currency is null or empty.</exception>
    public TradingPair(string baseCurrency, string quoteCurrency)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency))
        {
            throw new ArgumentException("Base currency cannot be null or empty.", nameof(baseCurrency));
        }

        if (string.IsNullOrWhiteSpace(quoteCurrency))
        {
            throw new ArgumentException("Quote currency cannot be null or empty.", nameof(quoteCurrency));
        }

        BaseCurrency = baseCurrency.ToUpperInvariant();
        QuoteCurrency = quoteCurrency.ToUpperInvariant();
    }

    /// <summary>
    /// Parses a trading pair from a string in the format "BASE/QUOTE".
    /// </summary>
    /// <param name="tradingPairString">The trading pair string to parse.</param>
    /// <returns>A new <see cref="TradingPair"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the input string is in an invalid format.</exception>
    public static TradingPair Parse(string tradingPairString)
    {
        if (string.IsNullOrWhiteSpace(tradingPairString))
        {
            throw new ArgumentException("Trading pair string cannot be null or empty.", nameof(tradingPairString));
        }

        var parts = tradingPairString.Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Trading pair string must be in the format 'BASE/QUOTE'.", nameof(tradingPairString));
        }

        return new TradingPair(parts[0], parts[1]);
    }

    /// <summary>
    /// Tries to parse a trading pair from a string in the format "BASE/QUOTE".
    /// </summary>
    /// <param name="tradingPairString">The trading pair string to parse.</param>
    /// <param name="tradingPair">When this method returns, contains the parsed trading pair if the parsing succeeded, or default(TradingPair) if the parsing failed.</param>
    /// <returns>true if the parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string tradingPairString, out TradingPair tradingPair)
    {
        tradingPair = default;

        if (string.IsNullOrWhiteSpace(tradingPairString))
        {
            return false;
        }

        var parts = tradingPairString.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            tradingPair = new TradingPair(parts[0], parts[1]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns a string representation of the trading pair in the format "BASE/QUOTE".
    /// </summary>
    /// <returns>The string representation of the trading pair.</returns>
    public override string ToString() => $"{BaseCurrency}/{QuoteCurrency}";
    
    // Common trading pairs
    public static TradingPair BTCUSDT => new("BTC", "USDT");
    public static TradingPair ETHUSDT => new("ETH", "USDT");
    public static TradingPair XRPUSDT => new("XRP", "USDT");
    public static TradingPair BTCUSD => new("BTC", "USD");
    public static TradingPair ETHUSD => new("ETH", "USD");
    public static TradingPair BTCEUR => new("BTC", "EUR");
    public static TradingPair ETHEUR => new("ETH", "EUR");
} 