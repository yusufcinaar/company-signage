namespace CompanySignage.Application.DTOs.Screen;

public class ScreenDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScreenCode { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public string Orientation { get; set; } = "Landscape";
    public string DisplayMode { get; set; } = "Fill";
    public int? CurrentMediaId { get; set; }
    public string? CurrentMediaName { get; set; }
    public int? CurrentPlaylistId { get; set; }
    public string? CurrentPlaylistName { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateScreenDto
{
    public string Name { get; set; } = string.Empty;
    public string ScreenCode { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public string Orientation { get; set; } = "Landscape";
    public string DisplayMode { get; set; } = "Fill";
}

public class UpdateScreenDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public string Orientation { get; set; } = "Landscape";
    public string DisplayMode { get; set; } = "Fill";
}

public class ScreenStatusDto
{
    public int Id { get; set; }
    public string ScreenCode { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? CurrentMediaName { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
}
