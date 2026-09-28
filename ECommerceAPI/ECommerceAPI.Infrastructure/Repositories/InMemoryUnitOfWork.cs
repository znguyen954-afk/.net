using ECommerceAPI.Domain.Interfaces;
using ECommerceAPI.Infrastructure.Repositories;

namespace ECommerceAPI.Infrastructure.Repositories;

/// <summary>
/// In-memory Unit of Work - coordinates all repositories within a single scope
/// </summary>
public class InMemoryUnitOfWork : IUnitOfWork
{
    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IOrderRepository Orders { get; }
    public IUserRepository Users { get; }

    public InMemoryUnitOfWork(
        IProductRepository products,
        ICategoryRepository categories,
        IOrderRepository orders,
        IUserRepository users)
    {
        Products = products;
        Categories = categories;
        Orders = orders;
        Users = users;
    }

    // In-memory store doesn't need real transaction management
    public Task<int> SaveChangesAsync() => Task.FromResult(1);

    public void Dispose() { }
}
