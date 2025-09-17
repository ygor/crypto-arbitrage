using CryptoArbitrage.Api.Hubs;
using CryptoArbitrage.Api.Services;
using CryptoArbitrage.Application;
using CryptoArbitrage.Infrastructure;
using CryptoArbitrage.Infrastructure.Database;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using DotNetEnv;

// Load environment variables from .env file
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

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

// Add application services using the newer version with business logic services
CryptoArbitrage.Application.ServiceCollectionExtensions.AddApplicationServices(builder.Services);

// Also register additional services from DependencyInjection class
CryptoArbitrage.Application.DependencyInjection.AddApplicationServices(builder.Services);

// Check if MongoDB should be used instead of file storage
var useMongoDb = builder.Configuration.GetValue<bool>("Database:UseMongoDb", false);
if (useMongoDb)
{
    builder.Services.AddInfrastructureServices(builder.Configuration, useMongoDb: true);
    builder.Services.AddHealthChecks()
        .AddTypeActivatedCheck<MongoDbHealthCheck>("mongodb", 
            args: new object[] { builder.Configuration });
}
else
{
    builder.Services.AddInfrastructureServices();
}

// Add the SignalR broadcast services
builder.Services.AddScoped<SignalRBroadcastService>();
builder.Services.AddScoped<MarketDataBroadcastService>();

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
app.MapHub<MarketDataHub>("/hubs/market-data").RequireCors("AllowFrontend");

// Ensure database and configurations are initialized
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Initialize MongoDB if enabled
        if (useMongoDb)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CryptoArbitrage.Infrastructure.Database.CryptoArbitrageDbContext>();
            await dbContext.InitializeAsync();
            Log.Information("MongoDB database initialized successfully");

            // Check if data migration should be performed
            var shouldMigrate = builder.Configuration.GetValue<bool>("Database:MigrateFromFiles", false);
            if (shouldMigrate)
            {
                var migrationService = scope.ServiceProvider.GetRequiredService<CryptoArbitrage.Infrastructure.Database.DataMigrationService>();
                
                // Create backup before migration
                var backupCreated = await migrationService.CreateBackupAsync();
                if (backupCreated)
                {
                    Log.Information("Created backup of existing data before migration");
                }

                // Perform migration
                var migrationResult = await migrationService.MigrateAllDataAsync();
                if (migrationResult.Success)
                {
                    Log.Information("Data migration completed successfully: {Message}", migrationResult.Message);
                }
                else
                {
                    Log.Warning("Data migration completed with issues: {Message}", migrationResult.Message);
                }
            }
        }

        var configService = scope.ServiceProvider.GetRequiredService<CryptoArbitrage.Application.Interfaces.IConfigurationService>();
        await configService.LoadConfigurationAsync();
        Log.Information("Configuration loaded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing database and configuration");
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