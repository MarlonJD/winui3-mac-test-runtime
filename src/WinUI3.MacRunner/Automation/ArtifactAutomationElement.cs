using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Automation;

/// <summary>
/// FlaUI/UIA3-shaped projection of a single macOS runtime element. State mirrors a
/// snapshot of a <c>FlaUI.Core.AutomationElement</c>: control type, identity,
/// enabled/focus/selection/toggle/expand state, value/text, and bounds when layout
/// data is present. Patterns are exposed as read-only state, not invokable methods.
/// </summary>
public sealed class ArtifactAutomationElement
{
    internal ArtifactAutomationElement(
        string? automationId,
        string? name,
        string role,
        string? frameworkType,
        ArtifactControlType controlType,
        bool? isEnabled,
        bool hasKeyboardFocus,
        bool? isKeyboardFocusable,
        bool? isSelected,
        bool? isChecked,
        bool? isExpanded,
        string? value,
        string? label,
        string? helpText,
        ArtifactBoundingRectangle? boundingRectangle,
        IReadOnlyList<ArtifactAutomationElement> children)
    {
        AutomationId = automationId;
        Name = name;
        Role = role;
        FrameworkType = frameworkType;
        ControlType = controlType;
        IsEnabled = isEnabled;
        HasKeyboardFocus = hasKeyboardFocus;
        IsKeyboardFocusable = isKeyboardFocusable;
        IsSelected = isSelected;
        IsChecked = isChecked;
        IsExpanded = isExpanded;
        Value = value;
        Label = label;
        HelpText = helpText;
        BoundingRectangle = boundingRectangle;
        Children = children;
    }

    /// <summary>UIA <c>AutomationIdProperty</c>.</summary>
    public string? AutomationId { get; }

    /// <summary>UIA <c>NameProperty</c>.</summary>
    public string? Name { get; }

    /// <summary>Runtime accessibility role (e.g. <c>navigation-item</c>, <c>button</c>).</summary>
    public string Role { get; }

    /// <summary>Original WinUI control type name, when correlated from the tree artifact.</summary>
    public string? FrameworkType { get; }

    /// <summary>UIA <c>ControlTypeProperty</c> equivalent.</summary>
    public ArtifactControlType ControlType { get; }

    /// <summary>UIA <c>IsEnabledProperty</c> (null when the artifact did not record it).</summary>
    public bool? IsEnabled { get; }

    /// <summary>UIA <c>HasKeyboardFocusProperty</c>.</summary>
    public bool HasKeyboardFocus { get; }

    /// <summary>UIA <c>IsKeyboardFocusableProperty</c>.</summary>
    public bool? IsKeyboardFocusable { get; }

    /// <summary>UIA <c>SelectionItemPattern.IsSelected</c>.</summary>
    public bool? IsSelected { get; }

    /// <summary>Boolean form of UIA <c>TogglePattern.ToggleState</c>.</summary>
    public bool? IsChecked { get; }

    /// <summary>Boolean form of UIA <c>ExpandCollapsePattern.ExpandCollapseState</c>.</summary>
    public bool? IsExpanded { get; }

    /// <summary>UIA <c>ValuePattern.Value</c> / text content.</summary>
    public string? Value { get; }

    /// <summary>Accessible label (automation name / header / content).</summary>
    public string? Label { get; }

    /// <summary>UIA <c>HelpTextProperty</c>.</summary>
    public string? HelpText { get; }

    /// <summary>UIA <c>BoundingRectangleProperty</c>, present only when layout was captured.</summary>
    public ArtifactBoundingRectangle? BoundingRectangle { get; }

    /// <summary>Child elements in document order.</summary>
    public IReadOnlyList<ArtifactAutomationElement> Children { get; }

    /// <summary>UIA <c>TogglePattern.ToggleState</c> when toggle state was recorded.</summary>
    public ArtifactToggleState? ToggleState => IsChecked switch
    {
        true => ArtifactToggleState.On,
        false => ArtifactToggleState.Off,
        _ => null
    };

    /// <summary>UIA <c>ExpandCollapsePattern.ExpandCollapseState</c> when recorded.</summary>
    public ArtifactExpandCollapseState? ExpandCollapseState => IsExpanded switch
    {
        true => ArtifactExpandCollapseState.Expanded,
        false => ArtifactExpandCollapseState.Collapsed,
        _ => null
    };
}

/// <summary>UIA-shaped bounding rectangle in runtime layout coordinates.</summary>
public sealed record ArtifactBoundingRectangle(double X, double Y, double Width, double Height);

/// <summary>UIA <c>ToggleState</c> equivalent.</summary>
public enum ArtifactToggleState
{
    Off,
    On,
    Indeterminate
}

/// <summary>UIA <c>ExpandCollapseState</c> equivalent.</summary>
public enum ArtifactExpandCollapseState
{
    Collapsed,
    Expanded,
    PartiallyExpanded,
    LeafNode
}

/// <summary>UIA <c>ControlType</c> equivalent surfaced by the artifact adapter.</summary>
public enum ArtifactControlType
{
    Unknown = 0,
    Custom,
    Button,
    CheckBox,
    RadioButton,
    ComboBox,
    Edit,
    Text,
    Image,
    Hyperlink,
    List,
    ListItem,
    Tree,
    TreeItem,
    Tab,
    TabItem,
    Menu,
    MenuBar,
    MenuItem,
    ToolBar,
    StatusBar,
    ProgressBar,
    Slider,
    ScrollBar,
    ToolTip,
    Window,
    Pane,
    Group,
    Document,
    Separator
}

/// <summary>
/// Maps runtime control identity (WinUI framework type and/or accessibility role)
/// to a UIA-shaped <see cref="ArtifactControlType"/>. The WinUI type is preferred
/// when available because it is more specific; the role is the fallback.
/// </summary>
internal static class ControlTypeMap
{
    public static ArtifactControlType Map(string? frameworkType, string role)
    {
        var fromType = FromFrameworkType(frameworkType);
        return fromType != ArtifactControlType.Unknown ? fromType : FromRole(role);
    }

    private static ArtifactControlType FromFrameworkType(string? frameworkType)
    {
        if (string.IsNullOrWhiteSpace(frameworkType))
        {
            return ArtifactControlType.Unknown;
        }

        return frameworkType switch
        {
            var t when Ends(t, ".Window") => ArtifactControlType.Window,
            var t when Ends(t, ".ContentDialog") => ArtifactControlType.Window,
            var t when Ends(t, ".Page") => ArtifactControlType.Pane,
            var t when Ends(t, ".Frame") => ArtifactControlType.Pane,
            var t when Ends(t, ".HyperlinkButton") => ArtifactControlType.Hyperlink,
            var t when Ends(t, ".AppBarButton") => ArtifactControlType.Button,
            var t when Ends(t, ".RepeatButton") => ArtifactControlType.Button,
            var t when Ends(t, ".ToggleButton") => ArtifactControlType.Button,
            var t when Ends(t, ".Button") => ArtifactControlType.Button,
            var t when Ends(t, ".CheckBox") => ArtifactControlType.CheckBox,
            var t when Ends(t, ".RadioButton") => ArtifactControlType.RadioButton,
            var t when Ends(t, ".ComboBox") => ArtifactControlType.ComboBox,
            var t when Ends(t, ".PasswordBox") => ArtifactControlType.Edit,
            var t when Ends(t, ".RichEditBox") => ArtifactControlType.Edit,
            var t when Ends(t, ".AutoSuggestBox") => ArtifactControlType.Edit,
            var t when Ends(t, ".TextBox") => ArtifactControlType.Edit,
            var t when Ends(t, ".TextBlock") => ArtifactControlType.Text,
            var t when Ends(t, ".NavigationViewItem") => ArtifactControlType.ListItem,
            var t when Ends(t, ".NavigationView") => ArtifactControlType.List,
            var t when Ends(t, ".ListViewItem") => ArtifactControlType.ListItem,
            var t when Ends(t, ".GridViewItem") => ArtifactControlType.ListItem,
            var t when Ends(t, ".ListBoxItem") => ArtifactControlType.ListItem,
            var t when Ends(t, ".ListView") => ArtifactControlType.List,
            var t when Ends(t, ".GridView") => ArtifactControlType.List,
            var t when Ends(t, ".ListBox") => ArtifactControlType.List,
            var t when Ends(t, ".ItemsControl") => ArtifactControlType.List,
            var t when Ends(t, ".ProgressBar") => ArtifactControlType.ProgressBar,
            var t when Ends(t, ".ProgressRing") => ArtifactControlType.ProgressBar,
            var t when Ends(t, ".Slider") => ArtifactControlType.Slider,
            var t when Ends(t, ".InfoBar") => ArtifactControlType.StatusBar,
            var t when Ends(t, ".MenuFlyoutItem") => ArtifactControlType.MenuItem,
            var t when Ends(t, ".MenuBarItem") => ArtifactControlType.MenuItem,
            var t when Ends(t, ".MenuBar") => ArtifactControlType.MenuBar,
            var t when Ends(t, ".MenuFlyout") => ArtifactControlType.Menu,
            var t when Ends(t, ".CommandBarFlyout") => ArtifactControlType.Menu,
            var t when Ends(t, ".CommandBar") => ArtifactControlType.ToolBar,
            var t when Ends(t, ".Flyout") => ArtifactControlType.Pane,
            var t when Ends(t, ".ToolTip") => ArtifactControlType.ToolTip,
            var t when Ends(t, ".TeachingTip") => ArtifactControlType.ToolTip,
            var t when Ends(t, ".Image") => ArtifactControlType.Image,
            var t when Ends(t, ".ScrollBar") => ArtifactControlType.ScrollBar,
            var t when Ends(t, ".AnnotatedScrollBar") => ArtifactControlType.ScrollBar,
            var t when Ends(t, ".Expander") => ArtifactControlType.Group,
            _ => ArtifactControlType.Unknown
        };
    }

    private static ArtifactControlType FromRole(string role)
    {
        return role switch
        {
            "window" => ArtifactControlType.Window,
            "dialog" => ArtifactControlType.Window,
            "button" => ArtifactControlType.Button,
            "toggle-button" => ArtifactControlType.Button,
            "checkbox" => ArtifactControlType.CheckBox,
            "radio" => ArtifactControlType.RadioButton,
            "combobox" => ArtifactControlType.ComboBox,
            "textbox" => ArtifactControlType.Edit,
            "passwordbox" => ArtifactControlType.Edit,
            "text" => ArtifactControlType.Text,
            "navigation" => ArtifactControlType.List,
            "navigation-item" => ArtifactControlType.ListItem,
            "list" => ArtifactControlType.List,
            "menuitem" => ArtifactControlType.MenuItem,
            "menubar" => ArtifactControlType.MenuBar,
            "toolbar" => ArtifactControlType.ToolBar,
            "status" => ArtifactControlType.StatusBar,
            "progress" => ArtifactControlType.ProgressBar,
            "scrollbar" => ArtifactControlType.ScrollBar,
            "tooltip" => ArtifactControlType.ToolTip,
            "popup" => ArtifactControlType.Menu,
            "frame" => ArtifactControlType.Pane,
            "group" => ArtifactControlType.Group,
            _ => ArtifactControlType.Custom
        };
    }

    private static bool Ends(string value, string suffix) =>
        value.EndsWith(suffix, StringComparison.Ordinal);
}

/// <summary>
/// FlaUI/UIA-shaped projection of a single recorded scenario action result from
/// <c>interactions.json</c>.
/// </summary>
public sealed class ArtifactActionResult
{
    internal ArtifactActionResult(InteractionStepResult step)
    {
        Index = step.Index;
        Type = step.Type;
        Status = step.Status;
        Target = step.Target;
        Selector = step.Selector;
        SelectorKind = step.SelectorKind;
        TargetType = step.TargetType;
        Expected = step.Expected;
        Actual = step.Actual;
        Message = step.Message;
        ObservedState = step.ObservedState;
    }

    public int Index { get; }

    public string Type { get; }

    public string Status { get; }

    public string? Target { get; }

    public string? Selector { get; }

    public string? SelectorKind { get; }

    public string? TargetType { get; }

    public string? Expected { get; }

    public string? Actual { get; }

    public string? Message { get; }

    public IReadOnlyDictionary<string, string?>? ObservedState { get; }
}
