using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Abstractions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Services;

namespace Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' was not configured.");

        services.AddSingleton<IClock, SystemClock>();
        services.AddPersistence((_, options) => options.UseSqlServer(connectionString));

        return services;
    }

    /// <summary>
    /// Registers persistence with a caller-supplied provider. The provider is the only thing that
    /// varies between production and a test that must not reach the real database.
    /// </summary>
    /// <remarks>
    /// Everything else — the interceptor, the context, the adapter — is registered here so a test
    /// calling this gets the same graph production does. Splitting it any finer means a future
    /// interceptor or query-tracking default lands in one graph and silently not the other, and the
    /// tests keep passing against a shape the application never uses.
    /// <para>
    /// Internal rather than public: production goes through <see cref="AddInfrastructure"/>, and only
    /// <c>Api.Tests</c> needs the other provider.
    /// </para>
    /// </remarks>
    internal static IServiceCollection AddPersistence(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureProvider)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            configureProvider(provider, options);
            options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IAppDbContext>(provider => new AppDbContextAdapter(
            provider.GetRequiredService<AppDbContext>()));

        return services;
    }
}
