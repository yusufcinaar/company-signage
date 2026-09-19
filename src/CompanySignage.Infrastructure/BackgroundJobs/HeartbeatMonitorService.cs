using CompanySignage.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CompanySignage.Infrastructure.BackgroundJobs;

/// <summary>
/// Periyodik olarak ekranların heartbeat durumunu kontrol eder.
/// Son heartbeat'ten bu yana belirli bir süre geçen (varsayılan 90 sn)
/// çevrimiçi ekranları offline olarak işaretler.
/// </summary>
public class HeartbeatMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatMonitorService>? _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromSeconds(90);

    public HeartbeatMonitorService(IServiceProvider serviceProvider, ILogger<HeartbeatMonitorService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("HeartbeatMonitorService başlatıldı. Kontrol aralığı: {Interval}, Offline eşiği: {Threshold}",
            _checkInterval, _offlineThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckScreensAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Heartbeat kontrol sırasında hata oluştu");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckScreensAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory>();

        using var context = dbContextFactory.CreateDbContext();
        var threshold = DateTime.UtcNow - _offlineThreshold;

        var staleScreens = context.Screens
            .Where(s => s.IsOnline && s.LastSeenAt.HasValue && s.LastSeenAt.Value < threshold)
            .ToList();

        if (staleScreens.Count == 0) return;

        foreach (var screen in staleScreens)
        {
            screen.IsOnline = false;
        }

        await context.SaveChangesAsync();

        foreach (var screen in staleScreens)
        {
            _logger?.LogWarning("Ekran {ScreenCode} ({ScreenName}) offline olarak işaretlendi. Son görülme: {LastSeen}",
                screen.ScreenCode, screen.Name, screen.LastSeenAt);
        }
    }
}
