using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using QuickerLite.Models;

namespace QuickerLite.Services;

public sealed class ActionConfigService
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private ActionConfig? current;

    public string ConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, "actions.json");

    public ActionConfig Load()
    {
        EnsureConfigFile();

        var json = File.ReadAllText(ConfigPath);
        current = Normalize(JsonSerializer.Deserialize<ActionConfig>(json, options) ?? new ActionConfig());
        return current;
    }

    public void Save(ActionConfig config)
    {
        current = Normalize(config);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(current, options));
    }

    public bool IsAppDisabled(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var config = current ?? Load();
        return config.DisabledApps.Any(app => string.Equals(app, processName, StringComparison.OrdinalIgnoreCase));
    }

    public bool AddDisabledApp(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var config = current ?? Load();
        if (config.DisabledApps.Any(app => string.Equals(app, processName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        config.DisabledApps.Add(processName);
        config.DisabledApps.Sort(StringComparer.OrdinalIgnoreCase);
        Save(config);
        return true;
    }

    public IReadOnlyList<string> GetDisabledApps()
    {
        var config = current ?? Load();
        return config.DisabledApps
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void RemoveDisabledApps(IEnumerable<string> processNames)
    {
        var names = processNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (names.Count == 0)
        {
            return;
        }

        var config = current ?? Load();
        config.DisabledApps = config.DisabledApps
            .Where(app => !names.Contains(app))
            .ToList();
        Save(config);
    }

    public void ClearDisabledApps()
    {
        var config = current ?? Load();
        config.DisabledApps.Clear();
        Save(config);
    }

    private void EnsureConfigFile()
    {
        if (File.Exists(ConfigPath))
        {
            return;
        }

        var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "actions.json");
        if (!string.Equals(source, ConfigPath, StringComparison.OrdinalIgnoreCase) && File.Exists(source))
        {
            File.Copy(source, ConfigPath, overwrite: true);
            return;
        }

        var fallback = new ActionConfig
        {
            Global =
            [
                new() { Title = "记事本", Icon = "📝", Type = "process", Target = "notepad.exe" },
                new() { Title = "计算器", Icon = "🧮", Type = "process", Target = "calc.exe" }
            ]
        };

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(fallback, options));
    }

    private static ActionConfig Normalize(ActionConfig config)
    {
        config.Global ??= [];
        config.DisabledApps = config.DisabledApps
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase)
            .ToList();
        config.Apps = new Dictionary<string, List<ActionItem>>(config.Apps ?? [], StringComparer.OrdinalIgnoreCase);
        return config;
    }
}
