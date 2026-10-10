using Microsoft.Extensions.DependencyInjection;
using Application.Programs;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProgramService>();

        return services;
    }
}
