using CompanySignage.Domain.Common;

namespace CompanySignage.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
}
