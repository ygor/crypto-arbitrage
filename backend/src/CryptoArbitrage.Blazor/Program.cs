using CryptoArbitrage.Application;
using CryptoArbitrage.Infrastructure;
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
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

// Add MudBlazor
builder.Services.AddMudServices();

// Add application and infrastructure services (Direct RPC access)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// Configure options from appsettings
builder.Services.Configure<CryptoArbitrage.Infrastructure.Services.CryptoArbitrageOptions>(
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
app.MapFallbackToPage("/_Host");
app.MapRazorPages();

app.Run(); 