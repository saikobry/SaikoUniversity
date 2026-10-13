using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Application.ReferenceData.Dtos;

namespace Application.ReferenceData;

/// <summary>
/// Read-only option lists for selectors, returned as (Id, Code, Name). Entities with a richer
/// shape, parameters, or their own rules get their own feature.
/// </summary>
public sealed class ReferenceDataService(IAppDbContext context)
{
    public async Task<IReadOnlyList<ReferenceItemDto>> GetActiveAcademicLevelsAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.AcademicLevels
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferenceItemDto>> GetActiveDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Departments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
    }
}
