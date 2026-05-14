using System;
using System.Windows;
using System.Windows.Input;

namespace QuickerLite;

public partial class ClipboardEditWindow : Window
{
    public ClipboardEditWindow()
    {
        InitializeComponent();
        LoadClipboardText();
        Loaded += (_, _) =>
        {
            ClipboardTextBox.Focus();
            ClipboardTextBox.SelectAll();
        };
    }

    private void LoadClipboardText()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                ClipboardTextBox.Text = System.Windows.Clipboard.GetText();
                EmptyHintTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                ClipboardTextBox.Text = "";
                EmptyHintTextBlock.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ClipboardTextBox.Text = "";
            EmptyHintTextBlock.Text = "读取剪贴板失败：" + ex.Message;
            EmptyHintTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            return;
        }

        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
            SaveAndClose();
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAndClose();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveAndClose()
    {
        try
        {
            System.Windows.Clipboard.SetText(ClipboardTextBox.Text ?? "");
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "写入剪贴板失败：" + ex.Message, "编辑剪贴板", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
