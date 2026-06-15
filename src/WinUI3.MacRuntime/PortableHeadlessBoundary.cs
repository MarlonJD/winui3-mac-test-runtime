namespace WinUI3.MacRuntime;

/// <summary>
/// Contract for the platform-independent portable headless core.
/// </summary>
public static class PortableHeadlessBoundary
{
    public static IReadOnlyList<string> CoreProjectRelativePaths { get; } =
    [
        Path.Combine("src", "WinUI3.MacCompat"),
        Path.Combine("src", "WinUI3.MacRuntime"),
        Path.Combine("src", "WinUI3.MacXaml"),
    ];

    public static IReadOnlyList<ForbiddenPlatformDependency> ForbiddenPlatformDependencies { get; } =
    [
        new("AppKit", "AppKit"),
        new("NSApplication", "NSApplication"),
        new("NSWindow", "NSWindow"),
        new("NSView", "NSView"),
        new("NSAccessibility", "NSAccessibility"),
        new("macOS AX", "AXUIElement"),
        new("Metal", "Metal"),
        new("CAMetalLayer", "CAMetalLayer"),
        new("FlaUI", "FlaUI"),
        new("Windows UI Automation", "System.Windows.Automation"),
        new("Windows UI Automation", "UIAutomationClient"),
        new("Windows UI Automation", "UIAutomationTypes"),
        new("Windows UI Automation", "IUIAutomation"),
        new("Win32 window handle", "HWND"),
        new("Win32 user32 import", "user32.dll"),
        new("P/Invoke", "DllImport"),
        new("Native library loading", "NativeLibrary"),
        new("macOS runtime branch", "OperatingSystem.IsMacOS"),
        new("Windows runtime branch", "OperatingSystem.IsWindows"),
    ];

    public static IReadOnlyList<string> AllowedFoundationDependencies { get; } =
    [
        ".NET runtime",
        "System.Text.Json",
        "System.Xml.Linq",
        "Microsoft.UI.Xaml facade types",
        "WinUI source/XAML materialization",
        "logical tree and diagnostics",
    ];
}

public sealed record ForbiddenPlatformDependency(string Name, string Token);
