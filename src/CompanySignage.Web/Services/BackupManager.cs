using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using CompanySignage.Web.Models;
using Microsoft.Data.SqlClient;

namespace CompanySignage.Web.Services;

public sealed class BackupManager
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly SemaphoreSlim _backupLock = new(1, 1);
    private readonly SemaphoreSlim _settingsLock = new(1, 1);
    private readonly string _settingsPath;

    public BackupManager(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "Data", "backup-settings.json");
    }

    public async Task<BackupSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _settingsLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_settingsPath)) return new BackupSettings();
            await using var stream = File.OpenRead(_settingsPath);
            return await JsonSerializer.DeserializeAsync<BackupSettings>(stream, cancellationToken: cancellationToken)
                ?? new BackupSettings();
        }
        catch
        {
            return new BackupSettings();
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    public async Task SaveAsync(BackupSettings settings, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        await _settingsLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var tempPath = _settingsPath + ".tmp";
            await using (var stream = File.Create(tempPath))
                await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
            File.Move(tempPath, _settingsPath, true);
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    public async Task<(bool Success, string Message)> RunBackupAsync(CancellationToken cancellationToken = default)
    {
        if (!await _backupLock.WaitAsync(0, cancellationToken))
            return (false, "Başka bir yedekleme işlemi devam ediyor.");

        var settings = await LoadAsync(cancellationToken);
        string? sqlStagingFile = null;
        try
        {
            Validate(settings);
            var backupRoot = Path.GetFullPath(settings.BackupFolder);
            Directory.CreateDirectory(backupRoot);
            var backupFolder = Path.Combine(backupRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupFolder);

            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("MSSQL bağlantı bilgisi bulunamadı.");
            var sqlBuilder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = sqlBuilder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("MSSQL veritabanı adı bulunamadı.");

            sqlBuilder.InitialCatalog = "master";
            await using (var connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await using var pathCommand = connection.CreateCommand();
                pathCommand.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
                var sqlBackupFolder = Convert.ToString(await pathCommand.ExecuteScalarAsync(cancellationToken));
                if (string.IsNullOrWhiteSpace(sqlBackupFolder))
                    throw new InvalidOperationException("SQL Server varsayılan yedek klasörü bulunamadı.");

                await using var editionCommand = connection.CreateCommand();
                editionCommand.CommandText = "SELECT CAST(SERVERPROPERTY('EngineEdition') AS int)";
                var engineEdition = Convert.ToInt32(await editionCommand.ExecuteScalarAsync(cancellationToken));
                var compressionOption = engineEdition == 4 ? "" : ", COMPRESSION";

                var safeFileName = $"CompanySignage_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                sqlStagingFile = Path.Combine(sqlBackupFolder, safeFileName);
                await using var backupCommand = connection.CreateCommand();
                backupCommand.CommandTimeout = 0;
                backupCommand.CommandText = $"BACKUP DATABASE [{databaseName.Replace("]", "]]", StringComparison.Ordinal)}] TO DISK = @path WITH INIT, CHECKSUM{compressionOption}";
                backupCommand.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = sqlStagingFile });
                await backupCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            File.Copy(sqlStagingFile, Path.Combine(backupFolder, "database.bak"), false);
            File.Delete(sqlStagingFile);
            sqlStagingFile = null;

            var uploadFolder = _configuration["FileStorage:UploadFolder"] ?? @"C:\CompanySignagePublic\Server\Uploads";
            if (Directory.Exists(uploadFolder))
                CopyDirectory(uploadFolder, Path.Combine(backupFolder, "Uploads"));

            var appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            if (File.Exists(appSettingsPath))
                File.Copy(appSettingsPath, Path.Combine(backupFolder, "appsettings.json"), false);

            CleanupOldBackups(backupRoot, settings.RetentionDays);
            settings.LastBackupAt = DateTime.Now;
            settings.LastBackupSucceeded = true;
            settings.LastBackupMessage = $"Yedekleme tamamlandı: {backupFolder}";
            await SaveAsync(settings, cancellationToken);
            return (true, settings.LastBackupMessage);
        }
        catch (Exception ex)
        {
            settings.LastBackupAt = DateTime.Now;
            settings.LastBackupSucceeded = false;
            settings.LastBackupMessage = ex.Message;
            try { await SaveAsync(settings, CancellationToken.None); } catch { }
            return (false, ex.Message);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(sqlStagingFile))
            {
                try { File.Delete(sqlStagingFile); } catch { }
            }
            _backupLock.Release();
        }
    }

    public static void Validate(BackupSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BackupFolder) || !Path.IsPathFullyQualified(settings.BackupFolder))
            throw new ArgumentException("Yedek klasörü tam bir Windows yolu olmalıdır.");
        var fullPath = Path.GetFullPath(settings.BackupFolder).TrimEnd(Path.DirectorySeparatorChar);
        var root = Path.GetPathRoot(fullPath)?.TrimEnd(Path.DirectorySeparatorChar);
        if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Disk kök dizini doğrudan yedek klasörü olarak kullanılamaz.");
        if (!TimeSpan.TryParseExact(settings.BackupTime, @"hh\:mm", null, out _))
            throw new ArgumentException("Yedekleme saati HH:mm biçiminde olmalıdır.");
        if (settings.RetentionDays is < 1 or > 3650)
            throw new ArgumentException("Saklama süresi 1–3650 gün arasında olmalıdır.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, false);
        }
    }

    private static void CleanupOldBackups(string backupRoot, int retentionDays)
    {
        var cutoff = DateTime.Now.AddDays(-retentionDays);
        foreach (var directory in Directory.EnumerateDirectories(backupRoot))
        {
            var name = Path.GetFileName(directory);
            if (Regex.IsMatch(name, @"^\d{8}_\d{6}$") && Directory.GetCreationTime(directory) < cutoff)
                Directory.Delete(directory, true);
        }
    }
}
