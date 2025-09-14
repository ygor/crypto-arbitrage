using System.Net.Http.Headers;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Infrastructure.Database;
using CryptoArbitrage.Infrastructure.Exchanges;
using CryptoArbitrage.Infrastructure.Repositories;
using CryptoArbitrage.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Infrastructure;

/// <summary>
/// Contains extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="useMongoDb">Whether to use MongoDB instead of file-based storage.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration? configuration = null, 
        bool useMongoDb = false)
    {
        // Register repositories based on configuration
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        
        if (useMongoDb && configuration != null)
        {
            // Add MongoDB services
            services.AddMongoDbServices(configuration);
            services.AddSingleton<IArbitrageRepository, MongoDbArbitrageRepository>();
        }
        else
        {
            // Use file-based repository
            services.AddSingleton<IArbitrageRepository, ArbitrageRepository>();
        }
        
        // Register configuration services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ConfigurationLoader>();
        
        // Register exchange services
        services.AddSingleton<IExchangeFactory, ExchangeFactory>();
        services.AddSingleton<WebSocketConnectionPool>();
        services.AddSingleton<OrderBookPool>();
        
        // Register remaining infrastructure services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPaperTradingService, CryptoArbitrage.Infrastructure.Services.PaperTradingService>();
        
        // Register HTTP clients for exchanges
        services.AddHttpClient<CoinbaseExchangeClient>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        
        services.AddHttpClient<KrakenExchangeClient>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        
        // Register performance monitoring
        services.AddSingleton<PerformanceMetricsService>();
        
        // Register Prometheus metrics and system health monitoring
        services.AddSingleton<Monitoring.PrometheusMetrics>();
        services.AddHostedService<Monitoring.SystemHealthMonitor>();
        
        return services;
    }
    
    /// <summary>
    /// Adds MongoDB services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMongoDbServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB settings
        services.Configure<MongoDbConfiguration>(options =>
        {
            var connectionString = configuration.GetConnectionString("MongoDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string 'MongoDb' is not configured");
            }
            
            options.ConnectionString = connectionString;
            options.DatabaseName = configuration.GetValue<string>("MongoDb:DatabaseName") ?? "CryptoArbitrage";
            options.MaxConnectionPoolSize = configuration.GetValue<int>("MongoDb:MaxConnectionPoolSize", 100);
            options.ConnectionTimeoutMs = configuration.GetValue<int>("MongoDb:ConnectionTimeoutMs", 30000);
            options.SocketTimeoutMs = configuration.GetValue<int>("MongoDb:SocketTimeoutMs", 30000);
            options.ServerSelectionTimeoutMs = configuration.GetValue<int>("MongoDb:ServerSelectionTimeoutMs", 30000);
            options.UseSsl = configuration.GetValue<bool>("MongoDb:UseSsl", false);
        });
        
        // Register MongoDB context
        services.AddSingleton<CryptoArbitrageDbContext>();
        
        // Register migration service
        services.AddTransient<DataMigrationService>();
        
        // Register health check
        services.AddTransient<HealthChecks.MongoDbHealthCheck>();
        
        return services;
    }
    
    /// <summary>
    /// Adds high-performance services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHighPerformanceServices(this IServiceCollection services)
    {
        // Register high-performance services
        
        return services;
    }
} 