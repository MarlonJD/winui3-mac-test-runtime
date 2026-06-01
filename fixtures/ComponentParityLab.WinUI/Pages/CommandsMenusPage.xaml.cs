using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class CommandsMenusPage : Page
{
    public CommandsMenusPage()
    {
        InitializeComponent();
        NativeControlSamples.PopulateCommandsAndMenus(
            DiagnosticCommandBarContent,
            DiagnosticCommandBarFlyout,
            DiagnosticMenuFlyout,
            DiagnosticMenuBar,
            DiagnosticContextMenuPattern);
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("commands-menus", StringComparison.OrdinalIgnoreCase))
        {
            CommandStateText.Text = "Saved";
        }
    }

    private void OnSaveClicked(object sender, RoutedEventArgs args)
    {
        CommandStateText.Text = "Saved";
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        CommandStateText.Text = "Refreshed";
    }
}
