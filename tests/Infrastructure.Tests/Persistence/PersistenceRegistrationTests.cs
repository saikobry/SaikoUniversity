using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Tests.Support;
using Application.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises <c>AddPersistence</c> through DI, which nothing else does.
/// </summary>
/// <remarks>
/// <c>TestDbContext</c> attaches the interceptor by hand on a <c>DbContextOptionsBuilder</c>, so the
/// existing interceptor tests prove the interceptor works but say nothing about whether the DI
/// registration attaches it. This is the path production and <c>Api.Tests</c> both take.
/// </remarks>
public sealed class PersistenceRegistrationTests
{
    [Fact]
    public async Task AddPersistence_StampsCreatedAt_WhenResolvedFromContainer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(_ => new FixedClock(BiasedTimestamp));
        services.AddPersistence((_, options) => options.UseInMemoryDatabase("seam-probe"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var level = new AcademicLevel("UG", "Undergraduate");
        var department = new Department("COMP", "Faculty of Computing");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AcademicLevels.Add(level);
        db.Departments.Add(department);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var program = new AcademicProgram(
            "BSCS",
            "BS Computer Science",
            level.Id,
            department.Id,
            Regulator.Ched,
            120,
            CreditBasis.Hours);

        context.AddProgram(program);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        program.CreatedAt.Should().Be(BiasedTimestamp);
    }

    [Fact]
    public async Task AddPersistence_ExposesAdapterThroughIAppDbContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(_ => new FixedClock(BiasedTimestamp));
        services.AddPersistence((_, options) => options.UseInMemoryDatabase("seam-probe-adapter"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var level = new AcademicLevel("UG", "Undergraduate");
        db.AcademicLevels.Add(level);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        level.CreatedAt.Should().Be(BiasedTimestamp);
    }

    private static readonly DateTimeOffset BiasedTimestamp =
        new(2026, 3, 14, 15, 9, 26, TimeSpan.Zero);
}
