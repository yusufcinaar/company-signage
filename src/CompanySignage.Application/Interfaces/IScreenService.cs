using CompanySignage.Application.DTOs.Screen;

namespace CompanySignage.Application.Interfaces;

public interface IScreenService
{
    Task<List<ScreenDto>> GetAllAsync();
    Task<ScreenDto?> GetByIdAsync(int id);
    Task<ScreenDto?> GetByCodeAsync(string screenCode);
    Task<ScreenDto> CreateAsync(CreateScreenDto dto);
    Task<ScreenDto?> UpdateAsync(int id, UpdateScreenDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> RefreshAsync(int id);
    Task<bool> StopAsync(int id);
    Task<bool> RestartAsync(int id);
    Task<bool> ClearCacheAsync(int id);
    Task UpdateHeartbeatAsync(string screenCode, string? ipAddress, string? currentMedia, string? playerVersion, long? freeDiskSpace, string? errorMessage);
    Task<List<ScreenStatusDto>> GetStatusesAsync();
}
