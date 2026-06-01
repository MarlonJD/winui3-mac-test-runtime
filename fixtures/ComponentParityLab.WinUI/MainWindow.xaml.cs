using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
#if WINDOWS
using WinRT.Interop;
#endif

namespace ComponentParityLab.WinUI;

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
        NavigateToScenario(launchOptions.ScenarioName);
    }

    public void ApplyLaunchBounds()
    {
#if WINDOWS
        ResizeClientArea(launchOptions.ViewportWidth, launchOptions.ViewportHeight);
#endif
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        NavigateToTag(args.SelectedItemContainer?.Tag?.ToString());
    }

    private void OnResetClicked(object sender, RoutedEventArgs args)
    {
        NavigateToTag((RootNavigation.SelectedItem as NavigationViewItem)?.Tag?.ToString());
    }

    private void NavigateToTag(string? tag)
    {
        _ = tag switch
        {
            "text" => LabFrame.Navigate(typeof(TextFormsPage)),
            "collections" => LabFrame.Navigate(typeof(CollectionsPage)),
            "dialogs" => LabFrame.Navigate(typeof(DialogsFlyoutsPage)),
            "commands" => LabFrame.Navigate(typeof(CommandsMenusPage)),
            "navigation" => LabFrame.Navigate(typeof(NavigationWorkbenchPage)),
            "status" => LabFrame.Navigate(typeof(StatusPickersPage)),
            "layout" => LabFrame.Navigate(typeof(LayoutMediaPage)),
            _ => LabFrame.Navigate(typeof(BasicInputPage))
        };
    }

    private void NavigateToScenario(string scenarioName)
    {
        var item = NavigationItemForScenario(scenarioName);
        RootNavigation.SelectedItem = item;
        NavigateToTag(item.Tag?.ToString());
        ApplyScenarioState(scenarioName);
    }

    private NavigationViewItem NavigationItemForScenario(string scenarioName)
    {
        if (scenarioName.Contains("text-forms", StringComparison.OrdinalIgnoreCase))
        {
            return TextFormsNavigationItem;
        }

        if (scenarioName.Contains("collections", StringComparison.OrdinalIgnoreCase))
        {
            return CollectionsNavigationItem;
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.OrdinalIgnoreCase))
        {
            return DialogsNavigationItem;
        }

        if (scenarioName.Contains("commands-menus", StringComparison.OrdinalIgnoreCase))
        {
            return CommandsNavigationItem;
        }

        if (scenarioName.Contains("navigation-workbench", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationWorkbenchNavigationItem;
        }

        if (scenarioName.Contains("status-pickers", StringComparison.OrdinalIgnoreCase))
        {
            return StatusPickersNavigationItem;
        }

        return scenarioName.Contains("layout-media", StringComparison.OrdinalIgnoreCase)
            ? LayoutMediaNavigationItem
            : BasicInputNavigationItem;
    }

    private void ApplyScenarioState(string scenarioName)
    {
        switch (LabFrame.Content)
        {
            case BasicInputPage page:
                page.ApplyScenarioState(scenarioName);
                break;
            case TextFormsPage page:
                page.ApplyScenarioState(scenarioName);
                break;
            case CollectionsPage page:
                page.ApplyScenarioState(scenarioName);
                break;
            case CommandsMenusPage page:
                page.ApplyScenarioState(scenarioName);
                break;
            case NavigationWorkbenchPage page:
                page.ApplyScenarioState(scenarioName);
                break;
            case StatusPickersPage page:
                page.ApplyScenarioState(scenarioName);
                break;
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
