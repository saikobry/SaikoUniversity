using FluentAssertions;
using Application.Common;
using Application.Programs;
using Application.Programs.Dtos;
using Application.Tests.Support;
using Domain.Enums;

namespace Application.Tests.Programs;

/// <summary>
/// Covers the rules <see cref="ProgramService"/> enforces before it touches the database, and the
/// lifecycle transitions it applies on write.
/// </summary>
/// <remarks>
/// Every test owns its context, so no test can observe another's rows. Search filtering is absent on
/// purpose: it goes through <c>EF.Functions.Like</c>, which InMemory does not implement.
/// </remarks>
public sealed class ProgramServiceTests : IDisposable
{
    private readonly TestAppDbContext _context = TestAppDbContext.Create();

    private readonly ProgramService _service;

    public ProgramServiceTests() => _service = new ProgramService(_context);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Create_UnknownRegulator_ReturnsFailure()
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId, regulator: "Bogus"), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Message.Should().Contain("regulator");
    }

    [Fact]
    public async Task Create_UnknownCreditBasis_ReturnsFailure()
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId, creditBasis: "Points"), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Message.Should().Contain("credit basis");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankCode_ReturnsFailure(string code)
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId, code: code), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Message.Should().Contain("Code");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_NonPositiveCredits_ReturnsFailure(int credits)
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId, credits: credits), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Message.Should().Contain("RequiredCredits");
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsProgramCodeAlreadyExists()
    {
        var (levelId, departmentId) = _context.SeedReferenceData();
        await _service.CreateAsync(Request(levelId, departmentId), Token);

        var result = await _service.CreateAsync(Request(levelId, departmentId), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCode.ProgramCodeAlreadyExists);
    }

    [Fact]
    public async Task Create_UnknownAcademicLevel_ReturnsProgramInvalidAcademicLevel()
    {
        var (_, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(Guid.CreateVersion7(), departmentId), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCode.ProgramInvalidAcademicLevel);
    }

    [Fact]
    public async Task Create_UnknownDepartment_ReturnsProgramInvalidDepartment()
    {
        var (levelId, _) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, Guid.CreateVersion7()), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCode.ProgramInvalidDepartment);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsLevelAndDepartmentNames()
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId), Token);

        result.Succeeded.Should().BeTrue();
        result.Value!.AcademicLevelCode.Should().Be("UG");
        result.Value.DepartmentName.Should().Be("Faculty of Computing");
    }

    [Fact]
    public async Task Create_RecognisedRequest_ReturnsCanAcceptStudents()
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId, isRecognised: true), Token);

        result.Value!.CanAcceptStudents.Should().BeTrue();
    }

    [Fact]
    public async Task Create_UnrecognisedRequest_ReturnsCanAcceptStudentsFalse()
    {
        var (levelId, departmentId) = _context.SeedReferenceData();

        var result = await _service.CreateAsync(Request(levelId, departmentId, isRecognised: false), Token);

        result.Value!.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public async Task Update_BlankName_ReturnsFailure()
    {
        var program = _context.SeedProgram();

        var result = await _service.UpdateAsync(program.Id, new UpdateProgramRequest("  ", 120, true, false), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Message.Should().Contain("Name");
    }

    [Fact]
    public async Task Update_NonPositiveCredits_ReturnsFailure()
    {
        var program = _context.SeedProgram();

        var result = await _service.UpdateAsync(program.Id, new UpdateProgramRequest("Renamed", 0, true, false), Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Message.Should().Contain("RequiredCredits");
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsProgramNotFound()
    {
        var request = new UpdateProgramRequest("Renamed", 120, true, false);

        var result = await _service.UpdateAsync(Guid.CreateVersion7(), request, Token);

        result.Succeeded.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCode.ProgramNotFound);
    }

    [Fact]
    public async Task Update_ValidRequest_RenamesProgram()
    {
        var program = _context.SeedProgram();

        var result = await _service.UpdateAsync(
            program.Id,
            new UpdateProgramRequest("BS Information Technology", 144, true, false), Token);

        result.Value!.Name.Should().Be("BS Information Technology");
    }

    [Fact]
    public async Task Update_ValidRequest_AppliesCreditChange()
    {
        var program = _context.SeedProgram(requiredCredits: 120);

        var result = await _service.UpdateAsync(
            program.Id,
            new UpdateProgramRequest(program.Name, 180, true, false), Token);

        result.Value!.RequiredCredits.Should().Be(180);
    }

    [Fact]
    public async Task Update_RetireRequested_ReturnsRetiredLifecycle()
    {
        var program = _context.SeedProgram();

        var result = await _service.UpdateAsync(program.Id, new UpdateProgramRequest(program.Name, 120, true, true), Token);

        result.Value!.Lifecycle.Should().Be(nameof(ProgramLifecycle.Retired));
        result.Value.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public async Task Update_RecognitionWithdrawn_ReturnsCanAcceptStudentsFalse()
    {
        var program = _context.SeedProgram(isRegulatorRecognised: true);

        var result = await _service.UpdateAsync(program.Id, new UpdateProgramRequest(program.Name, 120, false, false), Token);

        result.Value!.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsProgram()
    {
        var program = _context.SeedProgram();

        var result = await _service.GetByIdAsync(program.Id, Token);

        result.Should().NotBeNull();
        result!.Code.Should().Be(program.Code);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.CreateVersion7(), Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_NoFilter_ReturnsEveryProgram()
    {
        _context.SeedProgram(code: "AAA");
        _context.SeedProgram(code: "BBB");

        var result = await _service.GetAllAsync(new ProgramFilter(), Token);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_AcademicLevelFilter_ReturnsMatchingProgramsOnly()
    {
        var target = _context.SeedProgram(code: "AAA");
        _context.SeedProgram(code: "BBB");

        var result = await _service.GetAllAsync(new ProgramFilter(AcademicLevelId: target.AcademicLevelId), Token);

        result.Items.Should().ContainSingle().Which.Code.Should().Be("AAA");
    }

    [Fact]
    public async Task GetAll_DepartmentFilter_ReturnsMatchingProgramsOnly()
    {
        var target = _context.SeedProgram(code: "AAA");
        _context.SeedProgram(code: "BBB");

        var result = await _service.GetAllAsync(new ProgramFilter(DepartmentId: target.DepartmentId), Token);

        result.Items.Should().ContainSingle().Which.Code.Should().Be("AAA");
    }

    [Fact]
    public async Task GetAll_CanAcceptStudentsOnly_ExcludesUnrecognisedPrograms()
    {
        _context.SeedProgram(code: "AAA", isRegulatorRecognised: true);
        _context.SeedProgram(code: "BBB", isRegulatorRecognised: false);

        var result = await _service.GetAllAsync(new ProgramFilter(CanAcceptStudentsOnly: true), Token);

        result.Items.Should().ContainSingle().Which.Code.Should().Be("AAA");
    }

    [Fact]
    public async Task GetAll_SecondPage_ReturnsRemainderAndFullCount()
    {
        _context.SeedProgram(code: "AAA");
        _context.SeedProgram(code: "BBB");
        _context.SeedProgram(code: "CCC");

        var result = await _service.GetAllAsync(new ProgramFilter(Page: 2, PageSize: 2), Token);

        result.Items.Should().ContainSingle().Which.Code.Should().Be("CCC");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAll_PageBelowOne_ClampsToFirstPage()
    {
        _context.SeedProgram(code: "AAA");

        var result = await _service.GetAllAsync(new ProgramFilter(Page: 0, PageSize: 25), Token);

        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_PageSizeAboveMaximum_ClampsToMaximum()
    {
        _context.SeedProgram(code: "AAA");

        var result = await _service.GetAllAsync(new ProgramFilter(PageSize: ProgramFilter.MaxPageSize + 1), Token);

        result.PageSize.Should().Be(ProgramFilter.MaxPageSize);
    }

    private static CreateProgramRequest Request(
        Guid levelId,
        Guid departmentId,
        string code = "BSCS",
        string regulator = "Ched",
        int credits = 120,
        string creditBasis = "Hours",
        bool isRecognised = true) =>
        new(code, "BS Computer Science", levelId, departmentId, regulator, credits, creditBasis, isRecognised);
}
