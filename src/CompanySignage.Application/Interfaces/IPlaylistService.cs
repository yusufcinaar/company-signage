using CompanySignage.Application.DTOs.Playlist;

namespace CompanySignage.Application.Interfaces;

public interface IPlaylistService
{
    Task<List<PlaylistDto>> GetAllAsync();
    Task<PlaylistDto?> GetByIdAsync(int id);
    Task<PlaylistDto> CreateAsync(CreatePlaylistDto dto);
    Task<PlaylistDto?> UpdateAsync(int id, UpdatePlaylistDto dto);
    Task<bool> DeleteAsync(int id);
    Task<PlaylistDto?> AddItemAsync(int playlistId, AddPlaylistItemDto dto);
    Task<bool> RemoveItemAsync(int playlistItemId);
    Task<bool> UpdateItemDurationAsync(int playlistId, int playlistItemId, int displayDuration);
    Task<bool> ReorderItemsAsync(int playlistId, List<int> itemIds);
    Task<bool> IncrementVersionAsync(int playlistId);
}
