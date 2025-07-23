using CryptoArbitrage.Blazor.Mapping;
using CryptoArbitrage.Blazor.Services;

namespace CryptoArbitrage.Blazor.Extensions;

/// <summary>
/// Extension methods for configuring services specific to the Blazor project.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Blazor-specific services including AutoMapper and model mapping services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlazorServices(this IServiceCollection services)
    {
        // Add AutoMapper with our Blazor mapping profile
        services.AddAutoMapper(typeof(BlazorMappingProfile));

        // Register Blazor-specific services
        services.AddScoped<IBlazorModelService, BlazorModelService>();

        return services;
    }

    /// <summary>
    /// Adds AutoMapper with the Blazor mapping profile.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlazorAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(BlazorMappingProfile));
        return services;
    }

    /// <summary>
    /// Adds the Blazor model service for ViewModel mapping.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlazorModelService(this IServiceCollection services)
    {
        services.AddScoped<IBlazorModelService, BlazorModelService>();
        return services;
    }
} 