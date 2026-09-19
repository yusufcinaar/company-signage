using CompanySignage.Domain.Common;
using CompanySignage.Domain.Enums;

namespace CompanySignage.Domain.Entities;

public class MediaFile : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public double Duration { get; set; }
    public int DisplayDuration { get; set; } = 10;
    public string? FileHash { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public ICollection<PlaylistItem> PlaylistItems { get; set; } = new List<PlaylistItem>();
    public ICollection<ScreenAssignment> Assignments { get; set; } = new List<ScreenAssignment>();
}
