using CompanySignage.Domain.Common;
using CompanySignage.Domain.Enums;

namespace CompanySignage.Domain.Entities;

public class ScreenAssignment : BaseEntity
{
    public int ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;

    public int? PlaylistId { get; set; }
    public Playlist? Playlist { get; set; }

    public int? MediaFileId { get; set; }
    public MediaFile? MediaFile { get; set; }

    public AssignmentType AssignmentType { get; set; }
    public int Priority { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
