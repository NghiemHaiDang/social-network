using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Common.Validation;

public static class DependencyInjection
{
    /// <summary>
    /// Adds FluentValidation services and registers all validators from the specified assembly
    /// </summary>
    public static IServiceCollection AddValidation(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IValidationService, ValidationService>();

        return services;
    }

    /// <summary>
    /// Adds FluentValidation services and registers all validators from multiple assemblies
    /// </summary>
    public static IServiceCollection AddValidation(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        services.AddScoped<IValidationService, ValidationService>();

        return services;
    }
}
