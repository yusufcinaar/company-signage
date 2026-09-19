using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using CompanySignage.Player.Models;
using Newtonsoft.Json;

namespace CompanySignage.Player.Services;

/// <summary>
/// 30 saniyede bir sunucuya heartbeat gönderir.
/// Ekran kodu, IP, player versiyonu, oynatılan içerik, boş disk alanı ve hata bilgisi iletir.
/// </summary>
public class HeartbeatService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private readonly PlayerSettings _settings;
    private readonly HttpClient _httpClient;
    private Timer? _timer;

    public string? CurrentMediaName { get; set; }
    public string? LastError { get; set; }
    public const string PlayerVersion = "1.1.0";

    public HeartbeatService(PlayerSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Add("X-Screen-Code", settings.ScreenCode.Trim().ToUpperInvariant());
        _httpClient.DefaultRequestHeaders.Add("X-Device-Token", settings.DeviceToken);
    }

    public void Start()
    {
        _timer = new Timer(async _ => await SendHeartbeatAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/player/{_settings.ScreenCode}/heartbeat";
            var payload = new
            {
                screenCode = _settings.ScreenCode,
                ipAddress = GetLocalIpAddress(),
                currentMediaName = CurrentMediaName,
                playerVersion = PlayerVersion,
                freeDiskSpace = GetFreeDiskSpace(),
                screenWidth = GetSystemMetrics(0),
                screenHeight = GetSystemMetrics(1),
                errorMessage = LastError
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            LastError = null;
        }
        catch
        {
            // Sunucuya ulaşılamıyor — offline modda devam
        }
    }

    private static string? GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    private static long GetFreeDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(PlayerSettings.CacheFolder) ?? "C:\\");
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    public async Task ReportErrorAsync(string errorMessage)
    {
        LastError = errorMessage;
        try
        {
            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/player/{_settings.ScreenCode}/error";
            var payload = new { screenCode = _settings.ScreenCode, errorMessage };
            var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(url, content);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _httpClient.Dispose();
    }
}
