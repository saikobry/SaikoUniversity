namespace Application.Common;

/// <summary>
/// The single list-response shape used by every paged endpoint.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
/// <param name="Items">The page of results.</param>
/// <param name="TotalCount">Total matching rows, ignoring paging.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Rows per page.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int PageCount => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Empty(int page, int pageSize)
        => new([], 0, page, pageSize);
}
