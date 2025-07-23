using AutoMapper;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Blazor.ViewModels;

namespace CryptoArbitrage.Blazor.Services;

/// <summary>
/// Implementation of the Blazor model service that provides properly mapped ViewModels to Blazor components.
/// Uses AutoMapper to convert domain models to UI-optimized ViewModels with proper error handling.
/// </summary>
public class BlazorModelService : IBlazorModelService
{
    private readonly IArbitrageRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<BlazorModelService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorModelService"/> class.
    /// </summary>
    /// <param name="repository">The arbitrage repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <param name="logger">The logger.</param>
    public BlazorModelService(
        IArbitrageRepository repository,
        IMapper mapper,
        ILogger<BlazorModelService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ICollection<ArbitrageOpportunityViewModel>> GetOpportunitiesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting {Limit} recent opportunities for Blazor UI", limit);
            
            var opportunities = await _repository.GetRecentOpportunitiesAsync(limit);
            var viewModels = _mapper.Map<ICollection<ArbitrageOpportunityViewModel>>(opportunities);
            
            _logger.LogDebug("Successfully mapped {Count} opportunities to ViewModels", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opportunities for Blazor UI with limit {Limit}", limit);
            return new List<ArbitrageOpportunityViewModel>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<TradeResultViewModel>> GetTradesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting {Limit} recent trades for Blazor UI", limit);
            
            var trades = await _repository.GetRecentTradesAsync(limit);
            var viewModels = _mapper.Map<ICollection<TradeResultViewModel>>(trades);
            
            _logger.LogDebug("Successfully mapped {Count} trades to ViewModels", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trades for Blazor UI with limit {Limit}", limit);
            return new List<TradeResultViewModel>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<ArbitrageOpportunityViewModel>> GetOpportunitiesByTimeRangeAsync(
        DateTimeOffset startTime, 
        DateTimeOffset endTime, 
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting opportunities for time range {StartTime} to {EndTime} with limit {Limit}", 
                startTime, endTime, limit);
            
            var opportunities = await _repository.GetOpportunitiesByTimeRangeAsync(startTime, endTime, limit);
            var viewModels = _mapper.Map<ICollection<ArbitrageOpportunityViewModel>>(opportunities);
            
            _logger.LogDebug("Successfully mapped {Count} time-ranged opportunities to ViewModels", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opportunities by time range for Blazor UI");
            return new List<ArbitrageOpportunityViewModel>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<TradeResultViewModel>> GetTradesByTimeRangeAsync(
        DateTimeOffset startTime, 
        DateTimeOffset endTime, 
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting trades for time range {StartTime} to {EndTime} with limit {Limit}", 
                startTime, endTime, limit);
            
            var trades = await _repository.GetTradesByTimeRangeAsync(startTime, endTime, limit);
            var viewModels = _mapper.Map<ICollection<TradeResultViewModel>>(trades);
            
            _logger.LogDebug("Successfully mapped {Count} time-ranged trades to ViewModels", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trades by time range for Blazor UI");
            return new List<TradeResultViewModel>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<ArbitrageOpportunityViewModel>> GetOpportunitiesByTradingPairAsync(
        string tradingPair, 
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting opportunities for trading pair {TradingPair} with limit {Limit}", 
                tradingPair, limit);
            
            // Get all recent opportunities and filter by trading pair
            // Note: This could be optimized with repository-level filtering
            var allOpportunities = await _repository.GetRecentOpportunitiesAsync(limit * 2); // Get more to account for filtering
            var filteredOpportunities = allOpportunities
                .Where(o => o.TradingPair.ToString().Equals(tradingPair, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();
            
            var viewModels = _mapper.Map<ICollection<ArbitrageOpportunityViewModel>>(filteredOpportunities);
            
            _logger.LogDebug("Successfully mapped {Count} trading pair filtered opportunities to ViewModels", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opportunities by trading pair {TradingPair} for Blazor UI", tradingPair);
            return new List<ArbitrageOpportunityViewModel>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<TradeResultViewModel>> GetTradesByTradingPairAsync(
        string tradingPair, 
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting trades for trading pair {TradingPair} with limit {Limit}", 
                tradingPair, limit);
            
            // Get all recent trades and filter by trading pair
            // Note: This could be optimized with repository-level filtering
            var allTrades = await _repository.GetRecentTradesAsync(limit * 2); // Get more to account for filtering
            var filteredTrades = allTrades
                .Where(t => t.TradingPair.Equals(tradingPair, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();
            
            var viewModels = _mapper.Map<ICollection<TradeResultViewModel>>(filteredTrades);
            
            _logger.LogDebug("Successfully mapped {Count} trading pair filtered trades to ViewModels", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trades by trading pair {TradingPair} for Blazor UI", tradingPair);
            return new List<TradeResultViewModel>();
        }
    }

    /// <inheritdoc />
    public async Task<int> GetOpportunitiesCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: This is a simplified implementation. 
            // In a real application, you'd want a dedicated count method in the repository
            var opportunities = await _repository.GetRecentOpportunitiesAsync(int.MaxValue);
            return opportunities.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opportunities count for Blazor UI");
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetTradesCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: This is a simplified implementation. 
            // In a real application, you'd want a dedicated count method in the repository
            var trades = await _repository.GetRecentTradesAsync(int.MaxValue);
            return trades.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trades count for Blazor UI");
            return 0;
        }
    }
} 