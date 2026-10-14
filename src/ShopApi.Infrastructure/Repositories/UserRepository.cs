using Microsoft.EntityFrameworkCore;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Infrastructure.Persistence;

namespace ShopApi.Infrastructure.Repositories;

public sealed class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email), cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(u => u.Email == NormalizeEmail(email), cancellationToken);

    // Must stay in lockstep with User's email normalization: Trim + lowercase.
    // If the Domain rule ever changes, this method must change with it.
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
