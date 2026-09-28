using ECommerceAPI.Application.Common;
using ECommerceAPI.Application.DTOs;

namespace ECommerceAPI.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId);
}

public interface IProductService
{
    Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PagedResult<ProductDto>>> GetPagedAsync(ProductQueryParams query);
    Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}

public interface ICategoryService
{
    Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync();
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryRequest request);
    Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}

public interface IOrderService
{
    Task<ApiResponse<OrderDto>> GetByIdAsync(Guid id, Guid? currentUserId = null, bool isAdmin = false);
    Task<ApiResponse<PagedResult<OrderDto>>> GetPagedAsync(OrderQueryParams query);
    Task<ApiResponse<IEnumerable<OrderDto>>> GetMyOrdersAsync(Guid userId);
    Task<ApiResponse<OrderDto>> CreateAsync(Guid userId, CreateOrderRequest request);
    Task<ApiResponse<OrderDto>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request);
    Task<ApiResponse<bool>> CancelOrderAsync(Guid id, Guid userId, bool isAdmin = false);
}
