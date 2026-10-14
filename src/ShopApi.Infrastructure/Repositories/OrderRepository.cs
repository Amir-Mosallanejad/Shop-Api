using Microsoft.EntityFrameworkCore;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Infrastructure.Persistence;

namespace ShopApi.Infrastructure.Repositories;

public sealed class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    public OrderRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        OrdersWithDetails().FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<PagedResult<Order>> GetByUserPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);

        var query = Set
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(items, page, pageSize, totalCount);
    }

    private IQueryable<Order> OrdersWithDetails() =>
        Set.Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Address);
}
