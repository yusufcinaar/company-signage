using CompanySignage.Domain.Common;

namespace CompanySignage.Domain.Entities;

public class Playlist : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsLoop { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
    public ICollection<ScreenAssignment> Assignments { get; set; } = new List<ScreenAssignment>();
    public ICollection<Screen> Screens { get; set; } = new List<Screen>();
}
