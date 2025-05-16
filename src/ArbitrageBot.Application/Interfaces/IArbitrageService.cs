using ArbitrageBot.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ArbitrageBot.Application.Interfaces;

/// <summary>
/// Service interface for detecting and executing arbitrage opportunities.
/// </summary>
public interface IArbitrageService
{
    /// <summary>
    /// Gets a value indicating whether the arbitrage service is running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Gets the current risk profile.
    /// </summary>
    RiskProfile RiskProfile { get; }
    
    /// <summary>
    /// Starts the arbitrage service with specified trading pairs.
    /// </summary>
    /// <param name="tradingPairs">The trading pairs to monitor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(IEnumerable<TradingPair> tradingPairs, CancellationToken cancellationToken);
    
    /// <summary>
    /// Starts the arbitrage service with the provided risk profile.
    /// </summary>
    /// <param name="riskProfile">The risk profile to use for arbitrage operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(RiskProfile riskProfile, CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops the arbitrage service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Updates the risk profile.
    /// </summary>
    /// <param name="riskProfile">The new risk profile.</param>
    void UpdateRiskProfile(RiskProfile riskProfile);
    
    /// <summary>
    /// Gets a stream of detected arbitrage opportunities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of arbitrage opportunities.</returns>
    IAsyncEnumerable<ArbitrageOpportunity> GetOpportunitiesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a stream of executed trade results.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of trade results.</returns>
    IAsyncEnumerable<ArbitrageTradeResult> GetTradeResultsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the statistics for the arbitrage service.
    /// </summary>
    /// <returns>The current arbitrage statistics.</returns>
    ArbitrageStatistics GetStatistics();
} 