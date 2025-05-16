using System;

namespace CryptoArbitrage.Domain.Exceptions;

/// <summary>
/// Exception thrown when an exchange client encounters an error.
/// </summary>
public class ExchangeClientException : Exception
{
    /// <summary>
    /// Gets the exchange identifier.
    /// </summary>
    public string ExchangeId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeClientException"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="message">The error message.</param>
    public ExchangeClientException(string exchangeId, string message)
        : base(message)
    {
        ExchangeId = exchangeId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeClientException"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExchangeClientException(string exchangeId, string message, Exception innerException)
        : base(message, innerException)
    {
        ExchangeId = exchangeId;
    }
} 