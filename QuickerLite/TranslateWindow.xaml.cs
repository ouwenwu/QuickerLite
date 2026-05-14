using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QuickerLite.Services;

namespace QuickerLite;

public partial class TranslateWindow : Window, INotifyPropertyChanged
{
    private readonly GoogleTranslateService translateService;
    private readonly TranslateSettingsService settingsService = new();
    private CancellationTokenSource? translateCancellation;
    private TranslateLanguage selectedSourceLanguage = null!;
    private TranslateLanguage selectedTargetLanguage = null!;
    private string languageSummary = "";

    public TranslateWindow(GoogleTranslateService translateService)
    {
        this.translateService = translateService;
        InitializeComponent();
        DataContext = this;
        InitializeLanguages();
        Loaded += (_, _) => InputTextBox.Focus();
        Closed += (_, _) => translateCancellation?.Cancel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TranslateLanguage> SourceLanguages { get; } = [];

    public ObservableCollection<TranslateLanguage> TargetLanguages { get; } = [];

    public TranslateLanguage SelectedSourceLanguage
    {
        get => selectedSourceLanguage;
        set
        {
            selectedSourceLanguage = value;
            OnPropertyChanged();
            UpdateLanguageSummary();
        }
    }

    public TranslateLanguage SelectedTargetLanguage
    {
        get => selectedTargetLanguage;
        set
        {
            selectedTargetLanguage = value;
            OnPropertyChanged();
            UpdateLanguageSummary();
            settingsService.SaveTargetLanguageCode(value.Code);
        }
    }

    public string LanguageSummary
    {
        get => languageSummary;
        private set
        {
            languageSummary = value;
            OnPropertyChanged();
        }
    }

    private void InitializeLanguages()
    {
        var auto = new TranslateLanguage("自动识别", "auto");
        var languages = new[]
        {
            new TranslateLanguage("中文", "zh-CN"),
            new TranslateLanguage("英文", "en"),
            new TranslateLanguage("日文", "ja"),
            new TranslateLanguage("韩文", "ko"),
            new TranslateLanguage("法文", "fr"),
            new TranslateLanguage("德文", "de"),
            new TranslateLanguage("西班牙文", "es"),
            new TranslateLanguage("俄文", "ru")
        };

        SourceLanguages.Add(auto);
        foreach (var language in languages)
        {
            SourceLanguages.Add(language);
            TargetLanguages.Add(language);
        }

        selectedSourceLanguage = auto;
        var savedTargetCode = settingsService.LoadTargetLanguageCode();
        selectedTargetLanguage = TargetLanguages.FirstOrDefault(
            language => string.Equals(language.Code, savedTargetCode, StringComparison.OrdinalIgnoreCase))
            ?? TargetLanguages.First(language => language.Code == "zh-CN");

        OnPropertyChanged(nameof(SelectedSourceLanguage));
        OnPropertyChanged(nameof(SelectedTargetLanguage));
        UpdateLanguageSummary();
    }

    private async void InputTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        e.Handled = true;
        await TranslateInputAsync();
    }

    private async Task TranslateInputAsync()
    {
        var text = InputTextBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ResultTextBlock.Text = "请输入要翻译的文本。";
            return;
        }

        translateCancellation?.Cancel();
        translateCancellation = new CancellationTokenSource();

        ResultTextBlock.Text = "正在翻译...";
        try
        {
            var result = await translateService.TranslateAsync(
                text,
                SelectedSourceLanguage.Code,
                SelectedTargetLanguage.Code,
                translateCancellation.Token);
            ResultTextBlock.Text = result;
            AutoSizeForText(text, result);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = "翻译失败：" + ex.Message;
        }
    }

    public async void SetInputAndTranslate(string text)
    {
        InputTextBox.Text = text;
        InputTextBox.CaretIndex = InputTextBox.Text.Length;
        await TranslateInputAsync();
    }

    private void AutoSizeForText(string input, string output)
    {
        var longestLine = input
            .Split('\n')
            .Concat(output.Split('\n'))
            .Select(line => line.TrimEnd('\r').Length)
            .DefaultIfEmpty(0)
            .Max();

        var desiredWidth = Math.Clamp(380 + longestLine * 7, 560, 900);
        var desiredHeight = Math.Clamp(360 + (input.Length + output.Length) / 6, 420, 760);

        Width = Math.Max(Width, desiredWidth);
        Height = Math.Max(Height, desiredHeight);

        ResultTextBlock.MaxWidth = Math.Max(260, Width - 48);
        Dispatcher.BeginInvoke(() => ResultTextBlock.InvalidateMeasure());
    }

    private void UpdateLanguageSummary()
    {
        if (selectedSourceLanguage is null || selectedTargetLanguage is null)
        {
            return;
        }

        LanguageSummary = $"{selectedSourceLanguage.Name} → {selectedTargetLanguage.Name}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
