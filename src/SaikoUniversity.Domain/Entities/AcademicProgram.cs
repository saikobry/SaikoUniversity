using SaikoUniversity.Domain.Common;
using SaikoUniversity.Domain.Enums;

namespace SaikoUniversity.Domain.Entities;

/// <summary>
/// A degree or qualification a student can enroll in.
/// </summary>
public sealed class AcademicProgram : Entity
{
    private AcademicProgram()
    {
        // For EF Core materialization. Invariants are enforced in the public constructor.
    }

    public AcademicProgram(
        string code,
        string name,
        Guid academicLevelId,
        Guid departmentId,
        Regulator regulator,
        int requiredCredits,
        CreditBasis creditBasis)
    {
        Code = code;
        Name = name;
        AcademicLevelId = academicLevelId;
        DepartmentId = departmentId;
        Regulator = regulator;
        RequiredCredits = requiredCredits;
        CreditBasis = creditBasis;
        IsRegulatorRecognised = false;
        Lifecycle = ProgramLifecycle.Active;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid AcademicLevelId { get; private set; }

    public AcademicLevel? AcademicLevel { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Department? Department { get; private set; }

    /// <summary>The external authority that recognizes this program.</summary>
    public Regulator Regulator { get; private set; }

    /// <summary>
    /// Whether the regulator recognizes this program. An unrecognised program cannot accept
    /// registration or enrollment.
    /// </summary>
    public bool IsRegulatorRecognised { get; private set; }

    /// <summary>Whether the program can currently accept students.</summary>
    public ProgramLifecycle Lifecycle { get; private set; }

    public int RequiredCredits { get; private set; }

    public CreditBasis CreditBasis { get; private set; }

    public bool IsDeleted { get; private set; }

    /// <summary>
    /// True when the program may accept students. Both conditions are required: the program must
    /// be active and recognized.
    /// </summary>
    public bool CanAcceptStudents => Lifecycle == ProgramLifecycle.Active && IsRegulatorRecognised;

    public void Rename(string name) => Name = name;

    public void SetRequiredCredits(int requiredCredits) => RequiredCredits = requiredCredits;

    public void Recognise() => IsRegulatorRecognised = true;

    public void WithdrawRecognition() => IsRegulatorRecognised = false;

    public void Retire() => Lifecycle = ProgramLifecycle.Retired;

    public void Reinstate() => Lifecycle = ProgramLifecycle.Active;

    public void SoftDelete() => IsDeleted = true;
}
