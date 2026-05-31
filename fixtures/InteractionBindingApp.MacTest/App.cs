using Microsoft.UI.Xaml;

namespace InteractionBindingApp.MacTest;

public sealed class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
    }
}
