using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuickerLite;

public sealed class DisabledAppEntry(string processName) : INotifyPropertyChanged
{
    private bool isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProcessName { get; } = processName;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value)
            {
                return;
            }

            isSelected = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
