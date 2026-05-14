using System.Net;
using System.Windows;
using QuickerLite.Services;

namespace QuickerLite;

public partial class LanShareSettingsWindow : Window
{
    private readonly LanShareSettingsService settingsService;

    public LanShareSettingsWindow(LanShareSettingsService settingsService)
    {
        this.settingsService = settingsService;
        InitializeComponent();

        var settings = settingsService.Load();
        IpTextBox.Text = string.IsNullOrWhiteSpace(settings.IpAddress)
            ? LanShareService.GetDefaultLanIpAddress()
            : settings.IpAddress;
        PortTextBox.Text = settings.Port.ToString();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var ip = IpTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(ip) && !IPAddress.TryParse(ip, out _))
        {
            System.Windows.MessageBox.Show(this, "IP 地址格式不正确。", "局域网共享设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(PortTextBox.Text.Trim(), out var port) || port is < 1 or > 65535)
        {
            System.Windows.MessageBox.Show(this, "端口必须是 1 到 65535 之间的数字。", "局域网共享设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        settingsService.Save(new LanShareSettings
        {
            IpAddress = ip,
            Port = port
        });
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
