using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using CryptoArbitrage.Application.Features.Arbitrage.Commands.ExecuteArbitrageOpportunity;
using CryptoArbitrage.Application.Features.Arbitrage.Queries.GetArbitrageOpportunities;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.BusinessBehavior.Infrastructure;
using System.Text.Json;

namespace CryptoArbitrage.Tests.BusinessBehavior.Integration;

/// <summary>
/// 🎯 ARBITRAGE API INTEGRATION BEHAVIOR TESTS
/// 
/// These tests verify that the arbitrage API delivers REAL business value through HTTP endpoints:
/// - API endpoints correctly execute business operations
/// - Users can scan for opportunities via REST API
/// - Users can execute arbitrage trades via API calls
/// - API responses contain meaningful business information
/// - Error handling provides clear business feedback
/// </summary>
[Trait("Category", "BusinessBehavior")]
[Trait("Category", "Integration")]
public class ArbitrageApiIntegrationBehaviorTests : IClassFixture<ArbitrageWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ArbitrageWebApplicationFactory _factory;

    public ArbitrageApiIntegrationBehaviorTests(ArbitrageWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task When_ScanningOpportunitiesViaAPI_Then_RealArbitrageOpportunitiesReturned()
    {
        // 🎯 BUSINESS BEHAVIOR: API should expose real arbitrage scanning functionality

        // Arrange: Setup market conditions with profitable spreads
        _factory.SetupMarketCondition("coinbase", new TradingPair("BTC", "USDT"), askPrice: 50000m, bidPrice: 49950m);
        _factory.SetupMarketCondition("kraken", new TradingPair("BTC", "USDT"), askPrice: 50300m, bidPrice: 50250m);

        var scanRequest = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { new TradingPair("BTC", "USDT").ToString() },
            ExchangeIds = new[] { "coinbase", "kraken" },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m,
            MaxResults = 10
        };

        // Act: Call API endpoint to scan for opportunities
        var response = await _client.PostAsJsonAsync("/api/arbitrage/scan", scanRequest);

        // Assert: Business outcomes via API
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetArbitrageOpportunitiesResult>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.NotNull(result);
        Assert.True(result.IsSuccess, "API should return successful scan result");
        Assert.True(result.Opportunities.Any(), "API should return detected opportunities");
        Assert.Equal(2, result.ExchangesScanned);
        Assert.True(result.ScanTimeMs > 0, "API should report scan performance");

        // Verify business data quality in API response
        var opportunity = result.Opportunities.First();
        Assert.True(opportunity.ProfitPercentage > 0.1m, "API should return profitable opportunities");
        Assert.Equal("BTC", opportunity.TradingPair.BaseCurrency);
        Assert.Equal("USDT", opportunity.TradingPair.QuoteCurrency);
        Assert.True(opportunity.EffectiveQuantity > 0, "API should calculate tradeable quantities");
    }

    [Fact]
    public async Task When_QuickScanningSpecificPairViaAPI_Then_BestOpportunityReturned()
    {
        // 🎯 BUSINESS BEHAVIOR: API should provide convenient quick scan functionality

        // Arrange: Setup multiple profitable opportunities with different profit levels
        _factory.SetupMarketCondition("coinbase", new TradingPair("ETH", "USDT"), askPrice: 3000m, bidPrice: 2995m);
        _factory.SetupMarketCondition("kraken", new TradingPair("ETH", "USDT"), askPrice: 3045m, bidPrice: 3040m);

        // Act: Call quick scan API endpoint
        var response = await _client.GetAsync("/api/arbitrage/scan/ETH/USDT?minProfitPercentage=0.1&maxTradeAmount=500");

        // Assert: Business convenience via API
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(result.TryGetProperty("tradingPair", out var tradingPairElement));
        Assert.Equal("ETH-USDT", tradingPairElement.GetString());

        Assert.True(result.TryGetProperty("opportunity", out var opportunityElement));
        Assert.True(opportunityElement.TryGetProperty("profitPercentage", out var profitElement));
        
        var profit = profitElement.GetDecimal();
        Assert.True(profit > 1.0m, "Quick scan should return highly profitable ETH opportunity");

        Assert.True(result.TryGetProperty("scanMetrics", out var metricsElement));
        Assert.True(metricsElement.TryGetProperty("scanTimeMs", out var timeElement));
        Assert.True(timeElement.GetInt64() > 0, "API should report scan performance metrics");
    }

    [Fact]
    public async Task When_ExecutingArbitrageViaAPI_Then_TradesExecutedWithRealProfit()
    {
        // 🎯 BUSINESS BEHAVIOR: API should enable real arbitrage execution

        // Arrange: Setup profitable arbitrage opportunity
        _factory.SetupMarketCondition("coinbase", new TradingPair("BTC", "USDT"), askPrice: 50000m, bidPrice: 49950m);
        _factory.SetupMarketCondition("kraken", new TradingPair("BTC", "USDT"), askPrice: 50200m, bidPrice: 50150m);
        _factory.EnablePaperTrading(); // Safe execution for testing

        var executeRequest = new ExecuteArbitrageOpportunityCommand
        {
            TradingPair = new TradingPair("BTC", "USDT"),
            BuyExchangeId = "coinbase",
            SellExchangeId = "kraken",
            MaxTradeAmount = 500m,
            MinProfitPercentage = 0.1m,
            AutoExecute = true
        };

        // Act: Call API endpoint to execute arbitrage
        var response = await _client.PostAsJsonAsync("/api/arbitrage/execute", executeRequest);

        // Assert: Business execution via API
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ExecuteArbitrageOpportunityResult>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"API execution should succeed: {result.ErrorMessage}");
        Assert.True(result.WasExecuted, "API should confirm trades were executed");
        Assert.NotNull(result.BuyTradeResult);
        Assert.NotNull(result.SellTradeResult);

        // Verify business outcomes via API
        Assert.True(result.BuyTradeResult.IsSuccess, "Buy trade should succeed via API");
        Assert.True(result.SellTradeResult.IsSuccess, "Sell trade should succeed via API");
        Assert.True(result.RealizedProfit > 0, "API should return actual profit realized");
        Assert.True(result.ProfitPercentage > 0, "API should calculate profit percentage");
        Assert.True(result.ExecutionTimeMs > 0, "API should report execution performance");
    }

    [Fact]
    public async Task When_AnalyzingOpportunityViaAPI_Then_BusinessInsightsProvided()
    {
        // 🎯 BUSINESS BEHAVIOR: API should provide analysis without execution

        // Arrange: Setup opportunity for analysis
        _factory.SetupMarketCondition("coinbase", new TradingPair("ETH", "USDT"), askPrice: 3000m, bidPrice: 2995m);
        _factory.SetupMarketCondition("kraken", new TradingPair("ETH", "USDT"), askPrice: 3020m, bidPrice: 3015m);

        var analyzeRequest = new ExecuteArbitrageOpportunityCommand
        {
            TradingPair = new TradingPair("ETH", "USDT"),
            BuyExchangeId = "coinbase",
            SellExchangeId = "kraken",
            MaxTradeAmount = 1000m,
            MinProfitPercentage = 0.1m,
            AutoExecute = false // Analysis only
        };

        // Act: Call API endpoint to analyze opportunity
        var response = await _client.PostAsJsonAsync("/api/arbitrage/analyze", analyzeRequest);

        // Assert: Business analysis via API
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ExecuteArbitrageOpportunityResult>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.NotNull(result);
        Assert.True(result.IsSuccess, "API analysis should succeed");
        Assert.False(result.WasExecuted, "API should only analyze, not execute");
        Assert.NotNull(result.Opportunity);

        // Verify business insights via API
        Assert.True(result.ProfitPercentage > 0.1m, "API should provide profit analysis");
        Assert.Equal("coinbase", result.Opportunity.BuyExchangeId);
        Assert.Equal("kraken", result.Opportunity.SellExchangeId);
        Assert.True(result.Opportunity.EffectiveQuantity > 0, "API should calculate tradeable quantity");
        Assert.True(result.ExecutionTimeMs > 0, "API should report analysis time");
    }

    [Fact]
    public async Task When_RequestingMultipleOpportunitiesViaAPI_Then_SortedByProfitability()
    {
        // 🎯 BUSINESS BEHAVIOR: API should prioritize opportunities by business value

        // Arrange: Setup multiple opportunities with different profit levels
        _factory.SetupMarketCondition("coinbase", new TradingPair("BTC", "USDT"), askPrice: 50000m, bidPrice: 49950m);
        _factory.SetupMarketCondition("kraken", new TradingPair("BTC", "USDT"), askPrice: 50150m, bidPrice: 50100m);

        _factory.SetupMarketCondition("coinbase", new TradingPair("ETH", "USDT"), askPrice: 3000m, bidPrice: 2995m);
        _factory.SetupMarketCondition("kraken", new TradingPair("ETH", "USDT"), askPrice: 3045m, bidPrice: 3040m);

        // Act: Call API to get multiple opportunities
        var response = await _client.GetAsync("/api/arbitrage/opportunities?pairs=BTC-USDT,ETH-USDT&exchanges=coinbase,kraken&minProfit=0.1&limit=10");

        // Assert: Business prioritization via API
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetArbitrageOpportunitiesResult>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.NotNull(result);
        Assert.True(result.IsSuccess, "API should return opportunities");
        Assert.True(result.Opportunities.Count >= 2, "API should return multiple opportunities");

        // Verify sorting by profitability (highest first)
        var opportunities = result.Opportunities.ToList();
        for (int i = 0; i < opportunities.Count - 1; i++)
        {
            Assert.True(opportunities[i].ProfitPercentage >= opportunities[i + 1].ProfitPercentage,
                "API should sort opportunities by profit percentage");
        }

        // ETH should be first (higher profit ~1.5% vs BTC ~0.3%)
        var topOpportunity = opportunities.First();
        Assert.Equal("ETH", topOpportunity.TradingPair.BaseCurrency);
    }

    [Fact]
    public async Task When_NoOpportunitiesExistViaAPI_Then_ClearBusinessExplanation()
    {
        // 🎯 BUSINESS BEHAVIOR: API should clearly communicate when no opportunities exist

        // Arrange: Setup markets with no arbitrage opportunities (same prices)
        _factory.SetupMarketCondition("coinbase", new TradingPair("BTC", "USDT"), askPrice: 50000m, bidPrice: 49950m);
        _factory.SetupMarketCondition("kraken", new TradingPair("BTC", "USDT"), askPrice: 50000m, bidPrice: 49950m);

        // Act: Call API quick scan for pair with no opportunities
        var response = await _client.GetAsync("/api/arbitrage/scan/BTC/USDT?minProfitPercentage=0.1");

        // Assert: Business communication via API
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(result.TryGetProperty("message", out var messageElement));
        var message = messageElement.GetString();
        Assert.Contains("No profitable arbitrage opportunities found", message);
        Assert.Contains("BTC-USDT", message);
    }

    [Fact]
    public async Task When_InvalidRequestViaAPI_Then_BusinessValidationErrors()
    {
        // 🎯 BUSINESS BEHAVIOR: API should validate business rules and provide clear errors

        // Arrange: Invalid arbitrage request (same exchange for buy and sell)
        var invalidRequest = new ExecuteArbitrageOpportunityCommand
        {
            TradingPair = new TradingPair("BTC", "USDT"),
            BuyExchangeId = "coinbase",
            SellExchangeId = "coinbase", // Same exchange - invalid for arbitrage
            MaxTradeAmount = 1000m,
            MinProfitPercentage = 0.1m,
            AutoExecute = true
        };

        // Act: Call API with invalid request
        var response = await _client.PostAsJsonAsync("/api/arbitrage/execute", invalidRequest);

        // Assert: Business validation via API
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        
        // The response should contain business validation errors
        Assert.Contains("different", content.ToLower());
    }

    [Fact]
    public async Task When_ExchangeUnavailableViaAPI_Then_GracefulDegradationWithWarnings()
    {
        // 🎯 BUSINESS BEHAVIOR: API should handle exchange failures gracefully

        // Arrange: Setup one working exchange and simulate failure of another
        _factory.SetupMarketCondition("coinbase", new TradingPair("BTC", "USDT"), askPrice: 50000m, bidPrice: 49950m);
        _factory.SimulateExchangeFailure("kraken");

        var scanRequest = new GetArbitrageOpportunitiesQuery
        {
            TradingPairs = new[] { new TradingPair("BTC", "USDT").ToString() },
            ExchangeIds = new[] { "coinbase", "kraken" },
            MinProfitPercentage = 0.1m,
            MaxTradeAmount = 1000m
        };

        // Act: Call API with partially failing exchanges
        var response = await _client.PostAsJsonAsync("/api/arbitrage/scan", scanRequest);

        // Assert: Business resilience via API
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetArbitrageOpportunitiesResult>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        Assert.NotNull(result);
        Assert.True(result.IsSuccess, "API should succeed with partial exchange availability");
        Assert.True(result.Warnings.Any(), "API should report exchange failures");
        
        // Should report the exchange failure
        var krakenWarning = result.Warnings.FirstOrDefault(w => w.Contains("kraken"));
        Assert.NotNull(krakenWarning);

        // No opportunities expected with only one exchange
        Assert.False(result.Opportunities.Any(), "Should not find opportunities with only one exchange");
        Assert.True(result.ExchangesScanned > 0, "Should scan available exchanges");
    }
} 