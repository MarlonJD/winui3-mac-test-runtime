using Microsoft.UI.Xaml;

namespace PublicAdminWorkbench.WinUI;

public sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeComponent();
        var window = new MainWindow(NativeLaunchOptions.Parse(args.Arguments));
#if !WINDOWS
        MainWindow = window;
#endif
        window.Activate();
        window.ApplyLaunchBounds();
    }
}
