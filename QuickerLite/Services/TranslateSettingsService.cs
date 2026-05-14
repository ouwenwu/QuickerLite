using System;
using System.IO;
using System.Text.Json;

namespace QuickerLite.Services;

public sealed class TranslateSettingsService
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickerLite",
        "translate-settings.json");

    public string LoadTargetLanguageCode()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return "zh-CN";
            }

            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<TranslateSettings>(json, options);
            return string.IsNullOrWhiteSpace(settings?.TargetLanguageCode) ? "zh-CN" : settings.TargetLanguageCode;
        }
        catch
        {
            return "zh-CN";
        }
    }

    public void SaveTargetLanguageCode(string languageCode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var settings = new TranslateSettings
            {
                TargetLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "zh-CN" : languageCode
            };
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, options));
        }
        catch
        {
        }
    }

    private sealed class TranslateSettings
    {
        public string TargetLanguageCode { get; set; } = "zh-CN";
    }
}
