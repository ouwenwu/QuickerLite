using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using QuickerLite.Models;

namespace QuickerLite.Services;

public sealed class SoftwareListService
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string appDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickerLite");

    private readonly string iconDirectory;

    public SoftwareListService()
    {
        iconDirectory = Path.Combine(appDataDirectory, "SoftwareIcons");
    }

    public string ListPath => Path.Combine(appDataDirectory, "software-list.json");

    private static string DefaultIconPath => Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

    public IReadOnlyList<SoftwareItem> Load()
    {
        EnsureStorage();
        if (!File.Exists(ListPath))
        {
            Save([]);
            return [];
        }

        var json = File.ReadAllText(ListPath);
        var items = JsonSerializer.Deserialize<List<SoftwareItem>>(json, options) ?? [];
        return Normalize(items);
    }

    public void Save(IEnumerable<SoftwareItem> items)
    {
        EnsureStorage();
        File.WriteAllText(ListPath, JsonSerializer.Serialize(Normalize(items), options));
    }

    public AddSoftwareResult AddFromExe(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return AddSoftwareResult.NotFound;
        }

        if (!string.Equals(Path.GetExtension(exePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return AddSoftwareResult.NotExe;
        }

        var items = Load().ToList();
        if (items.Any(item => string.Equals(item.Path, exePath, StringComparison.OrdinalIgnoreCase)))
        {
            return AddSoftwareResult.Duplicate;
        }

        items.Add(CreateSoftwareItem(exePath));
        Save(items);
        return AddSoftwareResult.Added;
    }

    public void Remove(string exePath)
    {
        var items = Load()
            .Where(item => !string.Equals(item.Path, exePath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save(items);
    }

    private SoftwareItem CreateSoftwareItem(string exePath)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
        var name = FirstNonEmpty(versionInfo.FileDescription, versionInfo.ProductName, Path.GetFileNameWithoutExtension(exePath));
        var iconPath = ExtractIcon(exePath);

        return new SoftwareItem
        {
            Name = name,
            ExeName = Path.GetFileName(exePath),
            Path = exePath,
            IconPath = iconPath
        };
    }

    private string ExtractIcon(string exePath)
    {
        try
        {
            EnsureStorage();
            using var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon is null)
            {
                return DefaultIconPath;
            }

            using var bitmap = icon.ToBitmap();
            var fileName = $"{Guid.NewGuid():N}.png";
            var iconPath = Path.Combine(iconDirectory, fileName);
            bitmap.Save(iconPath, ImageFormat.Png);
            return iconPath;
        }
        catch
        {
            return DefaultIconPath;
        }
    }

    private void EnsureStorage()
    {
        Directory.CreateDirectory(appDataDirectory);
        Directory.CreateDirectory(iconDirectory);
    }

    private static List<SoftwareItem> Normalize(IEnumerable<SoftwareItem> items)
    {
        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Path))
            .GroupBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(item =>
            {
                item.Name = FirstNonEmpty(item.Name, Path.GetFileNameWithoutExtension(item.Path));
                item.ExeName = FirstNonEmpty(item.ExeName, Path.GetFileName(item.Path));
                item.IconPath = FirstNonEmpty(item.IconPath, DefaultIconPath);
                return item;
            })
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "";
    }
}

public enum AddSoftwareResult
{
    Added,
    Duplicate,
    NotFound,
    NotExe
}
