using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Application.Interfaces;

public interface IDbContextFactory
{
    SignageDbContextBase CreateDbContext();
}

public abstract class SignageDbContextBase : DbContext
{
    protected SignageDbContextBase() { }
    protected SignageDbContextBase(DbContextOptions options) : base(options) { }

    public DbSet<Domain.Entities.User> Users { get; set; } = null!;
    public DbSet<Domain.Entities.Screen> Screens { get; set; } = null!;
    public DbSet<Domain.Entities.MediaFile> MediaFiles { get; set; } = null!;
    public DbSet<Domain.Entities.Playlist> Playlists { get; set; } = null!;
    public DbSet<Domain.Entities.PlaylistItem> PlaylistItems { get; set; } = null!;
    public DbSet<Domain.Entities.ScreenAssignment> ScreenAssignments { get; set; } = null!;
    public DbSet<Domain.Entities.ScreenHeartbeat> ScreenHeartbeats { get; set; } = null!;
    public DbSet<Domain.Entities.ActivityLog> ActivityLogs { get; set; } = null!;
}
