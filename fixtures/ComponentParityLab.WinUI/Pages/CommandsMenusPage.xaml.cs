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
#if !WINDOWS
        PopulateManagedPopupFixtures();
#endif
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
#if !WINDOWS
            SetPopupOpenState(DiagnosticCommandBarFlyout, true);
            SetPopupOpenState(DiagnosticMenuFlyout, true);
#endif
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

#if !WINDOWS
    private void PopulateManagedPopupFixtures()
    {
        DiagnosticCommandBarFlyout.Content = new Button
        {
            Content = "Open command flyout",
            Flyout = new CommandBarFlyout
            {
                PrimaryCommands =
                {
                    new AppBarButton { Label = "Pin" },
                    new AppBarButton { Label = "Archive" }
                }
            }
        };
        DiagnosticMenuFlyout.Content = new Button
        {
            Content = "Open menu flyout",
            Flyout = new MenuFlyout
            {
                Items =
                {
                    new MenuFlyoutItem { Text = "Approve" },
                    new MenuFlyoutItem { Text = "Defer" }
                }
            }
        };
        DiagnosticContextMenuPattern.Content = new Button
        {
            Content = "Right-click menu target",
            ContextFlyout = new MenuFlyout
            {
                Items =
                {
                    new MenuFlyoutItem { Text = "Context action" }
                }
            }
        };
    }

    private static void SetPopupOpenState(ContentControl host, bool isOpen)
    {
        if (host.Content is Button { Flyout: CommandBarFlyout commandBarFlyout })
        {
            commandBarFlyout.IsOpen = isOpen;
        }

        if (host.Content is Button { Flyout: MenuFlyout menuFlyout })
        {
            menuFlyout.IsOpen = isOpen;
        }
    }
#endif
}
