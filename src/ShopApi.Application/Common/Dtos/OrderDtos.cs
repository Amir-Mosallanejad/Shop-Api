namespace ShopApi.Application.Common.Dtos;

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    string Currency,
    AddressDto Address,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt);
