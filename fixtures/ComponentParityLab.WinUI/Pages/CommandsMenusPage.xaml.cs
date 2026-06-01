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
        if (scenarioName.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            SaveCommandButton.IsEnabled = false;
            RefreshCommandButton.IsEnabled = false;
            CommandStateText.Text = "Commands disabled";
            return;
        }

        if (scenarioName.Contains("open-popup", StringComparison.OrdinalIgnoreCase))
        {
            CommandStateText.Text = "Open menu targets visible";
            return;
        }

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
