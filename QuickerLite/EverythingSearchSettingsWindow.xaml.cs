using System.IO;
using System.Windows;
using QuickerLite.Services;

namespace QuickerLite;

public partial class EverythingSearchSettingsWindow : Window
{
    private readonly EverythingSearchService searchService;

    public EverythingSearchSettingsWindow(EverythingSearchService searchService)
    {
        this.searchService = searchService;
        InitializeComponent();

        var settings = searchService.Load();
        PathTextBox.Text = string.IsNullOrWhiteSpace(settings.EverythingPath)
            ? searchService.ResolveEverythingPath()
            : settings.EverythingPath;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 Everything.exe",
            Filter = "Everything.exe|Everything.exe|应用程序 (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            PathTextBox.Text = dialog.FileName;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var path = PathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !string.Equals(Path.GetFileName(path), "Everything.exe", System.StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show(this, "请选择有效的 Everything.exe。", "Everything搜索设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        searchService.Save(new EverythingSearchSettings
        {
            EverythingPath = path
        });
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
