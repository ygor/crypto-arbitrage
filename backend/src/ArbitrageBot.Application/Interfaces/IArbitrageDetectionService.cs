using System.Runtime.CompilerServices;
using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Service interface for detecting arbitrage opportunities across exchanges.
/// </summary>
public interface IArbitrageDetectionService
{
    /// <summary>
    /// Gets a value indicating whether the arbitrage detection service is running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Gets the risk profile.
    /// </summary>
    RiskProfile RiskProfile { get; }
    
    /// <summary>
    /// Updates the risk profile.
    /// </summary>
    /// <param name="riskProfile">The new risk profile.</param>
    void UpdateRiskProfile(RiskProfile riskProfile);
    
    /// <summary>
    /// Starts the arbitrage detection service.
    /// </summary>
    /// <param name="tradingPairs">The trading pairs to monitor for arbitrage opportunities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(IEnumerable<TradingPair> tradingPairs, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the arbitrage detection service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a stream of detected arbitrage opportunities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of arbitrage opportunities.</returns>
    IAsyncEnumerable<ArbitrageOpportunity> GetOpportunitiesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a stream of trade results.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of trade results.</returns>
    IAsyncEnumerable<ArbitrageTradeResult> GetTradeResultsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a trading pair to the arbitrage detection service.
    /// </summary>
    /// <param name="tradingPair">The trading pair to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddTradingPairAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a trading pair from the arbitrage detection service.
    /// </summary>
    /// <param name="tradingPair">The trading pair to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveTradingPairAsync(TradingPair tradingPair, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publishes a trade result to the trade results channel.
    /// </summary>
    /// <param name="tradeResult">The trade result to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishTradeResultAsync(ArbitrageTradeResult tradeResult, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the active trading pairs being monitored.
    /// </summary>
    /// <returns>A read-only collection of active trading pairs.</returns>
    IReadOnlyCollection<TradingPair> GetActiveTradingPairs();
} 