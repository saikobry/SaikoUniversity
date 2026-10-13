using FluentAssertions;
using Application.ReferenceData;
using Application.Tests.Support;

namespace Application.Tests.ReferenceData;

public sealed class ReferenceDataServiceTests : IDisposable
{
    private readonly TestAppDbContext _context = TestAppDbContext.Create();

    private readonly ReferenceDataService _service;

    public ReferenceDataServiceTests() => _service = new ReferenceDataService(_context);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsItems_WhenReferenceDataExists()
    {
        _context.SeedAcademicLevel("UG", "Undergraduate");
        _context.SeedDepartment("COMP", "Faculty of Computing");

        var levels = await _service.GetActiveAcademicLevelsAsync(Token);
        var departments = await _service.GetActiveDepartmentsAsync(Token);

        levels.Should().ContainSingle().Which.Code.Should().Be("UG");
        departments.Should().ContainSingle().Which.Code.Should().Be("COMP");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoReferenceDataExists()
    {
        var levels = await _service.GetActiveAcademicLevelsAsync(Token);
        var departments = await _service.GetActiveDepartmentsAsync(Token);

        levels.Should().BeEmpty();
        departments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ExcludesInactiveEntries_WhenEntryIsDeactivated()
    {
        var activeLevel = _context.SeedAcademicLevel("UG", "Undergraduate");
        var inactiveLevel = _context.SeedAcademicLevel("GR", "Graduate");
        inactiveLevel.Deactivate();

        var activeDepartment = _context.SeedDepartment("COMP", "Faculty of Computing");
        var inactiveDepartment = _context.SeedDepartment("BUS", "School of Business");
        inactiveDepartment.Deactivate();

        _context.SaveChanges();

        var levels = await _service.GetActiveAcademicLevelsAsync(Token);
        var departments = await _service.GetActiveDepartmentsAsync(Token);

        levels.Should().ContainSingle().Which.Id.Should().Be(activeLevel.Id);
        departments.Should().ContainSingle().Which.Id.Should().Be(activeDepartment.Id);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName_WhenMultipleEntriesExist()
    {
        _context.SeedAcademicLevel("UG", "Undergraduate");
        _context.SeedAcademicLevel("GR", "Graduate");
        _context.SeedAcademicLevel("CERT", "Certificate");

        _context.SeedDepartment("SCI", "Science");
        _context.SeedDepartment("ENG", "Engineering");
        _context.SeedDepartment("BUS", "Business");

        var levels = await _service.GetActiveAcademicLevelsAsync(Token);
        var departments = await _service.GetActiveDepartmentsAsync(Token);

        levels.Select(x => x.Name).Should().ContainInConsecutiveOrder("Certificate", "Graduate", "Undergraduate");
        departments.Select(x => x.Name).Should().ContainInConsecutiveOrder("Business", "Engineering", "Science");
    }

    [Fact]
    public async Task GetAllAsync_MapsIdCodeAndName_WhenReturningItems()
    {
        var level = _context.SeedAcademicLevel("UG", "Undergraduate");
        var department = _context.SeedDepartment("COMP", "Faculty of Computing");

        var levels = await _service.GetActiveAcademicLevelsAsync(Token);
        var departments = await _service.GetActiveDepartmentsAsync(Token);

        var levelItem = levels.Should().ContainSingle().Subject;
        levelItem.Id.Should().Be(level.Id);
        levelItem.Code.Should().Be("UG");
        levelItem.Name.Should().Be("Undergraduate");

        var departmentItem = departments.Should().ContainSingle().Subject;
        departmentItem.Id.Should().Be(department.Id);
        departmentItem.Code.Should().Be("COMP");
        departmentItem.Name.Should().Be("Faculty of Computing");
    }
}
