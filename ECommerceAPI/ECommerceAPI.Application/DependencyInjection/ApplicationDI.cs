using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceAPI.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
