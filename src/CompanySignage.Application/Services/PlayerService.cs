using CompanySignage.Application.DTOs.Player;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class PlayerService : IPlayerService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly ILogService _logService;

    public PlayerService(IDbContextFactory dbContextFactory, ILogService logService)
    {
        _dbContextFactory = dbContextFactory;
        _logService = logService;
    }

    public async Task<PlayerConfigurationDto?> GetConfigurationAsync(string screenCode)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        if (screen == null) return null;
        return new PlayerConfigurationDto
        {
            ScreenCode = screen.ScreenCode,
            ScreenName = screen.Name,
            SoundEnabled = true,
            AutoStart = true,
            HeartbeatIntervalSeconds = 30,
            ScreenWidth = screen.ScreenWidth,
            ScreenHeight = screen.ScreenHeight,
            Orientation = screen.Orientation,
            DisplayMode = screen.DisplayMode
        };
    }

    public async Task<PlayerPublicationDto?> GetCurrentPublicationAsync(string screenCode)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screenId = await context.Screens.Where(s => s.ScreenCode == screenCode && s.IsActive)
            .Select(s => (int?)s.Id).FirstOrDefaultAsync();
        if (!screenId.HasValue) return null;
        var publicationRevision = await PublicationSelection.RefreshAsync(context, screenId.Value);
        context.ChangeTracker.Clear();
        var screen = await context.Screens
            .Include(s => s.CurrentMedia)
            .Include(s => s.CurrentPlaylist)
                .ThenInclude(p => p!.Items.OrderBy(i => i.OrderNumber))
                    .ThenInclude(i => i.MediaFile)
            .FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        if (screen == null) return null;

        if (screen.CurrentPlaylistId.HasValue && screen.CurrentPlaylist != null)
        {
            return new PlayerPublicationDto
            {
                PlaylistId = screen.CurrentPlaylistId,
                PublicationRevision = publicationRevision,
                AssignmentType = AssignmentType.Playlist,
                PlaylistVersion = screen.CurrentPlaylist.Version,
                DisplayMode = screen.DisplayMode,
                PlaylistItems = screen.CurrentPlaylist.Items.OrderBy(i => i.OrderNumber).Select(i => new PlayerPlaylistItemDto
                {
                    MediaFileId = i.MediaFileId,
                    StoredFileName = i.MediaFile?.StoredFileName ?? "",
                    FileUrl = i.MediaFile?.FileUrl ?? "",
                    OriginalFileName = i.MediaFile?.OriginalFileName ?? "",
                    MediaType = i.MediaFile?.MediaType ?? MediaType.Image,
                    OrderNumber = i.OrderNumber,
                    DisplayDuration = i.DisplayDuration,
                    SoundEnabled = i.SoundEnabled,
                    FileHash = i.MediaFile?.FileHash ?? ""
                }).ToList()
            };
        }

        if (screen.CurrentMediaId.HasValue && screen.CurrentMedia != null)
        {
            return new PlayerPublicationDto
            {
                MediaFileId = screen.CurrentMediaId,
                PublicationRevision = publicationRevision,
                AssignmentType = AssignmentType.SingleMedia,
                DisplayMode = screen.DisplayMode,
                MediaName = screen.CurrentMedia.Name,
                FileUrl = screen.CurrentMedia.FileUrl,
                StoredFileName = screen.CurrentMedia.StoredFileName,
                MediaType = screen.CurrentMedia.MediaType,
                DisplayDuration = screen.CurrentMedia.DisplayDuration,
                SoundEnabled = screen.CurrentMedia.SoundEnabled,
                FileHash = screen.CurrentMedia.FileHash ?? ""
            };
        }

        return new PlayerPublicationDto { AssignmentType = AssignmentType.SingleMedia, DisplayMode = screen.DisplayMode, PublicationRevision = publicationRevision };
    }

    public async Task<bool> HeartbeatAsync(string screenCode, HeartbeatDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        if (screen == null) return false;

        screen.IpAddress = dto.IpAddress;
        screen.LastSeenAt = DateTime.UtcNow;
        screen.IsOnline = true;

        if (dto.ScreenWidth is > 0 && dto.ScreenHeight is > 0)
        {
            screen.ScreenWidth = dto.ScreenWidth.Value;
            screen.ScreenHeight = dto.ScreenHeight.Value;
            screen.Orientation = dto.ScreenWidth.Value >= dto.ScreenHeight.Value ? "Landscape" : "Portrait";
        }

        context.ScreenHeartbeats.Add(new Domain.Entities.ScreenHeartbeat
        {
            ScreenId = screen.Id,
            IpAddress = dto.IpAddress,
            CurrentMediaName = dto.CurrentMediaName,
            PlayerVersion = dto.PlayerVersion,
            FreeDiskSpace = dto.FreeDiskSpace,
            ErrorMessage = dto.ErrorMessage
        });
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DownloadCompletedAsync(string screenCode, DownloadCompletedDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        if (screen == null) return false;
        await _logService.LogAsync(null, "DOWNLOAD_COMPLETED", $"Ekran {screenCode} dosya indirdi: {dto.StoredFileName}", null);
        return true;
    }

    public async Task<bool> ReportErrorAsync(string screenCode, PlayerErrorDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screen = await context.Screens.FirstOrDefaultAsync(s => s.ScreenCode == screenCode);
        if (screen == null) return false;
        await _logService.LogAsync(null, "PLAYER_ERROR", $"Ekran {screenCode} hata bildirdi: {dto.ErrorMessage}", null);
        return true;
    }
}
