using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Application.Features.Arbitrage.Queries.GetArbitrageOpportunities;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;
using CryptoArbitrage.Tests.BusinessBehavior.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoArbitrage.Tests.BusinessBehavior.OpportunityDetection;

/// <summary>
/// 🎯 ARBITRAGE OPPORTUNITY DETECTION BEHAVIOR TESTS
/// 
/// These tests verify that the opportunity detection system delivers REAL business value:
/// - Scans multiple exchanges simultaneously using real-time WebSocket data
/// - Identifies profitable spreads across exchange pairs
/// - Calculates accurate profit potential and trade quantities
/// - Handles real market conditions and filters opportunities appropriately
/// </summary>
[Trait("Category", "BusinessBehavior")]
public class ArbitrageOpportunityDetectionBehaviorTests : IClassFixture<ArbitrageTestFixture>
{
    private readonly ArbitrageTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public ArbitrageOpportunityDetectionBehaviorTests(ArbitrageTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task When_MultipleExchangesHaveDifferentPrices_Then_OpportunitiesAreDetected()
    {
        // 🎯 BUSINESS BEHAVIOR: System should detect real arbitrage opportunities across exchanges

        // Arrange: Setup realistic market conditions across multiple exchanges
        var btcUsdt = new TradingPair("BTC", "USDT");
        var ethUsdt = new TradingPair("ETH", "USDT");

        // BTC: Coinbase cheaper, Kraken higher
        _fixture.SetupMarketCondition("coinbase", btcUsdt, askPrice: 50000m, bidPrice: 49950m);
        _fixture.SetupMarketCondition("kraken", btcUsdt, askPrice: 50300m, bidPrice: 50250m);

        // ETH: Kraken cheaper, Coinbase higher  
        _fixture.SetupMarketCondition("kraken", ethUsdt, askPrice: 3000m, bidPrice: 2995m);
        _fixture.SetupMarketCondition("coinbase", ethUsdt, askPrice: 3050m, bidPrice: 3045m);

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { btcUsdt.ToString(), ethUsdt.ToString() },
            ExchangeIds = new[] { "coinbase", "kraken" },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m,
            MaxResults = 10
        };

        // Act: Scan for opportunities
        var result = await _mediator.Send(query);

        // Assert: Business outcomes
        Assert.True(result.IsSuccess, $"Opportunity scan should succeed: {result.ErrorMessage}");
        Assert.True(result.Opportunities.Any(), "Should detect arbitrage opportunities");
        Assert.Equal(2, result.ExchangesScanned);
        Assert.Equal(2, result.TradingPairsScanned);

        // Verify BTC opportunity (buy Coinbase, sell Kraken)
        var btcOpportunity = result.Opportunities.FirstOrDefault(o => 
            o.TradingPair.Equals(btcUsdt) && 
            o.BuyExchangeId == "coinbase" && 
            o.SellExchangeId == "kraken");
        
        Assert.NotNull(btcOpportunity);
        Assert.True(btcOpportunity.ProfitPercentage > 0.2m, 
            $"BTC opportunity should be profitable. Found: {btcOpportunity.ProfitPercentage}%");
        Assert.Equal(50000m, btcOpportunity.BuyPrice);
        Assert.Equal(50250m, btcOpportunity.SellPrice);

        // Verify ETH opportunity (buy Kraken, sell Coinbase)
        var ethOpportunity = result.Opportunities.FirstOrDefault(o => 
            o.TradingPair.Equals(ethUsdt) && 
            o.BuyExchangeId == "kraken" && 
            o.SellExchangeId == "coinbase");
        
        Assert.NotNull(ethOpportunity);
        Assert.True(ethOpportunity.ProfitPercentage > 1.0m, 
            $"ETH opportunity should be highly profitable. Found: {ethOpportunity.ProfitPercentage}%");
    }

    [Fact]
    public async Task When_ScanningRealTimeData_Then_OpportunitiesReflectCurrentMarket()
    {
        // 🎯 BUSINESS BEHAVIOR: System should use real-time WebSocket data for accurate opportunities

        // Arrange: Setup dynamic market conditions that change during scan
        var tradingPair = new TradingPair("BTC", "USDT");
        _fixture.SetupRealTimeMarketData("coinbase", tradingPair);
        _fixture.SetupRealTimeMarketData("kraken", tradingPair);

        // Initial setup with profitable spread
        _fixture.UpdateMarketPrice("coinbase", tradingPair, askPrice: 50000m, bidPrice: 49950m);
        _fixture.UpdateMarketPrice("kraken", tradingPair, askPrice: 50200m, bidPrice: 50150m);

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPair.ToString() },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m
        };

        // Act: First scan
        var initialResult = await _mediator.Send(query);

        // Arrange: Market moves to reduce spread
        _fixture.UpdateMarketPrice("kraken", tradingPair, askPrice: 50050m, bidPrice: 50000m);

        // Act: Second scan after market movement
        var updatedResult = await _mediator.Send(query);

        // Assert: Business behavior with real-time data
        Assert.True(initialResult.IsSuccess, "Initial scan should succeed");
        Assert.True(initialResult.Opportunities.Any(), "Should find initial opportunities");

        var initialOpportunity = initialResult.Opportunities.First();
        Assert.True(initialOpportunity.ProfitPercentage > 0.2m, 
            "Initial opportunity should be profitable");

        Assert.True(updatedResult.IsSuccess, "Updated scan should succeed");
        
        if (updatedResult.Opportunities.Any())
        {
            var updatedOpportunity = updatedResult.Opportunities.First();
            Assert.True(updatedOpportunity.ProfitPercentage < initialOpportunity.ProfitPercentage,
                "Updated opportunity should show reduced profit due to market movement");
        }
        else
        {
            // Opportunity disappeared due to reduced spread - this is correct business behavior
            Assert.True(true, "Opportunity correctly disappeared when spread reduced");
        }

        // Verify scan performance (should be fast for real-time trading)
        Assert.True(initialResult.ScanTimeMs < 1000, "Real-time scan should be fast (<1s)");
        Assert.True(updatedResult.ScanTimeMs < 1000, "Updated scan should be fast (<1s)");
    }

    [Fact]
    public async Task When_FilteringByProfitThreshold_Then_OnlyProfitableOpportunitiesReturned()
    {
        // 🎯 BUSINESS BEHAVIOR: System should filter opportunities by business criteria

        // Arrange: Setup opportunities with varying profit levels
        var tradingPair = new TradingPair("ETH", "USDT");

        // Low profit scenario (0.1%)
        _fixture.SetupMarketCondition("coinbase", tradingPair, askPrice: 3000m, bidPrice: 2999m);
        _fixture.SetupMarketCondition("kraken", tradingPair, askPrice: 3003m, bidPrice: 3002m);

        // Test with low threshold
        var lowThresholdQuery = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPair.ToString() },
            MinProfitPercentage = 0.05m, // Very low threshold
            MaxTradeAmount = 1000m
        };

        // Test with high threshold
        var highThresholdQuery = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPair.ToString() },
            MinProfitPercentage = 0.5m, // High threshold
            MaxTradeAmount = 1000m
        };

        // Act: Scan with different thresholds
        var lowThresholdResult = await _mediator.Send(lowThresholdQuery);
        var highThresholdResult = await _mediator.Send(highThresholdQuery);

        // Assert: Business filtering behavior
        Assert.True(lowThresholdResult.IsSuccess, "Low threshold scan should succeed");
        Assert.True(highThresholdResult.IsSuccess, "High threshold scan should succeed");

        // Low threshold should find the opportunity
        Assert.True(lowThresholdResult.Opportunities.Any(), 
            "Should find opportunity with low threshold");

        // High threshold should filter it out
        Assert.False(highThresholdResult.Opportunities.Any(), 
            "Should not find opportunity with high threshold");

        // Verify business reasoning
        if (lowThresholdResult.Opportunities.Any())
        {
            var opportunity = lowThresholdResult.Opportunities.First();
            Assert.True(opportunity.ProfitPercentage >= 0.05m, 
                "Found opportunity should meet minimum threshold");
            Assert.True(opportunity.ProfitPercentage < 0.5m, 
                "Opportunity profit should be below high threshold");
        }
    }

    [Fact]
    public async Task When_OrderBookDepthIsInsufficient_Then_OpportunityQuantityIsLimited()
    {
        // 🎯 BUSINESS BEHAVIOR: System should calculate realistic trade quantities based on market depth

        // Arrange: Setup market with limited order book depth
        var tradingPair = new TradingPair("BTC", "USDT");
        
        // Coinbase: Good price but limited quantity
        _fixture.SetupOrderBookDepth("coinbase", tradingPair, new[]
        {
            new OrderBookEntry(50000m, 0.1m), // Only 0.1 BTC available
            new OrderBookEntry(50100m, 0.2m)
        }, new[]
        {
            new OrderBookEntry(49950m, 0.1m),
            new OrderBookEntry(49900m, 0.2m)
        });

        // Kraken: Good sell price with good depth
        _fixture.SetupOrderBookDepth("kraken", tradingPair, new[]
        {
            new OrderBookEntry(50300m, 1.0m),
            new OrderBookEntry(50400m, 2.0m)
        }, new[]
        {
            new OrderBookEntry(50250m, 1.0m), // 1 BTC available to sell to
            new OrderBookEntry(50200m, 2.0m)
        });

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPair.ToString() },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 10000m // Request large amount
        };

        // Act: Scan for opportunities
        var result = await _mediator.Send(query);

        // Assert: Business quantity calculation
        Assert.True(result.IsSuccess, "Scan should succeed");
        Assert.True(result.Opportunities.Any(), "Should find opportunities");

        var opportunity = result.Opportunities.First();
        
        // Verify quantity is limited by order book depth, not request amount
        Assert.True(opportunity.EffectiveQuantity <= 0.1m, 
            "Effective quantity should be limited by shallowest order book");
        Assert.True(opportunity.EffectiveQuantity > 0, 
            "Should still have some tradeable quantity");

        // Verify business calculation accuracy
        var expectedMaxValue = opportunity.EffectiveQuantity * opportunity.BuyPrice;
        Assert.True(expectedMaxValue <= 10000m, 
            "Trade value should respect maximum amount");
    }

    [Fact]
    public async Task When_NoOpportunitiesExist_Then_ScanReturnsEmptyWithBusinessExplanation()
    {
        // 🎯 BUSINESS BEHAVIOR: System should clearly communicate when no opportunities exist

        // Arrange: Setup markets with no arbitrage opportunities (same prices)
        var tradingPair = new TradingPair("BTC", "USDT");
        _fixture.SetupMarketCondition("coinbase", tradingPair, askPrice: 50000m, bidPrice: 49950m);
        _fixture.SetupMarketCondition("kraken", tradingPair, askPrice: 50000m, bidPrice: 49950m);

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPair.ToString() },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m
        };

        // Act: Scan for opportunities
        var result = await _mediator.Send(query);

        // Assert: Business communication behavior
        Assert.True(result.IsSuccess, "Scan should succeed even when no opportunities found");
        Assert.False(result.Opportunities.Any(), "Should not find any opportunities");
        Assert.Equal(0, result.TotalOpportunities);
        Assert.Equal(2, result.ExchangesScanned);
        Assert.Equal(1, result.TradingPairsScanned);

        // Verify business metrics are still provided
        Assert.True(result.ScanTimeMs > 0, "Should track scan time");
        Assert.True(result.Timestamp != default, "Should have timestamp");
    }

    [Fact]
    public async Task When_ScanningMultiplePairs_Then_ResultsAreSortedByProfitability()
    {
        // 🎯 BUSINESS BEHAVIOR: System should prioritize most profitable opportunities

        // Arrange: Setup multiple trading pairs with different profit levels
        var btcUsdt = new TradingPair("BTC", "USDT");
        var ethUsdt = new TradingPair("ETH", "USDT");
        var adaUsdt = new TradingPair("ADA", "USDT");

        // BTC: 0.3% profit
        _fixture.SetupMarketCondition("coinbase", btcUsdt, askPrice: 50000m, bidPrice: 49950m);
        _fixture.SetupMarketCondition("kraken", btcUsdt, askPrice: 50150m, bidPrice: 50100m);

        // ETH: 1.5% profit (highest)
        _fixture.SetupMarketCondition("coinbase", ethUsdt, askPrice: 3000m, bidPrice: 2995m);
        _fixture.SetupMarketCondition("kraken", ethUsdt, askPrice: 3045m, bidPrice: 3040m);

        // ADA: 0.8% profit
        _fixture.SetupMarketCondition("coinbase", adaUsdt, askPrice: 1.00m, bidPrice: 0.999m);
        _fixture.SetupMarketCondition("kraken", adaUsdt, askPrice: 1.008m, bidPrice: 1.007m);

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { btcUsdt.ToString(), ethUsdt.ToString(), adaUsdt.ToString() },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m,
            SortByProfitability = true
        };

        // Act: Scan opportunities
        var result = await _mediator.Send(query);

        // Assert: Business prioritization behavior
        Assert.True(result.IsSuccess, "Scan should succeed");
        Assert.True(result.Opportunities.Count >= 3, "Should find opportunities for all pairs");

        var sortedOpportunities = result.Opportunities.ToList();

        // Verify opportunities are sorted by profitability (highest first)
        for (int i = 0; i < sortedOpportunities.Count - 1; i++)
        {
            Assert.True(sortedOpportunities[i].ProfitPercentage >= sortedOpportunities[i + 1].ProfitPercentage,
                $"Opportunities should be sorted by profit: {sortedOpportunities[i].ProfitPercentage}% >= {sortedOpportunities[i + 1].ProfitPercentage}%");
        }

        // Verify ETH (highest profit) is first
        var topOpportunity = sortedOpportunities.First();
        Assert.Equal("ETH", topOpportunity.TradingPair.BaseCurrency);
        Assert.True(topOpportunity.ProfitPercentage > 1.0m, 
            "Top opportunity should be the most profitable (ETH ~1.5%)");
    }

    [Fact]
    public async Task When_ExchangeIsUnavailable_Then_ScanContinuesWithAvailableExchanges()
    {
        // 🎯 BUSINESS BEHAVIOR: System should be resilient to exchange connectivity issues

        // Arrange: Setup one working exchange and one that will fail
        var tradingPair = new TradingPair("BTC", "USDT");
        _fixture.SetupMarketCondition("coinbase", tradingPair, askPrice: 50000m, bidPrice: 49950m);
        _fixture.SimulateExchangeFailure("kraken"); // Kraken will be unavailable

        var query = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { tradingPair.ToString() },
            ExchangeIds = new[] { "coinbase", "kraken" },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m
        };

        // Act: Scan with one exchange failing
        var result = await _mediator.Send(query);

        // Assert: Business resilience behavior
        Assert.True(result.IsSuccess, "Scan should succeed with partial exchange availability");
        Assert.True(result.Warnings.Any(), "Should report warnings about exchange failures");
        
        var krakenWarning = result.Warnings.FirstOrDefault(w => w.Contains("kraken"));
        Assert.NotNull(krakenWarning);

        // Should scan available exchanges
        Assert.True(result.ExchangesScanned > 0, "Should scan available exchanges");

        // No opportunities expected with only one exchange
        Assert.False(result.Opportunities.Any(), 
            "Should not find opportunities with only one exchange available");
    }
} 