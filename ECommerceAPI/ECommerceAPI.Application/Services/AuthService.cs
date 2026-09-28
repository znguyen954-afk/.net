using ECommerceAPI.Application.Common;
using ECommerceAPI.Application.DTOs;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Enums;
using ECommerceAPI.Domain.Interfaces;

namespace ECommerceAPI.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<AuthResponse>.Fail("Email and password are required.");

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        if (user.IsDeleted)
            return ApiResponse<AuthResponse>.Fail("Account has been disabled. Please contact support.");

        var expiresAt = DateTime.UtcNow.AddHours(24);
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        }, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return ApiResponse<AuthResponse>.Fail("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return ApiResponse<AuthResponse>.Fail("Password must be at least 6 characters.");
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return ApiResponse<AuthResponse>.Fail("First name and last name are required.");

        if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email.ToLower()))
            return ApiResponse<AuthResponse>.Fail("Email already in use.");

        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Role = UserRole.Customer
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddHours(24);
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        }, "Registration successful.");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return ApiResponse<UserDto>.Fail("User not found.");

        return ApiResponse<UserDto>.Ok(MapToUserDto(user));
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}
