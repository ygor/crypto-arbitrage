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
            Id = Guid.NewGuid().ToString(),
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
        
        _trades[tradeResult.Id] = tradeResult;
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
            if (_opportunities.TryGetValue(trade.OpportunityId, out var opportunity))
            {
                if (trade.BuyResult != null && trade.SellResult != null)
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
                        TradeType = TradeType.Buy
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
        if (string.IsNullOrWhiteSpace(tradeResult.Id))
        {
            tradeResult.Id = Guid.NewGuid().ToString();
        }
        
        _trades[tradeResult.Id] = tradeResult;
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
        return _trades.TryGetValue(id, out var trade) 
            ? Task.FromResult<TradeResult?>(trade) 
            : Task.FromResult<TradeResult?>(null);
    }

    public Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId)
    {
        var result = _trades.Values
            .Where(t => t.OpportunityId == opportunityId)
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
            if (_trades.TryRemove(trade.Id, out _))
            {
                count++;
            }
        }
        
        return Task.FromResult(count);
    }
} 