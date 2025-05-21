namespace CryptoArbitrage.Infrastructure.Exchanges;

/// <summary>
/// Represents the order book response from Coinbase API
/// </summary>
internal class CoinbaseOrderBook
{
    /// <summary>
    /// Gets or sets the bids. Each entry is an array of [price, size, num-orders].
    /// </summary>
    public List<List<object>> Bids { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the asks. Each entry is an array of [price, size, num-orders].
    /// </summary>
    public List<List<object>> Asks { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the timestamp of the order book.
    /// </summary>
    public string Time { get; set; } = string.Empty;
} 