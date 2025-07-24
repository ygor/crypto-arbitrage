using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace CryptoArbitrage.UI.Tests.Infrastructure;

/// <summary>
/// 🎯 UI TEST INFRASTRUCTURE SETUP
/// 
/// Provides common setup and utilities for UI testing, including mock services
/// and test data generation for consistent testing across different test types.
/// </summary>
public static class TestInfrastructureSetup
{
    /// <summary>
    /// Creates a mock IArbitrageRepository with test data
    /// </summary>
    public static Mock<IArbitrageRepository> CreateMockRepository(
        List<ArbitrageOpportunity>? opportunities = null,
        List<TradeResult>? trades = null)
    {
        var mock = new Mock<IArbitrageRepository>();

        // Setup opportunities
        var testOpportunities = opportunities ?? CreateTestOpportunities();
        mock.Setup(r => r.GetRecentOpportunitiesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(testOpportunities);
        
        mock.Setup(r => r.GetOpportunitiesByTimeRangeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
            .ReturnsAsync(testOpportunities);

        // Setup trades  
        var testTrades = trades ?? CreateTestTrades();
        mock.Setup(r => r.GetTradesByTimeRangeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(testTrades.Select(t => (
                Opportunity: testOpportunities.FirstOrDefault() ?? CreateTestOpportunity(),
                BuyResult: t,
                SellResult: t,
                Profit: t.ProfitAmount,
                Timestamp: new DateTimeOffset(t.Timestamp)
            )).ToList());

        return mock;
    }

    /// <summary>
    /// Creates a mock IConfigurationService
    /// </summary>
    public static Mock<IConfigurationService> CreateMockConfigurationService()
    {
        var mock = new Mock<IConfigurationService>();
        
        mock.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestConfiguration());
            
        mock.Setup(c => c.GetRiskProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRiskProfile());

        return mock;
    }

    /// <summary>
    /// Creates test arbitrage opportunities
    /// </summary>
    public static List<ArbitrageOpportunity> CreateTestOpportunities()
    {
        return new List<ArbitrageOpportunity>
        {
            new ArbitrageOpportunity(
                TradingPair.BTCUSD,
                "coinbase",
                50000m,
                1.5m,
                "kraken",
                50300m,
                1.2m
            ),
            new ArbitrageOpportunity(
                TradingPair.ETHUSD,
                "binance", 
                3000m,
                2.0m,
                "coinbase",
                3050m,
                1.8m
            )
        };
    }

    /// <summary>
    /// Creates a single test opportunity
    /// </summary>
    public static ArbitrageOpportunity CreateTestOpportunity()
    {
        return new ArbitrageOpportunity(
            TradingPair.BTCUSD,
            "coinbase",
            50000m,
            1.0m,
            "kraken",
            50200m,
            1.0m
        );
    }

    /// <summary>
    /// Creates test trade results
    /// </summary>
    public static List<TradeResult> CreateTestTrades()
    {
        return new List<TradeResult>
        {
            new TradeResult
            {
                Id = Guid.NewGuid(),
                TradingPair = "BTC/USD",
                BuyExchangeId = "coinbase",
                SellExchangeId = "kraken",
                Quantity = 0.1m,
                ProfitAmount = 30.50m,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Status = TradeStatus.Completed,
                IsSuccess = true
            },
            new TradeResult
            {
                Id = Guid.NewGuid(),
                TradingPair = "ETH/USD",
                BuyExchangeId = "binance",
                SellExchangeId = "coinbase",
                Quantity = 2.0m,
                ProfitAmount = 45.75m,
                Timestamp = DateTime.UtcNow.AddHours(-4),
                Status = TradeStatus.Completed,
                IsSuccess = true
            }
        };
    }

    /// <summary>
    /// Creates test statistics
    /// </summary>
    public static ArbitrageStatistics CreateTestStatistics()
    {
        return new ArbitrageStatistics
        {
            Id = Guid.NewGuid(),
            TradingPair = "OVERALL",
            CreatedAt = DateTime.UtcNow,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow,
            TotalOpportunitiesCount = 125,
            QualifiedOpportunitiesCount = 38,
            TotalTradesCount = 25,
            SuccessfulTradesCount = 22,
            FailedTradesCount = 3,
            TotalProfitAmount = 856.25m,
            AverageProfitAmount = 38.92m,
            HighestProfitAmount = 125.50m,
            LowestProfit = 12.75m,
            AverageExecutionTimeMs = 234.6m,
            TotalFeesAmount = 48.30m,
            TotalVolume = 15000.00m,
            AverageProfitPercentage = 1.65m
        };
    }

    /// <summary>
    /// Creates test arbitrage configuration
    /// </summary>
    public static ArbitrageConfiguration CreateTestConfiguration()
    {
        return new ArbitrageConfiguration
        {
            IsEnabled = true,
            PaperTradingEnabled = true,
            MinProfitPercentage = 0.5m,
            MaxTradeAmount = 1000m,
            MaxConcurrentTrades = 3,
            PollingIntervalMs = 5000,
            TradingPairs = new List<TradingPair> { TradingPair.BTCUSD, TradingPair.ETHUSD },
            EnabledExchanges = new List<string> { "coinbase", "kraken", "binance" }
        };
    }

    /// <summary>
    /// Creates test risk profile
    /// </summary>
    public static RiskProfile CreateTestRiskProfile()
    {
        return new RiskProfile
        {
            Name = "Test Profile",
            Type = "Balanced",
            MinProfitPercentage = 0.5m,
            MaxTradeAmount = 1000m,
            MaxSlippagePercentage = 0.2m,
            MaxConcurrentTrades = 3
        };
    }
}

 