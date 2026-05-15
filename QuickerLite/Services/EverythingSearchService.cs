using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace QuickerLite.Services;

public sealed class EverythingSearchService
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickerLite",
        "everything-search-settings.json");

    public EverythingSearchSettings Load()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                return Normalize(JsonSerializer.Deserialize<EverythingSearchSettings>(json, options) ?? new EverythingSearchSettings());
            }
        }
        catch
        {
        }

        return Normalize(new EverythingSearchSettings());
    }

    public void Save(EverythingSearchSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(Normalize(settings), options));
    }

    public string ResolveEverythingPath()
    {
        var configured = Load().EverythingPath;
        if (IsValidEverythingPath(configured))
        {
            return configured;
        }

        foreach (var candidate in GetDefaultCandidates())
        {
            if (IsValidEverythingPath(candidate))
            {
                return candidate;
            }
        }

        return "";
    }

    public void Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("请输入搜索关键词。");
        }

        var everythingPath = ResolveEverythingPath();
        if (string.IsNullOrWhiteSpace(everythingPath))
        {
            throw new FileNotFoundException("未找到 Everything.exe，请右键“Everything搜索”设置路径。");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = everythingPath,
            ArgumentList = { "-search", query },
            UseShellExecute = true
        });
    }

    private static EverythingSearchSettings Normalize(EverythingSearchSettings settings)
    {
        settings.EverythingPath = settings.EverythingPath?.Trim() ?? "";
        return settings;
    }

    private static bool IsValidEverythingPath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && File.Exists(path)
            && string.Equals(Path.GetFileName(path), "Everything.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] GetDefaultCandidates()
    {
        return
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", "Everything.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", "Everything.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything", "Everything.exe")
        ];
    }
}

public sealed class EverythingSearchSettings
{
    public string EverythingPath { get; set; } = "";
}
