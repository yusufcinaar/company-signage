using CompanySignage.Application.DTOs.Auth;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly ILogService _logService;

    public AuthService(IDbContextFactory dbContextFactory, ILogService logService)
    {
        _dbContextFactory = dbContextFactory;
        _logService = logService;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto dto, string? ipAddress = null)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            await _logService.LogAsync(null, "LOGIN_FAILED", $"Başarısız giriş: {dto.Username}", ipAddress);
            return new LoginResultDto { Success = false, ErrorMessage = "Kullanıcı adı veya şifre hatalı." };
        }

        await _logService.LogAsync(user.Id, "LOGIN_SUCCESS", $"Giriş yapıldı: {user.Username}", ipAddress);
        return new LoginResultDto
        {
            Success = true,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }

    public async Task LogoutAsync(int? userId = null, string? ipAddress = null)
    {
        if (userId.HasValue)
            await _logService.LogAsync(userId, "LOGOUT", "Çıkış yapıldı", ipAddress);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var user = await context.Users.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
        await context.SaveChangesAsync();
        await _logService.LogAsync(userId, "PASSWORD_CHANGED", "Şifre değiştirildi", null);
        return true;
    }

    public async Task<bool> ValidateUserAsync(string username, string password)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<(int userId, string fullName, string role)?> GetUserAsync(string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user == null) return null;
        return (user.Id, user.FullName, user.Role.ToString());
    }
}
