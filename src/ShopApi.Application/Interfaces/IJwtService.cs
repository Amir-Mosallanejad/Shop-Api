using System.Security.Claims;
using ShopApi.Domain.Entities;

namespace ShopApi.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);

    ClaimsPrincipal? ValidateToken(string token);
}
