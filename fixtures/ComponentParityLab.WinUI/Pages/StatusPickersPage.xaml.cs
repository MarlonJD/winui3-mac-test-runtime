using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class StatusPickersPage : Page
{
    public StatusPickersPage()
    {
        InitializeComponent();
    }

    private void OnCompleteClicked(object sender, RoutedEventArgs args)
    {
        StatusInfoBar.Title = "Complete";
        StatusInfoBar.Message = "The public lab status completed.";
        StatusInfoBar.Severity = InfoBarSeverity.Success;
        CompletionProgressBar.Value = 100;
    }
}
