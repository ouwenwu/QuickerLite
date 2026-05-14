using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using QuickerLite.Services;

namespace QuickerLite;

public partial class DisabledAppsWindow : Window, INotifyPropertyChanged
{
    private readonly ActionConfigService configService;
    private Visibility emptyVisibility = Visibility.Collapsed;
    private bool hasItems;
    private bool hasSelection;

    public DisabledAppsWindow(ActionConfigService configService)
    {
        this.configService = configService;
        InitializeComponent();
        DataContext = this;
        Reload();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DisabledAppEntry> DisabledApps { get; } = [];

    public Visibility EmptyVisibility
    {
        get => emptyVisibility;
        private set
        {
            emptyVisibility = value;
            OnPropertyChanged();
        }
    }

    public bool HasItems
    {
        get => hasItems;
        private set
        {
            hasItems = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelection
    {
        get => hasSelection;
        private set
        {
            hasSelection = value;
            OnPropertyChanged();
        }
    }

    private void Reload()
    {
        DisabledApps.Clear();
        foreach (var processName in configService.GetDisabledApps())
        {
            var entry = new DisabledAppEntry(processName);
            entry.PropertyChanged += Entry_PropertyChanged;
            DisabledApps.Add(entry);
        }

        RefreshState();
    }

    private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DisabledAppEntry.IsSelected))
        {
            RefreshState();
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var entry in DisabledApps)
        {
            entry.IsSelected = true;
        }

        RefreshState();
    }

    private void RestoreSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = DisabledApps
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.ProcessName)
            .ToList();

        configService.RemoveDisabledApps(selected);
        Reload();
    }

    private void RestoreAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            "恢复全部软件的中键弹窗？",
            "全部恢复",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        configService.ClearDisabledApps();
        Reload();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RefreshState()
    {
        HasItems = DisabledApps.Count > 0;
        HasSelection = DisabledApps.Any(entry => entry.IsSelected);
        EmptyVisibility = HasItems ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
