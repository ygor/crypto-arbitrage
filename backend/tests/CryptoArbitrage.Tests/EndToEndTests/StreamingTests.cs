using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Application.Features.BotControl.Commands.Start;
using CryptoArbitrage.Application.Features.BotControl.Commands.Stop;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;
using CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CryptoArbitrage.Tests.EndToEndTests;

/// <summary>
/// Level 3: Real-time streaming and event-driven tests for the arbitrage system using vertical slice architecture.
/// Tests event-driven workflows and real-time data processing capabilities.
/// </summary>
public class StreamingTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IMediator _mediator;
    
    public StreamingTests(TestFixture fixture)
    {
        _fixture = fixture;
        _mediator = fixture.ServiceProvider.GetRequiredService<IMediator>();
    }
    
    [Fact]
    public async Task EventDrivenWorkflow_BotStateChanges_PropagateCorrectly()
    {
        // Arrange - Test event-driven state changes
        var stateChanges = new List<string>();
        
        // Act - Execute state changes and track them
        
        // 1. Initial state check
        var initialState = await _mediator.Send(new IsRunningQuery());
        stateChanges.Add($"Initial: {initialState}");
        
        // 2. Start bot (state change event)
        var startResult = await _mediator.Send(new StartCommand());
        Assert.True(startResult.Success);
        
        var runningState = await _mediator.Send(new IsRunningQuery());
        stateChanges.Add($"After Start: {runningState}");
        
        // 3. Stop bot (state change event)
        var stopResult = await _mediator.Send(new StopCommand());
        Assert.True(stopResult.Success);
        
        var stoppedState = await _mediator.Send(new IsRunningQuery());
        stateChanges.Add($"After Stop: {stoppedState}");
        
        // Assert - State changes are captured and consistent
        Assert.Equal(3, stateChanges.Count);
        Assert.Contains("After Start: True", stateChanges);
        Assert.Contains("After Stop: False", stateChanges);
    }
    
    [Fact]
    public async Task RealTimeStatistics_ContinuousQueries_UpdateCorrectly()
    {
        // Arrange - Test real-time statistics updates
        await _mediator.Send(new StartCommand());
        
        var statisticsSnapshots = new List<ArbitrageStatistics>();
        
        // Act - Take multiple statistics snapshots over time
        for (int i = 0; i < 5; i++)
        {
            var stats = await _mediator.Send(new GetStatisticsQuery());
            statisticsSnapshots.Add(stats);
            
            // Small delay to simulate real-time polling
            await Task.Delay(10);
        }
        
        // Assert - Each snapshot is unique and timestamped correctly
        Assert.Equal(5, statisticsSnapshots.Count);
        Assert.All(statisticsSnapshots, stats => Assert.NotNull(stats));
        
        // Each statistics instance should be unique (new GUID each time)
        var uniqueIds = statisticsSnapshots.Select(s => s.Id).Distinct().Count();
        Assert.Equal(5, uniqueIds);
        
        // Timestamps should be in order (within reasonable tolerance)
        for (int i = 1; i < statisticsSnapshots.Count; i++)
        {
            Assert.True(statisticsSnapshots[i].CreatedAt >= statisticsSnapshots[i-1].CreatedAt);
        }
        
        // Cleanup
        await _mediator.Send(new StopCommand());
    }
    
    [Fact]
    public async Task EventDrivenConcurrency_MultipleStateQueries_HandleCorrectly()
    {
        // Arrange - Test concurrent state queries
        await _mediator.Send(new StartCommand());
        
        // Act - Execute multiple concurrent state queries
        var concurrentTasks = new Task<bool>[10];
        for (int i = 0; i < 10; i++)
        {
            concurrentTasks[i] = _mediator.Send(new IsRunningQuery());
        }
        
        var results = await Task.WhenAll(concurrentTasks);
        
        // Assert - All concurrent queries return consistent state
        Assert.Equal(10, results.Length);
        Assert.All(results, result => Assert.True(result)); // All should be true since bot is running
        
        // Cleanup
        await _mediator.Send(new StopCommand());
        
        // Verify stopped state with concurrent queries
        var stoppedTasks = new Task<bool>[5];
        for (int i = 0; i < 5; i++)
        {
            stoppedTasks[i] = _mediator.Send(new IsRunningQuery());
        }
        
        var stoppedResults = await Task.WhenAll(stoppedTasks);
        Assert.All(stoppedResults, result => Assert.False(result)); // All should be false since bot is stopped
    }
    
    [Fact]
    public async Task StreamingSimulation_ContinuousOperations_MaintainPerformance()
    {
        // Arrange - Simulate streaming operations
        await _mediator.Send(new StartCommand());
        
        var operationTimes = new List<long>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - Simulate continuous streaming operations for a short period
        var endTime = DateTime.UtcNow.AddSeconds(2); // Run for 2 seconds
        
        while (DateTime.UtcNow < endTime)
        {
            var operationStart = System.Diagnostics.Stopwatch.StartNew();
            
            // Simulate rapid operations (like real-time data processing)
            await _mediator.Send(new IsRunningQuery());
            
            operationStart.Stop();
            operationTimes.Add(operationStart.ElapsedMilliseconds);
            
            // Small delay to prevent overwhelming the system
            await Task.Delay(10);
        }
        
        stopwatch.Stop();
        
        // Assert - Performance characteristics under streaming load
        Assert.True(operationTimes.Count > 50, $"Should have processed many operations, got {operationTimes.Count}");
        
        // Average operation time should be reasonable
        var averageTime = operationTimes.Average();
        Assert.True(averageTime < 50, $"Average operation time {averageTime}ms should be < 50ms");
        
        // No operation should take too long
        var maxTime = operationTimes.Max();
        Assert.True(maxTime < 200, $"Max operation time {maxTime}ms should be < 200ms");
        
        // Cleanup
        await _mediator.Send(new StopCommand());
    }
    
    [Fact]
    public async Task EventDrivenDataFlow_StatisticsUpdates_FlowCorrectly()
    {
        // Arrange - Test data flow in event-driven context
        await _mediator.Send(new StartCommand());
        
        // Act - Create a sequence of operations that should trigger data updates
        var initialStats = await _mediator.Send(new GetStatisticsQuery());
        
        // Simulate some activity (multiple statistics requests)
        var activityStats = new List<ArbitrageStatistics>();
        for (int i = 0; i < 3; i++)
        {
            var stats = await _mediator.Send(new GetStatisticsQuery());
            activityStats.Add(stats);
            await Task.Delay(5); // Small delay between operations
        }
        
        var finalStats = await _mediator.Send(new GetStatisticsQuery());
        
        // Assert - Data flows correctly through the system
        Assert.NotNull(initialStats);
        Assert.Equal(3, activityStats.Count);
        Assert.NotNull(finalStats);
        
        // Each statistics call should produce a unique instance
        var allStats = new List<ArbitrageStatistics> { initialStats };
        allStats.AddRange(activityStats);
        allStats.Add(finalStats);
        
        var uniqueStatsIds = allStats.Select(s => s.Id).Distinct().Count();
        Assert.Equal(5, uniqueStatsIds);
        
        // All should have consistent business data
        Assert.All(allStats, stats => Assert.Equal("OVERALL", stats.TradingPair));
        Assert.All(allStats, stats => Assert.Equal(45, stats.TotalTradesCount)); // Updated to expect realistic mock data
        
        // Cleanup
        await _mediator.Send(new StopCommand());
    }
} 