using CryptoArbitrage.Domain.Models;
using System.Collections.Generic;

namespace CryptoArbitrage.Application.Interfaces;

/// <summary>
/// Service interface for executing trades across different exchanges.
/// </summary>
public interface ITradingService
{
    /// <summary>
    /// Places a market buy order on the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity to buy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the trade operation.</returns>
    Task<TradeResult> PlaceMarketBuyOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Places a market sell order on the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity to sell.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the trade operation.</returns>
    Task<TradeResult> PlaceMarketSellOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Places a limit buy order on the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity to buy.</param>
    /// <param name="price">The limit price.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the trade operation.</returns>
    Task<TradeResult> PlaceLimitBuyOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, decimal price, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Places a limit sell order on the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity to sell.</param>
    /// <param name="price">The limit price.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the trade operation.</returns>
    Task<TradeResult> PlaceLimitSellOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, decimal price, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an arbitrage opportunity by placing coordinated orders across exchanges.
    /// </summary>
    /// <param name="opportunity">The arbitrage opportunity to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing both trade results (buy and sell).</returns>
    Task<(TradeResult BuyResult, TradeResult SellResult)> ExecuteArbitrageAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all balances across all exchanges.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary of exchange ID to a collection of balances.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>> GetAllBalancesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the balance for a specific asset on a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="asset">The asset symbol.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The balance for the specified asset.</returns>
    Task<Balance?> GetBalanceAsync(string exchangeId, string asset, CancellationToken cancellationToken = default);
} 