using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class BasicInputPage : Page
{
    public BasicInputPage()
    {
        InitializeComponent();
        StatusComboBox.Items.Add("Open");
        StatusComboBox.Items.Add("In review");
        StatusComboBox.Items.Add("Closed");
        StatusComboBox.SelectedIndex = 1;
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("basic-input", StringComparison.OrdinalIgnoreCase))
        {
            StatusComboBox.SelectedIndex = 2;
            BasicInputStateText.Text = "Primary action ran";
        }
    }

    private void OnPrimaryActionClicked(object sender, RoutedEventArgs args)
    {
        BasicInputStateText.Text = "Primary action ran";
    }

    private void OnPinnedClicked(object sender, RoutedEventArgs args)
    {
        BasicInputStateText.Text = PinnedToggleButton.IsChecked == true ? "Pinned" : "Unpinned";
    }
}
