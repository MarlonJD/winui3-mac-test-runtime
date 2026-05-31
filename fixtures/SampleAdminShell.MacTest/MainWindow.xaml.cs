using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SampleAdminShell.MacTest;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SignedOutFrame.Visibility = Visibility.Collapsed;
        RootNavigation.Visibility = Visibility.Visible;
        AdminNavigationItem.Visibility = Visibility.Visible;
        RootNavigation.SelectedItem = HomeNavigationItem;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ContentFrame.Content = args.SelectedItemContainer?.Tag?.ToString();
    }

    private void OnLogoutClicked(object sender, RoutedEventArgs args)
    {
        SignedOutFrame.Visibility = Visibility.Visible;
        RootNavigation.Visibility = Visibility.Collapsed;
    }
}
