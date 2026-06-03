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
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                SetPopupOpenState(DiagnosticCommandBarFlyout, true);
                SetPopupOpenState(DiagnosticMenuFlyout, true);
                SetPopupOpenState(DiagnosticMenuBar, true);
                SetPopupOpenState(DiagnosticContextMenuPattern, true);
            });
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
