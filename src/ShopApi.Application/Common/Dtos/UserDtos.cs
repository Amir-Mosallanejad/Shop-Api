namespace ShopApi.Application.Common.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role);

public sealed record AuthResponseDto(string Token, UserDto User);
