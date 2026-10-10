using FluentAssertions;
// Required for the EF Core query extensions. Without it the compiler binds to the
// System.Linq.AsyncEnumerable overloads instead of EntityFrameworkQueryableExtensions.
using Microsoft.EntityFrameworkCore;
using SaikoUniversity.Domain.Entities;
using SaikoUniversity.Domain.Enums;
using SaikoUniversity.Infrastructure.Tests.Support;

namespace SaikoUniversity.Infrastructure.Tests.Persistence.Interceptors;

public sealed class AuditableEntityInterceptorTests
{
    private static readonly DateTimeOffset InsertedAt = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static AcademicProgram CreateProgram() =>
        new(
            "BSCS",
            "Bachelor of Science in Computer Science",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Regulator.Ched,
            144,
            CreditBasis.Hours);

    [Fact]
    public async Task Insert_StampsCreatedAtFromTheClock()
    {
        var clock = new FixedClock(InsertedAt);
        await using var context = TestDbContext.Create(clock);
        var program = CreateProgram();

        context.AcademicPrograms.Add(program);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        program.CreatedAt.Should().Be(InsertedAt);
    }

    [Fact]
    public async Task Insert_LeavesUpdatedAtNull()
    {
        // A row that has never been modified must not claim a modification time. Writing the default
        // DateTimeOffset here is what put year-one dates into the seed data.
        var clock = new FixedClock(InsertedAt);
        await using var context = TestDbContext.Create(clock);
        var program = CreateProgram();

        context.AcademicPrograms.Add(program);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        program.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Update_StampsUpdatedAtFromTheClock()
    {
        var clock = new FixedClock(InsertedAt);
        await using var context = TestDbContext.Create(clock);
        var program = CreateProgram();
        context.AcademicPrograms.Add(program);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updatedAt = InsertedAt.AddDays(1);
        clock.Set(updatedAt);
        program.Rename("Bachelor of Science in Computer Science (Revised)");

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        program.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task Update_DoesNotRewriteCreatedAt()
    {
        // The interceptor clears CreatedAt from the modified set. Without that, every save would
        // overwrite the creation timestamp with the update time.
        var clock = new FixedClock(InsertedAt);
        await using var context = TestDbContext.Create(clock);
        var program = CreateProgram();
        context.AcademicPrograms.Add(program);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        clock.Advance(TimeSpan.FromDays(30));
        program.Rename("Renamed");

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        program.CreatedAt.Should().Be(InsertedAt);
        program.UpdatedAt.Should().Be(InsertedAt.AddDays(30));
    }

    [Fact]
    public async Task Update_PersistsTheOriginalCreatedAt()
    {
        // Same guarantee as above, but asserted against the stored row rather than the tracked
        // instance, because a change-tracker fix could pass the in-memory assertion and still write
        // a wrong value.
        var clock = new FixedClock(InsertedAt);
        var databaseName = $"saiko-test-{Guid.CreateVersion7()}";

        await using (var seedContext = TestDbContext.Create(clock, databaseName))
        {
            var program = CreateProgram();
            seedContext.AcademicPrograms.Add(program);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        clock.Advance(TimeSpan.FromDays(7));

        await using (var updateContext = TestDbContext.Create(clock, databaseName))
        {
            var stored = await updateContext.AcademicPrograms.FirstAsync(TestContext.Current.CancellationToken);
            stored.Rename("Renamed after a week");
            await updateContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var readContext = TestDbContext.Create(clock, databaseName))
        {
            var stored = await readContext.AcademicPrograms.FirstAsync(TestContext.Current.CancellationToken);

            stored.CreatedAt.Should().Be(InsertedAt);
            stored.UpdatedAt.Should().Be(InsertedAt.AddDays(7));
        }
    }

    [Fact]
    public async Task NoChanges_DoesNotStampAnything()
    {
        var clock = new FixedClock(InsertedAt);
        await using var context = TestDbContext.Create(clock);
        var program = CreateProgram();
        context.AcademicPrograms.Add(program);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        clock.Advance(TimeSpan.FromDays(1));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        program.CreatedAt.Should().Be(InsertedAt);
        program.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task EveryEntityType_IsStamped()
    {
        // The interceptor works off the Entity base type, so reference data is covered too.
        var clock = new FixedClock(InsertedAt);
        await using var context = TestDbContext.Create(clock);

        var level = new AcademicLevel("UG", "Undergraduate");
        var department = new Department("COMP", "Faculty of Computing");
        var yearLevel = new YearLevel(level.Id, "Y1", "First Year", 1);

        context.AcademicLevels.Add(level);
        context.Departments.Add(department);
        context.YearLevels.Add(yearLevel);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        level.CreatedAt.Should().Be(InsertedAt);
        department.CreatedAt.Should().Be(InsertedAt);
        yearLevel.CreatedAt.Should().Be(InsertedAt);
    }
}
