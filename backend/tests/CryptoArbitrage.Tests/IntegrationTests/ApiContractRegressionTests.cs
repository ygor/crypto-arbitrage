using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Text;
using CryptoArbitrage.Api;
using CryptoArbitrage.Api.Models;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;

namespace CryptoArbitrage.Tests.IntegrationTests;

/// <summary>
/// Regression tests to prevent API contract issues from reoccurring.
/// These tests verify the specific issues that were discovered and fixed:
/// 1. Missing /api/statistics endpoint (was returning 404)
/// 2. Bot start/stop returning success: false instead of true
/// 3. Activity logs and exchange status endpoints working correctly
/// </summary>
public class ApiContractRegressionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiContractRegressionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task StatisticsEndpoint_ShouldReturnData_NotReturn404()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/statistics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify it's valid JSON with expected structure
        var statistics = JsonSerializer.Deserialize<CryptoArbitrage.Api.Models.ArbitrageStatistics>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(statistics);
        Assert.NotNull(statistics.startDate);
        Assert.NotNull(statistics.endDate);
        // Properties should have default/zero values even if no data exists yet
        Assert.True(statistics.detectedOpportunities >= 0);
        Assert.True(statistics.executedTrades >= 0);
        Assert.True(statistics.totalProfitAmount >= 0);
    }

    [Fact]
    public async Task BotStartEndpoint_ShouldReturnSuccessTrue()
    {
        // Arrange & Act
        var response = await _client.PostAsync("/api/settings/bot/start", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify response structure and that success is true
        var botResponse = JsonSerializer.Deserialize<CryptoArbitrage.Api.Models.BotResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(botResponse);
        Assert.True(botResponse.success, "Bot start should return success: true, not false");
        Assert.Contains("started successfully", botResponse.message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BotStopEndpoint_ShouldReturnSuccessTrue()
    {
        // Arrange & Act
        var response = await _client.PostAsync("/api/settings/bot/stop", null);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify response structure and that success is true
        var botResponse = JsonSerializer.Deserialize<CryptoArbitrage.Api.Models.BotResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(botResponse);
        Assert.True(botResponse.success, "Bot stop should return success: true, not false");
        Assert.Contains("stopped successfully", botResponse.message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActivityLogsEndpoint_ShouldReturnArrayData()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/settings/bot/activity-logs");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify it's a valid JSON array
        var activityLogs = JsonSerializer.Deserialize<CryptoArbitrage.Api.Models.ActivityLogEntry[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(activityLogs);
        Assert.IsType<CryptoArbitrage.Api.Models.ActivityLogEntry[]>(activityLogs);
        
        // If there are entries, verify their structure
        if (activityLogs.Length > 0)
        {
            var firstLog = activityLogs[0];
            Assert.NotNull(firstLog.id);
            Assert.NotNull(firstLog.message);
            Assert.True(firstLog.timestamp != default);
        }
    }

    [Fact]
    public async Task ExchangeStatusEndpoint_ShouldReturnArrayData()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/settings/bot/exchange-status");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify it's a valid JSON array
        var exchangeStatus = JsonSerializer.Deserialize<CryptoArbitrage.Api.Models.ExchangeStatus[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(exchangeStatus);
        Assert.IsType<CryptoArbitrage.Api.Models.ExchangeStatus[]>(exchangeStatus);
        
        // Should have at least some exchange data
        Assert.True(exchangeStatus.Length > 0, "Should return at least one exchange status");
        
        // Verify structure of first entry
        var firstExchange = exchangeStatus[0];
        Assert.NotNull(firstExchange.exchangeId);
        Assert.NotNull(firstExchange.exchangeName);
        Assert.True(firstExchange.lastChecked != default);
        Assert.True(firstExchange.responseTimeMs >= 0);
    }

    [Fact]
    public async Task BotStatusEndpoint_ShouldReturnProperStructure()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/settings/bot/status");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify it's valid JSON with expected structure
        var botStatus = JsonSerializer.Deserialize<CryptoArbitrage.Api.Models.BotStatus>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(botStatus);
        Assert.NotNull(botStatus.state);
        Assert.True(botStatus.state == "Running" || botStatus.state == "Stopped");
        Assert.True(botStatus.uptimeSeconds >= 0);
    }

    [Theory]
    [InlineData("/api/arbitrage/opportunities")]
    [InlineData("/api/arbitrage/trades")]
    [InlineData("/api/settings/exchanges")]
    [InlineData("/api/settings/risk-profile")]
    [InlineData("/api/settings/arbitrage")]
    [InlineData("/api/health")]
    public async Task CriticalEndpoints_ShouldNotReturn404(string endpoint)
    {
        // Arrange & Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify it's valid JSON
        Assert.True(IsValidJson(content), $"Endpoint {endpoint} should return valid JSON");
    }

    [Fact]
    public async Task AllRequiredEndpoints_ShouldBeAccessible()
    {
        // This test verifies that all endpoints defined in the OpenAPI spec are accessible
        var requiredEndpoints = new[]
        {
            "/api/arbitrage/opportunities",
            "/api/arbitrage/trades", 
            "/api/arbitrage/statistics",
            "/api/statistics", // This was missing and caused 404
            "/api/settings/bot/activity-logs", // This was missing
            "/api/settings/bot/exchange-status", // This was missing
            "/api/settings/bot/status",
            "/api/settings/exchanges",
            "/api/settings/risk-profile",
            "/api/settings/arbitrage",
            "/api/health"
        };

        foreach (var endpoint in requiredEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
} 