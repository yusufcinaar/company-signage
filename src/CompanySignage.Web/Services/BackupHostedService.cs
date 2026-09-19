namespace CompanySignage.Web.Services;

public sealed class BackupHostedService : BackgroundService
{
    private readonly BackupManager _backupManager;
    private DateOnly? _lastAttemptDate;

    public BackupHostedService(BackupManager backupManager)
    {
        _backupManager = backupManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = await _backupManager.LoadAsync(stoppingToken);
                if (settings.AutoBackupEnabled && TimeSpan.TryParse(settings.BackupTime, out var scheduled))
                {
                    var now = DateTime.Now;
                    var today = DateOnly.FromDateTime(now);
                    if (now.TimeOfDay >= scheduled && _lastAttemptDate != today && settings.LastBackupAt?.Date != now.Date)
                    {
                        _lastAttemptDate = today;
                        await _backupManager.RunBackupAsync(stoppingToken);
                    }
                }
            }
            catch when (!stoppingToken.IsCancellationRequested) { }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
