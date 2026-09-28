using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Enums;

namespace ECommerceAPI.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);
    Task<IEnumerable<Product>> SearchAsync(string keyword);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? keyword = null,
        Guid? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        ProductStatus? status = null,
        string? sortBy = null,
        bool sortDesc = false);
    Task<bool> IsSkuExistsAsync(string sku, Guid? excludeId = null);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetWithProductsAsync(Guid id);
    Task<IEnumerable<Category>> GetAllWithProductCountAsync();
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithDetailsAsync(Guid id);
    Task<IEnumerable<Order>> GetByUserAsync(Guid userId);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? userId = null,
        OrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? sortBy = null,
        bool sortDesc = false);
    Task<string> GenerateOrderNumberAsync();
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> IsEmailExistsAsync(string email, Guid? excludeId = null);
    Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? keyword = null,
        UserRole? role = null,
        string? sortBy = null,
        bool sortDesc = false);
}
