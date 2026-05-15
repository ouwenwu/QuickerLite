using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickerLite.Services;

namespace QuickerLite;

public partial class WindowTopMostPickerOverlay : Window
{
    private readonly TaskCompletionSource<WindowTopMostResult> completion = new();
    private readonly WindowTopMostService topMostService;

    private WindowTopMostPickerOverlay(WindowTopMostService topMostService)
    {
        this.topMostService = topMostService;
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        OverlayCanvas.Width = Width;
        OverlayCanvas.Height = Height;
    }

    public static Task<WindowTopMostResult> PickAndToggleAsync(WindowTopMostService topMostService)
    {
        var window = new WindowTopMostPickerOverlay(topMostService);
        window.Show();
        window.Activate();
        return window.completion.Task;
    }

    private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = PointToScreen(e.GetPosition(this));
        Hide();

        await Task.Delay(80);
        var result = topMostService.ToggleAt((int)Math.Round(point.X), (int)Math.Round(point.Y));
        completion.TrySetResult(result);
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            completion.TrySetResult(new WindowTopMostResult(WindowTopMostResultKind.Canceled, ""));
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        completion.TrySetResult(new WindowTopMostResult(WindowTopMostResultKind.Canceled, ""));
        base.OnClosed(e);
    }
}
