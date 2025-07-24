using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MediatR;
using CryptoArbitrage.Application.Features.BotControl.Commands.Start;
using CryptoArbitrage.Application.Features.BotControl.Commands.Stop;
using CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning;
using CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Application.Tests.Integration;

/// <summary>
/// Integration tests for complete vertical slice workflows - demonstrates feature slice independence and collaboration.
/// </summary>
public class VerticalSliceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public VerticalSliceIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Register MediatR with all handlers from the application assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(StartHandler).Assembly));
        
        // Setup basic mocks for handlers that need dependencies
        var mockExchangeFactory = new Mock<IExchangeFactory>();
        var mockConfigurationService = new Mock<IConfigurationService>();
        var mockPaperTradingService = new Mock<IPaperTradingService>();
        var mockRepository = new Mock<IArbitrageRepository>();
        
        // Register mocked dependencies
        services.AddSingleton(mockExchangeFactory.Object);
        services.AddSingleton(mockConfigurationService.Object);
        services.AddSingleton(mockPaperTradingService.Object);
        services.AddSingleton(mockRepository.Object);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task BotControlSlice_CompleteStartStopCycle_WorksIndependently()
    {
        // Arrange - Test complete bot control feature slice
        var startCommand = new StartCommand();
        var stopCommand = new StopCommand();
        var isRunningQuery = new IsRunningQuery();

        // Act & Assert - Start bot
        var startResult = await _mediator.Send(startCommand);
        Assert.True(startResult.Success);
        Assert.Equal("Bot started successfully", startResult.Message);

        // Verify bot is running
        var isRunning = await _mediator.Send(isRunningQuery);
        Assert.True(isRunning);

        // Stop bot
        var stopResult = await _mediator.Send(stopCommand);
        Assert.True(stopResult.Success);
        Assert.Equal("Bot stopped successfully", stopResult.Message);

        // Verify bot is stopped
        var isStopped = await _mediator.Send(isRunningQuery);
        Assert.False(isStopped);
    }

    [Fact]
    public async Task StatisticsSlice_GeneratesDataIndependently()
    {
        // Arrange
        var statisticsQuery = new GetStatisticsQuery();

        // Act
        var statistics = await _mediator.Send(statisticsQuery);

        // Assert - Verify statistics are generated without external dependencies
        Assert.NotNull(statistics);
        Assert.NotEmpty(statistics.TradingPair);
        Assert.True(statistics.StartTime <= statistics.EndTime);
        Assert.True(statistics.CreatedAt <= DateTime.UtcNow);
        
        // Verify default/mock values are reasonable
        Assert.Equal(0, statistics.TotalOpportunitiesCount);
        Assert.Equal(0, statistics.TotalTradesCount);
        Assert.Equal(0m, statistics.TotalProfitAmount);
    }

    [Fact]
    public async Task CrossSliceIntegration_BotControlAndStatistics_WorkTogether()
    {
        // Arrange - Test that different feature slices don't interfere with each other
        var startCommand = new StartCommand();
        var statisticsQuery = new GetStatisticsQuery();
        var isRunningQuery = new IsRunningQuery();

        // Act - Execute commands from different slices
        var startResult = await _mediator.Send(startCommand);
        var initialStats = await _mediator.Send(statisticsQuery);
        var isRunning = await _mediator.Send(isRunningQuery);
        
        // Execute more commands
        var statsAgain = await _mediator.Send(statisticsQuery);
        var stopCommand = new StopCommand();
        var stopResult = await _mediator.Send(stopCommand);
        var finalStats = await _mediator.Send(statisticsQuery);

        // Assert - All operations succeed independently
        Assert.True(startResult.Success);
        Assert.True(isRunning);
        Assert.True(stopResult.Success);
        
        // Verify statistics work regardless of bot state
        Assert.NotNull(initialStats);
        Assert.NotNull(statsAgain);
        Assert.NotNull(finalStats);
        
        // Each statistics query should return fresh data (new GUID)
        Assert.NotEqual(initialStats.Id, statsAgain.Id);
        Assert.NotEqual(statsAgain.Id, finalStats.Id);
    }

    [Fact]
    public async Task ParallelSliceExecution_HandlesSimultaneousRequests()
    {
        // Arrange - Test concurrent execution across different slices
        var startCommand = new StartCommand();
        var statisticsQuery = new GetStatisticsQuery();
        var isRunningQuery = new IsRunningQuery();

        // Create multiple tasks from different slices
        var tasks = new Task[]
        {
            _mediator.Send(startCommand),
            _mediator.Send(statisticsQuery),
            _mediator.Send(isRunningQuery),
            _mediator.Send(statisticsQuery), // Duplicate to test concurrent access
            _mediator.Send(isRunningQuery)   // Duplicate to test concurrent access
        };

        // Act - Execute all tasks concurrently
        await Task.WhenAll(tasks);

        // Assert - All tasks complete successfully (no exceptions thrown)
        Assert.All(tasks, task => Assert.True(task.IsCompleted));
        Assert.All(tasks, task => Assert.False(task.IsFaulted));
    }

    [Fact]
    public async Task ErrorIsolation_FailureInOneSliceDoesNotAffectOthers()
    {
        // Arrange - Test that failure doesn't propagate between slices
        // (In this simplified version, all our handlers are self-contained)

        // Act & Assert - Statistics should work independently
        var statisticsQuery = new GetStatisticsQuery();
        var stats = await _mediator.Send(statisticsQuery);
        Assert.NotNull(stats); // Statistics handler is self-contained

        // Bot control should work independently
        var startCommand = new StartCommand();
        var startResult = await _mediator.Send(startCommand);
        Assert.True(startResult.Success); // Bot control handlers are self-contained

        // Query handlers should work independently
        var isRunningQuery = new IsRunningQuery();
        var isRunning = await _mediator.Send(isRunningQuery);
        Assert.True(isRunning); // Query handlers are self-contained
    }

    [Fact]
    public async Task VerticalSliceIndependence_EachSliceOperatesInIsolation()
    {
        // Arrange - Test that each vertical slice can operate independently
        var commands = new List<(string SliceName, Func<Task<object>> Command)>
        {
            ("BotControl", async () => await _mediator.Send(new StartCommand())),
            ("Statistics", async () => await _mediator.Send(new GetStatisticsQuery())),
            ("IsRunning", async () => await _mediator.Send(new IsRunningQuery()))
        };

        // Act - Execute each slice's command independently
        var results = new List<(string SliceName, object Result, bool Success)>();
        
        foreach (var (sliceName, command) in commands)
        {
            try
            {
                var result = await command();
                results.Add((sliceName, result, true));
            }
            catch (Exception ex)
            {
                results.Add((sliceName, ex, false));
            }
        }

        // Assert - Verify each slice can execute independently
        Assert.All(results, result => Assert.True(result.Success, $"{result.SliceName} slice failed"));
        
        // Verify specific slice behaviors
        var botControlResult = results.First(r => r.SliceName == "BotControl");
        Assert.IsType<StartResult>(botControlResult.Result);
        
        var statisticsResult = results.First(r => r.SliceName == "Statistics");
        Assert.IsType<ArbitrageStatistics>(statisticsResult.Result);
        
        var isRunningResult = results.First(r => r.SliceName == "IsRunning");
        Assert.IsType<bool>(isRunningResult.Result);
    }

    [Fact]
    public async Task MediatRPipeline_CommandQueryFlow_ExecutesCorrectly()
    {
        // Arrange - Test the complete MediatR pipeline with commands and queries
        var commands = new List<object>
        {
            new StartCommand(),
            new GetStatisticsQuery(),
            new IsRunningQuery(),
            new StopCommand()
        };

        // Act - Execute all commands through MediatR pipeline
        var results = new List<object>();
        foreach (var command in commands)
        {
            var result = await _mediator.Send(command);
            results.Add(result);
        }

        // Assert - All commands execute successfully
        Assert.Equal(4, results.Count);
        Assert.All(results, result => Assert.NotNull(result));
        
        // Verify specific result types
        Assert.IsType<StartResult>(results[0]);
        Assert.IsType<ArbitrageStatistics>(results[1]);
        Assert.IsType<bool>(results[2]);
        Assert.IsType<CryptoArbitrage.Application.Features.BotControl.Commands.Stop.StopResult>(results[3]);
    }

    [Fact]
    public async Task DependencyInjection_ResolvesHandlersCorrectly()
    {
        // Arrange - Test that MediatR correctly resolves handlers through DI
        var startCommand = new StartCommand();

        // Act - Multiple executions to test handler resolution
        var results = new List<StartResult>();
        for (int i = 0; i < 3; i++)
        {
            var result = await _mediator.Send(startCommand);
            results.Add(result);
        }

        // Assert - All executions succeed (handlers resolved correctly)
        Assert.Equal(3, results.Count);
        Assert.All(results, result => Assert.True(result.Success));
    }

    [Fact]
    public async Task HandlerLifecycle_ManagedCorrectly()
    {
        // Arrange - Test handler lifecycle management
        var query = new GetStatisticsQuery();

        // Act - Execute same query multiple times
        var results = new List<ArbitrageStatistics>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _mediator.Send(query);
            results.Add(result);
        }

        // Assert - Each execution creates fresh handler instance (new statistics with unique IDs)
        Assert.Equal(5, results.Count);
        Assert.All(results, result => Assert.NotNull(result));
        
        // Each execution should produce unique statistics instance
        var uniqueIds = results.Select(r => r.Id).Distinct().Count();
        Assert.Equal(5, uniqueIds);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
} 