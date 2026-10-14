namespace ShopApi.Application.Common.Dtos;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId);
