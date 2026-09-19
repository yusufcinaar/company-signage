namespace CompanySignage.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string storedFileName, string filePath, string fileUrl)> SaveAsync(Stream stream, string originalFileName, string contentType);
    Task<bool> DeleteAsync(string storedFileName);
    string GetFilePath(string storedFileName);
    string GetFileUrl(string storedFileName);
    bool FileExists(string storedFileName);
    long GetFileSize(string storedFileName);
    string GetFileHash(string storedFileName);
}

public interface ILogService
{
    Task LogAsync(int? userId, string action, string? description, string? ipAddress);
    Task<List<Domain.Entities.ActivityLog>> GetRecentAsync(int count = 100);
}
