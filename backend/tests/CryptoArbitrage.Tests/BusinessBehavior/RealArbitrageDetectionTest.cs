using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Tests.BusinessBehavior;

/// <summary>
/// 🎯 REAL BUSINESS BEHAVIOR TEST
/// 
/// This test verifies the ACTUAL arbitrage detection functionality.
/// With the real business logic implemented, this test should now PASS
/// and demonstrate actual business value delivery.
/// </summary>
public class RealArbitrageDetectionTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly IArbitrageRepository _repository;

    public RealArbitrageDetectionTest()
    {
        _serviceProvider = SetupRealBusinessTestEnvironment();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _repository = _serviceProvider.GetRequiredService<IArbitrageRepository>();
    }

    [Fact]
    public async Task Given_RealBusinessLogic_When_ArbitrageStarts_Then_OpportunitiesAreActuallyDetected()
    {
        // 🎯 BUSINESS BEHAVIOR: Real arbitrage detection with actual business value
        
        // Clear any existing opportunities
        await ClearRepositoryAsync();
        
        // When: Start arbitrage with REAL business logic
        var startResult = await _mediator.Send(new StartArbitrageCommand());
        
        // Then: Command should succeed technically
        Assert.True(startResult.Success, $"StartArbitrage should succeed: {startResult.Message}");
        
        // AND: REAL BUSINESS OUTCOME - opportunities should be detected within 15 seconds
        await WaitForBusinessOutcome(async () =>
        {
            var opportunities = await _repository.GetRecentOpportunitiesAsync(limit: 10);
            
            if (opportunities.Any())
            {
                var opportunity = opportunities.First();
                
                // Verify REAL business logic worked
                Assert.NotNull(opportunity.TradingPair);
                Assert.NotEmpty(opportunity.BuyExchangeId);
                Assert.NotEmpty(opportunity.SellExchangeId);
                Assert.True(opportunity.BuyPrice > 0, "Buy price should be positive");
                Assert.True(opportunity.SellPrice > 0, "Sell price should be positive");
                Assert.True(opportunity.SellPrice > opportunity.BuyPrice, "Sell price should be higher than buy price");
                Assert.True(opportunity.SpreadPercentage > 0, "Spread percentage should be positive");
                Assert.True(opportunity.EstimatedProfit > 0, "Estimated profit should be positive");
                Assert.True(opportunity.DetectedAt > DateTime.UtcNow.AddMinutes(-1), "Should be recently detected");
                
                return true;
            }
            
            return false;
        }, timeoutSeconds: 15);
        
        // Cleanup: Stop arbitrage detection
        var detectionService = _serviceProvider.GetRequiredService<IArbitrageDetectionService>();
        await detectionService.StopDetectionAsync();
    }

    [Fact]
    public async Task Given_MultipleExchanges_When_ArbitrageRuns_Then_FindsRealOpportunities()
    {
        // 🎯 BUSINESS BEHAVIOR: Verify multiple exchange monitoring
        
        await ClearRepositoryAsync();
        
        // When: Start arbitrage detection
        var startResult = await _mediator.Send(new StartArbitrageCommand());
        Assert.True(startResult.Success);
        
        // Wait for opportunities to be detected
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // Then: Should find opportunities across different exchanges
        var opportunities = await _repository.GetRecentOpportunitiesAsync(limit: 50);
        
        Assert.NotEmpty(opportunities);
        
        // Verify we have opportunities with different exchange pairs
        var exchangePairs = opportunities
            .Select(o => $"{o.BuyExchangeId}->{o.SellExchangeId}")
            .Distinct()
            .ToList();
        
        Assert.True(exchangePairs.Count > 1, $"Should find opportunities across multiple exchange pairs, found: {string.Join(", ", exchangePairs)}");
        
        // Cleanup
        var detectionService = _serviceProvider.GetRequiredService<IArbitrageDetectionService>();
        await detectionService.StopDetectionAsync();
    }

    [Fact]
    public async Task Demonstrate_Real_Business_Value_Now_Delivered()
    {
        // 🎯 PROOF: Real business value is now being delivered
        
        await ClearRepositoryAsync();
        
        // The SAME StartArbitrageCommand that previously only set a flag
        var result = await _mediator.Send(new StartArbitrageCommand());
        
        // ✅ Still passes technical tests
        Assert.True(result.Success);
        
        // ✅ NOW ALSO delivers business value!
        await Task.Delay(TimeSpan.FromSeconds(8));
        
        var opportunities = await _repository.GetRecentOpportunitiesAsync();
        
        // 🎯 BUSINESS VALUE PROOF: Opportunities are actually being detected!
        Assert.NotEmpty(opportunities);
        
        var firstOpportunity = opportunities.First();
        
        // Real arbitrage opportunity with business meaning
        Assert.Contains("BTC/USD", firstOpportunity.TradingPair.ToString());
        Assert.True(firstOpportunity.EstimatedProfit > 0, "Real profit calculation exists");
        Assert.Contains(firstOpportunity.BuyExchangeId, new[] { "coinbase", "kraken", "binance" });
        Assert.Contains(firstOpportunity.SellExchangeId, new[] { "coinbase", "kraken", "binance" });
        
        // Cleanup
        var detectionService = _serviceProvider.GetRequiredService<IArbitrageDetectionService>();
        await detectionService.StopDetectionAsync();
        
        // 🎉 SUCCESS: Business behavior test now PASSES with real business value!
    }

    private IServiceProvider SetupRealBusinessTestEnvironment()
    {
        var services = new ServiceCollection();
        
        // Register MediatR and application services
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(CryptoArbitrage.Application.Features.BotControl.Commands.Start.StartHandler).Assembly));
        
        // Register REAL business services
        services.AddSingleton<IMarketDataAggregator, MarketDataAggregatorService>();
        services.AddSingleton<IArbitrageDetectionService, ArbitrageDetectionService>();
        
        // Register test implementations for data persistence
        services.AddSingleton<TestArbitrageRepository>();
        services.AddSingleton<IArbitrageRepository>(provider => provider.GetRequiredService<TestArbitrageRepository>());
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
        
        throw new TimeoutException($"Business outcome was not achieved within {timeoutSeconds} seconds. This means the business logic is not working correctly.");
    }

    private async Task ClearRepositoryAsync()
    {
        if (_repository is TestArbitrageRepository testRepo)
        {
            testRepo.Clear();
        }
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        // Stop any running detection services
        try
        {
            var detectionService = _serviceProvider.GetRequiredService<IArbitrageDetectionService>();
            detectionService.StopDetectionAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore cleanup errors
        }
        
        _serviceProvider?.Dispose();
    }
} 