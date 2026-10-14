using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Infrastructure.Persistence;
using ShopApi.Infrastructure.Repositories;

namespace ShopApi.Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IProductRepository Products { get; }

    public IOrderRepository Orders { get; }

    public IUserRepository Users { get; }

    public ICartRepository Cart { get; }

    public IGenericRepository<Category> Categories { get; }

    public IGenericRepository<Address> Addresses { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Products = new ProductRepository(_context);
        Orders = new OrderRepository(_context);
        Users = new UserRepository(_context);
        Cart = new CartRepository(_context);
        Categories = new GenericRepository<Category>(_context);
        Addresses = new GenericRepository<Address>(_context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}
