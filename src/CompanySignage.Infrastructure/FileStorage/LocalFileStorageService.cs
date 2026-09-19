using CompanySignage.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CompanySignage.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadFolder;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _uploadFolder = configuration["FileStorage:UploadFolder"] ?? @"C:\CompanySignagePublic\Server\Uploads";

        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    public async Task<(string storedFileName, string filePath, string fileUrl)> SaveAsync(Stream stream, string originalFileName, string contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadFolder, storedFileName);
        var fileUrl = $"/uploads/{storedFileName}";

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);

        return (storedFileName, filePath, fileUrl);
    }

    public Task<bool> DeleteAsync(string storedFileName)
    {
        var filePath = Path.Combine(_uploadFolder, storedFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public string GetFilePath(string storedFileName)
    {
        return Path.Combine(_uploadFolder, storedFileName);
    }

    public string GetFileUrl(string storedFileName)
    {
        return $"/uploads/{storedFileName}";
    }

    public bool FileExists(string storedFileName)
    {
        return File.Exists(Path.Combine(_uploadFolder, storedFileName));
    }

    public long GetFileSize(string storedFileName)
    {
        var filePath = Path.Combine(_uploadFolder, storedFileName);
        var fileInfo = new FileInfo(filePath);
        return fileInfo.Exists ? fileInfo.Length : 0;
    }

    public string GetFileHash(string storedFileName)
    {
        var filePath = Path.Combine(_uploadFolder, storedFileName);
        if (!File.Exists(filePath)) return string.Empty;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
