using CompanySignage.Application.DTOs.Dashboard;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory _dbContextFactory;

    public DashboardService(IDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var screens = await context.Screens
            .Include(s => s.CurrentMedia)
            .Include(s => s.CurrentPlaylist)
            .OrderBy(s => s.ScreenCode)
            .ToListAsync();

        var mediaFiles = await context.MediaFiles.Where(m => m.IsActive).ToListAsync();
        var activeAssignments = await context.ScreenAssignments.Where(a => a.IsActive).CountAsync();

        var dtos = screens.Select(s => new ScreenStatusDto
        {
            Id = s.Id,
            Name = s.Name,
            ScreenCode = s.ScreenCode,
            IpAddress = s.IpAddress,
            IsOnline = s.IsOnline,
            LastSeenAt = s.LastSeenAt,
            CurrentMediaName = s.CurrentMedia?.Name,
            CurrentPlaylistName = s.CurrentPlaylist?.Name,
            StatusText = GetStatusText(s),
            StatusColor = GetStatusColor(s)
        }).ToList();

        return new DashboardDto
        {
            TotalScreens = screens.Count,
            OnlineScreens = screens.Count(s => s.IsOnline && s.IsActive),
            OfflineScreens = screens.Count(s => (!s.IsOnline || !s.IsActive)),
            TotalImages = mediaFiles.Count(m => m.MediaType == MediaType.Image),
            TotalVideos = mediaFiles.Count(m => m.MediaType == MediaType.Video),
            ActiveAssignments = activeAssignments,
            Screens = dtos
        };
    }

    private static string GetStatusText(Domain.Entities.Screen s)
    {
        if (!s.IsActive) return "Devre Dışı";
        if (!s.IsOnline) return "Çevrimdışı";
        if (s.CurrentMediaId.HasValue || s.CurrentPlaylistId.HasValue) return "Yayın Yapıyor";
        return "Çevrimiçi";
    }

    private static string GetStatusColor(Domain.Entities.Screen s)
    {
        if (!s.IsActive) return "secondary";
        if (!s.IsOnline) return "danger";
        if (s.CurrentMediaId.HasValue || s.CurrentPlaylistId.HasValue) return "primary";
        return "success";
    }
}
