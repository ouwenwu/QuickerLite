using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace QuickerLite;

public partial class ScreenshotOverlayWindow : Window
{
    private readonly TaskCompletionSource<string?> completion = new();
    private readonly System.Drawing.Bitmap desktopBitmap;
    private readonly int virtualLeft;
    private readonly int virtualTop;
    private System.Windows.Point startPoint;
    private bool isSelecting;

    private ScreenshotOverlayWindow(System.Drawing.Bitmap desktopBitmap, int virtualLeft, int virtualTop)
    {
        this.desktopBitmap = desktopBitmap;
        this.virtualLeft = virtualLeft;
        this.virtualTop = virtualTop;

        InitializeComponent();
        Left = virtualLeft;
        Top = virtualTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        OverlayCanvas.Width = Width;
        OverlayCanvas.Height = Height;
    }

    public static Task<string?> CaptureRegionAsync()
    {
        var virtualLeft = (int)SystemParameters.VirtualScreenLeft;
        var virtualTop = (int)SystemParameters.VirtualScreenTop;
        var width = (int)SystemParameters.VirtualScreenWidth;
        var height = (int)SystemParameters.VirtualScreenHeight;

        var bitmap = new System.Drawing.Bitmap(width, height);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(virtualLeft, virtualTop, 0, 0, new System.Drawing.Size(width, height));
        }

        var window = new ScreenshotOverlayWindow(bitmap, virtualLeft, virtualTop);
        window.Show();
        window.Activate();
        return window.completion.Task;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(this);
        isSelecting = true;
        SelectionRectangle.Visibility = Visibility.Visible;
        CanvasSetSelection(startPoint, startPoint);
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!isSelecting)
        {
            return;
        }

        CanvasSetSelection(startPoint, e.GetPosition(this));
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isSelecting)
        {
            return;
        }

        isSelecting = false;
        ReleaseMouseCapture();

        var endPoint = e.GetPosition(this);
        var rect = NormalizeRect(startPoint, endPoint);
        if (rect.Width < 5 || rect.Height < 5)
        {
            Cancel();
            return;
        }

        var path = SaveCrop(rect);
        completion.TrySetResult(path);
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        desktopBitmap.Dispose();
        completion.TrySetResult(null);
        base.OnClosed(e);
    }

    private void Cancel()
    {
        completion.TrySetResult(null);
        Close();
    }

    private string SaveCrop(Rect rect)
    {
        var crop = new System.Drawing.Rectangle(
            Math.Max(0, (int)Math.Round(rect.X)),
            Math.Max(0, (int)Math.Round(rect.Y)),
            Math.Min(desktopBitmap.Width - (int)Math.Round(rect.X), (int)Math.Round(rect.Width)),
            Math.Min(desktopBitmap.Height - (int)Math.Round(rect.Y), (int)Math.Round(rect.Height)));

        using var cropped = desktopBitmap.Clone(crop, desktopBitmap.PixelFormat);
        var path = Path.Combine(Path.GetTempPath(), $"quicker-lite-ocr-{Guid.NewGuid():N}.png");
        cropped.Save(path, ImageFormat.Png);
        return path;
    }

    private void CanvasSetSelection(System.Windows.Point first, System.Windows.Point second)
    {
        var rect = NormalizeRect(first, second);
        System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, rect.X);
        System.Windows.Controls.Canvas.SetTop(SelectionRectangle, rect.Y);
        SelectionRectangle.Width = rect.Width;
        SelectionRectangle.Height = rect.Height;
    }

    private static Rect NormalizeRect(System.Windows.Point first, System.Windows.Point second)
    {
        var x = Math.Min(first.X, second.X);
        var y = Math.Min(first.Y, second.Y);
        var width = Math.Abs(first.X - second.X);
        var height = Math.Abs(first.Y - second.Y);
        return new Rect(x, y, width, height);
    }
}
