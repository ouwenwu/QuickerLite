using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace QuickerLite;

public partial class ColorPickerWindow : Window
{
    private MediaColor currentColor = Colors.White;

    public ColorPickerWindow()
    {
        InitializeComponent();
        BuildPalette();
        SetCurrentColor(Colors.White, copyHex: false);
    }

    private void BuildPalette()
    {
        var colors = new[]
        {
            Colors.Black,
            Colors.White,
            MediaColor.FromRgb(0x33, 0x33, 0x33),
            MediaColor.FromRgb(0x66, 0x66, 0x66),
            MediaColor.FromRgb(0x99, 0x99, 0x99),
            MediaColor.FromRgb(0xCC, 0xCC, 0xCC),
            MediaColor.FromRgb(0xF4, 0x43, 0x36),
            MediaColor.FromRgb(0xFF, 0x98, 0x00),
            MediaColor.FromRgb(0xFF, 0xEB, 0x3B),
            MediaColor.FromRgb(0x4C, 0xAF, 0x50),
            MediaColor.FromRgb(0x00, 0xBC, 0xD4),
            MediaColor.FromRgb(0x21, 0x96, 0xF3),
            MediaColor.FromRgb(0x3F, 0x51, 0xB5),
            MediaColor.FromRgb(0x9C, 0x27, 0xB0),
            MediaColor.FromRgb(0xE9, 0x1E, 0x63),
            MediaColor.FromRgb(0x79, 0x55, 0x48)
        };

        foreach (var color in colors)
        {
            var button = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("SwatchButtonStyle"),
                Background = new SolidColorBrush(color),
                Tag = color,
                ToolTip = ColorPickerOverlayWindow.ToHex(color)
            };
            button.Click += SwatchButton_Click;
            PalettePanel.Children.Add(button);
        }
    }

    private void SwatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MediaColor color })
        {
            SetCurrentColor(color, copyHex: true);
        }
    }

    private async void PickButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        var color = await ColorPickerOverlayWindow.PickColorAsync();
        Show();
        Activate();

        if (color is not null)
        {
            SetCurrentColor(color.Value, copyHex: true);
        }
    }

    private void CopyHexButton_Click(object sender, RoutedEventArgs e)
    {
        CopyText(HexTextBox.Text);
    }

    private void CopyRgbButton_Click(object sender, RoutedEventArgs e)
    {
        CopyText(RgbTextBox.Text);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void SetCurrentColor(MediaColor color, bool copyHex)
    {
        currentColor = color;
        var hex = ColorPickerOverlayWindow.ToHex(color);
        var rgb = $"rgb({color.R}, {color.G}, {color.B})";

        ColorPreview.Background = new SolidColorBrush(color);
        HexTextBox.Text = hex;
        RgbTextBox.Text = rgb;

        if (copyHex)
        {
            CopyText(hex);
        }
    }

    private void CopyText(string text)
    {
        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "复制失败：" + ex.Message, "屏幕取色", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
