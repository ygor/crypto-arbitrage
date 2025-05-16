using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ArbitrageBot.Application;

/// <summary>
/// Contains extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register core application services
        services.AddSingleton<IArbitrageDetectionService, ArbitrageDetectionService>();
        services.AddSingleton<IArbitrageService, ArbitrageService>();
        services.AddSingleton<ITradingService, TradingService>();
        services.AddSingleton<IMarketDataService, MarketDataService>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        return services;
    }
} 