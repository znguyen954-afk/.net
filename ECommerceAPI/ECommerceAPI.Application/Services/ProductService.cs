using ECommerceAPI.Application.Common;
using ECommerceAPI.Application.DTOs;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Interfaces;

namespace ECommerceAPI.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.IsDeleted)
            return ApiResponse<ProductDto>.Fail($"Product with id '{id}' not found.");

        return ApiResponse<ProductDto>.Ok(MapToDto(product));
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> GetPagedAsync(ProductQueryParams query)
    {
        var (items, total) = await _unitOfWork.Products.GetPagedAsync(
            query.Page, query.PageSize,
            query.Keyword, query.CategoryId,
            query.MinPrice, query.MaxPrice,
            query.Status, query.SortBy, query.SortDesc);

        return ApiResponse<PagedResult<ProductDto>>.Ok(new PagedResult<ProductDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<ProductDto>.Fail("Product name is required.");
        if (request.Price < 0)
            return ApiResponse<ProductDto>.Fail("Price must be non-negative.");
        if (request.StockQuantity < 0)
            return ApiResponse<ProductDto>.Fail("Stock quantity must be non-negative.");
        if (string.IsNullOrWhiteSpace(request.SKU))
            return ApiResponse<ProductDto>.Fail("SKU is required.");

        var categoryExists = await _unitOfWork.Categories.ExistsAsync(request.CategoryId);
        if (!categoryExists)
            return ApiResponse<ProductDto>.Fail("Category not found.");

        if (await _unitOfWork.Products.IsSkuExistsAsync(request.SKU))
            return ApiResponse<ProductDto>.Fail($"SKU '{request.SKU}' already exists.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl,
            SKU = request.SKU.Trim().ToUpper(),
            CategoryId = request.CategoryId
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // Reload with category name
        var created = await _unitOfWork.Products.GetByIdAsync(product.Id);
        return ApiResponse<ProductDto>.Ok(MapToDto(created!), "Product created successfully.");
    }

    public async Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.IsDeleted)
            return ApiResponse<ProductDto>.Fail($"Product with id '{id}' not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<ProductDto>.Fail("Product name is required.");
        if (request.Price < 0)
            return ApiResponse<ProductDto>.Fail("Price must be non-negative.");

        var categoryExists = await _unitOfWork.Categories.ExistsAsync(request.CategoryId);
        if (!categoryExists)
            return ApiResponse<ProductDto>.Fail("Category not found.");

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.ImageUrl = request.ImageUrl;
        product.Status = request.Status;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.Products.GetByIdAsync(product.Id);
        return ApiResponse<ProductDto>.Ok(MapToDto(updated!), "Product updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.IsDeleted)
            return ApiResponse<bool>.Fail($"Product with id '{id}' not found.");

        // Soft delete
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Product deleted successfully.");
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        ImageUrl = p.ImageUrl,
        SKU = p.SKU,
        Status = p.Status,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
