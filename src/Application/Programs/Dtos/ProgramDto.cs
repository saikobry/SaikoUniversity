namespace Application.Programs.Dtos;

/// <summary>
/// A program as returned by the API. Declared separately from the entity so a change to
/// <c>AcademicProgram</c> cannot silently change the wire shape.
/// </summary>
public sealed record ProgramDto(
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
    bool CanAcceptStudents);
