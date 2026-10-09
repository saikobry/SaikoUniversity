using Microsoft.EntityFrameworkCore;
using SaikoUniversity.Domain.Entities;

namespace SaikoUniversity.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AcademicLevel> AcademicLevels => Set<AcademicLevel>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<YearLevel> YearLevels => Set<YearLevel>();

    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
