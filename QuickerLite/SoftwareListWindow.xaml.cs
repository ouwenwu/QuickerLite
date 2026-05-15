using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using QuickerLite.Models;
using QuickerLite.Services;

namespace QuickerLite;

public partial class SoftwareListWindow : Window, INotifyPropertyChanged
{
    private readonly SoftwareListService softwareListService;
    private Visibility emptyVisibility = Visibility.Collapsed;

    public SoftwareListWindow(SoftwareListService softwareListService)
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

    private void SoftwareButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: SoftwareItem item })
        {
            return;
        }

        if (!File.Exists(item.Path))
        {
            System.Windows.MessageBox.Show(this, $"软件路径不存在：\n{item.Path}", "启动软件失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.Path,
                UseShellExecute = true
            });
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "启动软件失败：" + ex.Message, "常用软件", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
