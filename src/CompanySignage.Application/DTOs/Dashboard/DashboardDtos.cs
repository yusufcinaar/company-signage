namespace CompanySignage.Application.DTOs.Dashboard;

public class DashboardDto
{
    public int TotalScreens { get; set; }
    public int OnlineScreens { get; set; }
    public int OfflineScreens { get; set; }
    public int TotalImages { get; set; }
    public int TotalVideos { get; set; }
    public int ActiveAssignments { get; set; }
    public List<ScreenStatusDto> Screens { get; set; } = new();
}

public class ScreenStatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScreenCode { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? CurrentMediaName { get; set; }
    public string? CurrentPlaylistName { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
}
