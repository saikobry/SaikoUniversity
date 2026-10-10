using Microsoft.EntityFrameworkCore;
using SaikoUniversity.Application.Abstractions;
using SaikoUniversity.Infrastructure.Persistence;
using SaikoUniversity.Infrastructure.Persistence.Interceptors;

namespace SaikoUniversity.Infrastructure.Tests.Support;

/// <summary>
/// Builds an <see cref="AppDbContext"/> backed by the InMemory provider with the production
/// interceptors attached.
/// </summary>
/// <remarks>
/// InMemory is the default for speed. Anything asserting on relational behaviour runs against the
/// Compose SQL Server instead and says so in the test name.
/// </remarks>
internal static class TestDbContext
{
    public static AppDbContext Create(IClock clock, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"saiko-test-{Guid.CreateVersion7()}")
            .AddInterceptors(new AuditableEntityInterceptor(clock))
            .Options;

        return new AppDbContext(options);
    }
}
