using Application.ReferenceData.Dtos;

namespace Api.Contracts.v1;

public sealed record ReferenceItemResponse(Guid Id, string Code, string Name)
{
    public static ReferenceItemResponse From(ReferenceItemDto dto) => new(dto.Id, dto.Code, dto.Name);
}
