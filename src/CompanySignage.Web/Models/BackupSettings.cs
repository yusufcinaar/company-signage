namespace CompanySignage.Web.Models;

public sealed class BackupSettings
{
    public string BackupFolder { get; set; } = @"C:\CompanySignagePublic\Server\Backups";
    public bool AutoBackupEnabled { get; set; } = true;
    public string BackupTime { get; set; } = "02:00";
    public int RetentionDays { get; set; } = 30;
    public DateTime? LastBackupAt { get; set; }
    public bool? LastBackupSucceeded { get; set; }
    public string? LastBackupMessage { get; set; }
}
