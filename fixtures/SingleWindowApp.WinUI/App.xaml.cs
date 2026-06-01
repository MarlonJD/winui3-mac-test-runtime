using Microsoft.UI.Xaml;

namespace SingleWindowApp.WinUI;

public sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeComponent();
        MainWindow = new MainWindow();
    }
}
