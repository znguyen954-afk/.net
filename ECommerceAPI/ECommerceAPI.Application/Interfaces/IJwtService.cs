namespace ECommerceAPI.Application.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string role);
    (Guid userId, string email, string role)? ValidateToken(string token);
}
