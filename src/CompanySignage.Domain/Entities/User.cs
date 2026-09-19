using CompanySignage.Domain.Common;
using CompanySignage.Domain.Enums;

namespace CompanySignage.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Admin;
    public bool IsActive { get; set; } = true;

    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
