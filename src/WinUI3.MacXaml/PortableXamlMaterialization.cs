namespace WinUI3.MacXaml;

/// <summary>
/// Phase 2 portable XAML/source materialization contract.
/// </summary>
public static class PortableXamlMaterialization
{
    public const string Phase2Name = "Phase 2 portable XAML/source materialization MVP";

    public static IReadOnlyList<string> Phase2ElementNames { get; } =
    [
        "Window",
        "Page",
        "Grid",
        "StackPanel",
        "Border",
        "TextBlock",
        "Button",
        "TextBox",
        "CheckBox",
        "RadioButton",
        "Frame",
    ];

    public static IReadOnlyList<string> Phase2ResourceMarkupExtensions { get; } =
    [
        "StaticResource",
        "ThemeResource",
    ];

    public static IReadOnlyList<string> Phase2DiagnosticCodes { get; } =
    [
        "XAML1001",
        "XAML1002",
        "XAML1003",
        "XAML1004",
        "XAML1005",
        "XAML1006",
        "XAML1007",
        "XAML2001",
        "XAML2002",
        "XAML2003",
        "XAML2004",
    ];
}
