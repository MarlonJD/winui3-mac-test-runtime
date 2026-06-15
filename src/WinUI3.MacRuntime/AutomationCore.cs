namespace WinUI3.MacRuntime;

public sealed record AutomationDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    AutomationNode Root);

public sealed record AutomationBounds(
    double X,
    double Y,
    double Width,
    double Height);

public enum AutomationControlType
{
    Window,
    Pane,
    Group,
    Text,
    Button,
    Edit,
    CheckBox,
    RadioButton,
    List,
    ListItem,
    ComboBox,
    Slider,
    ProgressBar
}

public enum AutomationPattern
{
    Invoke,
    Value,
    Toggle,
    SelectionItem,
    Scroll
}

public enum AutomationToggleState
{
    Off,
    On,
    Indeterminate
}

public sealed record AutomationNode(
    string RuntimeId,
    string? AutomationId,
    string? Name,
    AutomationControlType ControlType,
    AutomationBounds Bounds,
    bool IsEnabled,
    bool IsOffscreen,
    bool IsKeyboardFocusable,
    bool HasKeyboardFocus,
    IReadOnlyList<AutomationNode> Children,
    IReadOnlySet<AutomationPattern> Patterns,
    string? Value = null,
    AutomationToggleState? ToggleState = null,
    bool IsSelected = false,
    bool VerticallyScrollable = false,
    bool HorizontallyScrollable = false,
    double HorizontalScrollOffset = 0,
    double VerticalScrollOffset = 0)
{
    public AutomationNode? FindByAutomationId(string automationId)
    {
        if (string.Equals(AutomationId, automationId, StringComparison.Ordinal))
        {
            return this;
        }

        foreach (var child in Children)
        {
            var found = child.FindByAutomationId(automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}

public static class AutomationCore
{
    public static AutomationDocument Build(UiTreeDocument tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var viewport = BoundsFor(tree.Root);
        return new AutomationDocument(
            ArtifactSchemas.AutomationCore,
            DateTimeOffset.UnixEpoch,
            BuildNode(tree.Root, "root", viewport));
    }

    private static AutomationNode BuildNode(UiNode node, string runtimeId, AutomationBounds viewport)
    {
        var bounds = BoundsFor(node);
        var children = node.Children
            .Select((child, index) => BuildNode(child, $"{runtimeId}/{index}", viewport))
            .ToArray();
        var patterns = PatternsFor(node);
        return new AutomationNode(
            RuntimeId: runtimeId,
            AutomationId: ReadString(node, "automationId"),
            Name: AutomationName(node),
            ControlType: ControlTypeFor(node),
            Bounds: bounds,
            IsEnabled: ReadBool(node, "isEnabled", fallback: true),
            IsOffscreen: IsOffscreen(bounds, viewport, ReadString(node, "visibility")),
            IsKeyboardFocusable: ReadBool(node, "isFocusable", fallback: false),
            HasKeyboardFocus: ReadBool(node, "isFocused", fallback: false),
            Children: children,
            Patterns: patterns,
            Value: ValueFor(node),
            ToggleState: ToggleStateFor(node),
            IsSelected: ReadBool(node, "isSelected", fallback: false),
            VerticallyScrollable: IsScrollable(ReadString(node, "verticalScrollBarVisibility")),
            HorizontallyScrollable: IsScrollable(ReadString(node, "horizontalScrollBarVisibility")));
    }

    private static HashSet<AutomationPattern> PatternsFor(UiNode node)
    {
        var simpleType = SimpleType(node);
        var patterns = new HashSet<AutomationPattern>();
        if (simpleType is "Button" or "RepeatButton" or "HyperlinkButton" or "DropDownButton" or "SplitButton" or "AppBarButton" or "NavigationViewItem")
        {
            patterns.Add(AutomationPattern.Invoke);
        }

        if (simpleType is "TextBox" or "PasswordBox" or "AutoSuggestBox")
        {
            patterns.Add(AutomationPattern.Value);
        }

        if (simpleType is "ToggleButton" or "ToggleSplitButton" or "CheckBox" or "ToggleSwitch")
        {
            patterns.Add(AutomationPattern.Toggle);
        }

        if (simpleType is "RadioButton" or "NavigationViewItem")
        {
            patterns.Add(AutomationPattern.SelectionItem);
        }

        if (simpleType is "ScrollViewer" or "AnnotatedScrollBar")
        {
            patterns.Add(AutomationPattern.Scroll);
        }

        return patterns;
    }

    private static AutomationControlType ControlTypeFor(UiNode node)
    {
        return SimpleType(node) switch
        {
            "Window" => AutomationControlType.Window,
            "Page" or "Frame" or "ScrollViewer" or "ContentControl" or "ContentPresenter" => AutomationControlType.Pane,
            "TextBlock" or "String" => AutomationControlType.Text,
            "Button" or "RepeatButton" or "HyperlinkButton" or "DropDownButton" or "SplitButton" or "AppBarButton" => AutomationControlType.Button,
            "ToggleButton" or "ToggleSplitButton" => AutomationControlType.Button,
            "TextBox" or "PasswordBox" or "AutoSuggestBox" => AutomationControlType.Edit,
            "CheckBox" => AutomationControlType.CheckBox,
            "RadioButton" => AutomationControlType.RadioButton,
            "ListView" or "ItemsControl" => AutomationControlType.List,
            "NavigationViewItem" => AutomationControlType.ListItem,
            "ComboBox" => AutomationControlType.ComboBox,
            "Slider" => AutomationControlType.Slider,
            "ProgressBar" or "ProgressRing" => AutomationControlType.ProgressBar,
            _ => AutomationControlType.Group
        };
    }

    private static string? AutomationName(UiNode node)
    {
        return ReadString(node, "automationName") ??
            ReadString(node, "header") ??
            ReadString(node, "text") ??
            ReadString(node, "content") ??
            ReadString(node, "placeholderText") ??
            node.Name;
    }

    private static string? ValueFor(UiNode node)
    {
        if (ReadBool(node, "isPassword", fallback: false))
        {
            return "********";
        }

        return ReadString(node, "text") ??
            ReadString(node, "selectedItem") ??
            ReadString(node, "value");
    }

    private static AutomationToggleState? ToggleStateFor(UiNode node)
    {
        var simpleType = SimpleType(node);
        if (simpleType is not ("ToggleButton" or "ToggleSplitButton" or "CheckBox" or "RadioButton" or "ToggleSwitch"))
        {
            return null;
        }

        if (simpleType == "ToggleSwitch")
        {
            return ReadBool(node, "isOn", fallback: false) ? AutomationToggleState.On : AutomationToggleState.Off;
        }

        var value = ReadString(node, "isChecked");
        if (bool.TryParse(value, out var boolean))
        {
            return boolean ? AutomationToggleState.On : AutomationToggleState.Off;
        }

        return AutomationToggleState.Indeterminate;
    }

    private static bool IsScrollable(string? visibility)
    {
        return visibility is not null &&
            !string.Equals(visibility, "Disabled", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(visibility, "Hidden", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOffscreen(AutomationBounds bounds, AutomationBounds viewport, string? visibility)
    {
        if (string.Equals(visibility, "Collapsed", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return bounds.Width <= 0 ||
            bounds.Height <= 0 ||
            bounds.X + bounds.Width <= viewport.X ||
            bounds.Y + bounds.Height <= viewport.Y ||
            bounds.X >= viewport.X + viewport.Width ||
            bounds.Y >= viewport.Y + viewport.Height;
    }

    private static AutomationBounds BoundsFor(UiNode node)
    {
        var layout = node.Layout;
        if (layout is null)
        {
            return new AutomationBounds(0, 0, 0, 0);
        }

        return new AutomationBounds(layout.X, layout.Y, layout.Width, layout.Height);
    }

    private static string SimpleType(UiNode node)
    {
        var type = node.Type;
        var dot = type.LastIndexOf('.');
        return dot < 0 ? type : type[(dot + 1)..];
    }

    private static string? ReadString(UiNode node, string key)
    {
        return node.Properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool ReadBool(UiNode node, string key, bool fallback)
    {
        if (!node.Properties.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return bool.TryParse(value.ToString(), out var boolean) ? boolean : fallback;
    }
}
