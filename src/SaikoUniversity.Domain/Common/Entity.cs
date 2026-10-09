namespace SaikoUniversity.Domain.Common;

/// <summary>
/// Base type for every persistent entity.
/// </summary>
/// <remarks>
/// Setters are public so the EF Core interceptors can populate audit columns and materialize
/// entities. Domain entities that need to hide their setters do so by declaring their own private
/// setters on top of this.
/// <para>
/// <see cref="Id"/> is a UUIDv7 rather than an <c>INT IDENTITY</c>. SQL Server compares
/// <c>uniqueidentifier</c> right-to-left, so this does not improve clustered-key locality. The
/// justification is client-side generation without a round trip, and ids that are not guessable or
/// enumerable.
/// </para>
/// <para>
/// Audit columns are stamped by <c>AuditableEntityInterceptor</c>, never by hand.
/// <see cref="UpdatedAt"/> is nullable so a row that has never been modified reads as null rather than
/// as a meaningless year-one date.
/// </para>
/// </remarks>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
