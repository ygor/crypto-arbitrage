namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents a balance for a specific currency on an exchange.
/// </summary>
public readonly record struct Balance
{
    /// <summary>
    /// Gets the exchange ID associated with this balance.
    /// </summary>
    public string ExchangeId { get; }
    
    /// <summary>
    /// Gets the currency code (e.g., BTC, ETH, USDT).
    /// </summary>
    public string Currency { get; }
    
    /// <summary>
    /// Gets the total balance amount.
    /// </summary>
    public decimal Total { get; }
    
    /// <summary>
    /// Gets the available balance amount (not tied up in open orders).
    /// </summary>
    public decimal Available { get; }
    
    /// <summary>
    /// Gets the reserved balance amount (tied up in open orders).
    /// </summary>
    public decimal Reserved { get; }
    
    /// <summary>
    /// Gets the timestamp when this balance was last updated (in UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Balance"/> struct.
    /// </summary>
    /// <param name="exchangeId">The exchange ID associated with this balance.</param>
    /// <param name="currency">The currency code (e.g., BTC, ETH, USDT).</param>
    /// <param name="total">The total balance amount.</param>
    /// <param name="available">The available balance amount.</param>
    /// <param name="reserved">The reserved balance amount. If null, calculated as (total - available).</param>
    /// <param name="timestamp">The timestamp when this balance was last updated (defaults to UTC now).</param>
    /// <exception cref="ArgumentException">Thrown when the parameters are invalid.</exception>
    public Balance(
        string exchangeId,
        string currency,
        decimal total,
        decimal available,
        decimal? reserved = null,
        DateTimeOffset? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));
        }

        if (total < 0)
        {
            throw new ArgumentException("Total balance cannot be negative.", nameof(total));
        }

        if (available < 0)
        {
            throw new ArgumentException("Available balance cannot be negative.", nameof(available));
        }

        if (available > total)
        {
            throw new ArgumentException("Available balance cannot exceed total balance.");
        }

        ExchangeId = exchangeId;
        Currency = currency.ToUpperInvariant();
        Total = total;
        Available = available;
        Reserved = reserved ?? (total - available);
        
        // Double-check that the reserved balance calculation is correct
        if (Math.Abs(Total - (Available + Reserved)) > 0.0000001m)
        {
            throw new ArgumentException("Total balance must equal available + reserved balance.");
        }
        
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Creates a new Balance instance with an updated available amount, adjusting reserved accordingly.
    /// </summary>
    /// <param name="newAvailable">The new available amount.</param>
    /// <returns>A new Balance instance with the updated values.</returns>
    /// <exception cref="ArgumentException">Thrown when the new available amount is invalid.</exception>
    public Balance WithAvailable(decimal newAvailable)
    {
        if (newAvailable < 0)
        {
            throw new ArgumentException("Available balance cannot be negative.", nameof(newAvailable));
        }

        if (newAvailable > Total)
        {
            throw new ArgumentException("Available balance cannot exceed total balance.");
        }

        return new Balance(ExchangeId, Currency, Total, newAvailable, Total - newAvailable, Timestamp);
    }
    
    /// <summary>
    /// Creates a new Balance instance with the given amount reserved (reducing available balance).
    /// </summary>
    /// <param name="amountToReserve">The amount to reserve.</param>
    /// <returns>A new Balance instance with the updated values.</returns>
    /// <exception cref="ArgumentException">Thrown when the amount to reserve is invalid.</exception>
    public Balance Reserve(decimal amountToReserve)
    {
        if (amountToReserve <= 0)
        {
            throw new ArgumentException("Amount to reserve must be greater than zero.", nameof(amountToReserve));
        }

        if (amountToReserve > Available)
        {
            throw new ArgumentException($"Cannot reserve {amountToReserve} {Currency}, only {Available} available.");
        }

        var newAvailable = Available - amountToReserve;
        var newReserved = Reserved + amountToReserve;

        return new Balance(ExchangeId, Currency, Total, newAvailable, newReserved, DateTimeOffset.UtcNow);
    }
    
    /// <summary>
    /// Creates a new Balance instance with the given amount released from the reserved balance.
    /// </summary>
    /// <param name="amountToRelease">The amount to release.</param>
    /// <returns>A new Balance instance with the updated values.</returns>
    /// <exception cref="ArgumentException">Thrown when the amount to release is invalid.</exception>
    public Balance Release(decimal amountToRelease)
    {
        if (amountToRelease <= 0)
        {
            throw new ArgumentException("Amount to release must be greater than zero.", nameof(amountToRelease));
        }

        if (amountToRelease > Reserved)
        {
            throw new ArgumentException($"Cannot release {amountToRelease} {Currency}, only {Reserved} reserved.");
        }

        var newAvailable = Available + amountToRelease;
        var newReserved = Reserved - amountToRelease;

        return new Balance(ExchangeId, Currency, Total, newAvailable, newReserved, DateTimeOffset.UtcNow);
    }
} 