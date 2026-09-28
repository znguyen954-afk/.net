using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Enums;
using ECommerceAPI.Domain.Interfaces;

namespace ECommerceAPI.Infrastructure.Repositories;

public class ProductRepository : InMemoryRepository<Product>, IProductRepository
{
    public Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId)
    {
        var result = _store.Values.Where(p => !p.IsDeleted && p.CategoryId == categoryId);
        return Task.FromResult(result);
    }

    public Task<IEnumerable<Product>> SearchAsync(string keyword)
    {
        var lower = keyword.ToLower();
        var result = _store.Values.Where(p =>
            !p.IsDeleted &&
            (p.Name.ToLower().Contains(lower) ||
             p.Description.ToLower().Contains(lower) ||
             p.SKU.ToLower().Contains(lower)));
        return Task.FromResult(result);
    }

    public Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? keyword = null, Guid? categoryId = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        ProductStatus? status = null, string? sortBy = null, bool sortDesc = false)
    {
        var query = _store.Values.Where(p => !p.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lower = keyword.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(lower) ||
                p.Description.ToLower().Contains(lower) ||
                p.SKU.ToLower().Contains(lower));
        }

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("name", false) => query.OrderBy(p => p.Name),
            ("name", true) => query.OrderByDescending(p => p.Name),
            ("price", false) => query.OrderBy(p => p.Price),
            ("price", true) => query.OrderByDescending(p => p.Price),
            ("stock", false) => query.OrderBy(p => p.StockQuantity),
            ("stock", true) => query.OrderByDescending(p => p.StockQuantity),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult((items.AsEnumerable(), total));
    }

    public Task<bool> IsSkuExistsAsync(string sku, Guid? excludeId = null)
    {
        var exists = _store.Values.Any(p =>
            !p.IsDeleted &&
            p.SKU.ToUpper() == sku.ToUpper() &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}

public class CategoryRepository : InMemoryRepository<Category>, ICategoryRepository
{
    private readonly ProductRepository _productRepo;

    public CategoryRepository(ProductRepository productRepo)
    {
        _productRepo = productRepo;
    }

    public Task<Category?> GetWithProductsAsync(Guid id)
    {
        _store.TryGetValue(id, out var category);
        if (category != null)
        {
            // Attach products
            category.Products = _productRepo._store.Values
                .Where(p => p.CategoryId == category.Id)
                .ToList();
        }
        return Task.FromResult(category);
    }

    public Task<IEnumerable<Category>> GetAllWithProductCountAsync()
    {
        var result = _store.Values.Select(c =>
        {
            c.Products = _productRepo._store.Values
                .Where(p => p.CategoryId == c.Id)
                .ToList();
            return c;
        });
        return Task.FromResult(result);
    }
}

public class UserRepository : InMemoryRepository<User>, IUserRepository
{
    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _store.Values.FirstOrDefault(u =>
            u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        return Task.FromResult(user);
    }

    public Task<bool> IsEmailExistsAsync(string email, Guid? excludeId = null)
    {
        var exists = _store.Values.Any(u =>
            !u.IsDeleted &&
            u.Email.ToLower() == email.ToLower() &&
            (!excludeId.HasValue || u.Id != excludeId.Value));
        return Task.FromResult(exists);
    }

    public Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? keyword = null, UserRole? role = null,
        string? sortBy = null, bool sortDesc = false)
    {
        var query = _store.Values.Where(u => !u.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lower = keyword.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(lower) ||
                u.FirstName.ToLower().Contains(lower) ||
                u.LastName.ToLower().Contains(lower));
        }
        if (role.HasValue) query = query.Where(u => u.Role == role.Value);

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("email", false) => query.OrderBy(u => u.Email),
            ("email", true) => query.OrderByDescending(u => u.Email),
            ("name", false) => query.OrderBy(u => u.LastName),
            ("name", true) => query.OrderByDescending(u => u.LastName),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items.AsEnumerable(), total));
    }
}

public class OrderRepository : InMemoryRepository<Order>, IOrderRepository
{
    private readonly ProductRepository _productRepo;
    private readonly UserRepository _userRepo;
    private int _orderCounter = 1000;

    public OrderRepository(ProductRepository productRepo, UserRepository userRepo)
    {
        _productRepo = productRepo;
        _userRepo = userRepo;
    }

    public Task<Order?> GetWithDetailsAsync(Guid id)
    {
        _store.TryGetValue(id, out var order);
        if (order != null)
        {
            _userRepo._store.TryGetValue(order.UserId, out var user);
            order.User = user;

            foreach (var item in order.OrderItems)
            {
                _productRepo._store.TryGetValue(item.ProductId, out var product);
                item.Product = product;
            }
        }
        return Task.FromResult(order);
    }

    public Task<IEnumerable<Order>> GetByUserAsync(Guid userId)
    {
        var orders = _store.Values
            .Where(o => !o.IsDeleted && o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        foreach (var order in orders)
        {
            _userRepo._store.TryGetValue(order.UserId, out var user);
            order.User = user;
            foreach (var item in order.OrderItems)
            {
                _productRepo._store.TryGetValue(item.ProductId, out var product);
                item.Product = product;
            }
        }
        return Task.FromResult(orders.AsEnumerable());
    }

    public Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? userId = null, OrderStatus? status = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        string? sortBy = null, bool sortDesc = false)
    {
        var query = _store.Values.Where(o => !o.IsDeleted).AsQueryable();

        if (userId.HasValue) query = query.Where(o => o.UserId == userId.Value);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (fromDate.HasValue) query = query.Where(o => o.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(o => o.CreatedAt <= toDate.Value);

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("total", false) => query.OrderBy(o => o.TotalAmount),
            ("total", true) => query.OrderByDescending(o => o.TotalAmount),
            ("status", false) => query.OrderBy(o => o.Status),
            ("status", true) => query.OrderByDescending(o => o.Status),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        foreach (var order in items)
        {
            _userRepo._store.TryGetValue(order.UserId, out var user);
            order.User = user;
            foreach (var item in order.OrderItems)
            {
                _productRepo._store.TryGetValue(item.ProductId, out var product);
                item.Product = product;
            }
        }

        return Task.FromResult((items.AsEnumerable(), total));
    }

    public Task<string> GenerateOrderNumberAsync()
    {
        var counter = Interlocked.Increment(ref _orderCounter);
        return Task.FromResult($"ORD-{DateTime.UtcNow:yyyyMMdd}-{counter:D5}");
    }
}
