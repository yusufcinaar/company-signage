using CompanySignage.Domain.Enums;

namespace CompanySignage.Application.DTOs.Player;

public class PlayerConfigurationDto
{
    public string ScreenCode { get; set; } = string.Empty;
    public string ScreenName { get; set; } = string.Empty;
    public bool SoundEnabled { get; set; }
    public bool AutoStart { get; set; }
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public string Orientation { get; set; } = "Landscape";
    public string DisplayMode { get; set; } = "Fill";
}

public class PlayerPublicationDto
{
    public int? MediaFileId { get; set; }
    public int? PlaylistId { get; set; }
    public AssignmentType AssignmentType { get; set; }
    public int PlaylistVersion { get; set; }
    public int PublicationRevision { get; set; }
    public string? MediaName { get; set; }
    public string? FileUrl { get; set; }
    public string? StoredFileName { get; set; }
    public MediaType? MediaType { get; set; }
    public int DisplayDuration { get; set; }
    public bool SoundEnabled { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string DisplayMode { get; set; } = "Fill";
    public List<PlayerPlaylistItemDto>? PlaylistItems { get; set; }
}

public class PlayerPlaylistItemDto
{
    public int MediaFileId { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public int OrderNumber { get; set; }
    public int DisplayDuration { get; set; }
    public bool SoundEnabled { get; set; }
    public string FileHash { get; set; } = string.Empty;
}

public class HeartbeatDto
{
    public string ScreenCode { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? CurrentMediaName { get; set; }
    public string? PlayerVersion { get; set; }
    public long? FreeDiskSpace { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DownloadCompletedDto
{
    public string ScreenCode { get; set; } = string.Empty;
    public int MediaFileId { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
}

public class PlayerErrorDto
{
    public string ScreenCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
