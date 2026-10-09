using Microsoft.EntityFrameworkCore;
using SaikoUniversity.Application.Abstractions;
using SaikoUniversity.Domain.Entities;

namespace SaikoUniversity.Infrastructure.Persistence;

/// <summary>
/// Adapts <see cref="AppDbContext"/> to the narrow surface <c>Application</c> is allowed to use.
/// </summary>
/// <remarks>
/// This is where EF Core stops. The application layer sees <c>IQueryable&lt;T&gt;</c> and nothing
/// about SQL Server.
/// </remarks>
internal sealed class AppDbContextAdapter(AppDbContext context) : IAppDbContext
{
    public IQueryable<AcademicProgram> AcademicPrograms => context.AcademicPrograms;

    public IQueryable<AcademicLevel> AcademicLevels => context.AcademicLevels;

    public IQueryable<Department> Departments => context.Departments;

    public IQueryable<YearLevel> YearLevels => context.YearLevels;

    public void AddProgram(AcademicProgram program) => context.AcademicPrograms.Add(program);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
