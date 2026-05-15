using System;
using System.Windows;
using System.Windows.Input;
using QuickerLite.Services;

namespace QuickerLite;

public partial class EverythingSearchWindow : Window
{
    private readonly EverythingSearchService searchService;

    public EverythingSearchWindow(EverythingSearchService searchService)
    {
        this.searchService = searchService;
        InitializeComponent();
        Loaded += (_, _) =>
        {
            QueryTextBox.Focus();
            QueryTextBox.SelectAll();
        };
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SearchAndClose();
        }
    }

    private void SearchAndClose()
    {
        try
        {
            searchService.Search(QueryTextBox.Text.Trim());
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Everything搜索", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
