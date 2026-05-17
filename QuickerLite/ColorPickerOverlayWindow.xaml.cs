using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace QuickerLite;

public partial class ColorPickerOverlayWindow : Window
{
    private const int SampleSize = 15;
    private const int MagnifierSize = 188;
    private readonly TaskCompletionSource<MediaColor?> completion = new();
    private readonly Bitmap desktopBitmap;
    private readonly int virtualLeft;
    private readonly int virtualTop;
    private MediaColor currentColor = Colors.White;

    private ColorPickerOverlayWindow(Bitmap desktopBitmap, int virtualLeft, int virtualTop)
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

    public static Task<MediaColor?> PickColorAsync()
    {
        var virtualLeft = (int)SystemParameters.VirtualScreenLeft;
        var virtualTop = (int)SystemParameters.VirtualScreenTop;
        var width = (int)SystemParameters.VirtualScreenWidth;
        var height = (int)SystemParameters.VirtualScreenHeight;

        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(virtualLeft, virtualTop, 0, 0, new System.Drawing.Size(width, height));
        }

        var window = new ColorPickerOverlayWindow(bitmap, virtualLeft, virtualTop);
        window.Show();
        window.Activate();
        return window.completion.Task;
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var screenPoint = PointToScreen(e.GetPosition(this));
        UpdateColorAt((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y), e.GetPosition(this));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var screenPoint = PointToScreen(e.GetPosition(this));
        UpdateColorAt((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y), e.GetPosition(this));
        completion.TrySetResult(currentColor);
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
        desktopBitmap.Dispose();
        completion.TrySetResult(null);
        base.OnClosed(e);
    }

    private void UpdateColorAt(int screenX, int screenY, System.Windows.Point localPoint)
    {
        var bitmapX = Clamp(screenX - virtualLeft, 0, desktopBitmap.Width - 1);
        var bitmapY = Clamp(screenY - virtualTop, 0, desktopBitmap.Height - 1);
        var pixel = desktopBitmap.GetPixel(bitmapX, bitmapY);
        currentColor = MediaColor.FromRgb(pixel.R, pixel.G, pixel.B);

        CurrentColorPreview.Background = new SolidColorBrush(currentColor);
        CurrentColorText.Text = $"{ToHex(currentColor)}  rgb({currentColor.R}, {currentColor.G}, {currentColor.B})";
        MagnifierImage.Source = CreateMagnifierSource(bitmapX, bitmapY);
        PositionMagnifier(localPoint);
    }

    private BitmapSource CreateMagnifierSource(int centerX, int centerY)
    {
        using var sample = new Bitmap(SampleSize, SampleSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var radius = SampleSize / 2;
        for (var y = 0; y < SampleSize; y++)
        {
            for (var x = 0; x < SampleSize; x++)
            {
                var sourceX = Clamp(centerX + x - radius, 0, desktopBitmap.Width - 1);
                var sourceY = Clamp(centerY + y - radius, 0, desktopBitmap.Height - 1);
                sample.SetPixel(x, y, desktopBitmap.GetPixel(sourceX, sourceY));
            }
        }

        using var stream = new MemoryStream();
        sample.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.StreamSource = stream;
        source.EndInit();
        source.Freeze();
        return source;
    }

    private void PositionMagnifier(System.Windows.Point point)
    {
        var left = point.X + 24;
        var top = point.Y + 24;

        if (left + MagnifierSize > ActualWidth)
        {
            left = point.X - MagnifierSize - 24;
        }

        if (top + 220 > ActualHeight)
        {
            top = point.Y - 220 - 24;
        }

        System.Windows.Controls.Canvas.SetLeft(MagnifierBorder, Math.Max(8, left));
        System.Windows.Controls.Canvas.SetTop(MagnifierBorder, Math.Max(8, top));
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(max, Math.Max(min, value));
    }

    public static string ToHex(MediaColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
