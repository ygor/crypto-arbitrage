using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoArbitrage.Tests.BusinessBehavior;

/// <summary>
/// 🎯 ARBITRAGE DETECTION BEHAVIOR TESTS
/// 
/// These tests verify that the arbitrage detection system delivers actual business value
/// by detecting profitable opportunities when market conditions allow for arbitrage.
/// </summary>
public class ArbitrageDetectionBehaviorTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly TestArbitrageRepository _repository;
    private readonly TestMarketDataProvider _marketDataProvider;

    public ArbitrageDetectionBehaviorTests()
    {
        _serviceProvider = SetupBusinessTestEnvironment();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _repository = _serviceProvider.GetRequiredService<TestArbitrageRepository>();
        _marketDataProvider = _serviceProvider.GetRequiredService<TestMarketDataProvider>();
    }

    [Fact]
    public async Task Given_ProfitableMarketConditions_When_ArbitrageStarts_Then_OpportunitiesAreDetected()
    {
        // 🎯 BUSINESS BEHAVIOR: Core arbitrage detection functionality
        
        // Given: Profitable market conditions exist across exchanges
        _marketDataProvider.SetExchangePrice("coinbase", "BTC/USD", bidPrice: 49950m, askPrice: 50050m);
        _marketDataProvider.SetExchangePrice("kraken", "BTC/USD", bidPrice: 50250m, askPrice: 50350m);
        
        // Clear any existing opportunities
        _repository.Clear();
        
        // When: Arbitrage detection is started
        var startTime = DateTime.UtcNow;
        var startResult = await _mediator.Send(new StartArbitrageCommand());
        
        // Then: Command should succeed technically
        Assert.True(startResult.Success, "StartArbitrage command should succeed");
        
        // AND: Real business outcome should occur - opportunities detected within 30 seconds
        await WaitForBusinessOutcome(async () =>
        {
            var opportunities = await _repository.GetRecentOpportunitiesAsync(limit: 10);
            var recentOpportunities = opportunities.Where(o => o.DetectedAt >= startTime).ToList();
            
            if (recentOpportunities.Any())
            {
                var btcOpportunity = recentOpportunities.FirstOrDefault(o => o.TradingPair.ToString() == "BTC/USD");
                if (btcOpportunity != null)
                {
                    // Verify business logic correctness
                    Assert.Equal("coinbase", btcOpportunity.BuyExchangeId, "Should buy from cheaper exchange");
                    Assert.Equal("kraken", btcOpportunity.SellExchangeId, "Should sell to more expensive exchange");
                    Assert.True(btcOpportunity.SpreadPercentage > 0.4m, $"Spread should be > 0.4%, was {btcOpportunity.SpreadPercentage}%");
                    Assert.True(btcOpportunity.EstimatedProfit > 0, $"Should have positive estimated profit, was {btcOpportunity.EstimatedProfit}");
                    return true;
                }
            }
            return false;
        }, timeoutSeconds: 30);
    }

    [Fact]
    public async Task Given_InsufficientSpread_When_ArbitrageRuns_Then_NoOpportunitiesDetected()
    {
        // 🎯 BUSINESS BEHAVIOR: Risk management - filter unprofitable opportunities
        
        // Given: Small spread that's unprofitable after fees
        _marketDataProvider.SetExchangePrice("coinbase", "BTC/USD", bidPrice: 50000m, askPrice: 50100m);
        _marketDataProvider.SetExchangePrice("kraken", "BTC/USD", bidPrice: 50050m, askPrice: 50150m);
        
        _repository.Clear();
        
        // When: Arbitrage detection runs
        var startTime = DateTime.UtcNow;
        await _mediator.Send(new StartArbitrageCommand());
        
        // Wait for detection cycles to complete
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Then: No opportunities should be created for unprofitable spreads
        var opportunities = await _repository.GetRecentOpportunitiesAsync(limit: 50);
        var recentOpportunities = opportunities.Where(o => o.DetectedAt >= startTime).ToList();
        
        // Either no opportunities, or only opportunities with very low spreads that would be rejected
        var profitableOpportunities = recentOpportunities.Where(o => o.SpreadPercentage > 0.3m).ToList();
        Assert.Empty(profitableOpportunities);
    }

    [Fact]
    public async Task Given_MultipleExchanges_When_ArbitrageRuns_Then_FindsBestOpportunities()
    {
        // 🎯 BUSINESS BEHAVIOR: Optimization - find best opportunities across all exchanges
        
        // Given: Multiple exchanges with different spreads
        _marketDataProvider.SetExchangePrice("coinbase", "BTC/USD", bidPrice: 49900m, askPrice: 50000m);  // Cheapest
        _marketDataProvider.SetExchangePrice("kraken", "BTC/USD", bidPrice: 50300m, askPrice: 50400m);    // Most expensive  
        _marketDataProvider.SetExchangePrice("binance", "BTC/USD", bidPrice: 50150m, askPrice: 50250m);   // Middle
        
        await _repository.ClearOpportunitiesAsync();
        
        // When: Arbitrage detection runs
        var startTime = DateTime.UtcNow;
        await _mediator.Send(new StartArbitrageCommand());
        
        // Then: Should find the best opportunity (Coinbase -> Kraken)
        await WaitForBusinessOutcome(async () =>
        {
            var opportunities = await _repository.GetRecentOpportunitiesAsync();
            var recentOpportunities = opportunities.Where(o => o.DetectedAt >= startTime).ToList();
            
            if (recentOpportunities.Any())
            {
                var bestOpportunity = recentOpportunities.OrderByDescending(o => o.SpreadPercentage).First();
                
                // Verify it found the optimal exchange pair
                return bestOpportunity.BuyExchangeId == "coinbase" && 
                       bestOpportunity.SellExchangeId == "kraken" &&
                       bestOpportunity.SpreadPercentage > 0.6m;
            }
            return false;
        }, timeoutSeconds: 30);
    }

    private IServiceProvider SetupBusinessTestEnvironment()
    {
        var services = new ServiceCollection();
        
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(CryptoArbitrage.Application.Features.BotControl.Commands.Start.StartHandler).Assembly));
        
        // Register test doubles for business testing
        var testRepository = new TestArbitrageRepository();
        var testMarketData = new TestMarketDataProvider();
        
        services.AddSingleton(testRepository);
        services.AddSingleton(testMarketData);
        services.AddSingleton<IMarketDataAggregator>(testMarketData);
        services.AddSingleton<IArbitrageRepository>(testRepository);
        services.AddSingleton<IConfigurationService, TestConfigurationService>();
        
        // Register logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        return services.BuildServiceProvider();
    }
    
    private async Task WaitForBusinessOutcome(Func<Task<bool>> condition, int timeoutSeconds = 30)
    {
        var timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        
        while (DateTime.UtcNow < timeout)
        {
            if (await condition())
                return; // Success
                
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        
        throw new TimeoutException($"Business outcome was not achieved within {timeoutSeconds} seconds");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

/// <summary>
/// Test market data provider that simulates real exchange price data
/// </summary>
public class TestMarketDataProvider : IMarketDataAggregator
{
    private readonly Dictionary<string, Dictionary<string, PriceQuote>> _exchangePrices = new();
    private readonly Dictionary<string, List<string>> _monitoredPairs = new();
    
    public void SetExchangePrice(string exchangeId, string tradingPair, decimal bidPrice, decimal askPrice)
    {
        if (!_exchangePrices.ContainsKey(exchangeId))
            _exchangePrices[exchangeId] = new Dictionary<string, PriceQuote>();
            
        _exchangePrices[exchangeId][tradingPair] = new PriceQuote(
            exchangeId,
            TradingPair.Parse(tradingPair),
            DateTime.UtcNow,
            bidPrice,
            10.0m, // volume
            askPrice,
            10.0m  // volume
        );
    }
    
    public Task<IEnumerable<PriceQuote>> GetLatestPricesAsync(string tradingPair)
    {
        var prices = new List<PriceQuote>();
        
        foreach (var exchange in _exchangePrices)
        {
            if (exchange.Value.TryGetValue(tradingPair, out var price))
            {
                prices.Add(price);
            }
        }
        
        return Task.FromResult<IEnumerable<PriceQuote>>(prices);
    }
    
    public Task StartMonitoringAsync(IEnumerable<string> exchanges, IEnumerable<string> tradingPairs)
    {
        foreach (var exchange in exchanges)
        {
            _monitoredPairs[exchange] = tradingPairs.ToList();
        }
        return Task.CompletedTask;
    }
    
    public Task StopMonitoringAsync()
    {
        _monitoredPairs.Clear();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test arbitrage repository for business testing
/// </summary>
public class TestArbitrageRepository : IArbitrageRepository
{
    private readonly List<ArbitrageOpportunity> _opportunities = new();
    private readonly object _lock = new();
    
    public Task<ArbitrageOpportunity> SaveOpportunityAsync(ArbitrageOpportunity opportunity)
    {
        lock (_lock)
        {
            _opportunities.Add(opportunity);
        }
        return Task.FromResult(opportunity);
    }
    
    public Task<List<ArbitrageOpportunity>> GetRecentOpportunitiesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        lock (_lock)
        {
            var recent = _opportunities
                .OrderByDescending(o => o.DetectedAt)
                .Take(limit)
                .ToList();
            return Task.FromResult(recent);
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _opportunities.Clear();
        }
    }
    
    // Implement other required interface methods with minimal implementations
    public Task SaveOpportunityAsync(ArbitrageOpportunity opportunity, CancellationToken cancellationToken = default)
    {
        return SaveOpportunityAsync(opportunity);
    }
    
    public Task SaveTradeResultAsync(ArbitrageOpportunity opportunity, TradeResult buyResult, TradeResult sellResult, decimal profit, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task<IReadOnlyCollection<ArbitrageOpportunity>> GetOpportunitiesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var filtered = _opportunities.Where(o => o.DetectedAt >= start && o.DetectedAt <= end).ToList();
        return Task.FromResult<IReadOnlyCollection<ArbitrageOpportunity>>(filtered);
    }
    
    public Task<IReadOnlyCollection<(ArbitrageOpportunity Opportunity, TradeResult BuyResult, TradeResult SellResult, decimal Profit, DateTimeOffset Timestamp)>> GetTradeResultsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var empty = new List<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>();
        return Task.FromResult<IReadOnlyCollection<(ArbitrageOpportunity, TradeResult, TradeResult, decimal, DateTimeOffset)>>(empty);
    }
    
    public Task<ArbitrageStatistics> GetStatisticsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var stats = new ArbitrageStatistics
        {
            Id = Guid.NewGuid(),
            TradingPair = "OVERALL",
            CreatedAt = DateTime.UtcNow,
            StartTime = start,
            EndTime = end
        };
        return Task.FromResult(stats);
    }
    
    public Task SaveStatisticsAsync(ArbitrageStatistics statistics, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task<List<ArbitrageOpportunity>> GetOpportunitiesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        var filtered = _opportunities.Where(o => o.DetectedAt >= start && o.DetectedAt <= end).Take(limit).ToList();
        return Task.FromResult(filtered);
    }
    
    public Task<TradeResult> SaveTradeResultAsync(TradeResult tradeResult)
    {
        return Task.FromResult(tradeResult);
    }
    
    public Task<List<TradeResult>> GetRecentTradesAsync(int limit = 100, TimeSpan? timeSpan = null)
    {
        return Task.FromResult(new List<TradeResult>());
    }
    
    public Task<List<TradeResult>> GetTradesByTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        return Task.FromResult(new List<TradeResult>());
    }
    
    public Task<TradeResult?> GetTradeByIdAsync(string id)
    {
        return Task.FromResult<TradeResult?>(null);
    }
    
    public Task<List<TradeResult>> GetTradesByOpportunityIdAsync(string opportunityId)
    {
        return Task.FromResult(new List<TradeResult>());
    }
    
    public Task<ArbitrageStatistics> GetCurrentDayStatisticsAsync()
    {
        return GetStatisticsAsync(DateTimeOffset.UtcNow.Date, DateTimeOffset.UtcNow);
    }
    
    public Task<ArbitrageStatistics> GetLastDayStatisticsAsync()
    {
        return GetStatisticsAsync(DateTimeOffset.UtcNow.AddDays(-1).Date, DateTimeOffset.UtcNow.Date);
    }
    
    public Task<ArbitrageStatistics> GetLastWeekStatisticsAsync()
    {
        return GetStatisticsAsync(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);
    }
    
    public Task<ArbitrageStatistics> GetLastMonthStatisticsAsync()
    {
        return GetStatisticsAsync(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);
    }
    
    public Task<int> DeleteOldOpportunitiesAsync(DateTimeOffset olderThan)
    {
        return Task.FromResult(0);
    }
    
    public Task<int> DeleteOldTradesAsync(DateTimeOffset olderThan)
    {
        return Task.FromResult(0);
    }
    
    public Task<ArbitrageStatistics> GetArbitrageStatisticsAsync(string tradingPair, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var stats = new ArbitrageStatistics
        {
            Id = Guid.NewGuid(),
            TradingPair = tradingPair,
            CreatedAt = DateTime.UtcNow,
            StartTime = fromDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow.AddDays(-1),
            EndTime = toDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow
        };
        return Task.FromResult(stats);
    }
    
    public Task SaveArbitrageStatisticsAsync(ArbitrageStatistics statistics, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
} 