namespace Application.Programs.Dtos;

/// <summary>
/// Payload for creating a program.
/// </summary>
public sealed record CreateProgramRequest(
    string Code,
    string Name,
    Guid AcademicLevelId,
    Guid DepartmentId,
    string Regulator,
    int RequiredCredits,
    string CreditBasis,
    bool IsRegulatorRecognised);
