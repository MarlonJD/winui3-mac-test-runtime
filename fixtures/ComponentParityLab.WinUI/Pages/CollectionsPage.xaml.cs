using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class CollectionsPage : Page
{
    public CollectionsPage()
    {
        InitializeComponent();
        SummaryItemsControl.Items.Add("Summary item one");
        SummaryItemsControl.Items.Add("Summary item two");
        CollectionListView.Items.Add("Review intake");
        CollectionListView.Items.Add("Confirm owner");
        CollectionListView.Items.Add("Publish summary");
        CollectionListView.SelectedIndex = 0;
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("collections", StringComparison.OrdinalIgnoreCase))
        {
            CollectionListView.SelectedIndex = 1;
        }
    }
}
