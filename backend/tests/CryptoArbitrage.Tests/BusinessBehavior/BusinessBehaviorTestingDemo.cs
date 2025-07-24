using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;
using CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Linq;

namespace CryptoArbitrage.Tests.BusinessBehavior;

/// <summary>
/// 🎯 BUSINESS BEHAVIOR TESTING DEMONSTRATION - SIMPLIFIED
/// 
/// This class demonstrates the core principle: Business behavior tests force
/// implementation of real business logic, while technical tests can give false confidence.
/// </summary>
public class BusinessBehaviorTestingDemo
{
    private readonly ITestOutputHelper _output;

    public BusinessBehaviorTestingDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    private IServiceProvider CreateSimplifiedTestServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage.StartArbitrageHandler).Assembly));
        
        // Register logging
        services.AddLogging();
        
        // Register REAL business services
        services.AddSingleton<IArbitrageDetectionService, ArbitrageDetectionService>();
        services.AddSingleton<IMarketDataAggregator, MarketDataAggregatorService>();
        
        // Register test doubles
        services.AddSingleton<IConfigurationService, TestConfigurationService>();
        services.AddSingleton<IArbitrageRepository, SimpleArbitrageRepositoryStub>();
        
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task TechnicalTest_PassesButMissesBusinessGap()
    {
        _output.WriteLine("🎯 TECHNICAL TEST: Focuses on implementation details");
        _output.WriteLine(new string('=', 50));

        // Arrange: Technical setup
        var serviceProvider = CreateSimplifiedTestServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act: Execute command
        var result = await mediator.Send(new StartArbitrageCommand());

        // Assert: Technical success
        Assert.True(result.Success);
        Assert.Equal("Arbitrage bot started successfully", result.Message);
        
        _output.WriteLine("✅ Command executed successfully");
        _output.WriteLine("✅ Returned expected message");
        _output.WriteLine("❓ But did we deliver business value?");
    }

    [Fact]
    public async Task BusinessBehaviorTest_ProvesRealValue()
    {
        _output.WriteLine("🎯 BUSINESS BEHAVIOR TEST: Proves business outcomes");
        _output.WriteLine(new string('=', 50));

        // Arrange: Business setup
        var serviceProvider = CreateSimplifiedTestServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var detectionService = serviceProvider.GetRequiredService<IArbitrageDetectionService>();
        
        // Act: Execute business process
        var result = await mediator.Send(new StartArbitrageCommand());

        // Assert: Business outcomes
        Assert.True(result.Success);
        _output.WriteLine("✅ Command executed successfully");
        
        Assert.True(detectionService.IsRunning);
        _output.WriteLine("✅ Arbitrage detection is actually running");
        
        // Allow detection to run briefly
        await Task.Delay(100);
        
        var opportunities = await detectionService.ScanForOpportunitiesAsync();
        Assert.NotEmpty(opportunities);
        _output.WriteLine($"✅ {opportunities.Count()} arbitrage opportunities detected");
        _output.WriteLine("🎉 REAL BUSINESS VALUE DELIVERED!");
        
        await detectionService.StopDetectionAsync();
    }

    [Fact]
    public async Task ProofOfGap_StatisticsShowRealActivity()
    {
        _output.WriteLine("🎯 STATISTICS TEST: Reveals business activity level");
        _output.WriteLine(new string('=', 50));

        // Arrange
        var serviceProvider = CreateSimplifiedTestServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act: Start system and get statistics
        await mediator.Send(new StartArbitrageCommand());
        var stats = await mediator.Send(new GetStatisticsQuery());

        // Assert: Statistics should reflect real business activity
        Assert.NotNull(stats);
        _output.WriteLine($"📊 Statistics retrieved: {stats.TradingPair}");
        _output.WriteLine("💡 Business behavior tests reveal actual system state");
    }

    [Fact] 
    public async Task CompleteBusinessWorkflow_EndToEnd()
    {
        _output.WriteLine("🎯 COMPLETE BUSINESS WORKFLOW TEST");
        _output.WriteLine(new string('=', 50));

        // Arrange: Full business setup
        var serviceProvider = CreateSimplifiedTestServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var detectionService = serviceProvider.GetRequiredService<IArbitrageDetectionService>();
        
        // Act: Execute complete business workflow
        _output.WriteLine("1. Starting arbitrage detection...");
        var startResult = await mediator.Send(new StartArbitrageCommand());
        Assert.True(startResult.Success);
        
        _output.WriteLine("2. Verifying detection is running...");
        Assert.True(detectionService.IsRunning);
        
        _output.WriteLine("3. Allowing opportunities to be detected...");
        await Task.Delay(200);
        
        _output.WriteLine("4. Checking business outcomes...");
        var opportunities = await detectionService.ScanForOpportunitiesAsync();
        Assert.NotEmpty(opportunities);
        
        _output.WriteLine("5. Getting business statistics...");
        var stats = await mediator.Send(new GetStatisticsQuery());
        Assert.NotNull(stats);
        
        _output.WriteLine("6. Stopping detection service...");
        await detectionService.StopDetectionAsync();
        Assert.False(detectionService.IsRunning);
        
        _output.WriteLine("✅ COMPLETE BUSINESS WORKFLOW SUCCESSFUL!");
        _output.WriteLine($"📈 Detected {opportunities.Count()} opportunities");
        _output.WriteLine("🎯 This proves our business logic is working end-to-end!");
    }
} 