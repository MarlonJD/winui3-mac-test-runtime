using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
#if WINDOWS
using WinRT.Interop;
#endif

namespace PublicAdminWorkbench.WinUI;

public sealed partial class MainWindow : Window
{
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    private readonly NativeLaunchOptions launchOptions;

    public MainWindow()
        : this(NativeLaunchOptions.Default)
    {
    }

    public MainWindow(NativeLaunchOptions launchOptions)
    {
        this.launchOptions = launchOptions;
        InitializeComponent();
        Title = launchOptions.WindowTitle;
#if WINDOWS
        RootNavigation.RequestedTheme = ToElementTheme(launchOptions.Theme);
#endif
        SelectStartupRoute(launchOptions.StartupRoute);
        ApplyScenarioState(launchOptions.ScenarioName);
    }

    public void ApplyLaunchBounds()
    {
#if WINDOWS
        ResizeClientArea(launchOptions.ViewportWidth, launchOptions.ViewportHeight);
#endif
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ApplyNavigationTitle(args.SelectedItemContainer?.Tag?.ToString());
    }

    private void ApplyNavigationTitle(string? tag)
    {
        WorkbenchTitleText.Text = tag switch
        {
            "overview" => "Overview",
            "reports" => "Reports",
            _ => "Review queue"
        };
    }

    private void OnApproveClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Approved";
        WorkbenchStatus.Message = "The selected public fixture request is approved.";
        WorkbenchStatus.Severity = InfoBarSeverity.Success;
    }

    private void OnDeferClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Deferred";
        WorkbenchStatus.Message = "The selected public fixture request is deferred.";
        WorkbenchStatus.Severity = InfoBarSeverity.Warning;
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Ready";
        WorkbenchStatus.Message = "No private data is used in this public fixture.";
        WorkbenchStatus.Severity = InfoBarSeverity.Informational;
    }

    private void SelectStartupRoute(string? route)
    {
        RootNavigation.SelectedItem = string.Equals(route, "overview", StringComparison.OrdinalIgnoreCase)
            ? OverviewNavigationItem
            : string.Equals(route, "reports", StringComparison.OrdinalIgnoreCase)
                ? ReportsNavigationItem
                : QueueNavigationItem;
        ApplyNavigationTitle((RootNavigation.SelectedItem as NavigationViewItem)?.Tag?.ToString());
    }

    private void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("public-admin-workbench", StringComparison.OrdinalIgnoreCase))
        {
            WorkbenchStatus.Title = "Approved";
            WorkbenchStatus.Message = "The selected public fixture request is approved.";
            WorkbenchStatus.Severity = InfoBarSeverity.Success;
        }
    }

#if WINDOWS
    private static ElementTheme ToElementTheme(string theme)
    {
        return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }

    private void ResizeClientArea(int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero ||
            !GetClientRect(hwnd, out var clientRect) ||
            !GetWindowRect(hwnd, out var windowRect))
        {
            return;
        }

        var nonClientWidth = Math.Max(0, windowRect.Width - clientRect.Width);
        var nonClientHeight = Math.Max(0, windowRect.Height - clientRect.Height);
        _ = SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0,
            0,
            Math.Max(1, width + nonClientWidth),
            Math.Max(1, height + nonClientHeight),
            SwpNoZOrder | SwpNoActivate);
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;

        public int Width => Right - Left;

        public int Height => Bottom - Top;
    }
#endif
}
