using ShopApi.Domain.Entities;

namespace ShopApi.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }

    IOrderRepository Orders { get; }

    IUserRepository Users { get; }

    ICartRepository Cart { get; }

    IGenericRepository<Category> Categories { get; }

    IGenericRepository<Address> Addresses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
