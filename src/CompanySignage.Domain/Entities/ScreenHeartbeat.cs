using CompanySignage.Domain.Common;

namespace CompanySignage.Domain.Entities;

public class ScreenHeartbeat : BaseEntity
{
    public int ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;

    public string? IpAddress { get; set; }
    public string? CurrentMediaName { get; set; }
    public string? PlayerVersion { get; set; }
    public long? FreeDiskSpace { get; set; }
    public string? ErrorMessage { get; set; }
}
