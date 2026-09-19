using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Infrastructure.Data;

public class SignageDbContext : SignageDbContextBase
{
    private readonly string _connectionString;

    public SignageDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SignageDbContext(DbContextOptions<SignageDbContext> options) : base(options)
    {
        _connectionString = "";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).IsRequired().HasMaxLength(100);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Screen>(e =>
        {
            e.ToTable("Screens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ScreenCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.DeviceTokenHash).IsRequired().HasMaxLength(500);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.ScreenCode).IsUnique();
            e.Property(x => x.ScreenWidth).HasDefaultValue(1920);
            e.Property(x => x.ScreenHeight).HasDefaultValue(1080);
            e.Property(x => x.Orientation).HasDefaultValue("Landscape").HasMaxLength(20);
            e.Property(x => x.DisplayMode).HasDefaultValue("Fill").HasMaxLength(20);
            e.HasOne(x => x.CurrentMedia)
                .WithMany()
                .HasForeignKey(x => x.CurrentMediaId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CurrentPlaylist)
                .WithMany(p => p.Screens)
                .HasForeignKey(x => x.CurrentPlaylistId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MediaFile>(e =>
        {
            e.ToTable("MediaFiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(300);
            e.Property(x => x.StoredFileName).IsRequired().HasMaxLength(300);
            e.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
            e.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.MediaType).HasConversion<int>();
            e.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
            e.Property(x => x.FileHash).HasMaxLength(100);
        });

        modelBuilder.Entity<Playlist>(e =>
        {
            e.ToTable("Playlists");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<PlaylistItem>(e =>
        {
            e.ToTable("PlaylistItems");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Playlist)
                .WithMany(p => p.Items)
                .HasForeignKey(x => x.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MediaFile)
                .WithMany(m => m.PlaylistItems)
                .HasForeignKey(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScreenAssignment>(e =>
        {
            e.ToTable("ScreenAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.AssignmentType).HasConversion<int>();
            e.HasOne(x => x.Screen)
                .WithMany(s => s.Assignments)
                .HasForeignKey(x => x.ScreenId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Playlist)
                .WithMany(p => p.Assignments)
                .HasForeignKey(x => x.PlaylistId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.MediaFile)
                .WithMany(m => m.Assignments)
                .HasForeignKey(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ScreenHeartbeat>(e =>
        {
            e.ToTable("ScreenHeartbeats");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Screen)
                .WithMany(s => s.Heartbeats)
                .HasForeignKey(x => x.ScreenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.ToTable("ActivityLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.HasOne(x => x.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

public class SignageDbContextFactory : IDbContextFactory
{
    private readonly string _connectionString;

    public SignageDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SignageDbContextBase CreateDbContext()
    {
        return new SignageDbContext(_connectionString);
    }
}
