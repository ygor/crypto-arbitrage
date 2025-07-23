using MongoDB.Driver;
using MongoDB.Bson;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Infrastructure.Database;
using CryptoArbitrage.Infrastructure.Database.Documents;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of the arbitrage repository.
/// </summary>
public class MongoDbArbitrageRepository : IArbitrageRepository
{
    private readonly CryptoArbitrageDbContext _dbContext;
    private readonly ILogger<MongoDbArbitrageRepository> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbArbitrageRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    public MongoDbArbitrageRepository(
        CryptoArbitrageDbContext dbContext,
        ILogger<MongoDbArbitrageRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Opportunity Operations

    /// <inheritdoc />
    public async Task<ArbitrageOpportunity> SaveOpportunityAsync(ArbitrageOpportunity opportunity)
    {
        try
        {
            // Ensure the opportunity has an ID
            if (string.IsNullOrWhiteSpace(opportunity.Id))
            {
                opportunity.Id = Guid.NewGuid().ToString();
            }

            var document = ArbitrageOpportunityDocument.FromDomainModel(opportunity);
            
            // Use upsert to handle both insert and update
            var filter = Builders<ArbitrageOpportunityDocument>.Filter
                .Eq(x => x.OpportunityId, opportunity.Id);
            
            var options = new ReplaceOptions { IsUpsert = true };
            
            await _dbContext.ArbitrageOpportunities.ReplaceOneAsync(filter, document, options);
            
            _logger.LogDebug("Saved opportunity {OpportunityId} to MongoDB", opportunity.Id);
            return opportunity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving opportunity {OpportunityId} to MongoDB", opportunity.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        await SaveOpportunityAsync(opportunity);
    }

    /// <inheritdoc />
    public async Task<List<ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        try
        {
            var cutoff = timeSpan.HasValue 
                ? DateTime.UtcNow.Subtract(timeSpan.Value)
                : DateTime.UtcNow.AddHours(-1);

            var filter = Builders<ArbitrageOpportunityDocument>.Filter
                .Gte(x => x.DetectedAt, cutoff);

            var sort = Builders<ArbitrageOpportunityDocument>.Sort
                .Descending(x => x.DetectedAt);

            var documents = await _dbContext.ArbitrageOpportunities
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();

            var opportunities = documents.Select(d => d.ToDomainModel()).ToList();
            
            _logger.LogDebug("Retrieved {Count} recent opportunities from MongoDB", opportunities.Count);
            return opportunities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent opportunities from MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        try
        {
            var filter = Builders<ArbitrageOpportunityDocument>.Filter.And(
                Builders<ArbitrageOpportunityDocument>.Filter.Gte(x => x.DetectedAt, start.DateTime),
                Builders<ArbitrageOpportunityDocument>.Filter.Lte(x => x.DetectedAt, end.DateTime)
            );

            var sort = Builders<ArbitrageOpportunityDocument>.Sort
                .Descending(x => x.DetectedAt);

            var documents = await _dbContext.ArbitrageOpportunities
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();

            var opportunities = documents.Select(d => d.ToDomainModel()).ToList();
            
            _logger.LogDebug("Retrieved {Count} opportunities from MongoDB for time range {Start} to {End}", 
                opportunities.Count, start, end);
            return opportunities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving opportunities by time range from MongoDB");
            throw;
        }
    }

    #endregion

    #region Trade Operations

    /// <inheritdoc />
    public async Task<TradeResult> SaveTradeResultAsync(TradeResult tradeResult)
    {
        try
        {
            // Ensure the trade has an ID
            if (tradeResult.Id == Guid.Empty)
            {
                tradeResult.Id = Guid.NewGuid();
            }

            var document = TradeResultDocument.FromDomainModel(tradeResult);
            
            // Use upsert to handle both insert and update
            var filter = Builders<TradeResultDocument>.Filter
                .Eq(x => x.TradeId, tradeResult.Id.ToString());
            
            var options = new ReplaceOptions { IsUpsert = true };
            
            await _dbContext.TradeResults.ReplaceOneAsync(filter, document, options);
            
            _logger.LogDebug("Saved trade result {TradeId} to MongoDB", tradeResult.Id);
            return tradeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving trade result {TradeId} to MongoDB", tradeResult.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveTradeResultAsync(ArbitrageOpportunity opportunity, TradeResult buyResult, TradeResult sellResult, decimal profit, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a comprehensive trade result from the information
            var tradeResult = new TradeResult
            {
                Id = Guid.NewGuid(),
                OpportunityId = Guid.Parse(opportunity.Id),
                TradingPair = opportunity.TradingPair.ToString(),
                BuyExchangeId = opportunity.BuyExchangeId,
                SellExchangeId = opportunity.SellExchangeId,
                BuyPrice = opportunity.BuyPrice,
                SellPrice = opportunity.SellPrice,
                Quantity = opportunity.EffectiveQuantity,
                Timestamp = timestamp.DateTime,
                Status = TradeStatus.Completed,
                IsSuccess = buyResult?.IsSuccess == true && sellResult?.IsSuccess == true,
                ProfitAmount = profit,
                ProfitPercentage = opportunity.EffectiveQuantity > 0 
                    ? (profit / (opportunity.BuyPrice * opportunity.EffectiveQuantity)) * 100m 
                    : 0m,
                Fees = (buyResult?.Fee ?? 0) + (sellResult?.Fee ?? 0),
                ExecutionTimeMs = 0, // Would need to be calculated from execution times
                BuyResult = buyResult != null ? new TradeSubResult
                {
                    OrderId = buyResult.OrderId,
                    ClientOrderId = buyResult.ClientOrderId,
                    Side = OrderSide.Buy,
                    Quantity = buyResult.RequestedQuantity,
                    Price = buyResult.RequestedPrice,
                    FilledQuantity = buyResult.ExecutedQuantity,
                    AverageFillPrice = buyResult.ExecutedPrice,
                    FeeAmount = buyResult.Fee,
                    FeeCurrency = buyResult.FeeCurrency,
                    Status = buyResult.IsSuccess ? OrderStatus.Filled : OrderStatus.Rejected,
                    Timestamp = buyResult.Timestamp,
                    ErrorMessage = buyResult.ErrorMessage
                } : null,
                SellResult = sellResult != null ? new TradeSubResult
                {
                    OrderId = sellResult.OrderId,
                    ClientOrderId = sellResult.ClientOrderId,
                    Side = OrderSide.Sell,
                    Quantity = sellResult.RequestedQuantity,
                    Price = sellResult.RequestedPrice,
                    FilledQuantity = sellResult.ExecutedQuantity,
                    AverageFillPrice = sellResult.ExecutedPrice,
                    FeeAmount = sellResult.Fee,
                    FeeCurrency = sellResult.FeeCurrency,
                    Status = sellResult.IsSuccess ? OrderStatus.Filled : OrderStatus.Rejected,
                    Timestamp = sellResult.Timestamp,
                    ErrorMessage = sellResult.ErrorMessage
                } : null
            };

            await SaveTradeResultAsync(tradeResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving composite trade result to MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<TradeResult>> GetRecentTradesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        try
        {
            var cutoff = timeSpan.HasValue 
                ? DateTime.UtcNow.Subtract(timeSpan.Value)
                : DateTime.UtcNow.AddHours(-24);

            var filter = Builders<TradeResultDocument>.Filter
                .Gte(x => x.Timestamp, cutoff);

            var sort = Builders<TradeResultDocument>.Sort
                .Descending(x => x.Timestamp);

            var documents = await _dbContext.TradeResults
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();

            var trades = documents.Select(d => d.ToDomainModel()).ToList();
            
            _logger.LogDebug("Retrieved {Count} recent trades from MongoDB", trades.Count);
            return trades;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent trades from MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<TradeResult>> GetTradesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        try
        {
            var filter = Builders<TradeResultDocument>.Filter.And(
                Builders<TradeResultDocument>.Filter.Gte(x => x.Timestamp, start.DateTime),
                Builders<TradeResultDocument>.Filter.Lte(x => x.Timestamp, end.DateTime)
            );

            var sort = Builders<TradeResultDocument>.Sort
                .Descending(x => x.Timestamp);

            var documents = await _dbContext.TradeResults
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();

            var trades = documents.Select(d => d.ToDomainModel()).ToList();
            
            _logger.LogDebug("Retrieved {Count} trades from MongoDB for time range {Start} to {End}", 
                trades.Count, start, end);
            return trades;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trades by time range from MongoDB");
            throw;
        }
    }

    #endregion

    #region Statistics Operations

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetStatisticsAsync(DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find existing statistics for the day
            var dayStart = timestamp.Date;
            var filter = Builders<ArbitrageStatisticsDocument>.Filter.And(
                Builders<ArbitrageStatisticsDocument>.Filter.Gte(x => x.Date, dayStart),
                Builders<ArbitrageStatisticsDocument>.Filter.Lt(x => x.Date, dayStart.AddDays(1)),
                Builders<ArbitrageStatisticsDocument>.Filter.Eq(x => x.PeriodType, "Daily")
            );

            var document = await _dbContext.ArbitrageStatistics
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

            if (document != null)
            {
                return document.ToDomainModel();
            }

            // If not found, calculate statistics from trade data
            return await CalculateStatisticsAsync(dayStart, dayStart.AddDays(1), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics from MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveStatisticsAsync(ArbitrageStatistics statistics, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = ArbitrageStatisticsDocument.FromDomainModel(statistics, timestamp.DateTime);
            
            // Use upsert based on date and period type
            var filter = Builders<ArbitrageStatisticsDocument>.Filter.And(
                Builders<ArbitrageStatisticsDocument>.Filter.Eq(x => x.Date, timestamp.Date),
                Builders<ArbitrageStatisticsDocument>.Filter.Eq(x => x.PeriodType, "Daily")
            );
            
            var options = new ReplaceOptions { IsUpsert = true };
            
            await _dbContext.ArbitrageStatistics.ReplaceOneAsync(filter, document, options, cancellationToken);
            
            _logger.LogDebug("Saved statistics for {Date} to MongoDB", timestamp.Date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving statistics to MongoDB");
            throw;
        }
    }

    #endregion

    #region Additional IArbitrageRepository Methods

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var opportunities = await GetOpportunitiesByTimeRangeAsync(start, end, int.MaxValue);
        return opportunities.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<TradeResultDocument>.Filter.And(
                Builders<TradeResultDocument>.Filter.Gte(x => x.Timestamp, start.DateTime),
                Builders<TradeResultDocument>.Filter.Lte(x => x.Timestamp, end.DateTime)
            );

            var trades = await _dbContext.TradeResults
                .Find(filter)
                .ToListAsync(cancellationToken);

            var result = new List<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>();

            foreach (var trade in trades)
            {
                if (trade.BuyResult != null && trade.SellResult != null)
                {
                    // Find the associated opportunity
                    var opportunityFilter = Builders<ArbitrageOpportunityDocument>.Filter
                        .Eq(x => x.OpportunityId, trade.OpportunityId);
                    
                    var opportunityDoc = await _dbContext.ArbitrageOpportunities
                        .Find(opportunityFilter)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (opportunityDoc != null)
                    {
                        var opportunity = opportunityDoc.ToDomainModel();

                        // Convert sub-results to TradeResult objects
                        var buyTradeResult = new TradeResult
                        {
                            OrderId = trade.BuyResult.OrderId,
                            RequestedPrice = trade.BuyResult.Price,
                            ExecutedPrice = trade.BuyResult.AverageFillPrice,
                            RequestedQuantity = trade.BuyResult.Quantity,
                            ExecutedQuantity = trade.BuyResult.FilledQuantity,
                            TotalValue = trade.BuyResult.AverageFillPrice * trade.BuyResult.FilledQuantity,
                            Fee = trade.BuyResult.FeeAmount,
                            FeeCurrency = trade.BuyResult.FeeCurrency,
                            Timestamp = trade.BuyResult.Timestamp,
                            IsSuccess = trade.BuyResult.Status == "Filled",
                            ErrorMessage = trade.BuyResult.ErrorMessage,
                            TradeType = TradeType.Buy,
                            TradingPair = trade.TradingPair
                        };

                        var sellTradeResult = new TradeResult
                        {
                            OrderId = trade.SellResult.OrderId,
                            RequestedPrice = trade.SellResult.Price,
                            ExecutedPrice = trade.SellResult.AverageFillPrice,
                            RequestedQuantity = trade.SellResult.Quantity,
                            ExecutedQuantity = trade.SellResult.FilledQuantity,
                            TotalValue = trade.SellResult.AverageFillPrice * trade.SellResult.FilledQuantity,
                            Fee = trade.SellResult.FeeAmount,
                            FeeCurrency = trade.SellResult.FeeCurrency,
                            Timestamp = trade.SellResult.Timestamp,
                            IsSuccess = trade.SellResult.Status == "Filled",
                            ErrorMessage = trade.SellResult.ErrorMessage,
                            TradeType = TradeType.Sell,
                            TradingPair = trade.TradingPair
                        };

                        result.Add((opportunity, buyTradeResult, sellTradeResult, trade.ProfitAmount, trade.Timestamp));
                    }
                }
            }

            return result.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trade results from MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetStatisticsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        return await CalculateStatisticsAsync(start.DateTime, end.DateTime, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TradeResult?> GetTradeByIdAsync(string id)
    {
        try
        {
            var filter = Builders<TradeResultDocument>.Filter.Eq(x => x.TradeId, id);
            var document = await _dbContext.TradeResults.Find(filter).FirstOrDefaultAsync();
            return document?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trade by ID {TradeId} from MongoDB", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId)
    {
        try
        {
            var filter = Builders<TradeResultDocument>.Filter.Eq(x => x.OpportunityId, opportunityId);
            var documents = await _dbContext.TradeResults
                .Find(filter)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();

            return documents.Select(d => d.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trades by opportunity ID {OpportunityId} from MongoDB", opportunityId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetCurrentDayStatisticsAsync()
    {
        var start = DateTimeOffset.UtcNow.Date;
        var end = start.AddDays(1).AddTicks(-1);
        return await GetStatisticsAsync(start, end);
    }

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetLastDayStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddDays(-1);
        return await GetStatisticsAsync(start, end);
    }

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetLastWeekStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-7);
        return await GetStatisticsAsync(start, end);
    }

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetLastMonthStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-30);
        return await GetStatisticsAsync(start, end);
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldOpportunitiesAsync(DateTimeOffset olderThan)
    {
        try
        {
            var filter = Builders<ArbitrageOpportunityDocument>.Filter
                .Lt(x => x.DetectedAt, olderThan.DateTime);

            var result = await _dbContext.ArbitrageOpportunities.DeleteManyAsync(filter);
            _logger.LogInformation("Deleted {Count} old opportunities from MongoDB", result.DeletedCount);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old opportunities from MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldTradesAsync(DateTimeOffset olderThan)
    {
        try
        {
            var filter = Builders<TradeResultDocument>.Filter
                .Lt(x => x.Timestamp, olderThan.DateTime);

            var result = await _dbContext.TradeResults.DeleteManyAsync(filter);
            _logger.LogInformation("Deleted {Count} old trades from MongoDB", result.DeletedCount);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old trades from MongoDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ArbitrageStatistics> GetArbitrageStatisticsAsync(string tradingPair, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var end = toDate ?? DateTime.UtcNow;

            // Build filter for trading pair and date range
            var filterBuilder = Builders<ArbitrageStatisticsDocument>.Filter;
            var filters = new List<FilterDefinition<ArbitrageStatisticsDocument>>
            {
                filterBuilder.Gte(x => x.Date, start),
                filterBuilder.Lte(x => x.Date, end)
            };

            if (!string.IsNullOrEmpty(tradingPair))
            {
                filters.Add(filterBuilder.Eq(x => x.TradingPair, tradingPair));
            }

            var combinedFilter = filterBuilder.And(filters);

            // Try to find existing aggregated statistics
            var existingStats = await _dbContext.ArbitrageStatistics
                .Find(combinedFilter)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingStats != null)
            {
                return existingStats.ToDomainModel();
            }

            // Calculate statistics from trade data
            return await CalculateStatisticsForTradingPairAsync(tradingPair, start, end, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving arbitrage statistics for trading pair {TradingPair}", tradingPair);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveArbitrageStatisticsAsync(ArbitrageStatistics statistics, CancellationToken cancellationToken = default)
    {
        await SaveStatisticsAsync(statistics, DateTimeOffset.UtcNow, cancellationToken);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculates statistics from trade data for a given time period.
    /// </summary>
    /// <param name="start">The start of the time period.</param>
    /// <param name="end">The end of the time period.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The calculated statistics.</returns>
    private async Task<ArbitrageStatistics> CalculateStatisticsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get opportunities in the time range
            var opportunityFilter = Builders<ArbitrageOpportunityDocument>.Filter.And(
                Builders<ArbitrageOpportunityDocument>.Filter.Gte(x => x.DetectedAt, start),
                Builders<ArbitrageOpportunityDocument>.Filter.Lt(x => x.DetectedAt, end)
            );

            var opportunities = await _dbContext.ArbitrageOpportunities
                .Find(opportunityFilter)
                .ToListAsync(cancellationToken);

            // Get trades in the time range
            var tradeFilter = Builders<TradeResultDocument>.Filter.And(
                Builders<TradeResultDocument>.Filter.Gte(x => x.Timestamp, start),
                Builders<TradeResultDocument>.Filter.Lt(x => x.Timestamp, end)
            );

            var trades = await _dbContext.TradeResults
                .Find(tradeFilter)
                .ToListAsync(cancellationToken);

            // Calculate statistics
            var successfulTrades = trades.Where(t => t.IsSuccess).ToList();
            var failedTrades = trades.Where(t => !t.IsSuccess).ToList();

            var statistics = new ArbitrageStatistics
            {
                CreatedAt = start,
                StartTime = start,
                EndTime = end,
                TotalOpportunitiesCount = opportunities.Count,
                TotalTradesCount = trades.Count,
                SuccessfulTradesCount = successfulTrades.Count,
                FailedTradesCount = failedTrades.Count,
                SuccessRate = trades.Count > 0 ? (decimal)successfulTrades.Count / trades.Count * 100 : 0,
                TotalProfitAmount = successfulTrades.Sum(t => t.ProfitAmount),
                AverageProfitAmount = successfulTrades.Count > 0 ? successfulTrades.Average(t => t.ProfitAmount) : 0,
                HighestProfitAmount = successfulTrades.Count > 0 ? successfulTrades.Max(t => t.ProfitAmount) : 0,
                LowestProfit = trades.Count > 0 ? trades.Min(t => t.ProfitAmount) : 0,
                TotalVolume = trades.Sum(t => t.Quantity * t.BuyPrice),
                TotalFeesAmount = trades.Sum(t => t.Fees),
                AverageExecutionTimeMs = trades.Count > 0 ? (decimal)trades.Average(t => t.ExecutionTimeMs) : 0,
                AverageProfitPercentage = successfulTrades.Count > 0 ? successfulTrades.Average(t => t.ProfitPercentage) : 0,
                HighestProfitPercentage = successfulTrades.Count > 0 ? successfulTrades.Max(t => t.ProfitPercentage) : 0
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics from trade data");
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Calculates statistics for a specific trading pair from trade data.
    /// </summary>
    /// <param name="tradingPair">The trading pair to calculate statistics for.</param>
    /// <param name="start">The start of the time period.</param>
    /// <param name="end">The end of the time period.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The calculated statistics.</returns>
    private async Task<ArbitrageStatistics> CalculateStatisticsForTradingPairAsync(string tradingPair, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        try
        {
            // Build filters
            var opportunityFilterBuilder = Builders<ArbitrageOpportunityDocument>.Filter;
            var opportunityFilters = new List<FilterDefinition<ArbitrageOpportunityDocument>>
            {
                opportunityFilterBuilder.Gte(x => x.DetectedAt, start),
                opportunityFilterBuilder.Lt(x => x.DetectedAt, end)
            };

            if (!string.IsNullOrEmpty(tradingPair))
            {
                opportunityFilters.Add(opportunityFilterBuilder.Eq(x => x.TradingPair, tradingPair));
            }

            var tradeFilterBuilder = Builders<TradeResultDocument>.Filter;
            var tradeFilters = new List<FilterDefinition<TradeResultDocument>>
            {
                tradeFilterBuilder.Gte(x => x.Timestamp, start),
                tradeFilterBuilder.Lt(x => x.Timestamp, end)
            };

            if (!string.IsNullOrEmpty(tradingPair))
            {
                tradeFilters.Add(tradeFilterBuilder.Eq(x => x.TradingPair, tradingPair));
            }

            // Get data
            var opportunities = await _dbContext.ArbitrageOpportunities
                .Find(opportunityFilterBuilder.And(opportunityFilters))
                .ToListAsync(cancellationToken);

            var trades = await _dbContext.TradeResults
                .Find(tradeFilterBuilder.And(tradeFilters))
                .ToListAsync(cancellationToken);

            // Calculate statistics
            var successfulTrades = trades.Where(t => t.IsSuccess).ToList();
            var failedTrades = trades.Where(t => !t.IsSuccess).ToList();

            var statistics = new ArbitrageStatistics
            {
                CreatedAt = start,
                StartTime = start,
                EndTime = end,
                TotalOpportunitiesCount = opportunities.Count,
                TotalTradesCount = trades.Count,
                SuccessfulTradesCount = successfulTrades.Count,
                FailedTradesCount = failedTrades.Count,
                SuccessRate = trades.Count > 0 ? (decimal)successfulTrades.Count / trades.Count * 100 : 0,
                TotalProfitAmount = successfulTrades.Sum(t => t.ProfitAmount),
                AverageProfitAmount = successfulTrades.Count > 0 ? successfulTrades.Average(t => t.ProfitAmount) : 0,
                HighestProfitAmount = successfulTrades.Count > 0 ? successfulTrades.Max(t => t.ProfitAmount) : 0,
                LowestProfit = trades.Count > 0 ? trades.Min(t => t.ProfitAmount) : 0,
                TotalVolume = trades.Sum(t => t.Quantity * t.BuyPrice),
                TotalFeesAmount = trades.Sum(t => t.Fees),
                AverageExecutionTimeMs = trades.Count > 0 ? (decimal)trades.Average(t => t.ExecutionTimeMs) : 0,
                AverageProfitPercentage = successfulTrades.Count > 0 ? successfulTrades.Average(t => t.ProfitPercentage) : 0,
                HighestProfitPercentage = successfulTrades.Count > 0 ? successfulTrades.Max(t => t.ProfitPercentage) : 0
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for trading pair {TradingPair}", tradingPair);
            throw;
        }
    }

    #endregion
} 