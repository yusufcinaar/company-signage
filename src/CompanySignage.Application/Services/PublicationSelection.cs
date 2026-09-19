using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Services;

public static class PublicationSelection
{
    // Resolve at read time as well as after edits: schedule boundaries need no UI action.
    public static async Task<int> RefreshAsync(SignageDbContextBase context, int screenId, DateTime? instant = null)
    {
        var screen = await context.Screens.FindAsync(screenId);
        if (screen == null) return 0;
        var now = instant ?? DateTime.UtcNow;
        var selected = screen.IsActive ? await context.ScreenAssignments
            .Where(a => a.ScreenId == screenId && a.IsActive
                && (!a.StartDate.HasValue || a.StartDate <= now)
                && (!a.EndDate.HasValue || a.EndDate > now)
                && ((a.AssignmentType == AssignmentType.SingleMedia && a.MediaFile != null && a.MediaFile.IsActive)
                    || (a.AssignmentType == AssignmentType.Playlist && a.Playlist != null && a.Playlist.IsActive
                        && a.Playlist.Items.Any(i => i.MediaFile.IsActive))))
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.StartDate)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync() : null;
        int? media = selected?.AssignmentType == AssignmentType.SingleMedia ? selected.MediaFileId : null;
        int? playlist = selected?.AssignmentType == AssignmentType.Playlist ? selected.PlaylistId : null;
        if (screen.CurrentMediaId != media || screen.CurrentPlaylistId != playlist)
        {
            screen.CurrentMediaId = media;
            screen.CurrentPlaylistId = playlist;
            await context.SaveChangesAsync();
        }
        return selected?.Id ?? 0;
    }
}
