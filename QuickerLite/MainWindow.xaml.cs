using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickerLite.Models;
using QuickerLite.Services;
using Forms = System.Windows.Forms;

namespace QuickerLite;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int GlobalPageSize = 8;
    private const double SwipeThreshold = 45;
    private readonly ActionConfigService configService;
    private readonly ActionExecutor executor = new();
    private readonly GoogleTranslateService translateService = new();
    private readonly WindowsOcrService ocrService = new();
    private readonly ExplorerPathService explorerPathService = new();
    private readonly LanShareService lanShareService = new();
    private readonly LanShareSettingsService lanShareSettingsService = new();
    private ActionConfig config = new();
    private string currentProcessName = "";
    private string currentHeader = "当前";
    private Visibility emptyVisibility = Visibility.Collapsed;
    private bool canDisableCurrentApp;
    private int lastAnchorX;
    private int lastAnchorY;
    private System.Windows.Point? globalSwipeStart;
    private int globalPageIndex;
    private int globalPageCount = 1;
    private string globalPageIndicator = "";
    private Visibility globalPageIndicatorVisibility = Visibility.Collapsed;
    private TranslateWindow? translateWindow;
    private ClipboardEditWindow? clipboardEditWindow;

    public MainWindow(ActionConfigService configService)
    {
        this.configService = configService;
        InitializeComponent();
        DataContext = this;
        Reload();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ActionItem> GlobalActions { get; } = [];

    public ObservableCollection<ActionItem> VisibleGlobalActions { get; } = [];

    public ObservableCollection<ActionItem> CurrentActions { get; } = [];

    public string GlobalPageIndicator
    {
        get => globalPageIndicator;
        private set
        {
            globalPageIndicator = value;
            OnPropertyChanged();
        }
    }

    public Visibility GlobalPageIndicatorVisibility
    {
        get => globalPageIndicatorVisibility;
        private set
        {
            globalPageIndicatorVisibility = value;
            OnPropertyChanged();
        }
    }

    public string CurrentHeader
    {
        get => currentHeader;
        private set
        {
            currentHeader = value;
            OnPropertyChanged();
        }
    }

    public Visibility EmptyVisibility
    {
        get => emptyVisibility;
        private set
        {
            emptyVisibility = value;
            OnPropertyChanged();
        }
    }

    public bool CanDisableCurrentApp
    {
        get => canDisableCurrentApp;
        private set
        {
            canDisableCurrentApp = value;
            OnPropertyChanged();
        }
    }

    public void Reload()
    {
        config = configService.Load();
        GlobalActions.Clear();
        foreach (var action in config.Global)
        {
            GlobalActions.Add(action);
        }

        globalPageCount = Math.Max(1, (int)Math.Ceiling(GlobalActions.Count / (double)GlobalPageSize));
        globalPageIndex = Math.Clamp(globalPageIndex, 0, globalPageCount - 1);
        RefreshVisibleGlobalActions();
    }

    public void ToggleAt(int screenX, int screenY, string processName)
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        ShowAt(screenX, screenY, processName);
    }

    public void ShowAt(int screenX, int screenY, string processName)
    {
        lastAnchorX = screenX;
        lastAnchorY = screenY;
        LoadCurrentActions(processName);

        Left = screenX + 10;
        Top = screenY + 10;
        Show();
        UpdateLayout();
        KeepInsideScreen(screenX, screenY);
        Activate();
    }

    public void HandleGlobalWheelPage(int delta)
    {
        if (delta == 0 || globalPageCount <= 1)
        {
            return;
        }

        GoToGlobalPage(delta < 0 ? globalPageIndex + 1 : globalPageIndex - 1);
    }

    private void LoadCurrentActions(string processName)
    {
        CurrentActions.Clear();

        var key = string.IsNullOrWhiteSpace(processName) ? "" : processName;
        currentProcessName = key;
        CurrentHeader = string.IsNullOrWhiteSpace(key) ? "当前" : key;
        CanDisableCurrentApp = !string.IsNullOrWhiteSpace(key) && !configService.IsAppDisabled(key);

        if (!string.IsNullOrWhiteSpace(key) && config.Apps.TryGetValue(key, out var actions))
        {
            foreach (var action in actions)
            {
                CurrentActions.Add(action);
            }
        }

        EmptyVisibility = CurrentActions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void KeepInsideScreen(int anchorX, int anchorY)
    {
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(anchorX, anchorY)).WorkingArea;
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;

        if (Left + width > screen.Right)
        {
            Left = screen.Right - width;
        }

        if (Top + height > screen.Bottom)
        {
            Top = screen.Bottom - height;
        }

        if (Left < screen.Left)
        {
            Left = screen.Left;
        }

        if (Top < screen.Top)
        {
            Top = screen.Top;
        }
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: ActionItem action })
        {
            if (IsTranslateAction(action))
            {
                Hide();
                ShowTranslateWindow();
                return;
            }

            if (IsLanShareAction(action))
            {
                Hide();
                StartOrPromptLanShare();
                return;
            }

            if (IsClipboardEditAction(action))
            {
                Hide();
                ShowClipboardEditWindow();
                return;
            }

            Hide();
            try
            {
                executor.Execute(action);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, ex.Message, "动作执行失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void RefreshVisibleGlobalActions()
    {
        VisibleGlobalActions.Clear();
        foreach (var action in GlobalActions.Skip(globalPageIndex * GlobalPageSize).Take(GlobalPageSize))
        {
            VisibleGlobalActions.Add(action);
        }

        GlobalPageIndicator = $"{globalPageIndex + 1}/{globalPageCount}";
        GlobalPageIndicatorVisibility = globalPageCount > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GlobalArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        globalSwipeStart = e.GetPosition(this);
    }

    private void GlobalArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (globalSwipeStart is null)
        {
            return;
        }

        var start = globalSwipeStart.Value;
        globalSwipeStart = null;
        var end = e.GetPosition(this);
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;

        if (Math.Abs(deltaX) < SwipeThreshold || Math.Abs(deltaX) < Math.Abs(deltaY) * 1.5)
        {
            return;
        }

        if (deltaX < 0)
        {
            GoToGlobalPage(globalPageIndex + 1);
        }
        else
        {
            GoToGlobalPage(globalPageIndex - 1);
        }
    }

    private void GoToGlobalPage(int pageIndex)
    {
        var next = Math.Clamp(pageIndex, 0, globalPageCount - 1);
        if (next == globalPageIndex)
        {
            return;
        }

        globalPageIndex = next;
        RefreshVisibleGlobalActions();
    }

    private void ActionButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: ActionItem action } button)
        {
            return;
        }

        var menu = new ContextMenu();
        AddDeleteActionMenuItem(menu, action);

        if (IsTranslateAction(action))
        {
            menu.Items.Add(new Separator());
            AddTranslateMenuItems(menu);
        }
        else if (IsLanShareAction(action))
        {
            menu.Items.Add(new Separator());
            AddLanShareMenuItems(menu);
        }

        button.ContextMenu = menu;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void AddDeleteActionMenuItem(ContextMenu menu, ActionItem action)
    {
        var deleteItem = new MenuItem
        {
            Header = "删除当前动作",
            FontWeight = FontWeights.SemiBold
        };

        deleteItem.Click += (_, _) => DeleteAction(action);
        menu.Items.Add(deleteItem);
    }

    private void DeleteAction(ActionItem action)
    {
        var location = GetActionLocation(action);
        if (location is null)
        {
            System.Windows.MessageBox.Show(this, "未能定位要删除的动作，请重新加载配置后再试。", "删除动作", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var title = string.IsNullOrWhiteSpace(action.Title) ? "(未命名动作)" : action.Title;
        var result = System.Windows.MessageBox.Show(
            this,
            $"确定删除“{title}”吗？\n\n位置：{location.Value.DisplayName}\n该操作会直接修改 actions.json。",
            "删除当前动作",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        var removed = location.Value.IsGlobal
            ? configService.RemoveGlobalAction(location.Value.Index)
            : configService.RemoveAppAction(location.Value.ProcessName, location.Value.Index);

        if (!removed)
        {
            System.Windows.MessageBox.Show(this, "删除失败：动作可能已经被修改或删除。", "删除动作", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Reload();
        LoadCurrentActions(currentProcessName);
    }

    private ActionLocation? GetActionLocation(ActionItem action)
    {
        var globalIndex = config.Global.FindIndex(item => ReferenceEquals(item, action));
        if (globalIndex >= 0)
        {
            return new ActionLocation(true, "", globalIndex, "通用栏");
        }

        if (!string.IsNullOrWhiteSpace(currentProcessName)
            && config.Apps.TryGetValue(currentProcessName, out var currentActions))
        {
            var currentIndex = currentActions.FindIndex(item => ReferenceEquals(item, action));
            if (currentIndex >= 0)
            {
                return new ActionLocation(false, currentProcessName, currentIndex, $"{currentProcessName} 当前栏");
            }
        }

        return null;
    }

    private void AddTranslateMenuItems(ContextMenu menu)
    {
        var inputItem = new MenuItem { Header = "输入翻译" };
        inputItem.Click += (_, _) =>
        {
            Hide();
            ShowTranslateWindow();
        };

        var screenshotItem = new MenuItem { Header = "截图翻译" };
        screenshotItem.Click += async (_, _) =>
        {
            Hide();
            await StartScreenshotTranslateAsync();
        };

        menu.Items.Add(inputItem);
        menu.Items.Add(screenshotItem);
    }

    private void AddLanShareMenuItems(ContextMenu menu)
    {
        var startItem = new MenuItem { Header = "启动/重新共享当前文件夹" };
        startItem.Click += (_, _) =>
        {
            Hide();
            StartLanShare(restartIfRunning: true);
        };

        var stopItem = new MenuItem { Header = "停止共享", IsEnabled = lanShareService.IsRunning };
        stopItem.Click += (_, _) =>
        {
            lanShareService.Stop();
            System.Windows.MessageBox.Show(this, "局域网共享已停止。", "局域网共享", MessageBoxButton.OK, MessageBoxImage.Information);
        };

        var settingsItem = new MenuItem { Header = "编辑 IP 和端口" };
        settingsItem.Click += (_, _) =>
        {
            var window = new LanShareSettingsWindow(lanShareSettingsService)
            {
                Owner = this
            };
            window.ShowDialog();
        };

        menu.Items.Add(startItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(settingsItem);
    }

    private void StartOrPromptLanShare()
    {
        if (!lanShareService.IsRunning)
        {
            StartLanShare(restartIfRunning: false);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            $"当前正在共享：\n{lanShareService.RootDirectory}\n\n地址：{lanShareService.ShareUrl}\n\n选择“是”停止共享，选择“否”重新共享当前文件夹。",
            "局域网共享",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            lanShareService.Stop();
            System.Windows.MessageBox.Show(this, "局域网共享已停止。", "局域网共享", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (result == MessageBoxResult.No)
        {
            StartLanShare(restartIfRunning: true);
        }
    }

    private void StartLanShare(bool restartIfRunning)
    {
        try
        {
            var folder = explorerPathService.GetFolderPathAt(lastAnchorX, lastAnchorY);
            if (string.IsNullOrWhiteSpace(folder))
            {
                System.Windows.MessageBox.Show(this, "未能获取当前资源管理器文件夹。", "局域网共享", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = lanShareSettingsService.Load();
            var ipAddress = string.IsNullOrWhiteSpace(settings.IpAddress)
                ? LanShareService.GetDefaultLanIpAddress()
                : settings.IpAddress;
            var port = settings.Port > 0 ? settings.Port : 8080;
            var url = lanShareService.Start(folder, ipAddress, port, restartIfRunning);
            System.Windows.Clipboard.SetText(url);
            System.Windows.MessageBox.Show(
                this,
                $"局域网共享已启动：\n{folder}\n\n地址已复制到剪贴板：\n{url}",
                "局域网共享",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "局域网共享启动失败：" + ex.Message, "局域网共享", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowTranslateWindow()
    {
        if (translateWindow is { IsVisible: true })
        {
            translateWindow.Activate();
            return;
        }

        translateWindow = new TranslateWindow(translateService)
        {
            Owner = this
        };
        translateWindow.Closed += (_, _) => translateWindow = null;
        translateWindow.Show();
        translateWindow.Activate();
    }

    private async Task StartScreenshotTranslateAsync()
    {
        try
        {
            var imagePath = await ScreenshotOverlayWindow.CaptureRegionAsync();
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            var text = await ocrService.RecognizeAsync(imagePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                System.Windows.MessageBox.Show(this, "未识别到文字。", "截图翻译", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowTranslateWindow();
            translateWindow?.SetInputAndTranslate(text);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "截图翻译失败：" + ex.Message, "截图翻译", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowClipboardEditWindow()
    {
        if (clipboardEditWindow is { IsVisible: true })
        {
            clipboardEditWindow.Activate();
            return;
        }

        clipboardEditWindow = new ClipboardEditWindow
        {
            Owner = this
        };
        clipboardEditWindow.Closed += (_, _) => clipboardEditWindow = null;
        clipboardEditWindow.Show();
        clipboardEditWindow.Activate();
    }

    private static bool IsTranslateAction(ActionItem action)
    {
        return string.Equals(action.Type, "translate", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLanShareAction(ActionItem action)
    {
        return string.Equals(action.Type, "lanShare", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClipboardEditAction(ActionItem action)
    {
        return string.Equals(action.Type, "clipboardEdit", StringComparison.OrdinalIgnoreCase);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void DisableCurrentApp_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(currentProcessName))
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            $"以后在 {currentProcessName} 中单击中键将不显示 Quicker Lite 面板，并保留软件自己的中键功能。",
            "禁用当前软件",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        configService.AddDisabledApp(currentProcessName);
        Reload();
        Hide();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Hide();
    }

    private readonly record struct ActionLocation(bool IsGlobal, string ProcessName, int Index, string DisplayName);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
