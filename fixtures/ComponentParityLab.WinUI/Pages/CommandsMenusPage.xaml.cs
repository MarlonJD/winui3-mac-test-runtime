using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class CommandsMenusPage : Page
{
    public CommandsMenusPage()
    {
        InitializeComponent();
        ReservePopupLayoutSpace();
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
            CommandStateText.Text = "Command state: Disabled";
            return;
        }

        if (scenarioName.Contains("open-popup", StringComparison.OrdinalIgnoreCase))
        {
            CommandStateText.Text = "Command state: Open menu targets visible";
#if !WINDOWS
            SetPopupOpenState(DiagnosticCommandBarFlyout, true);
            SetPopupOpenState(DiagnosticMenuFlyout, true);
            SetPopupOpenState(DiagnosticMenuBar, true);
            SetPopupOpenState(DiagnosticContextMenuPattern, true);
#endif
            return;
        }

        if (scenarioName.Contains("commands-menus", StringComparison.OrdinalIgnoreCase))
        {
            CommandStateText.Text = "Command state: Saved";
        }
    }

    private void ReservePopupLayoutSpace()
    {
        CommandsMenusDiagnostics.Children.Insert(2, new ContentControl { Height = 64 });
        CommandsMenusDiagnostics.Children.Insert(4, new ContentControl { Height = 82 });
    }

    private void OnSaveClicked(object sender, RoutedEventArgs args)
    {
        CommandStateText.Text = "Command state: Saved";
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        CommandStateText.Text = "Command state: Refreshed";
    }

    private void PopulateManagedPopupFixtures()
    {
#if !WINDOWS
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
#endif
    }

    private static void SetPopupOpenState(ContentControl host, bool isOpen)
    {
#if WINDOWS
        if (isOpen && host.Content is StackPanel panel)
        {
            foreach (var child in panel.Children.OfType<Button>())
            {
                child.Flyout?.ShowAt(child);
                child.ContextFlyout?.ShowAt(child);
            }

            foreach (var menuBar in panel.Children.OfType<MenuBar>())
            {
                if (menuBar.Items.Count > 0)
                {
                    menuBar.Items[0].GetType().GetProperty("IsSubMenuOpen")?.SetValue(menuBar.Items[0], true);
                }
            }
        }
#else
        if (host.Content is Button { Flyout: CommandBarFlyout commandBarFlyout })
        {
            commandBarFlyout.IsOpen = isOpen;
        }

        if (host.Content is Button { Flyout: MenuFlyout menuFlyout })
        {
            menuFlyout.IsOpen = isOpen;
        }
#endif
    }
}
