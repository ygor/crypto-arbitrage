using CryptoArbitrage.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Interfaces;

/// <summary>
/// Service interface for simulating trades without actual execution on exchanges.
/// </summary>
public interface IPaperTradingService
{
    /// <summary>
    /// Gets a value indicating whether paper trading is enabled.
    /// </summary>
    bool IsPaperTradingEnabled { get; }

    /// <summary>
    /// Initializes paper trading with starting balances.
    /// </summary>
    /// <param name="initialBalances">Optional starting balances for paper trading. If null, default balances will be used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(Dictionary<string, Dictionary<string, decimal>>? initialBalances = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates a market buy order without actual execution.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity to buy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The simulated result of the trade operation.</returns>
    Task<TradeResult> SimulateMarketBuyOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Simulates a market sell order without actual execution.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="quantity">The quantity to sell.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The simulated result of the trade operation.</returns>
    Task<TradeResult> SimulateMarketSellOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all simulated balances across all exchanges.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary of exchange ID to a collection of balances.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>> GetAllBalancesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the simulated balance for a specific asset on a specific exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="asset">The asset symbol.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The balance for the specified asset.</returns>
    Task<Balance?> GetBalanceAsync(string exchangeId, string asset, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a history of simulated trades.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of trade results.</returns>
    Task<IReadOnlyCollection<TradeResult>> GetTradeHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all paper trading data, including balances and trade history.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a simulated trade on the specified exchange.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="tradingPair">The trading pair.</param>
    /// <param name="orderSide">The side of the order (buy or sell).</param>
    /// <param name="quantity">The quantity to trade.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The simulated result of the trade operation.</returns>
    Task<TradeResult> ExecuteTradeAsync(string exchangeId, TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default);
} 