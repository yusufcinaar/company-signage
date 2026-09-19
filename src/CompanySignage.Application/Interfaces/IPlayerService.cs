using CompanySignage.Application.DTOs.Player;

namespace CompanySignage.Application.Interfaces;

public interface IPlayerService
{
    Task<PlayerConfigurationDto?> GetConfigurationAsync(string screenCode);
    Task<PlayerPublicationDto?> GetCurrentPublicationAsync(string screenCode);
    Task<bool> HeartbeatAsync(string screenCode, HeartbeatDto dto);
    Task<bool> DownloadCompletedAsync(string screenCode, DownloadCompletedDto dto);
    Task<bool> ReportErrorAsync(string screenCode, PlayerErrorDto dto);
}
