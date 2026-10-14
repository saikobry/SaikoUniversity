namespace Application.Programs.Dtos;

/// <summary>
/// Query parameters for listing programs.
/// </summary>
public sealed record ProgramFilter(
    string? Search = null,
    Guid? AcademicLevelId = null,
    Guid? DepartmentId = null,
    bool? CanAcceptStudents = null,
    int Page = 1,
    int PageSize = 25)
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Clamps paging into a valid range. A caller asking for page zero or a huge page size gets a
    /// bounded answer instead of an error or a full table scan.
    /// </summary>
    public ProgramFilter Normalised()
    {
        var page = Page < 1 ? 1 : Page;
        var size = PageSize switch
        {
            < 1 => 25,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        };

        return this with { Page = page, PageSize = size };
    }
}
