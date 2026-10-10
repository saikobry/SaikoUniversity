using Domain.Common;

namespace Application.Abstractions;

/// <summary>
/// Source of the current time. Injected rather than calling <see cref="DateTimeOffset.UtcNow"/>
/// directly so tests can control time without sleeping.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
