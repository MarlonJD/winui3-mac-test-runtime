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
        NativeControlSamples.PopulateCollections(
            DiagnosticDataTemplate,
            DiagnosticListViewItemTemplate,
            DiagnosticItemsControlItemTemplate,
            DiagnosticItemsView,
            DiagnosticGridView,
            DiagnosticFlipView,
            DiagnosticPipsPager,
            DiagnosticTreeView,
            DiagnosticItemsRepeater,
            DiagnosticSwipePattern,
            DiagnosticPullToRefreshPattern);
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("empty", StringComparison.OrdinalIgnoreCase))
        {
            CollectionListView.Items.Clear();
            SummaryItemsControl.Items.Clear();
            CollectionStateText.Text = "Empty collection";
            return;
        }

        if (scenarioName.Contains("collections", StringComparison.OrdinalIgnoreCase))
        {
            CollectionListView.SelectedIndex = 1;
            CollectionStateText.Text = "Confirm owner selected";
        }
    }
}
