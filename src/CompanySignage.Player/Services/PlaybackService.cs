using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CompanySignage.Player.Models;

namespace CompanySignage.Player.Services;

/// <summary>
/// WPF MediaElement ve Image kullanarak görsel/video oynatır.
/// Playlist modunda sıradaki içeriğe otomatik geçer.
/// Çevrimdışı modda cache'teki son manifest ile oynatmaya devam eder.
/// </summary>
public class PlaybackService : IDisposable
{
    private readonly Grid _container;
    private readonly CacheService _cacheService;
    private readonly PlayerSettings _settings;
    private readonly DispatcherTimer _imageTimer;
    private readonly DispatcherTimer _pollTimer;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private PlaylistManifest? _manifest;
    private int _currentIndex = -1;
    private MediaElement? _videoElement;
    private Image? _imageElement;

    public event Action<string?>? CurrentMediaChanged;
    public event Action<string>? ErrorOccurred;

    public PlaybackService(Grid container, CacheService cacheService, PlayerSettings settings)
    {
        _container = container;
        _cacheService = cacheService;
        _settings = settings;

        _imageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _imageTimer.Tick += (_, _) => Next();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _pollTimer.Tick += async (_, _) => await PollForChangesAsync();
    }

    /// <summary>
    /// Önce cache'teki son manifest ile oynatmaya başlar, arka planda sunucudan güncel yayını çeker.
    /// </summary>
    public async Task StartAsync()
    {
        // Offline fallback — cache'teki son manifest
        var cached = PlaylistManifest.Load();
        if (cached != null)
        {
            _manifest = cached;
            StartPlayback();
        }

        // Sunucudan güncel yayın çek
        await RefreshFromServerAsync();
        _pollTimer.Start();
    }

    private async Task PollForChangesAsync()
    {
        await RefreshFromServerAsync();
    }

    public async Task RefreshFromServerAsync(bool forceReload = false)
    {
        await _refreshLock.WaitAsync();
        try
        {
            var fresh = await _cacheService.FetchPublicationAsync();
            if (fresh == null) return;

            var publicationChanged =
                forceReload ||
                _manifest == null ||
                fresh.AssignmentType != _manifest.AssignmentType ||
                fresh.PlaylistId != _manifest.PlaylistId ||
                fresh.MediaFileId != _manifest.MediaFileId ||
                fresh.PlaylistVersion != _manifest.PlaylistVersion ||
                fresh.PublicationRevision != _manifest.PublicationRevision ||
                fresh.DisplayMode != _manifest.DisplayMode ||
                fresh.FileHash != _manifest.FileHash ||
                fresh.DisplayDuration != _manifest.DisplayDuration ||
                fresh.SoundEnabled != _manifest.SoundEnabled ||
                PlaylistItemsChanged(fresh, _manifest);

            if (!publicationChanged) return;

            var downloaded = await _cacheService.DownloadContentAsync(fresh);
            if (!downloaded)
            {
                var detail = string.IsNullOrWhiteSpace(_cacheService.LastError)
                    ? "Bilinmeyen indirme hatası."
                    : _cacheService.LastError;
                ErrorOccurred?.Invoke($"Yayın içeriği sunucudan indirilemedi: {detail}");
                return;
            }

            _manifest = fresh;
            _currentIndex = -1;
            StartPlayback();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool PlaylistItemsChanged(PlaylistManifest fresh, PlaylistManifest current)
    {
        if (fresh.PlaylistItems.Count != current.PlaylistItems.Count) return true;

        for (var index = 0; index < fresh.PlaylistItems.Count; index++)
        {
            var left = fresh.PlaylistItems[index];
            var right = current.PlaylistItems[index];
            if (left.MediaFileId != right.MediaFileId ||
                left.OrderNumber != right.OrderNumber ||
                left.DisplayDuration != right.DisplayDuration ||
                left.SoundEnabled != right.SoundEnabled ||
                !string.Equals(left.FileHash, right.FileHash, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void StartPlayback()
    {
        if (_manifest == null) return;
        ClearContent();
        _currentIndex = -1;
        Next();
    }

    private void Next()
    {
        if (_manifest == null) return;

        _imageTimer.Stop();

        // Tekil içerik modu
        if (!_manifest.IsPlaylist || _manifest.PlaylistItems.Count == 0)
        {
            PlaySingleMedia();
            return;
        }

        // Playlist modu — sıradaki içeriğe geç
        for (var attempt = 0; attempt < _manifest.PlaylistItems.Count; attempt++)
        {
            _currentIndex = (_currentIndex + 1) % _manifest.PlaylistItems.Count;
            var item = _manifest.PlaylistItems[_currentIndex];
            if (!File.Exists(_cacheService.GetLocalPath(item.StoredFileName))) continue;
            PlayPlaylistItem(item);
            return;
        }
        ClearContent();
    }

    private void PlaySingleMedia()
    {
        if (_manifest == null || string.IsNullOrEmpty(_manifest.StoredFileName)) return;

        var localPath = _cacheService.GetLocalPath(_manifest.StoredFileName!);
        if (!File.Exists(localPath)) return;

        if (_manifest.MediaType == PlayerMediaType.Video)
            PlayVideo(localPath, _manifest.SoundEnabled);
        else
            PlayImage(localPath, _manifest.DisplayDuration);

        CurrentMediaChanged?.Invoke(_manifest.MediaName);
    }

    private void PlayPlaylistItem(ManifestItem item)
    {
        var localPath = _cacheService.GetLocalPath(item.StoredFileName);
        if (!File.Exists(localPath))
        {
            ClearContent();
            return;
        }

        if (item.MediaType == PlayerMediaType.Video)
            PlayVideo(localPath, item.SoundEnabled);
        else
            PlayImage(localPath, item.DisplayDuration);

        CurrentMediaChanged?.Invoke(Path.GetFileNameWithoutExtension(item.OriginalFileName));
    }

    private void PlayVideo(string path, bool soundEnabled)
    {
        ClearContent();

        _videoElement = new MediaElement
        {
            Source = new Uri(path),
            Stretch = GetContentStretch(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Close,
            Volume = soundEnabled ? 1 : 0,
            IsMuted = !soundEnabled
        };

        RenderOptions.SetBitmapScalingMode(_videoElement, BitmapScalingMode.HighQuality);

        _videoElement.MediaEnded += (_, _) =>
        {
            if (_manifest?.IsPlaylist == true && _manifest.PlaylistItems.Count > 0)
                Next();
            else
            {
                // Tekil video — döngü
                _videoElement.Position = TimeSpan.Zero;
                _videoElement.Play();
            }
        };

        _videoElement.MediaFailed += (_, e) =>
        {
            ErrorOccurred?.Invoke($"Video oynatma hatası: {e.ErrorException.Message}");
        };

        _container.Children.Add(_videoElement);
        _videoElement.Play();
    }

    private void PlayImage(string path, int displayDuration)
    {
        ClearContent();

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        _imageElement = new Image
        {
            Source = bitmap,
            Stretch = GetContentStretch(),
            StretchDirection = StretchDirection.Both,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };

        RenderOptions.SetBitmapScalingMode(_imageElement, BitmapScalingMode.HighQuality);
        _container.Children.Add(_imageElement);

        if (displayDuration < 1) displayDuration = 10;
        _imageTimer.Interval = TimeSpan.FromSeconds(displayDuration);
        _imageTimer.Start();
    }
    private static Stretch GetContentStretch() => Stretch.Fill;
    public void Stop()
    {
        ClearContent();
    }

    private void ClearContent()
    {
        _imageTimer.Stop();
        _videoElement?.Close();
        _videoElement = null;
        _imageElement = null;
        _container.Children.Clear();
        CurrentMediaChanged?.Invoke(null);
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        Stop();
        _refreshLock.Dispose();
    }
}
