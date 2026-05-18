using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QuickerLite.Models;

public sealed class TaskItem : INotifyPropertyChanged
{
    private string date = "";
    private string priority = "P1";
    private string title = "";
    private string goal = "";
    private string status = "待办";
    private string notes = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("date")]
    public string Date
    {
        get => date;
        set => SetField(ref date, value);
    }

    [JsonPropertyName("priority")]
    public string Priority
    {
        get => priority;
        set => SetField(ref priority, value);
    }

    [JsonPropertyName("title")]
    public string Title
    {
        get => title;
        set => SetField(ref title, value);
    }

    [JsonPropertyName("goal")]
    public string Goal
    {
        get => goal;
        set => SetField(ref goal, value);
    }

    [JsonPropertyName("status")]
    public string Status
    {
        get => status;
        set => SetField(ref status, value);
    }

    [JsonPropertyName("notes")]
    public string Notes
    {
        get => notes;
        set => SetField(ref notes, value);
    }

    private void SetField(ref string field, string? value, [CallerMemberName] string? propertyName = null)
    {
        var next = value ?? "";
        if (field == next)
        {
            return;
        }

        field = next;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
