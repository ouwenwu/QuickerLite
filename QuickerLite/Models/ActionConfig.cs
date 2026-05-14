using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickerLite.Models;

public sealed class ActionConfig
{
    [JsonPropertyName("disabledApps")]
    public List<string> DisabledApps { get; set; } = [];

    [JsonPropertyName("global")]
    public List<ActionItem> Global { get; set; } = [];

    [JsonPropertyName("apps")]
    public Dictionary<string, List<ActionItem>> Apps { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
}
