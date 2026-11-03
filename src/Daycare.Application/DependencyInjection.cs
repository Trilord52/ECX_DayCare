using Daycare.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Daycare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationAssemblyMarker).Assembly;

        // Register FluentValidation validators
        var validatorResults = AssemblyScanner.FindValidatorsInAssembly(applicationAssembly);
        foreach (var result in validatorResults)
        {
            services.AddScoped(result.InterfaceType, result.ValidatorType);
        }

        // Scan and register every non-abstract class ending in UseCase as scoped per Section 5.3
        var useCaseTypes = applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("UseCase"));

        foreach (var type in useCaseTypes)
        {
            services.AddScoped(type);
        }

        services.AddScoped<ProfilePhotoManager>();

        return services;
    }
}
