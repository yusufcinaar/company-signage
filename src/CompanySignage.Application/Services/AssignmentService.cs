using CompanySignage.Application.DTOs.Assignment;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly ISignalRService _signalR;
    private readonly ILogService _logService;

    public AssignmentService(IDbContextFactory dbContextFactory, ISignalRService signalR, ILogService logService)
    {
        _dbContextFactory = dbContextFactory;
        _signalR = signalR;
        _logService = logService;
    }

    public async Task<List<AssignmentDto>> GetAllAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var assignments = await context.ScreenAssignments
            .Include(a => a.Screen)
            .Include(a => a.Playlist)
            .Include(a => a.MediaFile)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return assignments.Select(MapToDto).ToList();
    }

    public async Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto)
    {
        NormalizeDates(dto);
        using var context = _dbContextFactory.CreateDbContext();
        var assignment = new ScreenAssignment
        {
            ScreenId = dto.ScreenId,
            PlaylistId = dto.PlaylistId,
            MediaFileId = dto.MediaFileId,
            AssignmentType = dto.AssignmentType,
            Priority = dto.Priority,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true
        };
        Validate(dto);
        context.ScreenAssignments.Add(assignment);

        var screen = await context.Screens.FindAsync(dto.ScreenId);
        await context.SaveChangesAsync();
        await RefreshAndNotifyAsync(context, dto.ScreenId);
        await _logService.LogAsync(null, "ASSIGNMENT_CREATED", $"Yayın ataması yapıldı. Ekran: {screen?.ScreenCode}", null);
        return MapToDto(assignment);
    }

    public async Task<AssignmentDto?> UpdateAsync(int id, CreateAssignmentDto dto)
    {
        NormalizeDates(dto);
        using var context = _dbContextFactory.CreateDbContext();
        var assignment = await context.ScreenAssignments.FindAsync(id);
        if (assignment == null) return null;
        Validate(dto);
        var previousScreenId = assignment.ScreenId;
        assignment.ScreenId = dto.ScreenId;
        assignment.PlaylistId = dto.PlaylistId;
        assignment.MediaFileId = dto.MediaFileId;
        assignment.AssignmentType = dto.AssignmentType;
        assignment.Priority = dto.Priority;
        assignment.StartDate = dto.StartDate;
        assignment.EndDate = dto.EndDate;
        await context.SaveChangesAsync();
        await RefreshAndNotifyAsync(context, previousScreenId);
        if (previousScreenId != dto.ScreenId) await RefreshAndNotifyAsync(context, dto.ScreenId);
        return MapToDto(assignment);
    }

    private static void NormalizeDates(CreateAssignmentDto dto)
    {
        // JSON offsets are parsed into local DateTimes; persist and compare UTC.
        dto.StartDate = NormalizeUtc(dto.StartDate);
        dto.EndDate = NormalizeUtc(dto.EndDate);
    }

    private static DateTime? NormalizeUtc(DateTime? value) => value.HasValue
        ? value.Value.Kind == DateTimeKind.Local ? value.Value.ToUniversalTime()
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        : null;

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var assignment = await context.ScreenAssignments.FindAsync(id);
        if (assignment == null) return false;
        context.ScreenAssignments.Remove(assignment);
        await context.SaveChangesAsync();
        await RefreshAndNotifyAsync(context, assignment.ScreenId);
        return true;
    }

    public async Task<bool> SendNowAsync(SendNowDto dto)
    {
        if (dto.ScreenIds.Count == 0) return false;

        using var context = _dbContextFactory.CreateDbContext();

        if (dto.AssignmentType == AssignmentType.Playlist)
        {
            if (!dto.PlaylistId.HasValue) return false;
            var playlistIsValid = await context.Playlists
                .AnyAsync(p => p.Id == dto.PlaylistId.Value && p.IsActive && p.Items.Any());
            if (!playlistIsValid) return false;
        }
        else
        {
            if (!dto.MediaFileId.HasValue) return false;
            var mediaIsValid = await context.MediaFiles
                .AnyAsync(m => m.Id == dto.MediaFileId.Value && m.IsActive);
            if (!mediaIsValid) return false;
        }

        var distinctScreenIds = dto.ScreenIds.Distinct().ToList();
        var screens = await context.Screens
            .Where(s => distinctScreenIds.Contains(s.Id) && s.IsActive)
            .ToListAsync();

        if (screens.Count == 0) return false;

        var validScreenIds = screens.Select(s => s.Id).ToList();
        var previousAssignments = await context.ScreenAssignments
            .Where(a => validScreenIds.Contains(a.ScreenId) && a.IsActive
                && (!a.StartDate.HasValue || a.StartDate <= DateTime.UtcNow))
            .ToListAsync();

        foreach (var previous in previousAssignments)
            previous.IsActive = false;

        foreach (var screen in screens)
        {
            if (dto.AssignmentType == AssignmentType.Playlist)
            {
                screen.CurrentPlaylistId = dto.PlaylistId;
                screen.CurrentMediaId = null;
            }
            else
            {
                screen.CurrentMediaId = dto.MediaFileId;
                screen.CurrentPlaylistId = null;
            }

            context.ScreenAssignments.Add(new ScreenAssignment
            {
                ScreenId = screen.Id,
                PlaylistId = dto.AssignmentType == AssignmentType.Playlist ? dto.PlaylistId : null,
                MediaFileId = dto.AssignmentType == AssignmentType.SingleMedia ? dto.MediaFileId : null,
                AssignmentType = dto.AssignmentType,
                Priority = 100,
                StartDate = DateTime.UtcNow,
                IsActive = true
            });
        }

        await context.SaveChangesAsync();

        var screenCodes = screens.Select(s => s.ScreenCode).ToList();
        var payload = new
        {
            assignmentType = dto.AssignmentType.ToString(),
            mediaFileId = dto.MediaFileId,
            playlistId = dto.PlaylistId
        };

        await _signalR.SendToScreensAsync(screenCodes, "ContentUpdated", payload);
        await _logService.LogAsync(null, "CONTENT_SENT",
            $"Yayın gönderildi. Ekranlar: {string.Join(", ", screenCodes)}", null);
        return true;
    }
    private static void Validate(CreateAssignmentDto dto)
    {
        if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
            throw new ArgumentException("Yayin bitisi baslangictan sonra olmali.");
        if (dto.AssignmentType == AssignmentType.Playlist ? !dto.PlaylistId.HasValue : !dto.MediaFileId.HasValue)
            throw new ArgumentException("Yayin icerigi secilmelidir.");
    }

    private async Task RefreshAndNotifyAsync(SignageDbContextBase context, int screenId)
    {
        await PublicationSelection.RefreshAsync(context, screenId);
        var screen = await context.Screens.FindAsync(screenId);
        if (screen != null) await _signalR.SendToScreenAsync(screen.ScreenCode, "ContentUpdated", new { });
    }

    private static AssignmentDto MapToDto(ScreenAssignment a) => new()
    {
        Id = a.Id,
        ScreenId = a.ScreenId,
        ScreenName = a.Screen?.Name ?? "",
        ScreenCode = a.Screen?.ScreenCode ?? "",
        PlaylistId = a.PlaylistId,
        PlaylistName = a.Playlist?.Name,
        MediaFileId = a.MediaFileId,
        MediaName = a.MediaFile?.Name,
        AssignmentType = a.AssignmentType,
        Priority = a.Priority,
        StartDate = a.StartDate,
        EndDate = a.EndDate,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt
    };
}
