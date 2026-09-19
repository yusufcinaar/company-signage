using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using CompanySignage.Player.Models;
using Newtonsoft.Json;

namespace CompanySignage.Player.Services;

public class CacheService
{
    private readonly HttpClient _httpClient;
    private readonly PlayerSettings _settings;

    public string? LastError { get; private set; }

    public CacheService(PlayerSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _httpClient.DefaultRequestHeaders.Add("X-Screen-Code", settings.ScreenCode.Trim().ToUpperInvariant());
        _httpClient.DefaultRequestHeaders.Add("X-Device-Token", settings.DeviceToken);
    }

    public async Task<PlaylistManifest?> FetchPublicationAsync()
    {
        try
        {
            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/player/{_settings.ScreenCode}/current-publication";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonConvert.DeserializeObject<PlaylistManifest>(json);
            if (manifest != null && manifest.PlaylistItems == null)
                manifest.PlaylistItems = new List<ManifestItem>();
            return manifest;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DownloadContentAsync(PlaylistManifest manifest)
    {
        LastError = null;
        Directory.CreateDirectory(PlayerSettings.CacheFolder);
        var requiredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "manifest.json" };

        try
        {
            if (manifest.IsPlaylist || manifest.PlaylistItems.Count > 0)
            {
                foreach (var item in manifest.PlaylistItems)
                {
                    if (string.IsNullOrEmpty(item.StoredFileName)) continue;

                    requiredFiles.Add(item.StoredFileName);
                    await DownloadFileAsync(item.FileUrl, item.StoredFileName, item.FileHash);
                    await NotifyDownloadCompletedAsync(item.MediaFileId, item.StoredFileName, item.FileHash);
                }
            }
            else if (!string.IsNullOrEmpty(manifest.StoredFileName) && !string.IsNullOrEmpty(manifest.FileUrl))
            {
                requiredFiles.Add(manifest.StoredFileName!);
                await DownloadFileAsync(manifest.FileUrl!, manifest.StoredFileName!, manifest.FileHash);

                if (manifest.MediaFileId.HasValue)
                    await NotifyDownloadCompletedAsync(manifest.MediaFileId.Value, manifest.StoredFileName!, manifest.FileHash);
            }

            manifest.Save();
            CleanupUnusedFiles(requiredFiles);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private async Task DownloadFileAsync(string fileUrl, string storedFileName, string expectedHash)
    {
        var targetPath = Path.Combine(PlayerSettings.CacheFolder, storedFileName);

        if (File.Exists(targetPath))
        {
            if (string.IsNullOrWhiteSpace(expectedHash) || FileHashMatches(targetPath, expectedHash))
                return;

            File.Delete(targetPath);
        }

        var tempPath = targetPath + ".tmp";

        try
        {
            var downloadUri = ResolveDownloadUri(fileUrl);

            using var response = await _httpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            if (!string.IsNullOrWhiteSpace(expectedHash) && !FileHashMatches(tempPath, expectedHash))
                throw new InvalidDataException("İndirilen dosyanın SHA-256 doğrulaması başarısız.");

            if (File.Exists(targetPath)) File.Delete(targetPath);
            File.Move(tempPath, targetPath);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }

    private Uri ResolveDownloadUri(string fileUrl)
    {
        var serverBase = new Uri(_settings.ServerUrl.TrimEnd('/') + "/");

        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var downloadUri))
            return new Uri(serverBase, fileUrl.TrimStart('/'));

        // Eski kay?tlardaki localhost adresi uzak Player'da sunucu adresine ?evrilir.
        if (downloadUri.IsLoopback && !serverBase.IsLoopback)
            return new Uri(serverBase, downloadUri.PathAndQuery.TrimStart('/'));

        if (!string.Equals(downloadUri.GetLeftPart(UriPartial.Authority),
                serverBase.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Medya adresi ayarlanan sunucuya ait degil.");
        return downloadUri;
    }

    private static bool FileHashMatches(string path, string expectedHash)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha256 = SHA256.Create();
        var actualHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        return string.Equals(actualHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task NotifyDownloadCompletedAsync(int mediaFileId, string storedFileName, string fileHash)
    {
        try
        {
            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/player/{_settings.ScreenCode}/download-completed";
            var payload = new
            {
                screenCode = _settings.ScreenCode,
                mediaFileId,
                storedFileName,
                fileHash
            };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                System.Text.Encoding.UTF8,
                "application/json");
            await _httpClient.PostAsync(url, content);
        }
        catch
        {
            // Bildirim hatası oynatmayı engellemez.
        }
    }

    private static void CleanupUnusedFiles(HashSet<string> requiredFiles)
    {
        try
        {
            foreach (var file in Directory.GetFiles(PlayerSettings.CacheFolder))
            {
                var fileName = Path.GetFileName(file);
                if (!requiredFiles.Contains(fileName))
                    File.Delete(file);
            }
        }
        catch
        {
            // Temizlik hatası oynatmayı engellemez.
        }
    }

    public static void ClearAll()
    {
        try
        {
            if (Directory.Exists(PlayerSettings.CacheFolder))
                Directory.Delete(PlayerSettings.CacheFolder, recursive: true);
            Directory.CreateDirectory(PlayerSettings.CacheFolder);
        }
        catch
        {
        }
    }

    public string GetLocalPath(string storedFileName)
        => Path.Combine(PlayerSettings.CacheFolder, storedFileName);

    public bool IsFileCached(string storedFileName)
        => File.Exists(GetLocalPath(storedFileName));
}
