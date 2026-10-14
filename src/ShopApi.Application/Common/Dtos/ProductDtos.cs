namespace ShopApi.Application.Common.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    Guid CategoryId,
    string? CategoryName,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAt);

public sealed record ProductImageDto(
    Guid Id,
    string Url,
    bool IsMain);

public sealed record CreateProductImageRequest(
    string Url,
    bool IsMain);
