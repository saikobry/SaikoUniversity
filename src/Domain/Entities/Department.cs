using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// The organizational unit that administers programs, and that will own courses and instructors.
/// </summary>
/// <remarks>
/// This replaces the reference system's <c>AcademicDiscipline</c>, which was a classification bucket
/// used for regulator reporting rather than an organizational unit. Kept flat; a college tier above
/// faculties can be added as a nullable self-reference if one is ever needed.
/// </remarks>
public sealed class Department : Entity
{
    private Department()
    {
        // For EF Core materialization. Invariants are enforced in the public constructor.
    }

    public Department(string code, string name)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
