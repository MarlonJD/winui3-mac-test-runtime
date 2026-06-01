using System.ComponentModel;

namespace SettingsFormApp.WinUI;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string displayName = "Public Fixture User";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName
    {
        get => displayName;
        set
        {
            if (displayName == value)
            {
                return;
            }

            displayName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }
    }
}
