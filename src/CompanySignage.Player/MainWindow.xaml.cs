using System.IO;
using System.Windows;
using System.Windows.Input;
using CompanySignage.Player.Models;
using CompanySignage.Player.Services;
using CompanySignage.Player.Views;

namespace CompanySignage.Player;

public partial class MainWindow : Window
{
    private PlayerSettings _settings = null!;
    private CacheService _cacheService = null!;
    private PlaybackService _playbackService = null!;
    private SignalRClientService _signalRService = null!;
    private HeartbeatService _heartbeatService = null!;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var settings = PlayerSettings.Load();
        if (settings == null)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            settings = settingsWindow.Result;
            if (settings == null)
            {
                Application.Current.Shutdown();
                return;
            }
        }
        _settings = settings;

        Directory.CreateDirectory(PlayerSettings.CacheFolder);

        _cacheService = new CacheService(_settings);
        _playbackService = new PlaybackService(PlaybackGrid, _cacheService, _settings);
        _heartbeatService = new HeartbeatService(_settings);

        _playbackService.CurrentMediaChanged += mediaName =>
        {
            Dispatcher.Invoke(() => { _heartbeatService.CurrentMediaName = mediaName; });
        };
        _playbackService.ErrorOccurred += error =>
        {
            Dispatcher.Invoke(() =>
            {
                ErrorText.Text = error;
                ErrorBar.Visibility = Visibility.Visible;
                _ = _heartbeatService.ReportErrorAsync(error);
            });
        };

        _heartbeatService.Start();

        _signalRService = new SignalRClientService(_settings);
        _signalRService.ConnectionStateChanged += connected =>
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = connected ? "🟢 Çevrimiçi" : "🔴 Çevrimdışı";
            });
        };
        _signalRService.ContentUpdated += async () =>
        {
            await RefreshPlaybackAsync(forceReload: true);
        };
        _signalRService.PlaylistUpdated += async () =>
        {
            await RefreshPlaybackAsync(forceReload: true);
        };
        _signalRService.RefreshPlayer += async () =>
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                _playbackService.Stop();
                await _playbackService.StartAsync();
            });
        };
        _signalRService.StopPlayback += () => Dispatcher.Invoke(() => _playbackService.Stop());
        _signalRService.RestartPlayer += () => Dispatcher.Invoke(() =>
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath)) System.Diagnostics.Process.Start(exePath);
            Application.Current.Shutdown();
        });
        _signalRService.ClearCache += () => Dispatcher.Invoke(() =>
        {
            _playbackService.Stop();
            CacheService.ClearAll();
            _ = _playbackService.StartAsync();
        });

        _ = _signalRService.StartAsync();
        await _playbackService.StartAsync();
    }

    private async Task RefreshPlaybackAsync(bool forceReload)
    {
        if (Dispatcher.CheckAccess())
        {
            await _playbackService.RefreshFromServerAsync(forceReload);
            return;
        }

        var refreshTask = await Dispatcher.InvokeAsync(
            () => _playbackService.RefreshFromServerAsync(forceReload));
        await refreshTask;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Q &&
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
            (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            CleanupAndShutdown();
        }
        base.OnKeyDown(e);
    }

    private async void CleanupAndShutdown()
    {
        _playbackService?.Stop();
        _heartbeatService?.Dispose();
        if (_signalRService != null)
            await _signalRService.DisposeAsync();
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _playbackService?.Dispose();
        _heartbeatService?.Dispose();
        base.OnClosed(e);
    }
}
