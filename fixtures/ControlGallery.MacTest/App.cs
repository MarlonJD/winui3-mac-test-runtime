using Microsoft.UI.Xaml;

namespace ControlGallery.MacTest;

public sealed class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
    }
}
