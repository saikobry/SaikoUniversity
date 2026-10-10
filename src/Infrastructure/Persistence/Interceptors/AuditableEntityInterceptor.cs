using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Application.Abstractions;
using Domain.Common;

namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <c>CreatedAt</c> and <c>UpdatedAt</c> on every entity that derives from
/// <see cref="Entity"/>, so no application code has to set them by hand.
/// </summary>
/// <remarks>
/// Time comes from <see cref="IClock"/> rather than <see cref="DateTimeOffset.UtcNow"/>, so tests can
/// assert exact timestamps without sleeping.
/// </remarks>
public sealed class AuditableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Stamp(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Stamp(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext context)
    {
        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(x => x.CreatedAt).CurrentValue = now;
                    break;

                case EntityState.Modified:
                    entry.Property(x => x.UpdatedAt).CurrentValue = now;

                    // An update must never change the moment a row was created. Without this the
                    // interceptor would stamp both columns on every save.
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(entry.State),
                        entry.State,
                        $"Unexpected entity state in the change tracker: {entry.State}.");
            }
        }
    }
}
