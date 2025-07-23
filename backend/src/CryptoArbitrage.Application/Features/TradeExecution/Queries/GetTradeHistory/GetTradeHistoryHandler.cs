using MediatR;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.TradeExecution.Queries.GetTradeHistory;

/// <summary>
/// Handler for GetTradeHistoryQuery.
/// </summary>
public class GetTradeHistoryHandler : IRequestHandler<GetTradeHistoryQuery, GetTradeHistoryResult>
{
    private readonly IArbitrageRepository _repository;
    private readonly ILogger<GetTradeHistoryHandler> _logger;

    public GetTradeHistoryHandler(
        IArbitrageRepository repository,
        ILogger<GetTradeHistoryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetTradeHistoryResult> Handle(GetTradeHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting trade history with filters: Limit={Limit}, Skip={Skip}, StartDate={StartDate}, EndDate={EndDate}",
            request.Limit, request.Skip, request.StartDate, request.EndDate);

        try
        {
            // Validate and sanitize input
            var limit = Math.Min(Math.Max(request.Limit, 1), 100); // Between 1 and 100
            var skip = Math.Max(request.Skip, 0);

            // Set default date range if not specified
            var startDate = request.StartDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTimeOffset.UtcNow;

            // Ensure start date is before end date
            if (startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            // Get trades from repository with basic filtering
            var allTrades = await _repository.GetTradesByTimeRangeAsync(startDate, endDate);

            // Apply additional filters
            var filteredTrades = ApplyFilters(allTrades, request);

            // Apply sorting
            var sortedTrades = ApplySorting(filteredTrades, request.SortBy);

            // Get total count before pagination
            var totalCount = sortedTrades.Count();

            // Apply pagination
            var paginatedTrades = sortedTrades
                .Skip(skip)
                .Take(limit)
                .ToList();

            // Check if there are more results
            var hasMore = skip + limit < totalCount;

            _logger.LogInformation(
                "Retrieved {Count} trades out of {TotalCount} matching filters",
                paginatedTrades.Count, totalCount);

            return GetTradeHistoryResult.Success(
                paginatedTrades.AsReadOnly(),
                totalCount,
                hasMore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trade history");
            
            // Return empty result on error rather than throwing
            return GetTradeHistoryResult.Success(
                Array.Empty<TradeResult>(),
                0,
                false);
        }
    }

    private static IEnumerable<TradeResult> ApplyFilters(
        IEnumerable<TradeResult> trades, 
        GetTradeHistoryQuery request)
    {
        var filtered = trades;

        if (!string.IsNullOrWhiteSpace(request.TradingPair))
        {
            filtered = filtered.Where(t => 
                t.TradingPair.Equals(request.TradingPair, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.BuyExchangeId))
        {
            filtered = filtered.Where(t => 
                t.BuyExchangeId.Equals(request.BuyExchangeId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.SellExchangeId))
        {
            filtered = filtered.Where(t => 
                t.SellExchangeId.Equals(request.SellExchangeId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<TradeStatus>(request.Status, true, out var status))
            {
                filtered = filtered.Where(t => t.Status == status);
            }
        }

        if (request.MinProfit.HasValue)
        {
            filtered = filtered.Where(t => t.ProfitAmount >= request.MinProfit.Value);
        }

        if (request.MaxProfit.HasValue)
        {
            filtered = filtered.Where(t => t.ProfitAmount <= request.MaxProfit.Value);
        }

        if (request.MinProfitPercentage.HasValue)
        {
            filtered = filtered.Where(t => t.ProfitPercentage >= request.MinProfitPercentage.Value);
        }

        if (request.MaxProfitPercentage.HasValue)
        {
            filtered = filtered.Where(t => t.ProfitPercentage <= request.MaxProfitPercentage.Value);
        }

        if (request.SuccessfulOnly.HasValue)
        {
            filtered = filtered.Where(t => t.IsSuccess == request.SuccessfulOnly.Value);
        }

        return filtered;
    }

    private static IOrderedEnumerable<TradeResult> ApplySorting(
        IEnumerable<TradeResult> trades, 
        string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "oldest" => trades.OrderBy(t => t.Timestamp),
            "profit_desc" => trades.OrderByDescending(t => t.ProfitAmount),
            "profit_asc" => trades.OrderBy(t => t.ProfitAmount),
            "newest" or _ => trades.OrderByDescending(t => t.Timestamp)
        };
    }
} 