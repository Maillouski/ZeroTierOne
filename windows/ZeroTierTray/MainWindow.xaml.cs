using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ZeroTierTray;

public partial class MainWindow : Window
{
    private ZeroTierClient? _client;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            _client ??= await ZeroTierClient.CreateAsync();
            var status = await _client.GetStatusAsync();
            var nets = await _client.ListNetworksAsync();

            StatusLine.Text = status == null
                ? "No status."
                : $"Node {status.Address}   v{status.Version}   {(status.Online ? "ONLINE" : "OFFLINE")}";

            NetList.ItemsSource = nets.Select(n => new NetworkRow
            {
                Id = n.Nwid ?? n.Id ?? "?",
                Name = n.Name ?? "",
                Status = n.Status ?? "",
                Ips = n.AssignedAddresses != null ? string.Join(", ", n.AssignedAddresses) : ""
            }).ToList();
        }
        catch (Exception ex)
        {
            StatusLine.Text = $"Error: {ex.Message}";
            NetList.ItemsSource = null;
        }
    }

    private async void JoinBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new JoinDialog { Owner = this };
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.NetworkId))
        {
            try
            {
                _client ??= await ZeroTierClient.CreateAsync();
                await _client.JoinAsync(dlg.NetworkId);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Join failed: {ex.Message}", "ZeroTier",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LeaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (NetList.SelectedItem is not NetworkRow row) return;
        if (MessageBox.Show(this, $"Leave network {row.Id}?", "ZeroTier",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            _client ??= await ZeroTierClient.CreateAsync();
            await _client.LeaveAsync(row.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Leave failed: {ex.Message}", "ZeroTier",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void CopyIdBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _client ??= await ZeroTierClient.CreateAsync();
            var status = await _client.GetStatusAsync();
            if (!string.IsNullOrEmpty(status?.Address))
            {
                Clipboard.SetText(status.Address);
                MessageBox.Show(this, $"Node ID copied to clipboard: {status.Address}",
                    "ZeroTier", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Cannot read node ID: {ex.Message}", "ZeroTier",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed class NetworkRow
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string Ips { get; set; } = "";
    }
}
