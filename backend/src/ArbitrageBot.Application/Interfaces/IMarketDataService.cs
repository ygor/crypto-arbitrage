using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Interface for a service that provides market data across exchanges.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Subscribes to order book updates for a trading pair on all enabled exchanges.
    /// </summary>
    /// <param name="tradingPair">The trading pair to subscribe to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToOrderBookOnAllExchangesAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribes to order book updates for a trading pair on a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair to subscribe to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToOrderBookAsync(string exchangeId, TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unsubscribes from order book updates for a trading pair on a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair to unsubscribe from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromOrderBookAsync(string exchangeId, TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest order book for a trading pair on a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange identifier.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <returns>The latest order book, or null if not available.</returns>
    OrderBook? GetLatestOrderBook(string exchangeId, TradingPair tradingPair);
    
    /// <summary>
    /// Gets a list of active exchanges for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair.</param>
    /// <returns>A collection of exchange identifiers that support the trading pair.</returns>
    IReadOnlyCollection<string> GetActiveExchanges(TradingPair tradingPair);
    
    /// <summary>
    /// Gets a stream of price quotes for the specified trading pair from a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID to get quotes from.</param>
    /// <param name="tradingPair">The trading pair to get quotes for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of price quotes.</returns>
    IAsyncEnumerable<PriceQuote> GetPriceQuotesAsync(ExchangeId exchangeId, TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a stream of aggregated price quotes for the specified trading pair from all subscribed exchanges.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get quotes for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of price quotes from all exchanges.</returns>
    IAsyncEnumerable<IReadOnlyCollection<PriceQuote>> GetAggregatedPriceQuotesAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest best bid and ask across all exchanges for the specified trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get the best bid and ask for.</param>
    /// <returns>A tuple containing the best bid and best ask across all exchanges, or null if not available.</returns>
    (PriceQuote? BestBid, PriceQuote? BestAsk)? GetBestBidAskAcrossExchanges(TradingPair tradingPair);
    
    /// <summary>
    /// Gets all active trading pairs that are currently being tracked.
    /// </summary>
    /// <returns>A collection of active trading pairs.</returns>
    IReadOnlyCollection<TradingPair> GetActiveTradingPairs();
} 