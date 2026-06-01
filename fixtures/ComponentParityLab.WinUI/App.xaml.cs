using Microsoft.UI.Xaml;

namespace ComponentParityLab.WinUI;

public sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeComponent();
        var window = new MainWindow(NativeLaunchOptions.Parse(args.Arguments));
        MainWindow = window;
        window.Activate();
        window.ApplyLaunchBounds();
    }
}
