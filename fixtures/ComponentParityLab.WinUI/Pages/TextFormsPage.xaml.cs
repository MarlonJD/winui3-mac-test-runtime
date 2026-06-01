using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class TextFormsPage : Page
{
    public TextFormsPage()
    {
        InitializeComponent();
    }

    public void ApplyScenarioState(string scenarioName)
    {
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
