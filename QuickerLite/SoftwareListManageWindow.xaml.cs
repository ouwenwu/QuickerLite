using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using QuickerLite.Models;
using QuickerLite.Services;

namespace QuickerLite;

public partial class SoftwareListManageWindow : Window, INotifyPropertyChanged
{
    private readonly SoftwareListService softwareListService;
    private Visibility emptyVisibility = Visibility.Collapsed;

    public SoftwareListManageWindow(SoftwareListService softwareListService)
    {
        this.softwareListService = softwareListService;
        InitializeComponent();
        DataContext = this;
        Reload();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SoftwareItem> SoftwareItems { get; } = [];

    public Visibility EmptyVisibility
    {
        get => emptyVisibility;
        private set
        {
            emptyVisibility = value;
            OnPropertyChanged();
        }
    }

    public void Reload()
    {
        SoftwareItems.Clear();
        foreach (var item in softwareListService.Load())
        {
            SoftwareItems.Add(item);
        }

        EmptyVisibility = SoftwareItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择软件 exe",
            Filter = "应用程序 (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var result = softwareListService.AddFromExe(dialog.FileName);
        switch (result)
        {
            case AddSoftwareResult.Added:
                Reload();
                break;
            case AddSoftwareResult.Duplicate:
                System.Windows.MessageBox.Show(this, "这个软件已经在列表中了。", "添加软件", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            case AddSoftwareResult.NotExe:
                System.Windows.MessageBox.Show(this, "请选择 .exe 应用程序文件。", "添加软件", MessageBoxButton.OK, MessageBoxImage.Warning);
                break;
            default:
                System.Windows.MessageBox.Show(this, "软件路径不存在。", "添加软件", MessageBoxButton.OK, MessageBoxImage.Warning);
                break;
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: SoftwareItem item })
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            $"确定从常用软件中删除“{item.Name}”吗？\n\n不会删除真实软件文件。",
            "删除软件",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        softwareListService.Remove(item.Path);
        Reload();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
