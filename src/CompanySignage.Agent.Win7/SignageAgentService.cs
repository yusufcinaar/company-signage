using System.Diagnostics;
using System.Net.Http;
using System.ServiceProcess;
using Newtonsoft.Json;

namespace CompanySignage.Agent.Win7;

public sealed class SignageAgentService : ServiceBase
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private Timer? _timer;
    private int _busy;
    private static readonly string Root = @"C:\CompanySignagePublic\Player";
    private static readonly string SettingsPath = Path.Combine(Root, "settings.json");
    private static readonly string LogFolder = Path.Combine(Root, "Logs");
    private static readonly string PlayerExe = Path.Combine(Root, "App", "CompanySignage.Player.exe");

    public SignageAgentService()
    {
        ServiceName = "CompanySignageAgent";
        CanStop = true;
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        Directory.CreateDirectory(LogFolder);
        Log("Agent başladı.");
        _timer = new Timer(async _ => await CheckAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    protected override void OnStop()
    {
        _timer?.Dispose();
        _timer = null;
        Log("Agent durdu.");
    }

    public void StartConsole() => OnStart(Array.Empty<string>());
    public void StopConsole() => OnStop();

    private async Task CheckAsync()
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        try
        {
            EnsurePlayerRunning();

            if (!File.Exists(SettingsPath))
            {
                Log("settings.json bekleniyor.");
                return;
            }

            var settings = JsonConvert.DeserializeObject<AgentSettings>(File.ReadAllText(SettingsPath));
            if (settings == null || string.IsNullOrWhiteSpace(settings.ServerUrl) || string.IsNullOrWhiteSpace(settings.ScreenCode))
            {
                Log("Agent ayarları geçersiz.");
                return;
            }

            var url = settings.ServerUrl.TrimEnd('/') + "/api/player/" + Uri.EscapeDataString(settings.ScreenCode) + "/configuration";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Screen-Code", settings.ScreenCode.Trim().ToUpperInvariant());
            request.Headers.Add("X-Device-Token", settings.DeviceToken);
            using var response = await _http.SendAsync(request);
            Log(response.IsSuccessStatusCode
                ? "Sunucu bağlantısı başarılı."
                : "Sunucu yanıtı: " + (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            // Sunucu/ağ yokken Player önbellekten oynamaya devam eder.
            Log("Kontrol hatası: " + ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private static void EnsurePlayerRunning()
    {
        var processes = Process.GetProcessesByName("CompanySignage.Player");
        try
        {
            if (processes.Length > 0) return;
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }

        if (!File.Exists(PlayerExe))
        {
            Log("Player dosyası bulunamadı: " + PlayerExe);
            return;
        }

        Log("Player çalışmıyor; oturum açılış görevi tetikleniyor.");
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = "/Run /TN \"CompanySignagePlayer\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var taskProcess = Process.Start(startInfo);
        if (taskProcess == null)
        {
            Log("Player görevi başlatılamadı.");
            return;
        }

        if (!taskProcess.WaitForExit(10000))
        {
            taskProcess.Kill();
            Log("Player görev komutu zaman aşımına uğradı.");
            return;
        }

        Log(taskProcess.ExitCode == 0
            ? "Player görevi tetiklendi."
            : "Player görevi hata kodu: " + taskProcess.ExitCode);
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            File.AppendAllText(
                Path.Combine(LogFolder, "agent.log"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
        }
        catch
        {
        }
    }

    private sealed class AgentSettings
    {
        public string ServerUrl { get; set; } = "";
        public string ScreenCode { get; set; } = "";
        public string DeviceToken { get; set; } = "";
    }
}
