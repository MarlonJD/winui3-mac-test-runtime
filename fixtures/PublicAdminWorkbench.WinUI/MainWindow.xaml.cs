using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PublicAdminWorkbench.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RootNavigation.SelectedItem = QueueNavigationItem;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        WorkbenchTitleText.Text = args.SelectedItemContainer?.Tag?.ToString() switch
        {
            "overview" => "Overview",
            "reports" => "Reports",
            _ => "Review queue"
        };
    }

    private void OnApproveClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Approved";
        WorkbenchStatus.Message = "The selected public fixture request is approved.";
        WorkbenchStatus.Severity = InfoBarSeverity.Success;
    }

    private void OnDeferClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Deferred";
        WorkbenchStatus.Message = "The selected public fixture request is deferred.";
        WorkbenchStatus.Severity = InfoBarSeverity.Warning;
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Ready";
        WorkbenchStatus.Message = "No private data is used in this public fixture.";
        WorkbenchStatus.Severity = InfoBarSeverity.Informational;
    }
}
