using Microsoft.UI.Xaml;

namespace XamlTinyWinUIApp.MacTest;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public bool PrimaryButtonWasClicked { get; private set; }

    private void OnPrimaryClick(object sender, RoutedEventArgs args)
    {
        PrimaryButtonWasClicked = true;
    }
}
