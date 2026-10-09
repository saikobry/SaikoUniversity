using SaikoUniversity.Domain.Common;

namespace SaikoUniversity.Domain.Entities;

/// <summary>
/// A level of education, for example undergraduate or graduate.
/// </summary>
/// <remarks>
/// Intentionally flat with no <c>ParentId</c>. The reference system uses a self-referencing tree so
/// that reporting queries can filter by a parent and include its descendants, but that requirement
/// belongs to reporting, not to the curriculum. A grouping can be added later as a separate table
/// without changing this one.
/// </remarks>
public sealed class AcademicLevel : Entity
{
    private AcademicLevel()
    {
        // For EF Core materialization. Invariants are enforced in the public constructor.
    }

    public AcademicLevel(string code, string name)
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
