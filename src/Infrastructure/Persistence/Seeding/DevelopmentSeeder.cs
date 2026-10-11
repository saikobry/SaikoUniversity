using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent curriculum seed data for local development.
/// </summary>
/// <remarks>
/// Deterministic on purpose: the same codes and names every run, so a screenshot or a test can rely
/// on them. Idempotent because it keys off the unique code columns and checks before inserting.
/// The institution is fictional, so the data is internally consistent without claiming to represent
/// a real school.
/// </remarks>
public static class DevelopmentSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedAcademicLevelsAsync(context, cancellationToken);
        await SeedDepartmentsAsync(context, cancellationToken);
        await SeedYearLevelsAsync(context, cancellationToken);
        await SeedAcademicProgramsAsync(context, cancellationToken);
    }

    private static async Task SeedAcademicLevelsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var levels = new[]
        {
            new AcademicLevel("UG", "Undergraduate"),
            new AcademicLevel("GRAD", "Graduate"),
            new AcademicLevel("VOC", "Vocational")
        };

        foreach (var level in levels)
        {
            var exists = await context.AcademicLevels
                .AnyAsync(x => x.Code == level.Code, cancellationToken);

            if (!exists)
            {
                context.AcademicLevels.Add(level);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDepartmentsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var departments = new[]
        {
            new Department("COMP", "Faculty of Computing"),
            new Department("ENG", "Faculty of Engineering"),
            new Department("BUS", "Faculty of Business Administration"),
            new Department("EDU", "Faculty of Education"),
            new Department("MED", "Faculty of Medicine"),
            new Department("SBS", "Faculty of Social and Behavioral Sciences"),
            new Department("TECH", "Faculty of Technical and Vocational Studies")
        };

        foreach (var department in departments)
        {
            var exists = await context.Departments
                .AnyAsync(x => x.Code == department.Code, cancellationToken);

            if (!exists)
            {
                context.Departments.Add(department);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedYearLevelsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        // Year levels belong to a level rather than to a program, so every program under the level
        // shares this list. Only levels that are actually year-based get one.
        var definitions = new (string LevelCode, string Code, string DisplayName, int SortOrder)[]
        {
            ("UG", "Y1", "First Year", 1),
            ("UG", "Y2", "Second Year", 2),
            ("UG", "Y3", "Third Year", 3),
            ("UG", "Y4", "Fourth Year", 4),
            ("GRAD", "M1", "First Year", 1),
            ("GRAD", "M2", "Second Year", 2),
            ("VOC", "V1", "First Year", 1),
            ("VOC", "V2", "Second Year", 2)
        };

        var levels = await context.AcademicLevels
            .Where(x => definitions.Select(d => d.LevelCode).Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var definition in definitions)
        {
            if (!levels.TryGetValue(definition.LevelCode, out var level))
            {
                continue;
            }

            var exists = await context.YearLevels
                .AnyAsync(x => x.AcademicLevelId == level.Id && x.Code == definition.Code, cancellationToken);

            if (!exists)
            {
                context.YearLevels.Add(new YearLevel(
                    level.Id,
                    definition.Code,
                    definition.DisplayName,
                    definition.SortOrder));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAcademicProgramsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var definitions = new (string Code, string Name, string LevelCode, string DepartmentCode, Regulator Regulator, int Credits, CreditBasis Basis)[]
        {
            // Undergraduate, degree programs, credit hours.
            ("BSCS", "Bachelor of Science in Computer Science", "UG", "COMP", Regulator.Ched, 144, CreditBasis.Hours),
            ("BSIT", "Bachelor of Science in Information Technology", "UG", "COMP", Regulator.Ched, 144, CreditBasis.Hours),
            ("BSIS", "Bachelor of Science in Information Systems", "UG", "COMP", Regulator.Ched, 144, CreditBasis.Hours),
            ("BSCE", "Bachelor of Science in Civil Engineering", "UG", "ENG", Regulator.Ched, 168, CreditBasis.Hours),
            ("BSME", "Bachelor of Science in Mechanical Engineering", "UG", "ENG", Regulator.Ched, 168, CreditBasis.Hours),
            ("BSA", "Bachelor of Science in Accounting", "UG", "BUS", Regulator.Ched, 144, CreditBasis.Hours),
            ("BSBA", "Bachelor of Science in Business Administration", "UG", "BUS", Regulator.Ched, 144, CreditBasis.Hours),
            ("ABED", "Bachelor of Arts in Education, Mathematics", "UG", "EDU", Regulator.Ched, 144, CreditBasis.Hours),
            ("BSN", "Bachelor of Science in Nursing", "UG", "MED", Regulator.Ched, 152, CreditBasis.Hours),
            ("BSPS", "Bachelor of Science in Psychology", "UG", "SBS", Regulator.Ched, 144, CreditBasis.Hours),

            // Graduate, degree programs, credit hours.
            ("MIT", "Master of Information Technology", "GRAD", "COMP", Regulator.Ched, 36, CreditBasis.Hours),
            ("MBA", "Master of Business Administration", "GRAD", "BUS", Regulator.Ched, 42, CreditBasis.Hours),
            ("MAED", "Master of Arts in Education", "GRAD", "EDU", Regulator.Ched, 36, CreditBasis.Hours),
            ("MSN", "Master of Science in Nursing", "GRAD", "MED", Regulator.Ched, 40, CreditBasis.Hours),

            // Vocational, unit-based rather than hour-based.
            ("DICT", "Diploma in Information and Communications Technology", "VOC", "TECH", Regulator.Tesda, 24, CreditBasis.Units),
            ("DTS", "Diploma in Technical Studies", "VOC", "TECH", Regulator.Tesda, 36, CreditBasis.Units),
            ("CNC", "Certificate in Network Configuration", "VOC", "TECH", Regulator.Tesda, 18, CreditBasis.Units),

            // Not yet recognized by the regulator. Cannot accept registration until it is.
            ("BSARCH", "Bachelor of Science in Architecture", "UG", "ENG", Regulator.Ched, 168, CreditBasis.Hours)
        };

        var levels = await context.AcademicLevels.ToDictionaryAsync(x => x.Code, cancellationToken);
        var departments = await context.Departments.ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var definition in definitions)
        {
            var exists = await context.AcademicPrograms
                .AnyAsync(x => x.Code == definition.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            if (!levels.TryGetValue(definition.LevelCode, out var level) ||
                !departments.TryGetValue(definition.DepartmentCode, out var department))
            {
                continue;
            }

            var program = new AcademicProgram(
                definition.Code,
                definition.Name,
                level.Id,
                department.Id,
                definition.Regulator,
                definition.Credits,
                definition.Basis);

            // Everything except the last one is regulator-recognized, which is what makes it
            // eligible to accept students.
            if (definition.Code != "BSARCH")
            {
                program.Recognise();
            }

            context.AcademicPrograms.Add(program);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
