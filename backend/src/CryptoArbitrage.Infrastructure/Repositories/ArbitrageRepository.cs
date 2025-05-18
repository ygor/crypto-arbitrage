using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure.Repositories;

public class ArbitrageRepository : IArbitrageRepository
{
    private readonly ILogger<ArbitrageRepository> _logger;
    private readonly string _dataDirectory;
    private readonly string _opportunitiesFilePath;
    private readonly string _tradesFilePath;
    private readonly string _statisticsFilePath;
    
    private readonly ConcurrentDictionary<string, ArbitrageOpportunity> _opportunities = new();
    private readonly ConcurrentDictionary<string, TradeResult> _trades = new();
    private readonly ConcurrentDictionary<DateTimeOffset, ArbitrageStatistics> _statistics = new();
    
    // Limit for in-memory storage
    private const int MAX_OPPORTUNITIES_IN_MEMORY = 1000;
    private const int MAX_TRADES_IN_MEMORY = 1000;
    private const int MAX_STATISTICS_IN_MEMORY = 100;
    
    // Save threshold
    private const int SAVE_THRESHOLD = 10;
    private int _opportunityChangeCount = 0;
    private int _tradeChangeCount = 0;
    private int _statisticsChangeCount = 0;

    public ArbitrageRepository(ILogger<ArbitrageRepository> logger)
    {
        _logger = logger;
        
        // Create data directory if it doesn't exist
        _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CryptoArbitrage", "Data");
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
        
        _opportunitiesFilePath = Path.Combine(_dataDirectory, "opportunities.json");
        _tradesFilePath = Path.Combine(_dataDirectory, "trades.json");
        _statisticsFilePath = Path.Combine(_dataDirectory, "statistics.json");
        
        // Load data from files
        LoadData();
    }

    #region IArbitrageRepository Implementation

    public async Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        await SaveOpportunityAsync(opportunity);
    }

    public async Task SaveTradeResultAsync(ArbitrageOpportunity opportunity, TradeResult buyResult, TradeResult sellResult, decimal profit, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        // Create a trade result from the information
        var tradeResult = new TradeResult
        {
            Id = GenerateId(),
            OpportunityId = opportunity.Id,
            TradingPair = opportunity.TradingPair.ToString(),
            BuyExchangeId = opportunity.BuyExchangeId,
            SellExchangeId = opportunity.SellExchangeId,
            BuyPrice = opportunity.BuyPrice,
            SellPrice = opportunity.SellPrice,
            Quantity = opportunity.EffectiveQuantity,
            Timestamp = timestamp,
            Status = TradeStatus.Completed,
            ProfitAmount = profit,
            ProfitPercentage = (profit / (opportunity.BuyPrice * opportunity.EffectiveQuantity)) * 100m,
            Fees = (buyResult?.Fee ?? 0) + (sellResult?.Fee ?? 0),
            ExecutionTimeMs = 0, // Would need to be calculated from execution times
            BuyResult = buyResult != null ? new BuyTradeResult
            {
                OrderId = buyResult.OrderId,
                ClientOrderId = buyResult.ClientOrderId,
                RequestedPrice = buyResult.RequestedPrice,
                ExecutedPrice = buyResult.ExecutedPrice,
                RequestedQuantity = buyResult.RequestedQuantity,
                ExecutedQuantity = buyResult.ExecutedQuantity,
                TotalValue = buyResult.TotalValue,
                Fee = buyResult.Fee,
                FeeCurrency = buyResult.FeeCurrency,
                Timestamp = buyResult.Timestamp,
                IsSuccess = buyResult.IsSuccess,
                ErrorMessage = buyResult.ErrorMessage
            } : null,
            SellResult = sellResult != null ? new SellTradeResult
            {
                OrderId = sellResult.OrderId,
                ClientOrderId = sellResult.ClientOrderId,
                RequestedPrice = sellResult.RequestedPrice,
                ExecutedPrice = sellResult.ExecutedPrice,
                RequestedQuantity = sellResult.RequestedQuantity,
                ExecutedQuantity = sellResult.ExecutedQuantity,
                TotalValue = sellResult.TotalValue,
                Fee = sellResult.Fee,
                FeeCurrency = sellResult.FeeCurrency,
                Timestamp = sellResult.Timestamp,
                IsSuccess = sellResult.IsSuccess,
                ErrorMessage = sellResult.ErrorMessage
            } : null
        };
        
        await SaveTradeResultAsync(tradeResult);
    }

    public async Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var opportunities = await GetOpportunitiesByTimeRangeAsync(start, end, int.MaxValue);
        return opportunities.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var trades = await GetTradesByTimeRangeAsync(start, end, int.MaxValue);
        var result = new List<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>();
        
        foreach (var trade in trades)
        {
            if (trade.BuyResult != null && trade.SellResult != null && 
                _opportunities.TryGetValue(trade.OpportunityId, out var opportunity))
            {
                // Convert BuyTradeResult to TradeResult
                var buyTradeResult = new TradeResult
                {
                    OrderId = trade.BuyResult.OrderId,
                    ClientOrderId = trade.BuyResult.ClientOrderId,
                    RequestedPrice = trade.BuyResult.RequestedPrice,
                    ExecutedPrice = trade.BuyResult.ExecutedPrice,
                    RequestedQuantity = trade.BuyResult.RequestedQuantity,
                    ExecutedQuantity = trade.BuyResult.ExecutedQuantity,
                    TotalValue = trade.BuyResult.TotalValue,
                    Fee = trade.BuyResult.Fee,
                    FeeCurrency = trade.BuyResult.FeeCurrency,
                    Timestamp = trade.BuyResult.Timestamp,
                    IsSuccess = trade.BuyResult.IsSuccess,
                    ErrorMessage = trade.BuyResult.ErrorMessage,
                    TradeType = TradeType.Buy,
                    TradingPair = trade.TradingPair
                };
                
                // Convert SellTradeResult to TradeResult
                var sellTradeResult = new TradeResult
                {
                    OrderId = trade.SellResult.OrderId,
                    ClientOrderId = trade.SellResult.ClientOrderId,
                    RequestedPrice = trade.SellResult.RequestedPrice,
                    ExecutedPrice = trade.SellResult.ExecutedPrice,
                    RequestedQuantity = trade.SellResult.RequestedQuantity,
                    ExecutedQuantity = trade.SellResult.ExecutedQuantity,
                    TotalValue = trade.SellResult.TotalValue,
                    Fee = trade.SellResult.Fee,
                    FeeCurrency = trade.SellResult.FeeCurrency,
                    Timestamp = trade.SellResult.Timestamp,
                    IsSuccess = trade.SellResult.IsSuccess,
                    ErrorMessage = trade.SellResult.ErrorMessage,
                    TradeType = TradeType.Sell,
                    TradingPair = trade.TradingPair
                };
                
                result.Add((opportunity, buyTradeResult, sellTradeResult, trade.ProfitAmount, trade.Timestamp));
            }
        }
        
        return result;
    }

    public async Task<ArbitrageStatistics> GetStatisticsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cacheKey = $"{start:yyyyMMdd}_{end:yyyyMMdd}";
        if (_statistics.TryGetValue(start, out var cachedStats))
        {
            // Check if the cached stats cover the same time range
            if (cachedStats.StartTime == start && cachedStats.EndTime == end)
            {
                return cachedStats;
            }
        }
        
        // Get trades and opportunities in the time range
        var opportunities = await GetOpportunitiesByTimeRangeAsync(start, end, int.MaxValue);
        var trades = await GetTradesByTimeRangeAsync(start, end, int.MaxValue);
        
        // Calculate statistics
        var statistics = new ArbitrageStatistics
        {
            StartTime = start,
            EndTime = end,
            TotalOpportunitiesCount = opportunities.Count,
            QualifiedOpportunitiesCount = opportunities.Count(o => o.IsQualified),
            TotalTradesCount = trades.Count,
            SuccessfulTradesCount = trades.Count(t => t.Status == TradeStatus.Completed),
            FailedTradesCount = trades.Count(t => t.Status == TradeStatus.Failed),
            TotalProfitAmount = trades.Where(t => t.Status == TradeStatus.Completed).Sum(t => t.ProfitAmount),
            TotalFeesAmount = trades.Sum(t => t.Fees),
            AverageExecutionTimeMs = trades.Count > 0 ? trades.Average(t => t.ExecutionTimeMs) : 0,
            
            // Add properties for backward compatibility
            TotalOpportunitiesDetected = opportunities.Count,
            TotalTradesExecuted = trades.Count,
            SuccessfulTrades = trades.Count(t => t.Status == TradeStatus.Completed),
            FailedTrades = trades.Count(t => t.Status == TradeStatus.Failed),
            TotalProfit = trades.Where(t => t.Status == TradeStatus.Completed).Sum(t => t.ProfitAmount),
            TotalVolume = trades.Sum(t => t.Quantity * t.BuyPrice)
        };
        
        // Calculate average profit percentage
        if (statistics.SuccessfulTradesCount > 0)
        {
            statistics.AverageProfitPercentage = trades
                .Where(t => t.Status == TradeStatus.Completed)
                .Average(t => t.ProfitPercentage);
            
            statistics.HighestProfitPercentage = trades
                .Where(t => t.Status == TradeStatus.Completed)
                .Max(t => t.ProfitPercentage);
                
            // Add highest/lowest profit for backward compatibility
            statistics.HighestProfit = trades
                .Where(t => t.Status == TradeStatus.Completed)
                .Max(t => t.ProfitAmount);
            
            statistics.LowestProfit = trades
                .Where(t => t.Status == TradeStatus.Completed)
                .Min(t => t.ProfitAmount);
        }
        
        // Calculate trading pairs frequency
        var tradingPairsFrequency = opportunities
            .GroupBy(o => o.TradingPair.ToString())
            .Select(g => new { TradingPair = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();
        
        statistics.MostFrequentTradingPairs = tradingPairsFrequency.Select(x => x.TradingPair).ToList();
        statistics.OpportunitiesByTradingPair = tradingPairsFrequency.ToDictionary(x => x.TradingPair, x => x.Count);
        
        // Calculate exchange pair frequency
        var exchangePairsFrequency = opportunities
            .GroupBy(o => $"{o.BuyExchangeId}-{o.SellExchangeId}")
            .Select(g => new { ExchangePair = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionary(x => x.ExchangePair, x => x.Count);
        
        statistics.OpportunitiesByExchangePair = exchangePairsFrequency;
        
        // Calculate hourly distribution
        statistics.OpportunitiesByHour = opportunities
            .GroupBy(o => o.DetectedAt.ToString("HH"))
            .ToDictionary(g => g.Key, g => g.Count());
        
        statistics.TradesByHour = trades
            .GroupBy(t => t.Timestamp.ToString("HH"))
            .ToDictionary(g => g.Key, g => g.Count());
        
        statistics.ProfitByHour = trades
            .Where(t => t.Status == TradeStatus.Completed)
            .GroupBy(t => t.Timestamp.ToString("HH"))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.ProfitAmount));
        
        // Calculate exchange statistics
        var exchangeStats = new Dictionary<string, ExchangeStatistics>();
        var allExchanges = opportunities.Select(o => o.BuyExchangeId)
            .Concat(opportunities.Select(o => o.SellExchangeId))
            .Distinct();
        
        foreach (var exchangeId in allExchanges)
        {
            var buyTrades = trades.Where(t => t.BuyExchangeId == exchangeId).ToList();
            var sellTrades = trades.Where(t => t.SellExchangeId == exchangeId).ToList();
            
            exchangeStats[exchangeId] = new ExchangeStatistics
            {
                ExchangeId = exchangeId,
                TotalTrades = buyTrades.Count + sellTrades.Count,
                TotalProfit = sellTrades.Sum(t => t.ProfitAmount),
                TotalVolume = buyTrades.Sum(t => t.Quantity * t.BuyPrice) + sellTrades.Sum(t => t.Quantity * t.SellPrice),
                TotalFees = buyTrades.Sum(t => t.Fees) + sellTrades.Sum(t => t.Fees),
                AverageExecutionTimeMs = (buyTrades.Count > 0 ? (double)buyTrades.Average(t => t.ExecutionTimeMs) : 0) +
                                          (sellTrades.Count > 0 ? (double)sellTrades.Average(t => t.ExecutionTimeMs) : 0) / 2
            };
        }
        
        statistics.StatisticsByExchange = exchangeStats;
        
        // Calculate trading pair statistics
        var tradingPairStats = new Dictionary<string, TradingPairStatistics>();
        var allPairs = trades.Select(t => t.TradingPair).Distinct();
        
        foreach (var pair in allPairs)
        {
            var pairTrades = trades.Where(t => t.TradingPair == pair).ToList();
            var pairOpportunities = opportunities.Where(o => o.TradingPair.ToString() == pair).ToList();
            
            tradingPairStats[pair] = new TradingPairStatistics
            {
                TradingPair = pair,
                TotalTrades = pairTrades.Count,
                SuccessfulTrades = pairTrades.Count(t => t.Status == TradeStatus.Completed),
                FailedTrades = pairTrades.Count(t => t.Status == TradeStatus.Failed),
                TotalProfit = pairTrades.Where(t => t.Status == TradeStatus.Completed).Sum(t => t.ProfitAmount),
                TotalVolume = pairTrades.Sum(t => t.Quantity * t.BuyPrice),
                HighestSpreadPercentage = pairOpportunities.Count > 0 ? pairOpportunities.Max(o => o.SpreadPercentage) : 0,
                AverageSpreadPercentage = pairOpportunities.Count > 0 ? pairOpportunities.Average(o => o.SpreadPercentage) : 0
            };
        }
        
        statistics.StatisticsByTradingPair = tradingPairStats;
        
        // Cache the statistics
        _statistics[start] = statistics;
        
        // Manage cache size
        if (_statistics.Count > MAX_STATISTICS_IN_MEMORY)
        {
            var oldestKeys = _statistics.Keys
                .OrderBy(k => k)
                .Take(_statistics.Count - MAX_STATISTICS_IN_MEMORY);
            
            foreach (var key in oldestKeys)
            {
                _statistics.TryRemove(key, out _);
            }
        }
        
        // Save statistics periodically
        if (Interlocked.Increment(ref _statisticsChangeCount) >= SAVE_THRESHOLD)
        {
            _ = Task.Run(() => SaveStatistics());
            Interlocked.Exchange(ref _statisticsChangeCount, 0);
        }
        
        return statistics;
    }

    public async Task SaveStatisticsAsync(ArbitrageStatistics statistics, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        _statistics[timestamp] = statistics;
        
        // Save to file periodically
        if (Interlocked.Increment(ref _statisticsChangeCount) >= SAVE_THRESHOLD)
        {
            _ = Task.Run(() => SaveStatistics());
            Interlocked.Exchange(ref _statisticsChangeCount, 0);
        }
    }

    // Rest of IArbitrageRepository implementation
    public async Task<ArbitrageOpportunity> SaveOpportunityAsync(ArbitrageOpportunity opportunity)
    {
        // Ensure the opportunity has an ID
        if (string.IsNullOrWhiteSpace(opportunity.Id))
        {
            opportunity.Id = GenerateId();
        }
        
        // Store the opportunity
        _opportunities[opportunity.Id] = opportunity;
        
        // Manage memory usage
        if (_opportunities.Count > MAX_OPPORTUNITIES_IN_MEMORY)
        {
            // Remove the oldest opportunities
            var oldestOpportunities = _opportunities.Values
                .OrderBy(o => o.DetectedAt)
                .Take(_opportunities.Count - MAX_OPPORTUNITIES_IN_MEMORY);
            
            foreach (var oldOpportunity in oldestOpportunities)
            {
                _opportunities.TryRemove(oldOpportunity.Id, out _);
            }
        }
        
        // Save to file periodically
        if (Interlocked.Increment(ref _opportunityChangeCount) >= SAVE_THRESHOLD)
        {
            _ = Task.Run(() => SaveOpportunities());
            Interlocked.Exchange(ref _opportunityChangeCount, 0);
        }
        
        return opportunity;
    }

    public async Task<TradeResult> SaveTradeResultAsync(TradeResult tradeResult)
    {
        // Ensure the trade has an ID
        if (string.IsNullOrWhiteSpace(tradeResult.Id))
        {
            tradeResult.Id = GenerateId();
        }
        
        // Store the trade
        _trades[tradeResult.Id] = tradeResult;
        
        // Manage memory usage
        if (_trades.Count > MAX_TRADES_IN_MEMORY)
        {
            // Remove the oldest trades
            var oldestTrades = _trades.Values
                .OrderBy(t => t.Timestamp)
                .Take(_trades.Count - MAX_TRADES_IN_MEMORY);
            
            foreach (var oldTrade in oldestTrades)
            {
                _trades.TryRemove(oldTrade.Id, out _);
            }
        }
        
        // Save to file periodically
        if (Interlocked.Increment(ref _tradeChangeCount) >= SAVE_THRESHOLD)
        {
            _ = Task.Run(() => SaveTrades());
            Interlocked.Exchange(ref _tradeChangeCount, 0);
        }
        
        return tradeResult;
    }

    public async Task<List<ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        var cutoff = timeSpan.HasValue 
            ? DateTimeOffset.UtcNow.Subtract(timeSpan.Value)
            : DateTimeOffset.UtcNow.AddHours(-1);
        
        return _opportunities.Values
            .Where(o => o.DetectedAt >= cutoff.DateTime)
            .OrderByDescending(o => o.DetectedAt)
            .Take(limit)
            .ToList();
    }

    public async Task<List<ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        return _opportunities.Values
            .Where(o => o.DetectedAt >= start.DateTime && o.DetectedAt <= end.DateTime)
            .OrderByDescending(o => o.DetectedAt)
            .Take(limit)
            .ToList();
    }

    public async Task<List<TradeResult>> GetRecentTradesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        var cutoff = timeSpan.HasValue 
            ? DateTimeOffset.UtcNow.Subtract(timeSpan.Value)
            : DateTimeOffset.UtcNow.AddHours(-1);
        
        return _trades.Values
            .Where(t => t.Timestamp >= cutoff)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToList();
    }

    public async Task<List<TradeResult>> GetTradesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        return _trades.Values
            .Where(t => t.Timestamp >= start && t.Timestamp <= end)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToList();
    }

    public async Task<TradeResult?> GetTradeByIdAsync(string id)
    {
        return _trades.TryGetValue(id, out var tradeResult) ? tradeResult : null;
    }

    public async Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId)
    {
        return _trades.Values
            .Where(t => t.OpportunityId == opportunityId)
            .OrderByDescending(t => t.Timestamp)
            .ToList();
    }

    public async Task<ArbitrageStatistics> GetCurrentDayStatisticsAsync()
    {
        var start = DateTimeOffset.UtcNow.Date;
        var end = start.AddDays(1).AddTicks(-1);
        
        return await GetStatisticsAsync(start, end);
    }

    public async Task<ArbitrageStatistics> GetLastDayStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddDays(-1);
        
        return await GetStatisticsAsync(start, end);
    }

    public async Task<ArbitrageStatistics> GetLastWeekStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddDays(-7);
        
        return await GetStatisticsAsync(start, end);
    }

    public async Task<ArbitrageStatistics> GetLastMonthStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddMonths(-1);
        
        return await GetStatisticsAsync(start, end);
    }

    public async Task<int> DeleteOldOpportunitiesAsync(DateTimeOffset olderThan)
    {
        var oldOpportunities = _opportunities.Values
            .Where(o => o.DetectedAt < olderThan.DateTime)
            .ToList();
        
        int count = 0;
        foreach (var opportunity in oldOpportunities)
        {
            if (_opportunities.TryRemove(opportunity.Id, out _))
            {
                count++;
            }
        }
        
        if (count > 0)
        {
            await SaveOpportunities();
        }
        
        return count;
    }

    public async Task<int> DeleteOldTradesAsync(DateTimeOffset olderThan)
    {
        var oldTrades = _trades.Values
            .Where(t => t.Timestamp < olderThan)
            .ToList();
        
        int count = 0;
        foreach (var trade in oldTrades)
        {
            if (_trades.TryRemove(trade.Id, out _))
            {
                count++;
            }
        }
        
        if (count > 0)
        {
            await SaveTrades();
        }
        
        return count;
    }

    #endregion

    private async Task SaveOpportunities()
    {
        try
        {
            var opportunities = _opportunities.Values.ToList();
            var json = JsonSerializer.Serialize(opportunities, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_opportunitiesFilePath, json);
            _logger.LogInformation("Saved {Count} opportunities to file", opportunities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving opportunities to file");
        }
    }

    private async Task SaveTrades()
    {
        try
        {
            var trades = _trades.Values.ToList();
            var json = JsonSerializer.Serialize(trades, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_tradesFilePath, json);
            _logger.LogInformation("Saved {Count} trades to file", trades.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving trades to file");
        }
    }

    private async Task SaveStatistics()
    {
        try
        {
            var statistics = _statistics.Values.ToList();
            var json = JsonSerializer.Serialize(statistics, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_statisticsFilePath, json);
            _logger.LogInformation("Saved {Count} statistics to file", statistics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving statistics to file");
        }
    }

    private void LoadData()
    {
        LoadOpportunities();
        LoadTrades();
        LoadStatistics();
    }

    private void LoadOpportunities()
    {
        try
        {
            if (File.Exists(_opportunitiesFilePath))
            {
                var json = File.ReadAllText(_opportunitiesFilePath);
                var opportunities = JsonSerializer.Deserialize<List<ArbitrageOpportunity>>(json);
                
                if (opportunities != null)
                {
                    foreach (var opportunity in opportunities)
                    {
                        _opportunities[opportunity.Id] = opportunity;
                    }
                    
                    _logger.LogInformation("Loaded {Count} opportunities from file", opportunities.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading opportunities from file");
        }
    }

    private void LoadTrades()
    {
        try
        {
            if (File.Exists(_tradesFilePath))
            {
                var json = File.ReadAllText(_tradesFilePath);
                var trades = JsonSerializer.Deserialize<List<TradeResult>>(json);
                
                if (trades != null)
                {
                    foreach (var trade in trades)
                    {
                        _trades[trade.Id] = trade;
                    }
                    
                    _logger.LogInformation("Loaded {Count} trades from file", trades.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trades from file");
        }
    }

    private void LoadStatistics()
    {
        try
        {
            if (File.Exists(_statisticsFilePath))
            {
                var json = File.ReadAllText(_statisticsFilePath);
                var statistics = JsonSerializer.Deserialize<List<ArbitrageStatistics>>(json);
                
                if (statistics != null)
                {
                    foreach (var stat in statistics)
                    {
                        _statistics[stat.StartTime] = stat;
                    }
                    
                    _logger.LogInformation("Loaded {Count} statistics from file", statistics.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading statistics from file");
        }
    }

    private string GenerateId()
    {
        return Guid.NewGuid().ToString("N");
    }
} 