using Domain.Entities;

namespace Application.Abstractions;

/// <summary>
/// The narrow slice of persistence that use-case services are allowed to touch.
/// </summary>
/// <remarks>
/// Declared here so this layer compiles and tests without an EF Core reference. Only
/// <c>Infrastructure</c> knows which provider backs it.
/// </remarks>
public interface IAppDbContext
{
    IQueryable<AcademicProgram> AcademicPrograms { get; }

    IQueryable<AcademicLevel> AcademicLevels { get; }

    IQueryable<Department> Departments { get; }

    IQueryable<YearLevel> YearLevels { get; }

    /// <summary>
    /// Stages a new program for insert.
    /// </summary>
    void AddProgram(AcademicProgram program);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
