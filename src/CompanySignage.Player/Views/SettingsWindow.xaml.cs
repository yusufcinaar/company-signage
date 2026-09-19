using System.Windows;
using CompanySignage.Player.Models;
using Microsoft.Win32;

namespace CompanySignage.Player.Views;

public partial class SettingsWindow : Window
{
    public PlayerSettings? Result { get; private set; }

    public SettingsWindow(PlayerSettings? existing = null)
    {
        InitializeComponent();

        if (existing != null)
        {
            ServerUrlBox.Text = existing.ServerUrl;
            ScreenCodeBox.Text = existing.ScreenCode;
            DeviceTokenBox.Text = existing.DeviceToken;
            SoundEnabledBox.IsChecked = existing.SoundEnabled;
            AutoStartBox.IsChecked = existing.AutoStart;
        }
        else
        {
            ServerUrlBox.Text = "http://localhost:5000";
            ScreenCodeBox.Text = "SCREEN-001";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var serverUrl = ServerUrlBox.Text.Trim();
        var screenCode = ScreenCodeBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(serverUrl) || !Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ErrorText.Text = "Geçerli bir sunucu adresi girin. Örn: http://192.0.2.50:5000";
            return;
        }

        if (string.IsNullOrWhiteSpace(screenCode))
        {
            ErrorText.Text = "Ekran kodu zorunludur. Örn: SCREEN-001";
            return;
        }

        if (string.IsNullOrWhiteSpace(DeviceTokenBox.Text))
        {
            ErrorText.Text = "Panelde belirlediğiniz cihaz tokenını girin.";
            return;
        }

        Result = new PlayerSettings
        {
            ServerUrl = serverUrl,
            ScreenCode = screenCode.ToUpperInvariant(),
            DeviceToken = DeviceTokenBox.Text.Trim(),
            SoundEnabled = SoundEnabledBox.IsChecked == true,
            AutoStart = AutoStartBox.IsChecked == true
        };

        Result.Save();
        ApplyAutoStart(Result.AutoStart);

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Windows başlangıcına ekleme/çıkarma (registry Run anahtarı).
    /// </summary>
    private static void ApplyAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key == null) return;

            const string appName = "CompanySignagePlayer";
            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(appName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(appName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Registry erişim hatası kritik değil
        }
    }
}
