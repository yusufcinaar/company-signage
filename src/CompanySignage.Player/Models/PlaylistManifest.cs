using System.IO;
using Newtonsoft.Json;

namespace CompanySignage.Player.Models;

/// <summary>
/// Sunucudan alınan yayın bilgisi. Cache klasöründe manifest.json olarak saklanır
/// ve çevrimdışı modda son yayının oynatılmasını sağlar.
/// </summary>
public class PlaylistManifest
{
    public int? MediaFileId { get; set; }
    public int? PlaylistId { get; set; }
    public string AssignmentType { get; set; } = "SingleMedia";
    public string DisplayMode { get; set; } = "Fill";
    public int PlaylistVersion { get; set; }
    public int PublicationRevision { get; set; }
    public string? MediaName { get; set; }
    public string? FileUrl { get; set; }
    public string? StoredFileName { get; set; }
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public PlayerMediaType? MediaType { get; set; }
    public int DisplayDuration { get; set; } = 10;
    public bool SoundEnabled { get; set; }
    public string FileHash { get; set; } = "";
    public List<ManifestItem> PlaylistItems { get; set; } = new();

    [JsonIgnore]
    public bool IsPlaylist =>
        string.Equals(AssignmentType, "Playlist", StringComparison.OrdinalIgnoreCase) ||
        AssignmentType == "1";

    public static string ManifestPath => Path.Combine(PlayerSettings.CacheFolder, "manifest.json");

    public static PlaylistManifest? Load()
    {
        try
        {
            if (!File.Exists(ManifestPath)) return null;
            return JsonConvert.DeserializeObject<PlaylistManifest>(File.ReadAllText(ManifestPath));
        }
        catch
        {
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(PlayerSettings.CacheFolder);
        File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}

public class ManifestItem
{
    public int MediaFileId { get; set; }
    public string StoredFileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public PlayerMediaType MediaType { get; set; }
    public int OrderNumber { get; set; }
    public int DisplayDuration { get; set; } = 10;
    public bool SoundEnabled { get; set; }
    public string FileHash { get; set; } = "";
}
public enum PlayerMediaType
{
    Image = 0,
    Video = 1
}
