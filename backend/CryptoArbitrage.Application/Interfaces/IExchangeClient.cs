using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Domain.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CryptoArbitrage.Application.Interfaces;

/// <summary>
/// Interface for a client that communicates with a cryptocurrency exchange.
/// </summary>
public interface IExchangeClient
{
    /// <summary>
    /// Gets the exchange identifier.
    /// </summary>
    string ExchangeId { get; }
    
    /// <summary>
    /// Gets a value indicating whether this exchange supports real-time streaming.
    /// </summary>
    bool SupportsStreaming { get; }
    
    /// <summary>
    /// Gets a value indicating whether the client is connected to the exchange.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets a value indicating whether the client is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Connects to the exchange WebSocket API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnects from the exchange WebSocket API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the fee schedule for the exchange.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exchange fee schedule.</returns>
    Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest order book snapshot for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get the order book for.</param>
    /// <param name="depth">The depth of the order book to get (number of price levels).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The order book.</returns>
    Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribes to real-time order book updates for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to subscribe to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unsubscribes from order book updates for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to unsubscribe from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a stream of real-time order book updates for a specific trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to get updates for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of order book updates.</returns>
    IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Places a market order on the exchange.
    /// </summary>
    /// <param name="tradingPair">The trading pair to place the order for.</param>
    /// <param name="orderSide">The side of the order (buy or sell).</param>
    /// <param name="quantity">The quantity to buy or sell.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The order details.</returns>
    Task<Order> PlaceMarketOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Places a limit order on the exchange.
    /// </summary>
    /// <param name="tradingPair">The trading pair to place the order for.</param>
    /// <param name="orderSide">The side of the order (buy or sell).</param>
    /// <param name="price">The limit price for the order.</param>
    /// <param name="quantity">The quantity to buy or sell.</param>
    /// <param name="orderType">The type of order (limit, stop, etc.).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The trade result.</returns>
    Task<TradeResult> PlaceLimitOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal price, decimal quantity, OrderType orderType = OrderType.Limit, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the account balances.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account balances.</returns>
    Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Authenticates with the exchange API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AuthenticateAsync(CancellationToken cancellationToken = default);
} 