using CryptoArbitrage.Blazor.ViewModels;

namespace CryptoArbitrage.Blazor.Services;

/// <summary>
/// Service interface for providing properly mapped ViewModels to Blazor components.
/// Handles the conversion from domain models to UI-optimized ViewModels.
/// </summary>
public interface IBlazorModelService
{
    /// <summary>
    /// Gets recent arbitrage opportunities as ViewModels optimized for UI binding.
    /// </summary>
    /// <param name="limit">The maximum number of opportunities to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of arbitrage opportunity ViewModels.</returns>
    Task<ICollection<ArbitrageOpportunityViewModel>> GetOpportunitiesAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent trade results as ViewModels optimized for UI binding.
    /// </summary>
    /// <param name="limit">The maximum number of trades to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of trade result ViewModels.</returns>
    Task<ICollection<TradeResultViewModel>> GetTradesAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets arbitrage opportunities for a specific time range.
    /// </summary>
    /// <param name="startTime">The start time for the range.</param>
    /// <param name="endTime">The end time for the range.</param>
    /// <param name="limit">The maximum number of opportunities to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of arbitrage opportunity ViewModels.</returns>
    Task<ICollection<ArbitrageOpportunityViewModel>> GetOpportunitiesByTimeRangeAsync(
        DateTimeOffset startTime, 
        DateTimeOffset endTime, 
        int limit = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trade results for a specific time range.
    /// </summary>
    /// <param name="startTime">The start time for the range.</param>
    /// <param name="endTime">The end time for the range.</param>
    /// <param name="limit">The maximum number of trades to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of trade result ViewModels.</returns>
    Task<ICollection<TradeResultViewModel>> GetTradesByTimeRangeAsync(
        DateTimeOffset startTime, 
        DateTimeOffset endTime, 
        int limit = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets arbitrage opportunities filtered by trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to filter by (e.g., "BTC/USDT").</param>
    /// <param name="limit">The maximum number of opportunities to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of arbitrage opportunity ViewModels.</returns>
    Task<ICollection<ArbitrageOpportunityViewModel>> GetOpportunitiesByTradingPairAsync(
        string tradingPair, 
        int limit = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trade results filtered by trading pair.
    /// </summary>
    /// <param name="tradingPair">The trading pair to filter by (e.g., "BTC/USDT").</param>
    /// <param name="limit">The maximum number of trades to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of trade result ViewModels.</returns>
    Task<ICollection<TradeResultViewModel>> GetTradesByTradingPairAsync(
        string tradingPair, 
        int limit = 100, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of opportunities for pagination purposes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of opportunities.</returns>
    Task<int> GetOpportunitiesCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of trades for pagination purposes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of trades.</returns>
    Task<int> GetTradesCountAsync(CancellationToken cancellationToken = default);
} 