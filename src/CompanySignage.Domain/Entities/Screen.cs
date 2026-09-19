using CompanySignage.Domain.Common;
using CompanySignage.Domain.Enums;

namespace CompanySignage.Domain.Entities;

public class Screen : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ScreenCode { get; set; } = string.Empty;
    public string DeviceTokenHash { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; } = true;

    // Ekran boyutu ayarlari
    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public string Orientation { get; set; } = "Landscape"; // Landscape | Portrait
    public string DisplayMode { get; set; } = "Fill"; // Fill | Fit

    public int? CurrentMediaId { get; set; }
    public MediaFile? CurrentMedia { get; set; }

    public int? CurrentPlaylistId { get; set; }
    public Playlist? CurrentPlaylist { get; set; }

    public ICollection<ScreenAssignment> Assignments { get; set; } = new List<ScreenAssignment>();
    public ICollection<ScreenHeartbeat> Heartbeats { get; set; } = new List<ScreenHeartbeat>();
}
