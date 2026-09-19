using CompanySignage.Domain.Enums;

namespace CompanySignage.Application.DTOs.Assignment;

public class AssignmentDto
{
    public int Id { get; set; }
    public int ScreenId { get; set; }
    public string ScreenName { get; set; } = string.Empty;
    public string ScreenCode { get; set; } = string.Empty;
    public int? PlaylistId { get; set; }
    public string? PlaylistName { get; set; }
    public int? MediaFileId { get; set; }
    public string? MediaName { get; set; }
    public AssignmentType AssignmentType { get; set; }
    public string AssignmentTypeText => AssignmentType == AssignmentType.Playlist ? "Playlist" : "Tekil İçerik";
    public int Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssignmentDto
{
    public int ScreenId { get; set; }
    public int? PlaylistId { get; set; }
    public int? MediaFileId { get; set; }
    public AssignmentType AssignmentType { get; set; }
    public int Priority { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class SendNowDto
{
    public List<int> ScreenIds { get; set; } = new();
    public int? MediaFileId { get; set; }
    public int? PlaylistId { get; set; }
    public AssignmentType AssignmentType { get; set; }
}
