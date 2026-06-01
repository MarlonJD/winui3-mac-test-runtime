using Microsoft.UI.Xaml;

namespace PublicAdminWorkbench.WinUI;

public sealed partial class App : Application
{
    public App()
    {
#if WINDOWS
        UnhandledException += OnUnhandledException;
#endif
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception error)
            {
                WriteLaunchFailure("AppDomain unhandled exception", error);
            }
            else
            {
                WriteLaunchMessage("AppDomain unhandled exception: " + args.ExceptionObject);
            }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            InitializeComponent();
            var window = new MainWindow(NativeLaunchOptions.Parse(args.Arguments));
#if !WINDOWS
            MainWindow = window;
#endif
            window.Activate();
            window.ApplyLaunchBounds();
        }
        catch (Exception error)
        {
            WriteLaunchFailure("OnLaunched", error);
            throw;
        }
    }

#if WINDOWS
    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        WriteLaunchFailure("WinUI unhandled exception", args.Exception);
    }
#endif

    private static void WriteLaunchFailure(string stage, Exception error)
    {
        WriteLaunchMessage(stage + Environment.NewLine + error);
    }

    private static void WriteLaunchMessage(string message)
    {
        var path = Environment.GetEnvironmentVariable("WINUI3_MAC_NATIVE_LAUNCH_LOG");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                path,
                DateTimeOffset.UtcNow.ToString("O") + Environment.NewLine + message + Environment.NewLine);
        }
        catch
        {
            // Launch diagnostics must never change fixture behavior.
        }
    }
}
