using ShopApi.Domain.Entities;

namespace ShopApi.Application.Interfaces;

public interface ICartRepository : IGenericRepository<CartItem>
{
    Task<IReadOnlyList<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CartItem?> GetLineAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    Task ClearUserCartAsync(Guid userId, CancellationToken cancellationToken = default);
}
