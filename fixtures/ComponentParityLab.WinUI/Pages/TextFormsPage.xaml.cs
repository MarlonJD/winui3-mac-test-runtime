using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class TextFormsPage : Page
{
    public TextFormsPage()
    {
        InitializeComponent();
        NativeControlSamples.PopulateTextForms(
            DiagnosticRichTextBlock,
            DiagnosticRichEditBox,
            DiagnosticPasswordBox,
            DiagnosticNumberBox,
            DiagnosticAutoSuggestBox,
            DiagnosticAutoSuggestBoxQueryIcon,
            DiagnosticFormsPattern);
    }

    public void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            SearchTextBox.Text = string.Empty;
            TextFormsStateText.Text = "Validation error: query required";
            return;
        }

        if (scenarioName.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            SearchTextBox.IsEnabled = false;
            ApplyTextButton.IsEnabled = false;
            TextFormsStateText.Text = "Disabled state";
            return;
        }

        if (scenarioName.Contains("focused", StringComparison.OrdinalIgnoreCase))
        {
            SearchTextBox.Text = "Focused public query";
            TextFormsStateText.Text = "Focused public query";
            SearchTextBox.Focus(FocusState.Programmatic);
            return;
        }

        if (scenarioName.Contains("text-forms", StringComparison.OrdinalIgnoreCase))
        {
            SearchTextBox.Text = "Updated public query";
            TextFormsStateText.Text = "Updated public query";
        }
    }

    private void OnApplyTextClicked(object sender, RoutedEventArgs args)
    {
        TextFormsStateText.Text = SearchTextBox.Text ?? string.Empty;
    }
}
