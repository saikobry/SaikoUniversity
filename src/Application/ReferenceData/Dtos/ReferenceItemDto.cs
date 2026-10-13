namespace Application.ReferenceData.Dtos;

/// <summary>
/// A read-only reference entry for populating a selector: id, code, display name.
/// </summary>
public sealed record ReferenceItemDto(Guid Id, string Code, string Name);
