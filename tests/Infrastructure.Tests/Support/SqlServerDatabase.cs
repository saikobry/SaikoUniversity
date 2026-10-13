using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Infrastructure.Persistence;

namespace Infrastructure.Tests.Support;

/// <summary>
/// Creates a disposable SQL Server database for a test that must exercise real relational
/// behaviour, and drops it afterwards.
/// </summary>
/// <remarks>
/// Some of what <c>ProgramService</c> writes only means anything against the real engine.
/// <c>EF.Functions.Like</c> in particular is translated to a SQL <c>LIKE</c>, and the InMemory
/// provider does not implement it at all, so a filter test on InMemory would either throw or pass
/// for the wrong reason.
/// <para>
/// The database is created with a unique name and dropped on dispose, so tests never read or write
/// the development database and never depend on rows another test left behind. Migrations are
/// applied rather than <c>EnsureCreated</c>, because the schema under test is the migrated one.
/// </para>
/// <para>
/// This needs the Compose SQL container to be up. When it is not, tests using this fixture report
/// a skipped result rather than a failure, so the suite still runs on a machine with no Docker.
/// </para>
/// </remarks>
internal sealed class SqlServerDatabase : IAsyncDisposable
{
    private readonly string _connectionString;

    private SqlServerDatabase(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        DatabaseName = databaseName;
    }

    public string DatabaseName { get; }

    /// <summary>
    /// The Compose container's connection string. Read from the same source the application uses, so
    /// a change to the local setup is picked up here without editing a constant.
    /// </summary>
    /// <remarks>
    /// The repository root is found by walking up to the solution file rather than by counting
    /// directory levels, which breaks the moment the output path depth changes.
    /// </remarks>
    public static string BaseConnectionString =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepositoryRoot(), "src", "Api", "appsettings.Development.json"), optional: false)
            .Build()
            .GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Default was not found in src/Api/appsettings.Development.json.");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.slnx").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No solution file found at or above {AppContext.BaseDirectory}. Cannot locate the repository root.");
    }

    /// <summary>
    /// Creates the database and applies migrations. Returns null when the server is unreachable, so a
    /// caller can skip instead of fail.
    /// </summary>
    public static async Task<SqlServerDatabase?> TryCreateAsync(CancellationToken cancellationToken = default)
    {
        var databaseName = $"saiko-relational-test-{Guid.CreateVersion7().ToString()[..8]}";
        var masterConnectionString = BaseConnectionString;

        try
        {
            await using (var connection = new SqlConnection(masterConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await using var create = connection.CreateCommand();
                create.CommandText = $"CREATE DATABASE [{databaseName}];";
                await create.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException or TimeoutException)
        {
            // The Compose container is not up. Callers skip rather than fail, so the suite still runs
            // on a machine without Docker.
            return null;
        }

        var database = new SqlServerDatabase(masterConnectionString, databaseName);
        await database.ApplyMigrationsAsync(cancellationToken);
        return database;
    }

    /// <summary>
    /// Opens a context against the created database.
    /// </summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(WithDatabase(_connectionString, DatabaseName))
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Swaps the target catalog, keeping the server and credentials.
    /// </summary>
    private static string WithDatabase(string connectionString, string databaseName) =>
        new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName }.ConnectionString;

    private async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // Pools keep connections open past disposal, which blocks the drop. Clearing the pool is the
        // documented remedy.
        SqlConnection.ClearAllPools();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS [{DatabaseName}];";
            await drop.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Leaving an orphan test database behind is untidy but not a test failure.
        }
    }
}
