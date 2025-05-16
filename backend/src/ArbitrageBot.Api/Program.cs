using ArbitrageBot.Api.Hubs;
using ArbitrageBot.Api.Services;
using ArbitrageBot.Application;
using ArbitrageBot.Infrastructure;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ArbitrageBot API", Version = "v1" });
});

// Configure CORS for the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Allow credentials for SignalR
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Add application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// Add the SignalR broadcast service
builder.Services.AddHostedService<SignalRBroadcastService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

// Apply CORS
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<ArbitrageHub>("/hubs/arbitrage");
app.MapHub<TradeHub>("/hubs/trades");

// Ensure database and configurations are initialized
using (var scope = app.Services.CreateScope())
{
    try
    {
        var configService = scope.ServiceProvider.GetRequiredService<ArbitrageBot.Application.Interfaces.IConfigurationService>();
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
    Log.Information("Starting ArbitrageBot API");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "ArbitrageBot API terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
} 