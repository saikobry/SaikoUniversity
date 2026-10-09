using SaikoUniversity.Domain.Common;

namespace SaikoUniversity.Domain.Entities;

/// <summary>
/// An ordered position within an <see cref="AcademicLevel"/>, for example second year.
/// </summary>
/// <remarks>
/// Belongs to an academic level rather than to a program, so every program under that level shares
/// the same ordered list. This matches the reference system, where the equivalent table has only a
/// foreign key to the level and a sort order.
/// </remarks>
public sealed class YearLevel : Entity
{
    private YearLevel()
    {
        // For EF Core materialization. Invariants are enforced in the public constructor.
    }

    public YearLevel(Guid academicLevelId, string code, string displayName, int sortOrder)
    {
        AcademicLevelId = academicLevelId;
        Code = code;
        DisplayName = displayName;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public Guid AcademicLevelId { get; private set; }

    public AcademicLevel? AcademicLevel { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
