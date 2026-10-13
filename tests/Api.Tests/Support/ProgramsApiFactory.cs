using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Infrastructure;
using Infrastructure.Persistence;
using Domain.Entities;

namespace Api.Tests.Support;

/// <summary>
/// Boots the real host — real controllers, real DI graph, real <c>ProgramService</c> — against an
/// InMemory database instead of the Compose SQL Server.
/// </summary>
/// <remarks>
/// <c>AddInfrastructure</c> reads <c>ConnectionStrings:Default</c>, which in Development points at
/// localhost:1433. Left alone, every test here would read and write the developer's real database,
/// so a run would depend on Docker being up, on migrations applied, and on the rows already present.
/// <para>
/// Only the provider is swapped. Everything else comes from <c>AddPersistence</c>, the same internal
/// method production uses, so the interceptor that stamps <c>CreatedAt</c> and <c>UpdatedAt</c> is
/// wired here too. Hand-registering the graph instead would let a future interceptor land in one
/// graph and not the other, and nothing would go red.
/// <para>
/// InMemory does not enforce unique indexes or foreign keys, and compares strings case-sensitively
/// where SQL Server does not. A test that depends on any of those is asserting persistence behaviour
/// and belongs in <c>Infrastructure.Tests</c> against the real database.
/// </para>
/// </remarks>
public sealed class ProgramsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"saiko-api-test-{Guid.CreateVersion7()}";

    /// <summary>
    /// Ids of the reference data seeded into this host's database. Tests building a valid create
    /// payload use these rather than inventing ids, so a mismatch surfaces as a 400 on the reference
    /// rather than as an unexplained failure.
    /// </summary>
    public Guid AcademicLevelId { get; private set; }

    public Guid DepartmentId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove only what carries the SqlServer provider extension. EF stores it on
            // IDbContextOptionsConfiguration<T>, and leaving that in place makes EF refuse to start
            // because two providers are registered. These are EF mechanics rather than application
            // services, so they cannot go stale the way a copied registration list would.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddPersistence((_, options) => options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>
    /// Seeds once per host rather than per client. xUnit constructs the test class once per test and
    /// each constructor calls <c>CreateClient()</c>, so seeding from <c>ConfigureClient</c> would run
    /// seven times into a shared store — duplicate "UG" and "COMP" rows that InMemory tolerates only
    /// because it enforces no unique indexes.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var level = new AcademicLevel("UG", "Undergraduate");
        var department = new Department("COMP", "Faculty of Computing");

        context.AcademicLevels.Add(level);
        context.Departments.Add(department);
        context.SaveChanges();

        AcademicLevelId = level.Id;
        DepartmentId = department.Id;

        return host;
    }
}
