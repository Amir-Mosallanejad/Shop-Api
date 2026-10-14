using Microsoft.EntityFrameworkCore;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Infrastructure.Persistence;

namespace ShopApi.Infrastructure.Repositories;

public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    public ProductRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Set.Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        Set.AnyAsync(
            p => p.Slug == slug && (!excludeId.HasValue || p.Id != excludeId.Value),
            cancellationToken);

    public Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        Set.CountAsync(p => p.CategoryId == categoryId, cancellationToken);

    public async Task<PagedResult<Product>> GetPagedAsync(
        ProductFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, MinPageSize, MaxPageSize);

        var query = Set
            .Include(p => p.Images)
            .Include(p => p.Category)
            .AsQueryable();

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = EscapeLike(filter.Search.Trim());
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount <= filter.MaxPrice.Value);
        }

        var descending = filter.SortDescending;
        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "name" when !descending => query.OrderBy(p => p.Name),
            "name" => query.OrderByDescending(p => p.Name),
            "price" when !descending => query.OrderBy(p => p.Price.Amount),
            "price" => query.OrderByDescending(p => p.Price.Amount),
            _ when !descending => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, page, pageSize, totalCount);
    }

    private static string EscapeLike(string term) =>
        term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
