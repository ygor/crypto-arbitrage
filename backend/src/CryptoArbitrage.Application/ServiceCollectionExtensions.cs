using Microsoft.Extensions.DependencyInjection;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;
using MediatR;
using System.Reflection;

namespace CryptoArbitrage.Application;

/// <summary>
/// Extension methods for registering application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services including the new business logic services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // 🎯 REGISTER REAL BUSINESS LOGIC SERVICES
        services.AddSingleton<IMarketDataAggregator, MarketDataAggregatorService>();
        services.AddSingleton<IArbitrageDetectionService, ArbitrageDetectionService>();
        
        return services;
    }
} 