using Microsoft.Extensions.DependencyInjection;
using Application.Programs;
using Application.ReferenceData;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProgramService>();
        services.AddScoped<ReferenceDataService>();

        return services;
    }
}
