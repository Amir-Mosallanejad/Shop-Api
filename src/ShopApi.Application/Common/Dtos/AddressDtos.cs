namespace ShopApi.Application.Common.Dtos;

public sealed record AddressDto(
    Guid Id,
    string Province,
    string City,
    string PostalCode,
    string FullAddress);
