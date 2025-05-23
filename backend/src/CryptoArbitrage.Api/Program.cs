using CryptoArbitrage.Api.Hubs;
using CryptoArbitrage.Api.Services;
using CryptoArbitrage.Application;
using CryptoArbitrage.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NJsonSchema;
using NSwag.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger/OpenAPI generation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Crypto Arbitrage API",
        Version = "v1",
        Description = "API for cryptocurrency arbitrage operations"
    });
});

// Configure CORS for the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5001",
                "http://localhost:5002",
                "http://127.0.0.1:3000",
                "http://host.docker.internal:3000"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Allow credentials for SignalR
    });
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Add application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// Add the SignalR broadcast service
builder.Services.AddHostedService<SignalRBroadcastService>();

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
    app.UseSwagger(); // Serve OpenAPI/Swagger documents
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Arbitrage API v1");
    });
}

app.UseSerilogRequestLogging();

// Only use HTTPS redirection if not running in Docker, not in development mode, and if configured to use HTTPS
var isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var aspNetCoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
var isHttpsConfigured = aspNetCoreUrls.Contains("https://");

if (!isRunningInDocker && !app.Environment.IsDevelopment() && isHttpsConfigured)
{
    app.UseHttpsRedirection();
}

// Apply CORS
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<ArbitrageHub>("/hubs/arbitrage").RequireCors("AllowFrontend");
app.MapHub<TradeHub>("/hubs/trades").RequireCors("AllowFrontend");
app.MapHub<ActivityHub>("/hubs/activity").RequireCors("AllowFrontend");
app.MapHub<ExchangeStatusHub>("/hubs/exchanges").RequireCors("AllowFrontend");

// Ensure database and configurations are initialized
using (var scope = app.Services.CreateScope())
{
    try
    {
        var configService = scope.ServiceProvider.GetRequiredService<CryptoArbitrage.Application.Interfaces.IConfigurationService>();
        await configService.LoadConfigurationAsync();
        Log.Information("Configuration loaded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing configuration");
    }
}

try
{
    Log.Information("Starting CryptoArbitrage API");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "CryptoArbitrage API terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Make the Program class accessible for integration tests
public partial class Program { } 