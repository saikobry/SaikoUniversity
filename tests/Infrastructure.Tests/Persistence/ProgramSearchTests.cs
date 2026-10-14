using FluentAssertions;
using Application.Common;
using Application.Programs;
using Application.Programs.Dtos;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Tests.Support;

namespace Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises the program search filter against the real SQL Server engine.
/// </summary>
/// <remarks>
/// The filter goes through <c>EF.Functions.Like</c>, which is translated to a SQL <c>LIKE</c>. The
/// InMemory provider does not implement it, so the same assertions against InMemory would fail or
/// pass for the wrong reason — which is why these run here and not in
/// <c>Application.Tests</c>.
/// <para>
/// Each test names the relational behaviour it depends on. SQL Server's default collation is
/// case-insensitive and accent-tolerant, so <c>bscs</c> matches <c>BSCS</c> here. That is a
/// deliberate property of the engine, not of the query.
/// </para>
/// <para>
/// Each test owns a freshly created and dropped database, so no test sees another's rows and none
/// touch the development database.
/// </para>
/// </remarks>
public sealed class ProgramSearchTests : IAsyncLifetime
{
    private SqlServerDatabase? _database;

    private AppDbContext _context = null!;

    private ProgramService _service = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await SqlServerDatabase.TryCreateAsync(TestContext.Current.CancellationToken);

        if (_database is null)
        {
            return;
        }

        _context = _database.CreateContext();
        _service = new ProgramService(new AppDbContextAdapter(_context));

        var level = new AcademicLevel("UG", "Undergraduate");
        var department = new Department("COMP", "Faculty of Computing");
        await _context.AcademicLevels.AddAsync(level, TestContext.Current.CancellationToken);
        await _context.Departments.AddAsync(department, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SeedAsync("BSCS", "BS Computer Science", level.Id, department.Id);
        await SeedAsync("BSIT", "BS Information Technology", level.Id, department.Id);
        await SeedAsync("BSA", "Bachelor of Science in Accounting", level.Id, department.Id);
        await SeedAsync("ABED", "Bachelor of Arts in Education", level.Id, department.Id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [Fact]
    public async Task Search_MatchesCodeSubstring_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "BSC"),
            TestContext.Current.CancellationToken);

        result.Items.Select(x => x.Code).Should().BeEquivalentTo(["BSCS"]);
    }

    [Fact]
    public async Task Search_MatchesNameSubstring_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "Accounting"),
            TestContext.Current.CancellationToken);

        result.Items.Select(x => x.Code).Should().BeEquivalentTo(["BSA"]);
    }

    [Fact]
    public async Task Search_MatchesEitherCodeOrName_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        // "Bachelor of" appears in two names; no code contains it.
        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "Bachelor of"),
            TestContext.Current.CancellationToken);

        result.Items.Select(x => x.Code).Should().BeEquivalentTo(["BSA", "ABED"]);
    }

    [Fact]
    public async Task Search_IsCaseInsensitiveUnderDefaultCollation_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        // Proves the LIKE reaches the engine rather than being evaluated in memory: the same
        // comparison against InMemory is case-sensitive and would return nothing here.
        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "bscs"),
            TestContext.Current.CancellationToken);

        result.Items.Select(x => x.Code).Should().BeEquivalentTo(["BSCS"]);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyPage_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "ZZZNoSuchProgram"),
            TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Search_BlankTerm_ReturnsEveryProgram_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        // A whitespace-only term is normalised away, so the filter must not narrow to nothing.
        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "   "),
            TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Search_CombinesWithPaging_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "B", Page: 1, PageSize: 2),
            TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Search_CombinedWithCanAcceptStudentsOnly_OnSqlServer()
    {
        if (_database is null)
        {
            Assert.Skip("SQL Server is not reachable. Start the Compose container to run this test.");
        }

        // "B" matches all four codes; only the recognised one may survive the second filter.
        var result = await _service.GetAllAsync(
            new ProgramFilter(Search: "B", CanAcceptStudents: true),
            TestContext.Current.CancellationToken);

        result.Items.Select(x => x.Code).Should().BeEquivalentTo(["BSCS", "BSIT"]);
    }

    private async Task SeedAsync(string code, string name, Guid levelId, Guid departmentId)
    {
        var program = new AcademicProgram(code, name, levelId, departmentId, Regulator.Ched, 120, CreditBasis.Hours);

        if (code is "BSCS" or "BSIT")
        {
            program.Recognise();
        }

        _context.AcademicPrograms.Add(program);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
