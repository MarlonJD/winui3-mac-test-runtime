using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SingleWindowApp.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnContinueClicked(object sender, RoutedEventArgs args)
    {
        HeadingText.Text = "Continued";
    }
}
