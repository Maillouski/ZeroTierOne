using System.Text.RegularExpressions;
using System.Windows;

namespace ZeroTierTray;

public partial class JoinDialog : Window
{
    public string NetworkId { get; private set; } = "";

    public JoinDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NetworkIdBox.Focus();
    }

    private void JoinClicked(object sender, RoutedEventArgs e)
    {
        var id = (NetworkIdBox.Text ?? "").Trim();
        if (!Regex.IsMatch(id, "^[0-9a-fA-F]{16}$"))
        {
            MessageBox.Show(this, "Network ID must be exactly 16 hex characters.",
                "ZeroTier", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        NetworkId = id.ToLowerInvariant();
        DialogResult = true;
        Close();
    }
}
