namespace Application.Programs.Dtos;

/// <summary>
/// Payload for updating a program's mutable fields.
/// </summary>
/// <remarks>
/// The code, academic level, department, regulator, and credit basis are fixed after creation, so
/// they are not part of this request.
/// </remarks>
public sealed record UpdateProgramRequest(
    string Name,
    int RequiredCredits,
    bool IsRegulatorRecognised,
    bool IsRetired);
