using Microsoft.Extensions.DependencyInjection;
using SaikoUniversity.Application.Programs;

namespace SaikoUniversity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProgramService>();

        return services;
    }
}
