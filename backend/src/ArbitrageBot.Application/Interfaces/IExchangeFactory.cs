using System.Collections.Generic;
using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Interface for a factory that creates exchange clients.
/// </summary>
public interface IExchangeFactory
{
    /// <summary>
    /// Creates an exchange client for the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <returns>An exchange client.</returns>
    IExchangeClient CreateClient(string exchangeId);
    
    /// <summary>
    /// Gets all supported exchange identifiers.
    /// </summary>
    /// <returns>A collection of supported exchange identifiers.</returns>
    IReadOnlyCollection<string> GetSupportedExchanges();
} 