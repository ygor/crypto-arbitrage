using CryptoArbitrage.Application;
using CryptoArbitrage.Infrastructure;
using CryptoArbitrage.Blazor.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add health checks
builder.Services.AddHealthChecks();

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

// Add MudBlazor
builder.Services.AddMudServices();

// Add application services using the newer version with business logic services
CryptoArbitrage.Application.ServiceCollectionExtensions.AddApplicationServices(builder.Services);

// Also register additional services from DependencyInjection class
CryptoArbitrage.Application.DependencyInjection.AddApplicationServices(builder.Services);

// Check if MongoDB should be used instead of file storage
var useMongoDb = builder.Configuration.GetValue<bool>("Database:UseMongoDb", false);
if (useMongoDb)
{
    builder.Services.AddInfrastructureServices(builder.Configuration, useMongoDb: true);
}
else
{
    builder.Services.AddInfrastructureServices();
}

// Add Blazor-specific services (ViewModels and AutoMapper)
builder.Services.AddBlazorServices();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add real-time market data service
builder.Services.AddHostedService<CryptoArbitrage.Blazor.Services.RealTimeMarketDataService>();
builder.Services.AddScoped<CryptoArbitrage.Blazor.Services.IRealTimeMarketDataService, CryptoArbitrage.Blazor.Services.RealTimeMarketDataService>();

// Configure options from appsettings
builder.Services.Configure<CryptoArbitrage.Blazor.Services.CryptoArbitrageOptions>(
    builder.Configuration.GetSection("CryptoArbitrage"));

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            Results = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration.TotalMilliseconds
                })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

// Only use HTTPS redirection if not running in Docker and if configured for HTTPS
var isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var aspNetCoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
var isHttpsConfigured = aspNetCoreUrls.Contains("https://");

if (!isRunningInDocker && !app.Environment.IsDevelopment() && isHttpsConfigured)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// Map Blazor hub and pages
app.MapBlazorHub();

// Map SignalR hub for real-time market data
app.MapHub<CryptoArbitrage.Blazor.Hubs.MarketDataHub>("/marketdatahub");

app.MapFallbackToPage("/_Host");
app.MapRazorPages();

app.Run(); 