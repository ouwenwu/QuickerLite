using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace QuickerLite.Services;

public sealed class LanShareSettingsService
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickerLite",
        "lan-share-settings.json");

    public LanShareSettings Load()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<LanShareSettings>(json, options);
                return Normalize(settings ?? new LanShareSettings());
            }
        }
        catch
        {
        }

        return Normalize(new LanShareSettings());
    }

    public void Save(LanShareSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(Normalize(settings), options));
    }

    private static LanShareSettings Normalize(LanShareSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.IpAddress) && !IPAddress.TryParse(settings.IpAddress, out _))
        {
            settings.IpAddress = "";
        }

        if (settings.Port is < 1 or > 65535)
        {
            settings.Port = 8080;
        }

        return settings;
    }
}

public sealed class LanShareSettings
{
    public string IpAddress { get; set; } = "";

    public int Port { get; set; } = 8080;
}
