using Microsoft.EntityFrameworkCore;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Infrastructure.Persistence;

namespace ShopApi.Infrastructure.Repositories;

public sealed class CartRepository : GenericRepository<CartItem>, ICartRepository
{
    public CartRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<CartItem>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await Set.Include(l => l.Product).ThenInclude(p => p!.Images)
            .Where(l => l.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<CartItem?> GetLineAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(
            l => l.UserId == userId && l.ProductId == productId,
            cancellationToken);

    public Task ClearUserCartAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}
