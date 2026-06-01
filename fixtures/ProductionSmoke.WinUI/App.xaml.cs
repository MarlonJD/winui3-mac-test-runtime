using Microsoft.UI.Xaml;

namespace ProductionSmoke.WinUI;

public sealed partial class App : Application
{
    public App()
    {
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeComponent();
        var window = new MainWindow(NativeLaunchOptions.Parse(args.Arguments));
#if !WINDOWS
        MainWindow = window;
#endif
        window.Activate();
    }
}
