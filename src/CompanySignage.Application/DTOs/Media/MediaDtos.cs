using CompanySignage.Domain.Enums;

namespace CompanySignage.Application.DTOs.Media;

public class MediaFileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public double Duration { get; set; }
    public int DisplayDuration { get; set; }
    public bool SoundEnabled { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MediaTypeText => MediaType == MediaType.Video ? "Video" : "Görsel";
    public string FileSizeText
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F1} MB";
            return $"{FileSize / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}

public class UpdateMediaDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayDuration { get; set; } = 10;
    public bool SoundEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
