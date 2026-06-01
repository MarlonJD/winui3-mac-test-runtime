using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class NavigationWorkbenchPage : Page
{
    public NavigationWorkbenchPage()
    {
        InitializeComponent();
        InnerNavigationView.SelectedItem = WorkbenchQueueNavigationItem;
        WorkbenchQueueList.Items.Add("Queue item one");
        WorkbenchQueueList.Items.Add("Queue item two");
        WorkbenchQueueList.SelectedIndex = 0;
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("navigation-workbench", StringComparison.OrdinalIgnoreCase))
        {
            InnerNavigationView.SelectedItem = WorkbenchOverviewNavigationItem;
            WorkbenchFrameTitle.Text = "Overview";
        }
    }

    private void OnInnerNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        WorkbenchFrameTitle.Text = args.SelectedItemContainer?.Tag?.ToString() == "overview" ? "Overview" : "Queue";
    }

    private void OnFooterActionClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchDetailStateText.Text = "Footer action ran";
    }
}
