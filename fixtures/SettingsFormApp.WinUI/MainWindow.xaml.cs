using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SettingsFormApp.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly SettingsViewModel viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        SettingsPanel.DataContext = viewModel;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs args)
    {
        SaveStatus.Title = "Saved";
        SaveStatus.Message = "Public settings were saved for this fixture run.";
        SaveStatus.Severity = InfoBarSeverity.Success;
    }
}
