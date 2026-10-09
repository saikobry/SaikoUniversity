namespace SaikoUniversity.Domain.Enums;

/// <summary>
/// Whether a program can currently accept students.
/// Deliberately has no <c>Draft</c> state: the reference system hardcodes an active flag on
/// insert, so a program is either offerable or retired.
/// </summary>
public enum ProgramLifecycle
{
    Active = 1,

    Retired = 2
}
