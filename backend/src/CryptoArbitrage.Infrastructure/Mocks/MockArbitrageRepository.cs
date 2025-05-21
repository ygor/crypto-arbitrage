using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Infrastructure.Mocks;

/// <summary>
/// A mock implementation of IArbitrageRepository for testing purposes.
/// </summary>
public class MockArbitrageRepository : IArbitrageRepository
{
    private readonly ConcurrentDictionary<string, ArbitrageOpportunity> _opportunities = new();
    private readonly ConcurrentDictionary<string, TradeResult> _trades = new();
    private readonly ConcurrentDictionary<DateTimeOffset, ArbitrageStatistics> _statistics = new();

    public Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opportunity.Id))
        {
            opportunity.Id = Guid.NewGuid().ToString();
        }
        
        _opportunities[opportunity.Id] = opportunity;
        return Task.CompletedTask;
    }

    public Task SaveTradeResultAsync(ArbitrageOpportunity opportunity, TradeResult buyResult, TradeResult sellResult, decimal profit, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
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
            ProfitAmount = profit,
            ProfitPercentage = (profit / (opportunity.BuyPrice * opportunity.EffectiveQuantity)) * 100m,
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
        
        _trades[tradeResult.Id.ToString()] = tradeResult;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var result = _opportunities.Values
            .Where(o => o.DetectedAt >= start && o.DetectedAt <= end)
            .ToList();
            
        return Task.FromResult<IReadOnlyCollection<ArbitrageOpportunity>>(result);
    }

    public Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var result = new List<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>();
        
        foreach (var trade in _trades.Values.Where(t => t.Timestamp >= start && t.Timestamp <= end))
        {
            if (_opportunities.TryGetValue(trade.OpportunityId.ToString(), out var opportunity))
            {
                if (trade.BuyResult != null && trade.SellResult != null)
                {
                    // Convert BuyTradeResult to TradeResult
                    var buyTradeResult = new TradeResult
                    {
                        OrderId = trade.BuyResult.OrderId,
                        ClientOrderId = trade.BuyResult.ClientOrderId,
                        RequestedPrice = trade.BuyResult.Price,
                        ExecutedPrice = trade.BuyResult.AverageFillPrice,
                        RequestedQuantity = trade.BuyResult.Quantity,
                        ExecutedQuantity = trade.BuyResult.FilledQuantity,
                        TotalValue = trade.BuyResult.AverageFillPrice * trade.BuyResult.FilledQuantity,
                        Fee = trade.BuyResult.FeeAmount,
                        FeeCurrency = trade.BuyResult.FeeCurrency,
                        Timestamp = trade.BuyResult.Timestamp,
                        IsSuccess = trade.BuyResult.Status == OrderStatus.Filled,
                        ErrorMessage = trade.BuyResult.ErrorMessage,
                        TradeType = TradeType.Buy
                    };
                    
                    // Convert SellTradeResult to TradeResult
                    var sellTradeResult = new TradeResult
                    {
                        OrderId = trade.SellResult.OrderId,
                        ClientOrderId = trade.SellResult.ClientOrderId,
                        RequestedPrice = trade.SellResult.Price,
                        ExecutedPrice = trade.SellResult.AverageFillPrice,
                        RequestedQuantity = trade.SellResult.Quantity,
                        ExecutedQuantity = trade.SellResult.FilledQuantity,
                        TotalValue = trade.SellResult.AverageFillPrice * trade.SellResult.FilledQuantity,
                        Fee = trade.SellResult.FeeAmount,
                        FeeCurrency = trade.SellResult.FeeCurrency,
                        Timestamp = trade.SellResult.Timestamp,
                        IsSuccess = trade.SellResult.Status == OrderStatus.Filled,
                        ErrorMessage = trade.SellResult.ErrorMessage,
                        TradeType = TradeType.Sell
                    };
                    
                    result.Add((opportunity, buyTradeResult, sellTradeResult, trade.ProfitAmount, trade.Timestamp));
                }
            }
        }
        
        return Task.FromResult<IReadOnlyCollection<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>>(result);
    }

    public Task<ArbitrageStatistics> GetStatisticsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var stats = new ArbitrageStatistics
        {
            StartTime = start,
            EndTime = end,
            TotalOpportunitiesCount = _opportunities.Count,
            TotalTradesCount = _trades.Count,
            SuccessfulTradesCount = _trades.Values.Count(t => t.Status == TradeStatus.Completed),
            FailedTradesCount = _trades.Values.Count(t => t.Status == TradeStatus.Failed),
            TotalProfitAmount = _trades.Values.Sum(t => t.ProfitAmount)
        };
        
        return Task.FromResult(stats);
    }

    public Task SaveStatisticsAsync(ArbitrageStatistics statistics, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        _statistics[timestamp] = statistics;
        return Task.CompletedTask;
    }

    public Task<ArbitrageOpportunity> SaveOpportunityAsync(ArbitrageOpportunity opportunity)
    {
        if (string.IsNullOrWhiteSpace(opportunity.Id))
        {
            opportunity.Id = Guid.NewGuid().ToString();
        }
        
        _opportunities[opportunity.Id] = opportunity;
        return Task.FromResult(opportunity);
    }

    public Task<List<ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        var cutoff = timeSpan.HasValue 
            ? DateTimeOffset.UtcNow.Subtract(timeSpan.Value)
            : DateTimeOffset.UtcNow.AddDays(-1);
            
        var result = _opportunities.Values
            .Where(o => o.DetectedAt >= cutoff.DateTime)
            .OrderByDescending(o => o.DetectedAt)
            .Take(limit)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<List<ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        var result = _opportunities.Values
            .Where(o => o.DetectedAt >= start.DateTime && o.DetectedAt <= end.DateTime)
            .OrderByDescending(o => o.DetectedAt)
            .Take(limit)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<TradeResult> SaveTradeResultAsync(TradeResult tradeResult)
    {
        if (tradeResult.Id == Guid.Empty)
        {
            tradeResult.Id = Guid.NewGuid();
        }
        
        _trades[tradeResult.Id.ToString()] = tradeResult;
        return Task.FromResult(tradeResult);
    }

    public Task<List<TradeResult>> GetRecentTradesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        var cutoff = timeSpan.HasValue 
            ? DateTimeOffset.UtcNow.Subtract(timeSpan.Value)
            : DateTimeOffset.UtcNow.AddDays(-1);
            
        var result = _trades.Values
            .Where(t => t.Timestamp >= cutoff)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<List<TradeResult>> GetTradesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        var result = _trades.Values
            .Where(t => t.Timestamp >= start && t.Timestamp <= end)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<TradeResult?> GetTradeByIdAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            return Task.FromResult(_trades.TryGetValue(guidId.ToString(), out var tradeResult) ? tradeResult : null);
        }
        return Task.FromResult<TradeResult?>(null);
    }

    public Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId)
    {
        var result = _trades.Values
            .Where(t => t.OpportunityId.ToString() == opportunityId)
            .OrderByDescending(t => t.Timestamp)
            .ToList();
            
        return Task.FromResult(result);
    }

    public Task<ArbitrageStatistics> GetCurrentDayStatisticsAsync()
    {
        var start = DateTimeOffset.UtcNow.Date;
        var end = start.AddDays(1).AddTicks(-1);
        
        return GetStatisticsAsync(start, end);
    }

    public Task<ArbitrageStatistics> GetLastDayStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddDays(-1);
        
        return GetStatisticsAsync(start, end);
    }

    public Task<ArbitrageStatistics> GetLastWeekStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddDays(-7);
        
        return GetStatisticsAsync(start, end);
    }

    public Task<ArbitrageStatistics> GetLastMonthStatisticsAsync()
    {
        var end = DateTimeOffset.UtcNow.Date;
        var start = end.AddMonths(-1);
        
        return GetStatisticsAsync(start, end);
    }

    public Task<int> DeleteOldOpportunitiesAsync(DateTimeOffset olderThan)
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
        
        return Task.FromResult(count);
    }

    public Task<int> DeleteOldTradesAsync(DateTimeOffset olderThan)
    {
        var oldTrades = _trades.Values
            .Where(t => t.Timestamp < olderThan)
            .ToList();
        
        int count = 0;
        foreach (var trade in oldTrades)
        {
            if (_trades.TryRemove(trade.Id.ToString(), out _))
            {
                count++;
            }
        }
        
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<ArbitrageStatistics> GetArbitrageStatisticsAsync(string tradingPair, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        // Try to find statistics for the specified trading pair
        var stats = _statistics.Values
            .Where(s => s.TradingPair == tradingPair)
            .FirstOrDefault();
            
        if (stats == null)
        {
            // Create new statistics object if none exists
            stats = new ArbitrageStatistics
            {
                TradingPair = tradingPair,
                CreatedAt = DateTime.UtcNow,
                StartTime = startDate.HasValue ? new DateTimeOffset(startDate.Value) : DateTimeOffset.UtcNow.AddDays(-1),
                EndTime = endDate.HasValue ? new DateTimeOffset(endDate.Value) : DateTimeOffset.UtcNow
            };
        }
        
        return Task.FromResult(stats);
    }
    
    /// <inheritdoc />
    public Task SaveArbitrageStatisticsAsync(ArbitrageStatistics statistics, CancellationToken cancellationToken = default)
    {
        // Update LastUpdatedAt timestamp
        statistics.LastUpdatedAt = DateTime.UtcNow;
        
        // Store in the dictionary using the timestamp as key
        _statistics[DateTimeOffset.UtcNow] = statistics;
        
        return Task.CompletedTask;
    }

    private string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }
} 