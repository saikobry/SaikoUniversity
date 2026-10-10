using SaikoUniversity.Application.Abstractions;

namespace SaikoUniversity.Infrastructure.Tests.Support;

/// <summary>
/// A clock the test moves by hand, so audit timestamps can be asserted exactly instead of being
/// compared to a wall-clock reading that changes between assertions.
/// </summary>
internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;

    public void Advance(TimeSpan by) => UtcNow += by;

    public void Set(DateTimeOffset value) => UtcNow = value;
}
