using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickerLite.Services;

namespace QuickerLite;

public partial class WindowExePickerOverlay : Window
{
    private readonly TaskCompletionSource<string?> completion = new();
    private readonly WindowProcessService processService = new();

    private WindowExePickerOverlay()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        OverlayCanvas.Width = Width;
        OverlayCanvas.Height = Height;
    }

    public static Task<string?> PickExecutablePathAsync()
    {
        var window = new WindowExePickerOverlay();
        window.Show();
        window.Activate();
        return window.completion.Task;
    }

    private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = PointToScreen(e.GetPosition(this));
        Hide();

        await Task.Delay(80);
        var path = processService.GetProcessExecutablePathAt((int)Math.Round(point.X), (int)Math.Round(point.Y));
        completion.TrySetResult(string.IsNullOrWhiteSpace(path) ? null : path);
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            completion.TrySetResult(null);
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        completion.TrySetResult(null);
        base.OnClosed(e);
    }
}
