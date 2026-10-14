using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;

namespace ShopApi.Infrastructure.Services;

public sealed class JwtService : IJwtService
{
    private const int MinSecretLength = 32;
    private const int DefaultExpiryHours = 24;
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(2);

    private readonly byte[] _key;
    private readonly TimeSpan _lifetime;

    public JwtService(IConfiguration configuration)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < MinSecretLength)
        {
            throw new InvalidOperationException(
                $"JWT secret is not configured or too weak. Set the JWT_SECRET environment variable (at least {MinSecretLength} characters).");
        }

        _key = Encoding.UTF8.GetBytes(secret);

        var expiryHours = DefaultExpiryHours;
        if (int.TryParse(configuration["Jwt:ExpiryHours"], out var configured) && configured > 0)
        {
            expiryHours = configured;
        }

        _lifetime = TimeSpan.FromHours(expiryHours);
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(_lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_key),
                    ClockSkew = ClockSkew
                },
                out _);
        }
        catch
        {
            return null;
        }
    }
}
