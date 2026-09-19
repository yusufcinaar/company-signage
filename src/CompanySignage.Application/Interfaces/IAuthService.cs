using CompanySignage.Application.DTOs.Auth;

namespace CompanySignage.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(LoginDto dto, string? ipAddress = null);
    Task LogoutAsync(int? userId = null, string? ipAddress = null);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<bool> ValidateUserAsync(string username, string password);
    Task<(int userId, string fullName, string role)?> GetUserAsync(string username);
}
