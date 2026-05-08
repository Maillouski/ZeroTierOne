using System;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace ZeroTierTray;

public partial class App : Application
{
    private TaskbarIcon? _tray;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _tray = new TaskbarIcon
        {
            ToolTipText = "ZeroTier",
            ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayMenu"),
        };

        var iconUri = new Uri("pack://application:,,,/app.ico", UriKind.Absolute);
        try
        {
            _tray.IconSource = new System.Windows.Media.Imaging.BitmapImage(iconUri);
        }
        catch
        {
            // icon optional - tray still works
        }

        _tray.TrayLeftMouseDown += (_, _) => ShowMainWindow();
        _tray.TrayBalloonTipClicked += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_window == null || !_window.IsLoaded)
        {
            _window = new MainWindow();
            _window.Closed += (_, _) => _window = null;
        }
        _window.Show();
        _window.Activate();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
    }

    private void OnOpenClick(object sender, RoutedEventArgs e) => ShowMainWindow();

    private void OnJoinClick(object sender, RoutedEventArgs e)
    {
        var dlg = new JoinDialog();
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.NetworkId))
        {
            _ = JoinAsync(dlg.NetworkId);
        }
    }

    private async System.Threading.Tasks.Task JoinAsync(string id)
    {
        try
        {
            var client = await ZeroTierClient.CreateAsync();
            await client.JoinAsync(id);
            _tray?.ShowBalloonTip("ZeroTier", $"Joined {id}", BalloonIcon.Info);
            if (_window != null) await _window.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Join failed: {ex.Message}", "ZeroTier", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnWebUiClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://my.zerotier.com") { UseShellExecute = true });
    }

    private async void OnCopyNodeIdClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var client = await ZeroTierClient.CreateAsync();
            var status = await client.GetStatusAsync();
            if (!string.IsNullOrEmpty(status?.Address))
            {
                Clipboard.SetText(status.Address);
                _tray?.ShowBalloonTip("ZeroTier", $"Node ID copied: {status.Address}", BalloonIcon.Info);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Cannot read node ID: {ex.Message}", "ZeroTier", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (_window != null) await _window.RefreshAsync();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        _tray?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
