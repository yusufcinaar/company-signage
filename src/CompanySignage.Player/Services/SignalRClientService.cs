using CompanySignage.Player.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CompanySignage.Player.Services;

/// <summary>
/// Sunucudaki SignageHub'a bağlanır, ekran koduna ait gruba katılır
/// ve gelen komutları olaylar üzerinden iletir. Bağlantı koparsa otomatik yeniden bağlanır.
/// </summary>
public class SignalRClientService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly PlayerSettings _settings;
    private bool _disposed;

    public event Action? ContentUpdated;
    public event Action? PlaylistUpdated;
    public event Action? RefreshPlayer;
    public event Action? StopPlayback;
    public event Action? RestartPlayer;
    public event Action? ClearCache;
    public event Action? ScreenConfigurationUpdated;
    public event Action<bool>? ConnectionStateChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRClientService(PlayerSettings settings)
    {
        _settings = settings;
    }

    public async Task StartAsync()
    {
        var hubUrl = $"{_settings.ServerUrl.TrimEnd('/')}/signageHub";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers["X-Screen-Code"] = _settings.ScreenCode.Trim().ToUpperInvariant();
                options.Headers["X-Device-Token"] = _settings.DeviceToken;
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _connection.On<object>("ContentUpdated", _ => ContentUpdated?.Invoke());
        _connection.On<object>("PlaylistUpdated", _ => PlaylistUpdated?.Invoke());
        _connection.On<object>("RefreshPlayer", _ => RefreshPlayer?.Invoke());
        _connection.On<object>("StopPlayback", _ => StopPlayback?.Invoke());
        _connection.On<object>("RestartPlayer", _ => RestartPlayer?.Invoke());
        _connection.On<object>("ClearCache", _ => ClearCache?.Invoke());
        _connection.On<object>("ScreenConfigurationUpdated", _ => ScreenConfigurationUpdated?.Invoke());

        _connection.Reconnected += async _ =>
        {
            await JoinGroupAsync();
            ConnectionStateChanged?.Invoke(true);
        };

        _connection.Closed += async _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            if (_disposed) return;

            await Task.Delay(TimeSpan.FromSeconds(5));
            await ConnectWithRetryAsync();
        };

        await ConnectWithRetryAsync();
    }

    private async Task ConnectWithRetryAsync()
    {
        while (!_disposed && _connection != null && _connection.State != HubConnectionState.Connected)
        {
            try
            {
                await _connection.StartAsync();
                await JoinGroupAsync();
                ConnectionStateChanged?.Invoke(true);
            }
            catch
            {
                ConnectionStateChanged?.Invoke(false);
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }

    private async Task JoinGroupAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("JoinScreenGroup", _settings.ScreenCode.Trim().ToUpperInvariant());
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
