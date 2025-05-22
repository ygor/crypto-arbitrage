namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents a unique identifier for a cryptocurrency exchange.
/// </summary>
public readonly record struct ExchangeId
{
    /// <summary>
    /// Gets the value of the exchange identifier.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeId"/> struct.
    /// </summary>
    /// <param name="value">The exchange identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is null or empty.</exception>
    public ExchangeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Exchange ID cannot be null or empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Returns a string representation of the exchange identifier.
    /// </summary>
    /// <returns>The string value of the exchange identifier.</returns>
    public override string ToString() => Value;
    
    // Define some common exchanges as static properties
    public static ExchangeId Coinbase => new("coinbase");
    public static ExchangeId Kraken => new("kraken");
} 