using CompanySignage.Application.DTOs.Media;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class MediaService : IMediaService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogService _logService;

    public MediaService(IDbContextFactory dbContextFactory, IFileStorageService fileStorage, ILogService logService)
    {
        _dbContextFactory = dbContextFactory;
        _fileStorage = fileStorage;
        _logService = logService;
    }

    private static readonly HashSet<string> AllowedImageExtensions = new() { ".jpg", ".jpeg", ".png" };
    private static readonly HashSet<string> AllowedVideoExtensions = new() { ".mp4" };
    private static readonly HashSet<string> BlockedExtensions = new() { ".exe", ".bat", ".cmd", ".ps1", ".msi", ".dll", ".scr", ".com", ".vbs" };

    public async Task<List<MediaFileDto>> GetAllAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var files = await context.MediaFiles.OrderByDescending(m => m.CreatedAt).ToListAsync();
        return files.Select(MapToDto).ToList();
    }

    public async Task<MediaFileDto?> GetByIdAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var file = await context.MediaFiles.FindAsync(id);
        return file == null ? null : MapToDto(file);
    }

    public async Task<MediaFileDto> UploadAsync(Stream stream, string originalFileName, string contentType, long fileSize, string name, string? description, int displayDuration, bool soundEnabled)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (BlockedExtensions.Contains(extension))
            throw new ArgumentException("Bu dosya türüne izin verilmiyor.");

        var isImage = AllowedImageExtensions.Contains(extension);
        var isVideo = AllowedVideoExtensions.Contains(extension);

        if (!isImage && !isVideo)
            throw new ArgumentException("Desteklenmeyen dosya formatı. İzin verilen: JPG, JPEG, PNG ve MP4 (H.264/AAC)");

        var (storedFileName, filePath, fileUrl) = await _fileStorage.SaveAsync(stream, originalFileName, contentType);
        var fileHash = _fileStorage.GetFileHash(storedFileName);
        var actualSize = _fileStorage.GetFileSize(storedFileName);

        var mediaType = isImage ? MediaType.Image : MediaType.Video;
        var mimeType = GetMimeType(extension);

        using var context = _dbContextFactory.CreateDbContext();
        var media = new MediaFile
        {
            Name = name,
            Description = description,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            FileUrl = fileUrl,
            MediaType = mediaType,
            MimeType = mimeType,
            FileSize = actualSize,
            DisplayDuration = displayDuration,
            FileHash = fileHash,
            SoundEnabled = soundEnabled,
            IsActive = true
        };
        context.MediaFiles.Add(media);
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "MEDIA_UPLOADED", $"İçerik yüklendi: {name} ({originalFileName})", null);
        return MapToDto(media);
    }

    public async Task<MediaFileDto?> UpdateAsync(int id, UpdateMediaDto dto)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var media = await context.MediaFiles.FindAsync(id);
        if (media == null) return null;
        media.Name = dto.Name;
        media.Description = dto.Description;
        media.DisplayDuration = dto.DisplayDuration;
        media.SoundEnabled = dto.SoundEnabled;
        media.IsActive = dto.IsActive;
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "MEDIA_UPDATED", $"İçerik güncellendi: {media.Name}", null);
        return MapToDto(media);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var media = await context.MediaFiles.FindAsync(id);
        if (media == null) return false;
        await _fileStorage.DeleteAsync(media.StoredFileName);
        context.MediaFiles.Remove(media);
        await context.SaveChangesAsync();
        await _logService.LogAsync(null, "MEDIA_DELETED", $"İçerik silindi: {media.Name}", null);
        return true;
    }

    public async Task<string> GetFilePathAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var media = await context.MediaFiles.FindAsync(id);
        return media?.FilePath ?? string.Empty;
    }

    private static string GetMimeType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        _ => "application/octet-stream"
    };

    private static MediaFileDto MapToDto(MediaFile m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Description = m.Description,
        OriginalFileName = m.OriginalFileName,
        FileUrl = m.FileUrl,
        MediaType = m.MediaType,
        MimeType = m.MimeType,
        FileSize = m.FileSize,
        Duration = m.Duration,
        DisplayDuration = m.DisplayDuration,
        SoundEnabled = m.SoundEnabled,
        IsActive = m.IsActive,
        CreatedAt = m.CreatedAt
    };
}
