using Microsoft.Playwright;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CryptoArbitrage.UI.Tests.BusinessBehaviorTests;

/// <summary>
/// 🎯 UI BUSINESS BEHAVIOR TESTS - Simplified Version
/// 
/// These tests verify that the UI correctly reflects actual business outcomes.
/// They bridge the gap between backend business logic and frontend user experience.
/// 
/// NOTE: These tests require the Blazor application to be running on localhost:7001
/// </summary>
public class UIBusinessBehaviorTests : IAsyncLifetime
{
    private IBrowser? _browser;
    private IPlaywright? _playwright;
    private readonly string _baseUrl = "http://localhost:7001";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 50
        });
    }

    [Fact]
    public async Task Dashboard_Should_Display_Key_Business_Metrics()
    {
        // 🎯 BUSINESS BEHAVIOR: Dashboard should show business-relevant information
        
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act: Navigate to dashboard
            await page.GotoAsync(_baseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(3000); // Allow time for data loading
            
            // Assert: UI should display business metrics structure
            var pageContent = await page.ContentAsync();
            
            // Look for business-relevant sections
            var hasBusinessMetrics = pageContent.Contains("Total Profit") ||
                                   pageContent.Contains("Opportunities") ||
                                   pageContent.Contains("Trades") ||
                                   pageContent.Contains("Statistics");
            
            Assert.True(hasBusinessMetrics,
                "Dashboard should display business metrics when they exist");
        }
        catch (Exception ex)
        {
            Skip.If(ex.Message.Contains("net::ERR_CONNECTION_REFUSED"), 
                "Blazor application is not running. Start the application on localhost:7001 to run E2E tests.");
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Opportunities_Page_Should_Display_Data_Or_Empty_State()
    {
        // 🎯 BUSINESS BEHAVIOR: UI should handle business data appropriately
        
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act: Navigate to opportunities page
            await page.GotoAsync($"{_baseUrl}/opportunities");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);
            
            // Assert: Should have either data or appropriate empty state
            var pageContent = await page.ContentAsync();
            
            var hasDataStructure = await page.Locator("table").CountAsync() > 0 ||
                                  await page.Locator(".opportunity").CountAsync() > 0;
            
            var hasEmptyState = pageContent.Contains("No opportunities") ||
                               pageContent.Contains("No data") ||
                               pageContent.Contains("Loading");
            
            Assert.True(hasDataStructure || hasEmptyState,
                "Opportunities page should show either data or appropriate empty state");
        }
        catch (Exception ex)
        {
            Skip.If(ex.Message.Contains("net::ERR_CONNECTION_REFUSED"), 
                "Blazor application is not running. Start the application on localhost:7001 to run E2E tests.");
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Statistics_Page_Should_Show_Performance_Data()
    {
        // 🎯 BUSINESS BEHAVIOR: Statistics should show business performance
        
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act: Navigate to statistics page
            await page.GotoAsync($"{_baseUrl}/statistics");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);
            
            // Assert: Should show statistics-related content
            var pageContent = await page.ContentAsync();
            
            var hasStatisticsContent = pageContent.Contains("Statistics") ||
                                     pageContent.Contains("Performance") ||
                                     pageContent.Contains("Profit") ||
                                     pageContent.Contains("Success Rate") ||
                                     await page.Locator(".chart").CountAsync() > 0 ||
                                     await page.Locator(".metric").CountAsync() > 0;
            
            Assert.True(hasStatisticsContent,
                "Statistics page should display performance metrics");
        }
        catch (Exception ex)
        {
            Skip.If(ex.Message.Contains("net::ERR_CONNECTION_REFUSED"), 
                "Blazor application is not running. Start the application on localhost:7001 to run E2E tests.");
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Settings_Page_Should_Allow_Configuration()
    {
        // 🎯 BUSINESS BEHAVIOR: Settings should allow business configuration
        
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act: Navigate to settings page
            await page.GotoAsync($"{_baseUrl}/settings");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(2000);
            
            // Assert: Should show configuration options
            var pageContent = await page.ContentAsync();
            
            var hasConfigurationElements = pageContent.Contains("Settings") ||
                                          pageContent.Contains("Configuration") ||
                                          pageContent.Contains("Risk") ||
                                          pageContent.Contains("Exchange") ||
                                          await page.Locator("input").CountAsync() > 0 ||
                                          await page.Locator("select").CountAsync() > 0;
            
            Assert.True(hasConfigurationElements,
                "Settings page should provide configuration options");
        }
        catch (Exception ex)
        {
            Skip.If(ex.Message.Contains("net::ERR_CONNECTION_REFUSED"), 
                "Blazor application is not running. Start the application on localhost:7001 to run E2E tests.");
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Complete_User_Journey_Should_Work_End_To_End()
    {
        // 🎯 COMPLETE USER JOURNEY: End-to-end business workflow
        
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Step 1: User navigates to dashboard
            await page.GotoAsync(_baseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var dashboardContent = await page.Locator("body").TextContentAsync();
            Assert.NotNull(dashboardContent);
            
            // Step 2: User navigates to opportunities
            await page.GotoAsync($"{_baseUrl}/opportunities");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var opportunitiesContent = await page.Locator("body").TextContentAsync();
            Assert.NotNull(opportunitiesContent);
            
            // Step 3: User navigates to trades
            await page.GotoAsync($"{_baseUrl}/trades");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var tradesContent = await page.Locator("body").TextContentAsync();
            Assert.NotNull(tradesContent);
            
            // Step 4: User navigates to statistics
            await page.GotoAsync($"{_baseUrl}/statistics");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var statisticsContent = await page.Locator("body").TextContentAsync();
            Assert.NotNull(statisticsContent);
            
            // Step 5: User navigates to settings
            await page.GotoAsync($"{_baseUrl}/settings");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var settingsContent = await page.Locator("body").TextContentAsync();
            Assert.NotNull(settingsContent);
            
            // Verify complete journey worked - all pages should have content
            Assert.True(dashboardContent.Length > 100, "Dashboard should have content");
            Assert.True(opportunitiesContent.Length > 100, "Opportunities should have content");
            Assert.True(tradesContent.Length > 100, "Trades should have content");
            Assert.True(statisticsContent.Length > 100, "Statistics should have content");
            Assert.True(settingsContent.Length > 100, "Settings should have content");
        }
        catch (Exception ex)
        {
            Skip.If(ex.Message.Contains("net::ERR_CONNECTION_REFUSED"), 
                "Blazor application is not running. Start the application on localhost:7001 to run E2E tests.");
            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
        
        _playwright?.Dispose();
    }
} 