namespace CryptoArbitrage.Blazor.Services;

/// <summary>
/// Configuration options for the crypto arbitrage system.
/// </summary>
public class CryptoArbitrageOptions
{
    public string[] DefaultExchanges { get; set; } = Array.Empty<string>();
    public string[] DefaultTradingPairs { get; set; } = Array.Empty<string>();
} 