using System.Threading;
using System.Windows;
using CompanySignage.Player.Models;
using CompanySignage.Player.Views;

namespace CompanySignage.Player;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, "CompanySignage.Player.SingleInstance", out var createdNew);
        _ownsSingleInstanceMutex = createdNew;
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        // İlk çalıştırmada ayar penceresini göster, sonra MainWindow'u başlat.
        var settings = PlayerSettings.Load();
        if (settings == null)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            if (settingsWindow.Result == null)
            {
                Shutdown();
                return;
            }
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
