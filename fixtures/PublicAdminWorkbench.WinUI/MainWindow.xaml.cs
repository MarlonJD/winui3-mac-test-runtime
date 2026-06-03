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
        PopulatePolicyEditor();
#if WINDOWS
        RootNavigation.RequestedTheme = ToElementTheme(launchOptions.Theme);
#endif
        SelectStartupRoute(launchOptions.StartupRoute);
        ApplyScenarioState(launchOptions.ScenarioName);
#if WINDOWS
        RootNavigation.Loaded += OnRootNavigationLoaded;
#endif
    }

    public void ApplyLaunchBounds()
    {
#if WINDOWS
        ResizeClientArea(launchOptions.ViewportWidth, launchOptions.ViewportHeight);
        ScheduleNativeReferenceTargetExport();
#endif
    }

#if WINDOWS
    private void OnRootNavigationLoaded(object sender, RoutedEventArgs args)
    {
        ScheduleNativeReferenceTargetExport();
    }

    private void ScheduleNativeReferenceTargetExport()
    {
        NativeReferenceTargetExporter.ExportIfRequested(RootNavigation, launchOptions);
    }
#endif

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ApplyNavigationTitle(args.SelectedItemContainer?.Tag?.ToString());
    }

    private void ApplyNavigationTitle(string? tag)
    {
        WorkbenchTitleText.Text = tag switch
        {
            "overview" => "Settings home",
            "reports" => "Audit",
            _ => "Policy editor"
        };
    }

    private void PopulatePolicyEditor()
    {
        PolicyScopeComboBox.Items.Clear();
        PolicyScopeComboBox.Items.Add("All policies");
        PolicyScopeComboBox.Items.Add("External access");
        PolicyScopeComboBox.Items.Add("Publishing");
        PolicyScopeComboBox.SelectedIndex = 1;

        PolicyOwnerComboBox.Items.Clear();
        PolicyOwnerComboBox.Items.Add("Public administrators");
        PolicyOwnerComboBox.Items.Add("Review managers");
        PolicyOwnerComboBox.Items.Add("Audit operators");
        PolicyOwnerComboBox.SelectedIndex = 0;

        AuditLoggingToggleSwitch.Content = new ToggleSwitch
        {
            Header = "Audit logging",
            IsOn = true
        };
        ReviewWindowSlider.Content = new Slider
        {
            Minimum = 1,
            Maximum = 30,
            Value = 14
        };
    }

    private void OnApproveClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Approved";
        WorkbenchStatus.Message = "The public policy fixture is validated and ready to publish.";
        WorkbenchStatus.Severity = InfoBarSeverity.Success;
        PolicyCompletenessProgress.Value = 100;
    }

    private void OnDeferClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Review required";
        WorkbenchStatus.Message = "Two policy settings require validation before publishing.";
        WorkbenchStatus.Severity = InfoBarSeverity.Warning;
        PolicyCompletenessProgress.Value = 72;
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs args)
    {
        WorkbenchStatus.Title = "Review required";
        WorkbenchStatus.Message = "Two policy settings require validation before publishing.";
        WorkbenchStatus.Severity = InfoBarSeverity.Warning;
        PolicyCompletenessProgress.Value = 72;
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
            WorkbenchStatus.Message = "The public policy fixture is validated and ready to publish.";
            WorkbenchStatus.Severity = InfoBarSeverity.Success;
            PolicyCompletenessProgress.Value = 100;
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
