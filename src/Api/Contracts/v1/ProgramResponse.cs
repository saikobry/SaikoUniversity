using Application.Common;
using Application.Programs.Dtos;

namespace Api.Contracts.v1;

/// <summary>
/// Response shape for program endpoints. Independent of both the entity and the application DTO,
/// so a change to either cannot silently alter the wire contract.
/// </summary>
public sealed record ProgramResponse(
    Guid Id,
    string Code,
    string Name,
    Guid AcademicLevelId,
    string AcademicLevelCode,
    Guid DepartmentId,
    string DepartmentName,
    string Regulator,
    bool IsRegulatorRecognised,
    string Lifecycle,
    int RequiredCredits,
    string CreditBasis,
    bool CanAcceptStudents)
{
    public static ProgramResponse From(ProgramDto dto) => new(
        dto.Id,
        dto.Code,
        dto.Name,
        dto.AcademicLevelId,
        dto.AcademicLevelCode,
        dto.DepartmentId,
        dto.DepartmentName,
        dto.Regulator,
        dto.IsRegulatorRecognised,
        dto.Lifecycle,
        dto.RequiredCredits,
        dto.CreditBasis,
        dto.CanAcceptStudents);
}

public sealed record ProgramListResponse(
    IReadOnlyList<ProgramResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int PageCount)
{
    public static ProgramListResponse From(PagedResult<ProgramDto> result) => new(
        result.Items.Select(ProgramResponse.From).ToList(),
        result.TotalCount,
        result.Page,
        result.PageSize,
        result.PageCount);
}

public sealed record CreateProgramRequest(
    string Code,
    string Name,
    Guid AcademicLevelId,
    Guid DepartmentId,
    string Regulator,
    int RequiredCredits,
    string CreditBasis,
    bool IsRegulatorRecognised);

/// <summary>
/// Payload for updating a program. Only the mutable fields are exposed; the code, level, department,
/// regulator, and credit basis are fixed after creation because existing records reference them.
/// </summary>
public sealed record UpdateProgramRequest(
    string Name,
    int RequiredCredits);

public sealed record ErrorResponse(string Code, string Message);
