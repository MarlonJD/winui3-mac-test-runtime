using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class TextFormsPage : Page
{
    public TextFormsPage()
    {
        InitializeComponent();
    }

    private void OnApplyTextClicked(object sender, RoutedEventArgs args)
    {
        TextFormsStateText.Text = SearchTextBox.Text ?? string.Empty;
    }
}
