using System.Text.Json.Serialization;

namespace QuickerLite.Models;

public sealed class SoftwareItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("exeName")]
    public string ExeName { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("iconPath")]
    public string IconPath { get; set; } = "";
}
