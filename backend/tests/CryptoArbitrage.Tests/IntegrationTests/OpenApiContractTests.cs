using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using CryptoArbitrage.Api;
using CryptoArbitrage.Tests.TestInfrastructure;
using System;
using System.Linq;

namespace CryptoArbitrage.Tests.IntegrationTests;

/// <summary>
/// Tests that verify the backend implementation matches the OpenAPI specification.
/// These tests help catch contract violations during development and CI/CD.
/// </summary>
public class OpenApiContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    // These are the critical endpoints that the frontend expects based on our OpenAPI spec
    private readonly Dictionary<string, string> _requiredEndpoints = new()
    {
        // Arbitrage endpoints
        { "/api/arbitrage/opportunities", "GET" },
        { "/api/arbitrage/trades", "GET" },
        { "/api/arbitrage/statistics", "GET" },
        
        // Statistics endpoints
        { "/api/statistics", "GET" }, // This was missing - regression test
        
        // Settings endpoints
        { "/api/settings/exchanges", "GET" },
        { "/api/settings/risk-profile", "GET" },
        { "/api/settings/arbitrage", "GET" },
        
        // Bot control endpoints  
        { "/api/settings/bot/start", "POST" },
        { "/api/settings/bot/stop", "POST" },
        { "/api/settings/bot/status", "GET" },
        { "/api/settings/bot/activity-logs", "GET" }, // This was missing - regression test
        { "/api/settings/bot/exchange-status", "GET" }, // This was missing - regression test
        
        // Health endpoints
        { "/api/health", "GET" },
    };

    public OpenApiContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AllRequiredEndpoints_ShouldBeImplemented()
    {
        var failedEndpoints = new List<string>();

        foreach (var (endpoint, method) in _requiredEndpoints)
        {
            try
            {
                HttpResponseMessage response = method.ToUpper() switch
                {
                    "GET" => await _client.GetAsync(endpoint),
                    "POST" => await _client.PostAsync(endpoint, null),
                    _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
                };

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    failedEndpoints.Add($"{method} {endpoint} - returned 404 Not Found");
                }
                else if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadRequest)
                {
                    // BadRequest is acceptable for some POST endpoints without proper data
                    failedEndpoints.Add($"{method} {endpoint} - returned {(int)response.StatusCode} {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                failedEndpoints.Add($"{method} {endpoint} - threw exception: {ex.Message}");
            }
        }

        Assert.True(failedEndpoints.Count == 0, 
            $"The following required endpoints are not properly implemented:\n{string.Join("\n", failedEndpoints)}");
    }

    [Theory]
    [InlineData("/api/statistics")]
    [InlineData("/api/settings/bot/activity-logs")]
    [InlineData("/api/settings/bot/exchange-status")]
    public async Task PreviouslyMissingEndpoints_ShouldNowExist(string endpoint)
    {
        // These are the specific endpoints that were missing and caused the issues
        // we fixed. This test ensures they never go missing again.
        
        var response = await _client.GetAsync(endpoint);
        
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));
        
        // Verify it's valid JSON
        var isValidJson = IsValidJson(content);
        Assert.True(isValidJson, $"Endpoint {endpoint} should return valid JSON, but returned: {content}");
    }

    [Theory]
    [InlineData("/api/settings/bot/start")]
    [InlineData("/api/settings/bot/stop")]
    public async Task BotControlEndpoints_ShouldReturnSuccessTrue(string endpoint)
    {
        // These endpoints were returning success: false even when they succeeded
        // This test ensures they return the correct success status
        
        var response = await _client.PostAsync(endpoint, null);
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(content));
        
        // Parse the response and check the success field
        var jsonDocument = JsonDocument.Parse(content);
        var root = jsonDocument.RootElement;
        
        Assert.True(root.TryGetProperty("success", out var successProperty), 
            $"Response from {endpoint} should have a 'success' property");
        
        Assert.True(successProperty.GetBoolean(), 
            $"Endpoint {endpoint} should return success: true, but returned success: {successProperty.GetBoolean()}");
        
        Assert.True(root.TryGetProperty("message", out var messageProperty), 
            $"Response from {endpoint} should have a 'message' property");
        
        var message = messageProperty.GetString();
        Assert.Contains("successfully", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AllArrayEndpoints_ShouldReturnArrays()
    {
        var arrayEndpoints = new[]
        {
            "/api/arbitrage/opportunities",
            "/api/arbitrage/trades",
            "/api/settings/exchanges",
            "/api/settings/bot/activity-logs",
            "/api/settings/bot/exchange-status"
        };

        foreach (var endpoint in arrayEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var jsonDocument = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, jsonDocument.RootElement.ValueKind);
        }
    }

    [Fact]
    public async Task AllObjectEndpoints_ShouldReturnObjects()
    {
        var objectEndpoints = new[]
        {
            "/api/arbitrage/statistics",
            "/api/statistics",
            "/api/settings/risk-profile",
            "/api/settings/arbitrage",
            "/api/settings/bot/status",
            "/api/health"
        };

        foreach (var endpoint in objectEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var jsonDocument = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Object, jsonDocument.RootElement.ValueKind);
        }
    }

    [Fact]
    public async Task EndpointResponseTimes_ShouldBeReasonable()
    {
        // Ensure endpoints respond within reasonable time limits
        const int maxResponseTimeMs = 5000; // 5 seconds max

        var testEndpoints = _requiredEndpoints
            .Where(kv => kv.Value == "GET")
            .Select(kv => kv.Key)
            .ToArray();

        foreach (var endpoint in testEndpoints)
        {
            var startTime = DateTime.UtcNow;
            var response = await _client.GetAsync(endpoint);
            var endTime = DateTime.UtcNow;
            
            var responseTime = (endTime - startTime).TotalMilliseconds;
            
            Assert.True(responseTime < maxResponseTimeMs, 
                $"Endpoint {endpoint} took {responseTime}ms, which exceeds the {maxResponseTimeMs}ms limit");
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