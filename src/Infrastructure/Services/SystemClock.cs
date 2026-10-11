using Application.Abstractions;

namespace Infrastructure.Services;

/// <summary>
/// System clock. The single place <see cref="DateTimeOffset.UtcNow"/> is called in production code.
/// </summary>
/// <remarks>
/// Tests substitute <see cref="FixedClock"/> or a test double rather than sleeping.
/// </remarks>
internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
