using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;

namespace CryptoArbitrage.Tests.Mocks;

/// <summary>
/// A mock implementation of IExchangeFactory for testing.
/// </summary>
public class MockExchangeFactory : IExchangeFactory
{
    private readonly Dictionary<string, IExchangeClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="MockExchangeFactory"/> class.
    /// </summary>
    public MockExchangeFactory()
    {
        // Initialize default clients
        _clients["binance"] = new MockExchangeClient("binance");
        _clients["coinbase"] = new MockExchangeClient("coinbase");
        _clients["kraken"] = new MockExchangeClient("kraken");
    }

    /// <inheritdoc />
    public IExchangeClient CreateClient(string exchangeId)
    {
        if (_clients.TryGetValue(exchangeId, out var client))
        {
            return client;
        }

        client = new MockExchangeClient(exchangeId);
        _clients[exchangeId] = client;
        return client;
    }

    /// <inheritdoc />
    public Task<IExchangeClient> CreateExchangeClientAsync(string exchangeId)
    {
        return Task.FromResult(CreateClient(exchangeId));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetSupportedExchanges()
    {
        return new[] { "binance", "coinbase", "kraken", "kucoin", "okx" };
    }
} 