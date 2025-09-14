using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Services;

namespace CryptoArbitrage.Application;

/// <summary>
/// Dependency injection extensions for the Application layer.
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
        // Register MediatR and handlers from current assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add validation pipeline
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register remaining application services 
        services.AddSingleton<INotificationService, NotificationService>();

        return services;
    }
}

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var message = string.Join("; ", failures.Select(f => f.ErrorMessage));
                // Try to construct a failure response using any static Failure(string, ...optional) method
                var failureMethod = typeof(TResponse).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Failure" && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(string));
                if (failureMethod != null)
                {
                    var parameters = failureMethod.GetParameters();
                    var args = new object?[parameters.Length];
                    args[0] = message;
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        // Fill optional parameters with default/null
                        args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : (parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null);
                    }

                    var response = (TResponse?)failureMethod.Invoke(null, args);
                    if (response != null)
                    {
                        return response;
                    }
                }
                throw new ValidationException(message);
            }
        }

        return await next();
    }
} 