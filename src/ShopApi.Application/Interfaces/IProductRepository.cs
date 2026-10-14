using ShopApi.Application.Common.Wrappers;
using ShopApi.Domain.Entities;

namespace ShopApi.Application.Interfaces;

public sealed record ProductFilter(
    Guid? CategoryId,
    string? Search,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    bool SortDescending,
    int Page,
    int PageSize);

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<PagedResult<Product>> GetPagedAsync(ProductFilter filter, CancellationToken cancellationToken = default);

    Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
