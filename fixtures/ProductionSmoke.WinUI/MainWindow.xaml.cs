using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
#if WINDOWS
using WinRT.Interop;
#endif

namespace ProductionSmoke.WinUI;

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
        PopulateDeterministicItems();
        ConfigureManagedPopups();
        ApplyScenarioState(launchOptions.ScenarioName);
    }

    public void ApplyLaunchBounds()
    {
#if WINDOWS
        ResizeClientArea(launchOptions.ViewportWidth, launchOptions.ViewportHeight);
#endif
    }

    private void PopulateDeterministicItems()
    {
        PriorityComboBox.Items.Clear();
        PriorityComboBox.Items.Add("Low");
        PriorityComboBox.Items.Add("Normal");
        PriorityComboBox.Items.Add("High");
        PriorityComboBox.SelectedIndex = 1;

        OperationsList.Items.Clear();
        OperationsList.Items.Add("Validate public request");
        OperationsList.Items.Add("Review generated artifact");
        OperationsList.Items.Add("Publish sanitized result");
        OperationsList.SelectedIndex = 0;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        SmokeTitleText.Text = args.SelectedItemContainer?.Tag?.ToString() switch
        {
            "dashboard" => "Production smoke dashboard",
            "settings" => "Production smoke settings",
            _ => "Production smoke operations"
        };
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        SmokeStatus.Title = "Ready";
        SmokeStatus.Message = "Ready for deterministic public smoke run.";
        SmokeStatus.Severity = InfoBarSeverity.Informational;
    }

    private void OnSaveFormClicked(object sender, RoutedEventArgs args)
    {
        SmokeStatus.Title = "Saved";
        SmokeStatus.Message = "Public smoke form state saved.";
        SmokeStatus.Severity = InfoBarSeverity.Success;
        DetailStateText.Text = "State: Saved";
    }

    private void OnApproveClicked(object sender, RoutedEventArgs args)
    {
        SmokeStatus.Title = "Approved";
        SmokeStatus.Message = "The public smoke operation was approved.";
        SmokeStatus.Severity = InfoBarSeverity.Success;
        DetailStateText.Text = "State: Approved";
    }

    private void OnDeferClicked(object sender, RoutedEventArgs args)
    {
        SmokeStatus.Title = "Deferred";
        SmokeStatus.Message = "The public smoke operation was deferred.";
        SmokeStatus.Severity = InfoBarSeverity.Warning;
        DetailStateText.Text = "State: Deferred";
    }

    private void OnDecisionClicked(object sender, RoutedEventArgs args)
    {
        SmokeStatus.Title = "Decision";
        SmokeStatus.Message = "Decision dialog action was requested.";
        SmokeStatus.Severity = InfoBarSeverity.Informational;
    }

    private void ApplyScenarioState(string scenarioName)
    {
        RootNavigation.SelectedItem = OperationsNavigationItem;
        if (scenarioName.Contains("e2e", StringComparison.OrdinalIgnoreCase))
        {
            OperationsList.SelectedIndex = 1;
            SmokeStatus.Title = "Saved";
            SmokeStatus.Message = "Public smoke form state saved.";
            SmokeStatus.Severity = InfoBarSeverity.Success;
            DetailStateText.Text = "State: Saved";
        }
    }

    private void ConfigureManagedPopups()
    {
#if !WINDOWS
        DecisionDialogButton.Flyout = new ContentDialog
        {
            Title = "Public decision",
            Content = "Approve the public smoke operation?",
            PrimaryButtonText = "Approve"
        };
#endif
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
