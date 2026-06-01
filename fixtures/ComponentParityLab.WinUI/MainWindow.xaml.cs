using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RootNavigation.SelectedItem = BasicInputNavigationItem;
        LabFrame.Navigate(typeof(BasicInputPage));
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        NavigateToTag(args.SelectedItemContainer?.Tag?.ToString());
    }

    private void OnResetClicked(object sender, RoutedEventArgs args)
    {
        NavigateToTag((RootNavigation.SelectedItem as NavigationViewItem)?.Tag?.ToString());
    }

    private void NavigateToTag(string? tag)
    {
        _ = tag switch
        {
            "text" => LabFrame.Navigate(typeof(TextFormsPage)),
            "collections" => LabFrame.Navigate(typeof(CollectionsPage)),
            "dialogs" => LabFrame.Navigate(typeof(DialogsFlyoutsPage)),
            "commands" => LabFrame.Navigate(typeof(CommandsMenusPage)),
            "navigation" => LabFrame.Navigate(typeof(NavigationWorkbenchPage)),
            "status" => LabFrame.Navigate(typeof(StatusPickersPage)),
            "layout" => LabFrame.Navigate(typeof(LayoutMediaPage)),
            _ => LabFrame.Navigate(typeof(BasicInputPage))
        };
    }
}
