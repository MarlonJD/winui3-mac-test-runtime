using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
#if WINDOWS
using WinRT.Interop;
#endif

namespace SettingsFormApp.WinUI;

public sealed partial class MainWindow : Window
{
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    private readonly SettingsViewModel viewModel = new();
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
        SettingsPanel.RequestedTheme = ToElementTheme(launchOptions.Theme);
#endif
        SettingsPanel.DataContext = viewModel;
        ApplyScenarioState(launchOptions.ScenarioName);
    }

    public void ApplyLaunchBounds()
    {
#if WINDOWS
        ResizeClientArea(launchOptions.ViewportWidth, launchOptions.ViewportHeight);
#endif
    }

    private void OnSaveClicked(object sender, RoutedEventArgs args)
    {
        ApplySavedState();
    }

    private void ApplyScenarioState(string scenarioName)
    {
        if (scenarioName.Contains("saved", StringComparison.OrdinalIgnoreCase))
        {
            ApplySavedState();
        }
    }

    private void ApplySavedState()
    {
        SaveStatus.Title = "Saved";
        SaveStatus.Message = "Public settings were saved for this fixture run.";
        SaveStatus.Severity = InfoBarSeverity.Success;
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
            width + nonClientWidth,
            height + nonClientHeight,
            SwpNoZOrder | SwpNoActivate);
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr hwndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
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
