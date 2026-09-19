using CompanySignage.Domain.Enums;

namespace CompanySignage.Application.DTOs.Playlist;

public class PlaylistDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsLoop { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PlaylistItemDto> Items { get; set; } = new();
    public int ItemCount => Items.Count;
    public double TotalDuration => Items.Sum(i => i.EffectiveDuration);
}

public class PlaylistItemDto
{
    public int Id { get; set; }
    public int MediaFileId { get; set; }
    public string MediaName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public int OrderNumber { get; set; }
    public int DisplayDuration { get; set; }
    public bool SoundEnabled { get; set; }
    public double EffectiveDuration => MediaType == MediaType.Video ? 0 : DisplayDuration;
}

public class CreatePlaylistDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsLoop { get; set; } = true;
}

public class UpdatePlaylistDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsLoop { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class AddPlaylistItemDto
{
    public int MediaFileId { get; set; }
    public int DisplayDuration { get; set; } = 10;
    public bool SoundEnabled { get; set; } = true;
}

public class ReorderPlaylistItemsDto
{
    public List<int> ItemIds { get; set; } = new();
}
