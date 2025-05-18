using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoArbitrage.Application;

/// <summary>
/// Contains extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IArbitrageDetectionService, ArbitrageDetectionService>();
        services.AddSingleton<IArbitrageService, ArbitrageService>();
        services.AddSingleton<IMarketDataService, MarketDataService>();
        services.AddSingleton<ITradingService, TradingService>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        // Add channels for real-time data flow
        services.AddSingleton<System.Threading.Channels.Channel<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>>(
            provider => System.Threading.Channels.Channel.CreateUnbounded<CryptoArbitrage.Domain.Models.ArbitrageOpportunity>(
                new System.Threading.Channels.UnboundedChannelOptions { SingleReader = false, SingleWriter = false }));
        
        services.AddSingleton<System.Threading.Channels.Channel<CryptoArbitrage.Domain.Models.ArbitrageTradeResult>>(
            provider => System.Threading.Channels.Channel.CreateUnbounded<CryptoArbitrage.Domain.Models.ArbitrageTradeResult>(
                new System.Threading.Channels.UnboundedChannelOptions { SingleReader = false, SingleWriter = false }));
        
        return services;
    }
} 