namespace ShopApi.Application.Common.Dtos;

public sealed record CartItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal,
    string? ImageUrl);

public sealed record CartDto(
    IReadOnlyList<CartItemDto> Items,
    decimal TotalAmount,
    string Currency,
    int TotalItems);
