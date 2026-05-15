using System.Drawing;
using System.IO;
using System.Windows;
using QuickerLite.Services;
using Forms = System.Windows.Forms;

namespace QuickerLite;

public partial class App : System.Windows.Application
{
    private ActionConfigService? configService;
    private MouseHookService? mouseHook;
    private WindowProcessService? processService;
    private MainWindow? panel;
    private DisabledAppsWindow? disabledAppsWindow;
    private Forms.NotifyIcon? notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        configService = new ActionConfigService();
        processService = new WindowProcessService();
        panel = new MainWindow(configService);

        CreateTrayIcon();

        mouseHook = new MouseHookService(Dispatcher);
        mouseHook.ShouldSuppressMiddleClick = ShouldSuppressMiddleClick;
        mouseHook.ShouldHandleMouseWheel = ShouldHandleMouseWheel;
        mouseHook.MiddleClicked += OnMiddleClicked;
        mouseHook.MouseWheelScrolled += OnMouseWheelScrolled;
        mouseHook.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        mouseHook?.Dispose();
        notifyIcon?.Dispose();
        base.OnExit(e);
    }

    private void OnMiddleClicked(object? sender, MiddleClickEventArgs e)
    {
        if (panel is null || processService is null)
        {
            return;
        }

        var processName = processService.GetProcessNameAt(e.X, e.Y);
        panel.ToggleAt(e.X, e.Y, processName);
    }

    private void OnMouseWheelScrolled(object? sender, MouseWheelEventArgs e)
    {
        if (panel is null || !panel.IsVisible)
        {
            return;
        }

        panel.HandleGlobalWheelPage(e.Delta);
    }

    private bool ShouldSuppressMiddleClick(int x, int y)
    {
        if (configService is null || processService is null)
        {
            return true;
        }

        var processName = processService.GetProcessNameAt(x, y);
        return !configService.IsAppDisabled(processName);
    }

    private bool ShouldHandleMouseWheel(int x, int y)
    {
        return panel is { IsVisible: true };
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("重新加载配置", null, (_, _) => panel?.Reload());
        menu.Items.Add("管理禁用列表", null, (_, _) => ShowDisabledAppsWindow());
        menu.Items.Add("显示面板", null, (_, _) =>
        {
            var cursor = Forms.Cursor.Position;
            panel?.ShowAt(cursor.X, cursor.Y, "");
        });
        menu.Items.Add("退出", null, (_, _) => Shutdown());

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico")),
            Text = "Quicker Lite",
            Visible = true,
            ContextMenuStrip = menu
        };

        notifyIcon.DoubleClick += (_, _) =>
        {
            var cursor = Forms.Cursor.Position;
            panel?.ShowAt(cursor.X, cursor.Y, "");
        };
    }

    private void ShowDisabledAppsWindow()
    {
        if (configService is null)
        {
            return;
        }

        if (disabledAppsWindow is { IsVisible: true })
        {
            disabledAppsWindow.Activate();
            return;
        }

        disabledAppsWindow = new DisabledAppsWindow(configService)
        {
            Owner = panel
        };
        disabledAppsWindow.Closed += (_, _) =>
        {
            panel?.Reload();
            disabledAppsWindow = null;
        };
        disabledAppsWindow.Show();
        disabledAppsWindow.Activate();
    }
}
