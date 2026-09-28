using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Enums;
using ECommerceAPI.Domain.Interfaces;
using ECommerceAPI.Infrastructure.Repositories;
using ECommerceAPI.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceAPI.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Repositories - Singleton so in-memory data persists across requests
        services.AddSingleton<ProductRepository>();
        services.AddSingleton<CategoryRepository>();
        services.AddSingleton<UserRepository>();
        services.AddSingleton<OrderRepository>();

        services.AddSingleton<IProductRepository>(sp => sp.GetRequiredService<ProductRepository>());
        services.AddSingleton<ICategoryRepository>(sp => sp.GetRequiredService<CategoryRepository>());
        services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<UserRepository>());
        services.AddSingleton<IOrderRepository>(sp => sp.GetRequiredService<OrderRepository>());

        services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();

        // JWT Service
        services.AddSingleton<IJwtService, JwtService>();

        // Seed initial data
        services.AddSingleton<DataSeeder>();

        return services;
    }

    public static void SeedData(this IServiceProvider serviceProvider)
    {
        var seeder = serviceProvider.GetRequiredService<DataSeeder>();
        seeder.Seed();
    }
}

/// <summary>
/// Seeds demo data for development/demo purposes
/// </summary>
public class DataSeeder
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUserRepository _users;

    public DataSeeder(IProductRepository products, ICategoryRepository categories, IUserRepository users)
    {
        _products = products;
        _categories = categories;
        _users = users;
    }

    public void Seed()
    {
        // Seed categories
        var electronics = new Category { Name = "Electronics", Description = "Electronic devices and accessories", ImageUrl = "https://picsum.photos/seed/electronics/400/300" };
        var clothing = new Category { Name = "Clothing", Description = "Fashion and apparel", ImageUrl = "https://picsum.photos/seed/clothing/400/300" };
        var books = new Category { Name = "Books", Description = "Books and literature", ImageUrl = "https://picsum.photos/seed/books/400/300" };
        var homeKitchen = new Category { Name = "Home & Kitchen", Description = "Home appliances and kitchen tools", ImageUrl = "https://picsum.photos/seed/home/400/300" };

        _categories.AddAsync(electronics).GetAwaiter().GetResult();
        _categories.AddAsync(clothing).GetAwaiter().GetResult();
        _categories.AddAsync(books).GetAwaiter().GetResult();
        _categories.AddAsync(homeKitchen).GetAwaiter().GetResult();

        // Seed products
        var productsData = new[]
        {
            new Product { Name = "iPhone 15 Pro", Description = "Latest Apple smartphone with A17 Pro chip", Price = 999.99m, StockQuantity = 50, SKU = "APPL-IPH15P-256", CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/iphone/400/300" },
            new Product { Name = "MacBook Air M2", Description = "Thin and light laptop with Apple Silicon", Price = 1299.00m, StockQuantity = 30, SKU = "APPL-MBA-M2-256", CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/macbook/400/300" },
            new Product { Name = "Samsung Galaxy S24", Description = "Premium Android smartphone", Price = 849.99m, StockQuantity = 40, SKU = "SAMS-GS24-128", CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/samsung/400/300" },
            new Product { Name = "Sony WH-1000XM5", Description = "Industry-leading noise canceling headphones", Price = 349.99m, StockQuantity = 75, SKU = "SONY-WH1000XM5", CategoryId = electronics.Id, ImageUrl = "https://picsum.photos/seed/headphones/400/300" },
            new Product { Name = "Men's Classic T-Shirt", Description = "100% cotton comfortable t-shirt", Price = 29.99m, StockQuantity = 200, SKU = "CLO-TSHIRT-M-BLK", CategoryId = clothing.Id, ImageUrl = "https://picsum.photos/seed/tshirt/400/300" },
            new Product { Name = "Women's Denim Jacket", Description = "Stylish denim jacket for all seasons", Price = 79.99m, StockQuantity = 80, SKU = "CLO-DJACKET-W-BLU", CategoryId = clothing.Id, ImageUrl = "https://picsum.photos/seed/jacket/400/300" },
            new Product { Name = "Running Sneakers", Description = "High-performance running shoes", Price = 119.99m, StockQuantity = 60, SKU = "CLO-SNEAK-UNIV-WHT", CategoryId = clothing.Id, ImageUrl = "https://picsum.photos/seed/sneakers/400/300" },
            new Product { Name = "Clean Code by Robert Martin", Description = "A handbook of agile software craftsmanship", Price = 39.99m, StockQuantity = 100, SKU = "BOOK-CLEANCODE", CategoryId = books.Id, ImageUrl = "https://picsum.photos/seed/cleancode/400/300" },
            new Product { Name = "Design Patterns", Description = "Elements of Reusable Object-Oriented Software", Price = 49.99m, StockQuantity = 80, SKU = "BOOK-DESIGNPAT", CategoryId = books.Id, ImageUrl = "https://picsum.photos/seed/designpat/400/300" },
            new Product { Name = "Coffee Maker Pro", Description = "Programmable 12-cup coffee maker", Price = 89.99m, StockQuantity = 45, SKU = "HOME-COFFEE-12CUP", CategoryId = homeKitchen.Id, ImageUrl = "https://picsum.photos/seed/coffee/400/300" },
            new Product { Name = "Air Fryer XL", Description = "5.5L digital air fryer with 8 cooking presets", Price = 129.99m, StockQuantity = 35, SKU = "HOME-AIRFRYER-5L", CategoryId = homeKitchen.Id, ImageUrl = "https://picsum.photos/seed/airfryer/400/300" },
        };

        foreach (var product in productsData)
        {
            _products.AddAsync(product).GetAwaiter().GetResult();
        }

        // Seed admin user
        var admin = new User
        {
            Email = "admin@ecommerce.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "+84901234567",
            Address = "123 Admin Street, Hanoi, Vietnam",
            Role = UserRole.Admin
        };
        _users.AddAsync(admin).GetAwaiter().GetResult();

        // Seed customer user
        var customer = new User
        {
            Email = "customer@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+84987654321",
            Address = "456 Customer Ave, Ho Chi Minh City, Vietnam",
            Role = UserRole.Customer
        };
        _users.AddAsync(customer).GetAwaiter().GetResult();
    }
}
