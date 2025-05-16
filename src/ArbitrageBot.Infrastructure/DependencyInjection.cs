using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Infrastructure.Exchanges;
using ArbitrageBot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ArbitrageBot.Infrastructure;

/// <summary>
/// Contains extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register core infrastructure services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IArbitrageRepository, ArbitrageRepository>();
        services.AddSingleton<IExchangeFactory, ExchangeFactory>();
        services.AddSingleton<IPaperTradingService, PaperTradingService>();
        
        // Add HTTP client factory
        services.AddHttpClient();
        
        // Register exchange clients
        services.AddTransient<CoinbaseExchangeClient>();
        services.AddTransient<KrakenExchangeClient>();
        // StubExchangeClient is created directly by the ExchangeFactory
        
        // Register performance optimization services
        services.AddSingleton<PerformanceMetricsService>();
        services.AddSingleton<OrderBookPool>();
        services.AddSingleton<WebSocketConnectionPool>();
        
        return services;
    }
    
    /// <summary>
    /// Adds high-performance optimized infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHighPerformanceServices(this IServiceCollection services)
    {
        // Add basic infrastructure services first
        services.AddInfrastructureServices();
        
        // Register optimized HTTP clients
        services.AddHttpClient("HighPerformance")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 20,
                EnableMultipleHttp2Connections = true,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5)
            });
        
        // Configure HTTP clients for each exchange with optimized settings
        services.AddHttpClient<CoinbaseExchangeClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 20,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5)
            });
            
        services.AddHttpClient<KrakenExchangeClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 20,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5)
            });
        
        return services;
    }
} 