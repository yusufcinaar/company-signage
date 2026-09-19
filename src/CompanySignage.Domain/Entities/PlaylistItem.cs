using CompanySignage.Domain.Common;

namespace CompanySignage.Domain.Entities;

public class PlaylistItem : BaseEntity
{
    public int PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;

    public int MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; } = null!;

    public int OrderNumber { get; set; }
    public int DisplayDuration { get; set; } = 10;
    public bool SoundEnabled { get; set; } = true;
}
