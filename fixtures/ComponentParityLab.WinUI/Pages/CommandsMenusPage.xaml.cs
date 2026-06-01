using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class CommandsMenusPage : Page
{
    public CommandsMenusPage()
    {
        InitializeComponent();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs args)
    {
        CommandStateText.Text = "Saved";
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        CommandStateText.Text = "Refreshed";
    }
}
