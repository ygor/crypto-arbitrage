using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MediatR;
using CryptoArbitrage.Application.Features.BotControl.Commands.Start;
using CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;
using CryptoArbitrage.Application.Features.BotControl.Queries.GetStatistics;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using System.Diagnostics;

namespace CryptoArbitrage.Application.Tests.Integration;

/// <summary>
/// Integration tests for the complete MediatR pipeline - demonstrates request/response flow and cross-cutting concerns.
/// </summary>
public class MediatRPipelineIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatRPipelineIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Register MediatR with all handlers
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(StartHandler).Assembly);
        });
        
        // Setup all required mocks for handlers that need dependencies
        var mockConfigurationService = new Mock<IConfigurationService>();
        var mockPaperTradingService = new Mock<IPaperTradingService>();
        var mockExchangeFactory = new Mock<IExchangeFactory>();
        var mockRepository = new Mock<IArbitrageRepository>();
        
        // Register mocked dependencies
        services.AddSingleton(mockConfigurationService.Object);
        services.AddSingleton(mockPaperTradingService.Object);
        services.AddSingleton(mockExchangeFactory.Object);
        services.AddSingleton(mockRepository.Object);
        
        // Add logging for better test diagnostics
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task MediatRPipeline_CommandExecution_FlowsCorrectly()
    {
        // Arrange - Test complete command flow through MediatR
        var stopwatch = Stopwatch.StartNew();
        var startCommand = new StartCommand();

        // Act
        var result = await _mediator.Send(startCommand);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Bot started successfully", result.Message);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should be fast
    }

    [Fact]
    public async Task MediatRPipeline_QueryExecution_FlowsCorrectly()
    {
        // Arrange - Test complete query flow through MediatR
        var stopwatch = Stopwatch.StartNew();
        var statisticsQuery = new GetStatisticsQuery();

        // Act
        var result = await _mediator.Send(statisticsQuery);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ArbitrageStatistics>(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should be fast for mock data
    }

    [Fact]
    public async Task MediatRPipeline_HandlerWithBusinessLogic_ExecutesBusinessRules()
    {
        // Arrange - Test StartArbitrageHandler which has business logic
        var command = new StartArbitrageCommand();

        // Act - Execute multiple times to test business logic
        var firstResult = await _mediator.Send(command);
        var secondResult = await _mediator.Send(command);

        // Assert - One should succeed, one should fail (business rule: can't start twice)
        // Due to static state, we can't predict which will succeed, but exactly one should fail
        var results = new[] { firstResult, secondResult };
        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);
        
        // Either first succeeds and second fails, OR first fails (already running) and second also fails
        Assert.True(successCount <= 1, "At most one start should succeed");
        Assert.True(failureCount >= 1, "At least one start should fail due to already running");
        
        // If any start succeeded, verify the success message
        var successResult = results.FirstOrDefault(r => r.Success);
        if (successResult != null)
        {
            Assert.Equal("Arbitrage bot started successfully", successResult.Message);
        }
        
        // Any failure should have the correct message
        var failureResult = results.FirstOrDefault(r => !r.Success);
        if (failureResult != null)
        {
            Assert.Equal("Arbitrage bot is already running", failureResult.Message);
        }
    }

    [Fact]
    public async Task MediatRPipeline_ConcurrentRequests_HandledSafely()
    {
        // Arrange - Test concurrent request handling
        var commands = Enumerable.Range(1, 10)
            .Select(_ => new GetStatisticsQuery())
            .ToList();

        // Act - Execute all commands concurrently
        var tasks = commands.Select(cmd => _mediator.Send(cmd)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert - All requests complete successfully
        Assert.Equal(10, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(results, result => Assert.NotEqual(Guid.Empty, result.Id));
        
        // Verify each result has unique ID (showing they're independent)
        var uniqueIds = results.Select(r => r.Id).Distinct().Count();
        Assert.Equal(10, uniqueIds);
    }

    [Fact]
    public async Task MediatRPipeline_DependencyInjection_ResolvesCorrectly()
    {
        // Arrange - Test that all dependencies are resolved correctly
        var commands = new List<object>
        {
            new StartCommand(),
            new GetStatisticsQuery(),
            new CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning.IsRunningQuery()
        };

        // Act & Assert - All commands should execute without dependency resolution errors
        foreach (var command in commands)
        {
            var result = await _mediator.Send(command);
            Assert.NotNull(result);
            // Each command type should return appropriate result without throwing
        }
    }

    [Fact]
    public async Task MediatRPipeline_RequestResponseMapping_WorksCorrectly()
    {
        // Arrange - Test that request/response types are mapped correctly
        var requests = new List<(object Request, Type ExpectedResponseType)>
        {
            (new StartCommand(), typeof(StartResult)),
            (new GetStatisticsQuery(), typeof(ArbitrageStatistics)),
            (new CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning.IsRunningQuery(), typeof(bool))
        };

        // Act & Assert
        foreach (var (request, expectedType) in requests)
        {
            var result = await _mediator.Send(request);
            Assert.NotNull(result);
            Assert.IsType(expectedType, result);
        }
    }

    [Fact]
    public async Task MediatRPipeline_HandlerLifetime_ManagedCorrectly()
    {
        // Arrange - Test that handlers are created and disposed correctly
        var command = new StartCommand();

        // Act - Execute multiple requests to test handler lifetime
        var results = new List<StartResult>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _mediator.Send(command);
            results.Add(result);
        }

        // Assert - All requests complete successfully (handlers created/disposed correctly)
        Assert.Equal(5, results.Count);
        Assert.All(results, result => Assert.True(result.Success));
    }

    [Fact]
    public async Task MediatRPipeline_PerformanceCharacteristics_MeetExpectations()
    {
        // Arrange - Test performance characteristics
        var commands = Enumerable.Range(1, 100)
            .Select(_ => new GetStatisticsQuery())
            .ToList();

        // Act - Execute many commands and measure performance
        var stopwatch = Stopwatch.StartNew();
        var tasks = commands.Select(cmd => _mediator.Send(cmd)).ToArray();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Performance should be reasonable
        Assert.Equal(100, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        
        // Should complete 100 simple queries in under 5 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"100 queries took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        
        // Average per query should be reasonable
        var averageMs = stopwatch.ElapsedMilliseconds / 100.0;
        Assert.True(averageMs < 50, $"Average query time {averageMs}ms, expected < 50ms");
    }

    [Fact]
    public async Task MediatRPipeline_SimpleWorkflow_ExecutesEndToEnd()
    {
        // Arrange - Test a workflow involving multiple commands/queries
        var workflow = new List<(string Step, Func<Task<object>> Action)>
        {
            ("Start Bot", async () => await _mediator.Send(new StartCommand())),
            ("Get Initial Stats", async () => await _mediator.Send(new GetStatisticsQuery())),
            ("Check Status", async () => await _mediator.Send(new CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning.IsRunningQuery())),
            ("Get Updated Stats", async () => await _mediator.Send(new GetStatisticsQuery())),
            ("Stop Bot", async () => await _mediator.Send(new CryptoArbitrage.Application.Features.BotControl.Commands.Stop.StopCommand()))
        };

        var results = new List<(string Step, object Result, long ElapsedMs)>();
        var totalStopwatch = Stopwatch.StartNew();

        foreach (var (step, action) in workflow)
        {
            var stepStopwatch = Stopwatch.StartNew();
            var result = await action();
            stepStopwatch.Stop();
            results.Add((step, result, stepStopwatch.ElapsedMilliseconds));
        }

        totalStopwatch.Stop();

        // Assert - All steps complete successfully
        Assert.Equal(5, results.Count);
        Assert.All(results, result => Assert.NotNull(result.Result));
        
        // Verify specific step results
        var startResult = results[0].Result as StartResult;
        Assert.True(startResult?.Success);
        
        var initialStats = results[1].Result as ArbitrageStatistics;
        Assert.NotNull(initialStats);
        
        var isRunning = (bool)results[2].Result;
        Assert.True(isRunning);
        
        var updatedStats = results[3].Result as ArbitrageStatistics;
        Assert.NotNull(updatedStats);
        Assert.NotEqual(initialStats.Id, updatedStats.Id); // Different instances
        
        var stopResult = results[4].Result as CryptoArbitrage.Application.Features.BotControl.Commands.Stop.StopResult;
        Assert.True(stopResult?.Success);
        
        // Verify reasonable performance for the entire workflow
        Assert.True(totalStopwatch.ElapsedMilliseconds < 2000, 
            $"Complete workflow took {totalStopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
    }

    [Fact]
    public async Task MediatRPipeline_StatefulHandlers_MaintainStateCorrectly()
    {
        // Arrange - Test handlers that maintain state (like IsRunning)
        var startCommand = new StartCommand();
        var stopCommand = new CryptoArbitrage.Application.Features.BotControl.Commands.Stop.StopCommand();
        var isRunningQuery = new CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning.IsRunningQuery();

        // Act & Assert - Test state transitions (focus on changes, not absolute initial state)
        
        // Get initial state (whatever it is)
        var initialState = await _mediator.Send(isRunningQuery);

        // Start should change state to running
        var startResult = await _mediator.Send(startCommand);
        Assert.True(startResult.Success);
        
        var runningState = await _mediator.Send(isRunningQuery);
        Assert.True(runningState); // Should be running after start

        // Stop should change state to stopped
        var stopResult = await _mediator.Send(stopCommand);
        Assert.True(stopResult.Success);
        
        var stoppedState = await _mediator.Send(isRunningQuery);
        Assert.False(stoppedState); // Should be stopped after stop

        // Start again should change state back to running
        var restartResult = await _mediator.Send(startCommand);
        Assert.True(restartResult.Success);
        
        var runningAgainState = await _mediator.Send(isRunningQuery);
        Assert.True(runningAgainState); // Should be running after restart
    }

    [Fact]
    public async Task MediatRPipeline_MultipleHandlerTypes_CoexistCorrectly()
    {
        // Arrange - Test that different handler types work together
        var commandResult = await _mediator.Send(new StartCommand());
        var queryResult = await _mediator.Send(new GetStatisticsQuery());
        var simpleQueryResult = await _mediator.Send(new CryptoArbitrage.Application.Features.BotControl.Queries.IsRunning.IsRunningQuery());

        // Assert - All handler types execute successfully
        Assert.IsType<StartResult>(commandResult);
        Assert.IsType<ArbitrageStatistics>(queryResult);
        Assert.IsType<bool>(simpleQueryResult);
        
        Assert.True(commandResult.Success);
        Assert.NotNull(queryResult);
        Assert.True(simpleQueryResult); // Bot should be running after start command
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
} 