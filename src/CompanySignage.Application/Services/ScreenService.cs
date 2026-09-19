using CompanySignage.Application.DTOs.Screen;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class ScreenService : IScreenService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly ISignalRService _signalR;
    private readonly ILogService _logService;

    public ScreenService(IDbContextFactory dbContextFactory, ISignalRService signalR, ILogService logService)
    {
        _dbContextFactory = dbContextFactory;
        _signalR = signalR;
        _logService = logService;
    }

    public async Task<List<ScreenDto>> GetAllAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screens = await context.Screens
            .Include(s => s.CurrentMedia)
            .Include(s => s.CurrentPlaylist)
            .OrderBy(s => s.ScreenCode)
            .ToListAsync();
        return screens.Select(MapToDto).ToList();
    }

    public async Task<ScreenDto?> GetByIdAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens
            .Include(s => s.CurrentMedia)
            .Include(s => s.CurrentPlaylist)
            .FirstOrDefaultAsync(s => s.Id == id);
        return screen == null ? null : MapToDto(screen);
    }

    public async Task<ScreenDto?> GetByCodeAsync(string screenCode)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens
            .Include(s => s.CurrentMedia)
            .Include(s => s.CurrentPlaylist)
            .FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        return screen == null ? null : MapToDto(screen);
    }

    public async Task<ScreenDto> CreateAsync(CreateScreenDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = new Screen
        {
            Name = dto.Name,
            ScreenCode = dto.ScreenCode.Trim().ToUpperInvariant(),
            DeviceTokenHash = BCrypt.Net.BCrypt.HashPassword(dto.DeviceToken, workFactor: 12),
            Description = dto.Description,
            IsActive = true,
            ScreenWidth = dto.ScreenWidth,
            ScreenHeight = dto.ScreenHeight,
            Orientation = dto.Orientation,
            DisplayMode = "Fill"
        };
        context.Screens.Add(screen);
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "SCREEN_CREATED", $"Ekran oluşturuldu: {dto.Name} ({dto.ScreenCode})", null);
        return MapToDto(screen);
    }

    public async Task<ScreenDto?> UpdateAsync(int id, UpdateScreenDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FindAsync(id);
        if (screen == null) return null;
        screen.Name = dto.Name;
        screen.Description = dto.Description;
        screen.IsActive = dto.IsActive;
        screen.ScreenWidth = dto.ScreenWidth;
        screen.ScreenHeight = dto.ScreenHeight;
        screen.Orientation = dto.Orientation;
        screen.DisplayMode = "Fill";
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "SCREEN_UPDATED", $"Ekran güncellendi: {screen.Name}", null);
        return MapToDto(screen);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FindAsync(id);
        if (screen == null) return false;
        context.Screens.Remove(screen);
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "SCREEN_DELETED", $"Ekran silindi: {screen.Name}", null);
        return true;
    }

    public async Task<bool> RefreshAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FindAsync(id);
        if (screen == null) return false;
        await _signalR.SendToScreenAsync(screen.ScreenCode, "RefreshPlayer", new { });
        await _logService.LogAsync(null, "SCREEN_REFRESH", $"Ekran yenilendi: {screen.ScreenCode}", null);
        return true;
    }

    public async Task<bool> StopAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FindAsync(id);
        if (screen == null) return false;
        var now = DateTime.UtcNow;
        var assignments = await context.ScreenAssignments
            .Where(a => a.ScreenId == id && a.IsActive && (!a.StartDate.HasValue || a.StartDate <= now))
            .ToListAsync();
        foreach (var assignment in assignments) assignment.IsActive = false;
        screen.CurrentMediaId = null;
        screen.CurrentPlaylistId = null;
        await context.SaveChangesAsync();
        await _signalR.SendToScreenAsync(screen.ScreenCode, "StopPlayback", new { });
        await _logService.LogAsync(null, "SCREEN_STOP", $"Yayın durduruldu: {screen.ScreenCode}", null);
        return true;
    }

    public async Task<bool> RestartAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FindAsync(id);
        if (screen == null) return false;
        await _signalR.SendToScreenAsync(screen.ScreenCode, "RestartPlayer", new { });
        await _logService.LogAsync(null, "SCREEN_RESTART", $"Player yeniden başlatıldı: {screen.ScreenCode}", null);
        return true;
    }

    public async Task<bool> ClearCacheAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FindAsync(id);
        if (screen == null) return false;
        await _signalR.SendToScreenAsync(screen.ScreenCode, "ClearCache", new { });
        await _logService.LogAsync(null, "SCREEN_CACHE_CLEARED", $"Cache temizlendi: {screen.ScreenCode}", null);
        return true;
    }

    public async Task UpdateHeartbeatAsync(string screenCode, string? ipAddress, string? currentMedia, string? playerVersion, long? freeDiskSpace, string? errorMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        if (screen == null) return;

        screen.IpAddress = ipAddress;
        screen.LastSeenAt = DateTime.UtcNow;
        screen.IsOnline = true;

        var heartbeat = new ScreenHeartbeat
        {
            ScreenId = screen.Id,
            IpAddress = ipAddress,
            CurrentMediaName = currentMedia,
            PlayerVersion = playerVersion,
            FreeDiskSpace = freeDiskSpace,
            ErrorMessage = errorMessage
        };
        context.ScreenHeartbeats.Add(heartbeat);
        await context.SaveChangesAsync();
    }

    public async Task<List<ScreenStatusDto>> GetStatusesAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screens = await context.Screens
            .Include(s => s.CurrentMedia)
            .OrderBy(s => s.ScreenCode)
            .ToListAsync();
        return screens.Select(s => new ScreenStatusDto
        {
            Id = s.Id,
            ScreenCode = s.ScreenCode,
            IsOnline = s.IsOnline,
            IpAddress = s.IpAddress,
            LastSeenAt = s.LastSeenAt,
            CurrentMediaName = s.CurrentMedia?.Name,
            StatusText = GetStatusText(s),
            StatusColor = GetStatusColor(s)
        }).ToList();
    }

    private static ScreenDto MapToDto(Screen s)
    {
        return new ScreenDto
        {
            Id = s.Id,
            Name = s.Name,
            ScreenCode = s.ScreenCode,
            IpAddress = s.IpAddress,
            Description = s.Description,
            LastSeenAt = s.LastSeenAt,
            IsOnline = s.IsOnline,
            IsActive = s.IsActive,
            ScreenWidth = s.ScreenWidth,
            ScreenHeight = s.ScreenHeight,
            Orientation = s.Orientation,
            DisplayMode = s.DisplayMode,
            CurrentMediaId = s.CurrentMediaId,
            CurrentMediaName = s.CurrentMedia?.Name,
            CurrentPlaylistId = s.CurrentPlaylistId,
            CurrentPlaylistName = s.CurrentPlaylist?.Name,
            StatusText = GetStatusText(s),
            StatusColor = GetStatusColor(s),
            CreatedAt = s.CreatedAt
        };
    }

    private static string GetStatusText(Screen s)
    {
        if (!s.IsActive) return "Devre Dışı";
        if (!s.IsOnline) return "Çevrimdışı";
        if (s.CurrentMediaId.HasValue || s.CurrentPlaylistId.HasValue) return "Yayın Yapıyor";
        return "Çevrimiçi";
    }

    private static string GetStatusColor(Screen s)
    {
        if (!s.IsActive) return "secondary";
        if (!s.IsOnline) return "danger";
        if (s.CurrentMediaId.HasValue || s.CurrentPlaylistId.HasValue) return "primary";
        return "success";
    }
}
