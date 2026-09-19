using CompanySignage.Application.DTOs.Media;

namespace CompanySignage.Application.Interfaces;

public interface IMediaService
{
    Task<List<MediaFileDto>> GetAllAsync();
    Task<MediaFileDto?> GetByIdAsync(int id);
    Task<MediaFileDto> UploadAsync(Stream stream, string originalFileName, string contentType, long fileSize, string name, string? description, int displayDuration, bool soundEnabled);
    Task<MediaFileDto?> UpdateAsync(int id, UpdateMediaDto dto);
    Task<bool> DeleteAsync(int id);
    Task<string> GetFilePathAsync(int id);
}
