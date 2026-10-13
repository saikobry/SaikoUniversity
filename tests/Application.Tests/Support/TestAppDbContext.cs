using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Domain.Entities;

namespace Application.Tests.Support;

/// <summary>
/// A minimal EF context implementing <see cref="IAppDbContext"/>, backed by the InMemory provider.
/// </summary>
/// <remarks>
/// <c>Application</c> cannot reference <c>Infrastructure</c>, so this cannot reuse the production
/// <c>AppDbContext</c> or its <c>ApplyConfigurationsFromAssembly</c>. Relationships are configured
/// by hand instead, which is all a use-case test needs.
/// <para>
/// InMemory does not enforce foreign keys and does not implement <c>EF.Functions.Like</c>, so a test
/// that depends on either is testing persistence, not use-case logic. Those belong in
/// <c>Infrastructure.Tests</c> against the Compose SQL Server.
/// </para>
/// </remarks>
internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public IQueryable<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();

    public IQueryable<AcademicLevel> AcademicLevels => Set<AcademicLevel>();

    public IQueryable<Department> Departments => Set<Department>();

    public IQueryable<YearLevel> YearLevels => Set<YearLevel>();

    public void AddProgram(AcademicProgram program) => Set<AcademicProgram>().Add(program);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicProgram>(program =>
        {
            program.HasOne(x => x.AcademicLevel)
                .WithMany()
                .HasForeignKey(x => x.AcademicLevelId);

            program.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId);
        });

        modelBuilder.Entity<YearLevel>(level =>
            level.HasOne<AcademicLevel>()
                .WithMany()
                .HasForeignKey(x => x.AcademicLevelId));

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Creates a context on a database that no other test shares, so no test can observe another's
    /// writes.
    /// </summary>
    public static TestAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase($"saiko-application-test-{Guid.CreateVersion7()}")
            .Options;

        return new TestAppDbContext(options);
    }

    /// <summary>
    /// Adds the reference data a program must point at, and returns the ids.
    /// </summary>
    public (Guid LevelId, Guid DepartmentId) SeedReferenceData()
    {
        var level = new AcademicLevel("UG", "Undergraduate");
        var department = new Department("COMP", "Faculty of Computing");

        Set<AcademicLevel>().Add(level);
        Set<Department>().Add(department);
        SaveChanges();

        return (level.Id, department.Id);
    }

    /// <summary>
    /// Adds a program together with the reference data it needs, so a test can start from a valid
    /// row without arranging it by hand.
    /// </summary>
    public AcademicProgram SeedProgram(
        string code = "BSCS",
        string name = "BS Computer Science",
        int requiredCredits = 120,
        bool isRegulatorRecognised = true)
    {
        var (levelId, departmentId) = SeedReferenceData();

        var program = new AcademicProgram(
            code,
            name,
            levelId,
            departmentId,
            Domain.Enums.Regulator.Ched,
            requiredCredits,
            Domain.Enums.CreditBasis.Hours);

        if (isRegulatorRecognised)
        {
            program.Recognise();
        }

        Set<AcademicProgram>().Add(program);
        SaveChanges();

        return program;
    }
}
