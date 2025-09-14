using CryptoArbitrage.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Interfaces;

/// <summary>
/// Interface for aggregating market data from multiple exchanges.
/// Core business capability: Collect real-time price data for arbitrage detection.
/// </summary>
public interface IMarketDataAggregator
{
    /// <summary>
    /// Gets the latest price quotes for a trading pair from all monitored exchanges.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get prices for</param>
    /// <returns>Collection of price quotes from different exchanges</returns>
    Task<IEnumerable<PriceQuote>> GetLatestPricesAsync(string tradingPair);
    
    /// <summary>
    /// Starts monitoring price data from specified exchanges for given trading pairs.
    /// </summary>
    /// <param name="exchanges">List of exchange IDs to monitor</param>
    /// <param name="tradingPairs">List of trading pairs to monitor</param>
    /// <returns>Task representing the async operation</returns>
    Task StartMonitoringAsync(IEnumerable<string> exchanges, IEnumerable<string> tradingPairs);
    
    /// <summary>
    /// Stops monitoring price data from all exchanges.
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task StopMonitoringAsync();

    /// <summary>
    /// Gets a snapshot order book for the specified exchange and trading pair.
    /// </summary>
    Task<OrderBook> GetOrderBookAsync(string exchangeId, TradingPair tradingPair, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of exchanges that currently provide market data.
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableExchangesAsync(CancellationToken cancellationToken = default);
} 