using System.IO;
using Newtonsoft.Json;

namespace CompanySignage.Player.Models;

/// <summary>
/// Player ayarları. C:\CompanySignagePublic\Player\settings.json dosyasında saklanır.
/// </summary>
public class PlayerSettings
{
    public string ServerUrl { get; set; } = "http://localhost:5000";
    public string ScreenCode { get; set; } = "SCREEN-001";
    public string DeviceToken { get; set; } = "";
    public bool AutoStart { get; set; } = true;
    public bool SoundEnabled { get; set; } = false;

    [JsonIgnore]
    public static string SettingsFolder
    {
        get
        {
            var overrideFolder = Environment.GetEnvironmentVariable("COMPANY_SIGNAGE_DATA_DIR");
            return string.IsNullOrWhiteSpace(overrideFolder) ? @"C:\CompanySignagePublic\Player" : overrideFolder;
        }
    }

    [JsonIgnore]
    public static string SettingsFilePath => Path.Combine(SettingsFolder, "settings.json");

    [JsonIgnore]
    public static string CacheFolder => Path.Combine(SettingsFolder, "Cache");

    public static PlayerSettings? Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) return null;
            var json = File.ReadAllText(SettingsFilePath);
            return JsonConvert.DeserializeObject<PlayerSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsFolder);
        Directory.CreateDirectory(CacheFolder);
        File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
