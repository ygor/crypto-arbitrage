using System.Net.Http.Headers;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Infrastructure.Exchanges;
using CryptoArbitrage.Infrastructure.Repositories;
using CryptoArbitrage.Infrastructure.Services;
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
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IArbitrageRepository, ArbitrageRepository>();
        
        // Register configuration services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ConfigurationLoader>();
        
        // Register exchange services
        services.AddSingleton<IExchangeFactory, ExchangeFactory>();
        services.AddSingleton<WebSocketConnectionPool>();
        services.AddSingleton<OrderBookPool>();
        
        // Register application services
        services.AddSingleton<IMarketDataService, MarketDataService>();
        services.AddSingleton<ITradingService, TradingService>();
        services.AddSingleton<IArbitrageDetectionService, ArbitrageDetectionService>();
        services.AddSingleton<IArbitrageService, ArbitrageService>();
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
        services.AddSingleton<OptimizedArbitrageDetectionService>();
        
        return services;
    }
} 