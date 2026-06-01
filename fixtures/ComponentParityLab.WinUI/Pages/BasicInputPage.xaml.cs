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
        NativeControlSamples.PopulateBasicInput(
            DiagnosticRepeatButton,
            DiagnosticHyperlinkButton,
            DiagnosticDropDownButton,
            DiagnosticSplitButton,
            DiagnosticToggleSplitButton,
            DiagnosticSlider,
            DiagnosticToggleSwitch,
            DiagnosticRatingControl);
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            PrimaryActionButton.IsEnabled = false;
            PinnedToggleButton.IsEnabled = false;
            StatusComboBox.IsEnabled = false;
            BasicInputStateText.Text = "Disabled state";
            return;
        }

        if (scenarioName.Contains("checked", StringComparison.OrdinalIgnoreCase))
        {
            PinnedToggleButton.IsChecked = true;
            EnabledCheckBox.IsChecked = true;
            HighPriorityRadioButton.IsChecked = true;
            StatusComboBox.SelectedIndex = 1;
            BasicInputStateText.Text = "Checked state";
            return;
        }

        if (scenarioName.Contains("focused", StringComparison.OrdinalIgnoreCase))
        {
            StatusComboBox.Focus(FocusState.Programmatic);
            BasicInputStateText.Text = "Focused state";
            return;
        }

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
