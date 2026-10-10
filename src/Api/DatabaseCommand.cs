using Microsoft.EntityFrameworkCore;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seeding;

namespace Api;

/// <summary>
/// Maintenance entry points invoked as <c>dotnet run --project src/Api -- seed</c>.
/// </summary>
/// <remarks>
/// These run as a separate process from the web host, so a shared environment can never be seeded
/// by an instance booting to serve a request. Every command is idempotent.
/// </remarks>
internal static class DatabaseCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || !args[0].Equals("seed", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("usage: dotnet run --project src/Api -- seed");
            Console.Error.WriteLine();
            Console.Error.WriteLine("  seed   apply pending migrations, then insert curriculum reference data if absent");
            return 2;
        }

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddInfrastructure(builder.Configuration);

        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Console.WriteLine("Applying migrations...");
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            Console.WriteLine("  none pending.");
        }
        else
        {
            await context.Database.MigrateAsync();
            Console.WriteLine($"  applied {pending.Count} migration(s).");
        }

        Console.WriteLine("Seeding curriculum reference data...");
        await DevelopmentSeeder.SeedAsync(context);

        Console.WriteLine($"  AcademicLevels:   {await context.AcademicLevels.CountAsync()}");
        Console.WriteLine($"  Departments:      {await context.Departments.CountAsync()}");
        Console.WriteLine($"  YearLevels:       {await context.YearLevels.CountAsync()}");
        Console.WriteLine($"  AcademicPrograms: {await context.AcademicPrograms.CountAsync()}");

        Console.WriteLine("Done.");
        return 0;
    }
}
