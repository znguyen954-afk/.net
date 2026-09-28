using ECommerceAPI.Application.Common;
using ECommerceAPI.Application.DTOs;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Interfaces;

namespace ECommerceAPI.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id)
    {
        var categories = await _unitOfWork.Categories.GetAllWithProductCountAsync();
        var category = categories.FirstOrDefault(c => c.Id == id && !c.IsDeleted);
        if (category == null)
            return ApiResponse<CategoryDto>.Fail($"Category with id '{id}' not found.");

        return ApiResponse<CategoryDto>.Ok(MapToDto(category));
    }

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllWithProductCountAsync();
        var activeCats = categories.Where(c => !c.IsDeleted);
        return ApiResponse<IEnumerable<CategoryDto>>.Ok(activeCats.Select(MapToDto));
    }

    public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<CategoryDto>.Fail("Category name is required.");

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = request.ImageUrl
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CategoryDto>.Ok(MapToDto(category), "Category created successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null || category.IsDeleted)
            return ApiResponse<CategoryDto>.Fail($"Category with id '{id}' not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<CategoryDto>.Fail("Category name is required.");

        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();
        category.ImageUrl = request.ImageUrl;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Categories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CategoryDto>.Ok(MapToDto(category), "Category updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var category = await _unitOfWork.Categories.GetWithProductsAsync(id);
        if (category == null || category.IsDeleted)
            return ApiResponse<bool>.Fail($"Category with id '{id}' not found.");

        var hasProducts = category.Products.Any(p => !p.IsDeleted);
        if (hasProducts)
            return ApiResponse<bool>.Fail("Cannot delete category that still has active products.");

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Categories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Category deleted successfully.");
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        ProductCount = c.Products.Count(p => !p.IsDeleted),
        CreatedAt = c.CreatedAt
    };
}
