using CompanySignage.Application.DTOs.Playlist;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class PlaylistService : IPlaylistService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly ILogService _logService;

    public PlaylistService(IDbContextFactory dbContextFactory, ILogService logService)
    {
        _dbContextFactory = dbContextFactory;
        _logService = logService;
    }

    public async Task<List<PlaylistDto>> GetAllAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlists = await context.Playlists
            .Include(p => p.Items).ThenInclude(i => i.MediaFile)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return playlists.Select(MapToDto).ToList();
    }

    public async Task<PlaylistDto?> GetByIdAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlist = await context.Playlists
            .Include(p => p.Items).ThenInclude(i => i.MediaFile)
            .FirstOrDefaultAsync(p => p.Id == id);
        return playlist == null ? null : MapToDto(playlist);
    }

    public async Task<PlaylistDto> CreateAsync(CreatePlaylistDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlist = new Playlist
        {
            Name = dto.Name,
            Description = dto.Description,
            IsLoop = dto.IsLoop,
            IsActive = true,
            Version = 1
        };
        context.Playlists.Add(playlist);
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "PLAYLIST_CREATED", $"Playlist oluşturuldu: {dto.Name}", null);
        return MapToDto(playlist);
    }

    public async Task<PlaylistDto?> UpdateAsync(int id, UpdatePlaylistDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlist = await context.Playlists.FindAsync(id);
        if (playlist == null) return null;
        playlist.Name = dto.Name;
        playlist.Description = dto.Description;
        playlist.IsLoop = dto.IsLoop;
        playlist.IsActive = dto.IsActive;
        playlist.UpdatedAt = DateTime.UtcNow;
        playlist.Version++;
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "PLAYLIST_UPDATED", $"Playlist güncellendi: {playlist.Name}", null);
        return MapToDto(playlist);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlist = await context.Playlists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (playlist == null) return false;

        var affectedScreens = await context.Screens
            .Where(screen => screen.CurrentPlaylistId == id)
            .ToListAsync();
        foreach (var screen in affectedScreens)
        {
            screen.CurrentPlaylistId = null;
        }

        var affectedAssignments = await context.ScreenAssignments
            .Where(assignment => assignment.PlaylistId == id)
            .ToListAsync();
        foreach (var assignment in affectedAssignments)
        {
            assignment.IsActive = false;
        }

        context.PlaylistItems.RemoveRange(playlist.Items);
        context.Playlists.Remove(playlist);
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "PLAYLIST_DELETED", $"Playlist silindi: {playlist.Name}", null);
        return true;
    }

    public async Task<PlaylistDto?> AddItemAsync(int playlistId, AddPlaylistItemDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlist = await context.Playlists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == playlistId);
        if (playlist == null) return null;

        var media = await context.MediaFiles
            .FirstOrDefaultAsync(file => file.Id == dto.MediaFileId && file.IsActive);
        if (media == null) return null;

        var maxOrder = playlist.Items.Any() ? playlist.Items.Max(i => i.OrderNumber) : 0;
        var isVideo = media.MediaType == Domain.Enums.MediaType.Video;
        var item = new PlaylistItem
        {
            PlaylistId = playlistId,
            MediaFileId = dto.MediaFileId,
            OrderNumber = maxOrder + 1,
            DisplayDuration = isVideo ? 0 : Math.Clamp(dto.DisplayDuration, 1, 3600),
            SoundEnabled = isVideo && dto.SoundEnabled
        };
        context.PlaylistItems.Add(item);
        playlist.Version++;
        playlist.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return await GetByIdAsync(playlistId);
    }

    public async Task<bool> UpdateItemDurationAsync(int playlistId, int playlistItemId, int displayDuration)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var item = await context.PlaylistItems
            .Include(playlistItem => playlistItem.MediaFile)
            .Include(playlistItem => playlistItem.Playlist)
            .FirstOrDefaultAsync(playlistItem =>
                playlistItem.Id == playlistItemId &&
                playlistItem.PlaylistId == playlistId);

        if (item == null || item.MediaFile?.MediaType == Domain.Enums.MediaType.Video)
            return false;

        item.DisplayDuration = Math.Clamp(displayDuration, 1, 3600);
        if (item.Playlist != null)
        {
            item.Playlist.Version++;
            item.Playlist.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveItemAsync(int playlistItemId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var item = await context.PlaylistItems
            .Include(i => i.Playlist)
            .FirstOrDefaultAsync(i => i.Id == playlistItemId);
        if (item == null) return false;
        context.PlaylistItems.Remove(item);
        if (item.Playlist != null)
        {
            item.Playlist.Version++;
            item.Playlist.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderItemsAsync(int playlistId, List<int> itemIds)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var items = await context.PlaylistItems
            .Where(i => i.PlaylistId == playlistId)
            .ToListAsync();
        for (int i = 0; i < itemIds.Count; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == itemIds[i]);
            if (item != null) item.OrderNumber = i + 1;
        }
        var playlist = await context.Playlists.FindAsync(playlistId);
        if (playlist != null)
        {
            playlist.Version++;
            playlist.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IncrementVersionAsync(int playlistId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var playlist = await context.Playlists.FindAsync(playlistId);
        if (playlist == null) return false;
        playlist.Version++;
        playlist.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    private static PlaylistDto MapToDto(Playlist p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Version = p.Version,
        IsLoop = p.IsLoop,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Items = p.Items.OrderBy(i => i.OrderNumber).Select(i => new PlaylistItemDto
        {
            Id = i.Id,
            MediaFileId = i.MediaFileId,
            MediaName = i.MediaFile?.Name ?? "Bilinmeyen",
            FileUrl = i.MediaFile?.FileUrl ?? "",
            MediaType = i.MediaFile?.MediaType ?? Domain.Enums.MediaType.Image,
            OrderNumber = i.OrderNumber,
            DisplayDuration = i.DisplayDuration,
            SoundEnabled = i.SoundEnabled
        }).ToList()
    };
}
