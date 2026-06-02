namespace WinUI3.MacRuntime;

public sealed record AccessibilityDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    AccessibilityNode Root);

public sealed record AccessibilityNode(
    string Role,
    string? Name,
    string? AutomationId,
    string? Label,
    string? HelpText,
    bool IsFocused,
    bool? IsFocusable,
    bool? IsEnabled,
    bool? IsChecked,
    bool? IsSelected,
    bool? IsExpanded,
    string? Value,
    IReadOnlyList<AccessibilityNode> Children);

public static class AccessibilityTreeBuilder
{
    public static AccessibilityDocument Build(UiTreeDocument tree)
    {
        return new AccessibilityDocument(
            ArtifactSchemas.Accessibility,
            DateTimeOffset.UtcNow,
            BuildNode(tree.Root));
    }

    private static AccessibilityNode BuildNode(UiNode node)
    {
        var label = ReadString(node.Properties, "automationName") ??
            ReadString(node.Properties, "text") ??
            ReadString(node.Properties, "content") ??
            node.Name;
        return new AccessibilityNode(
            Role: MapRole(node.Type),
            Name: node.Name,
            AutomationId: ReadString(node.Properties, "automationId"),
            Label: label,
            HelpText: ReadString(node.Properties, "automationHelpText"),
            IsFocused: ReadBool(node.Properties, "isFocused"),
            IsFocusable: ReadNullableBool(node.Properties, "isFocusable"),
            IsEnabled: ReadNullableBool(node.Properties, "isEnabled"),
            IsChecked: ReadNullableBool(node.Properties, "isChecked"),
            IsSelected: ReadString(node.Properties, "selectedItem") is not null || ReadString(node.Properties, "selectedIndex") is not null
                ? ReadString(node.Properties, "selectedIndex") != "-1"
                : null,
            IsExpanded: ReadNullableBool(node.Properties, "isOpen"),
            Value: ReadString(node.Properties, "text") ??
                ReadString(node.Properties, "selectedItem") ??
                ReadString(node.Properties, "value"),
            Children: node.Children.Select(BuildNode).ToArray());
    }

    private static string MapRole(string typeName)
    {
        if (typeName.EndsWith(".Window", StringComparison.Ordinal))
        {
            return "window";
        }

        if (typeName.EndsWith(".Button", StringComparison.Ordinal))
        {
            return "button";
        }

        if (typeName.EndsWith(".AppBarButton", StringComparison.Ordinal))
        {
            return "button";
        }

        if (typeName.EndsWith(".CheckBox", StringComparison.Ordinal))
        {
            return "checkbox";
        }

        if (typeName.EndsWith(".RadioButton", StringComparison.Ordinal))
        {
            return "radio";
        }

        if (typeName.EndsWith(".ToggleButton", StringComparison.Ordinal))
        {
            return "toggle-button";
        }

        if (typeName.EndsWith(".TextBlock", StringComparison.Ordinal))
        {
            return "text";
        }

        if (typeName.EndsWith(".TextBox", StringComparison.Ordinal))
        {
            return "textbox";
        }

        if (typeName.EndsWith(".NavigationView", StringComparison.Ordinal))
        {
            return "navigation";
        }

        if (typeName.EndsWith(".ComboBox", StringComparison.Ordinal))
        {
            return "combobox";
        }

        if (typeName.EndsWith(".ListView", StringComparison.Ordinal) ||
            typeName.EndsWith(".ItemsControl", StringComparison.Ordinal))
        {
            return "list";
        }

        if (typeName.EndsWith(".ProgressBar", StringComparison.Ordinal) ||
            typeName.EndsWith(".ProgressRing", StringComparison.Ordinal))
        {
            return "progress";
        }

        if (typeName.EndsWith(".InfoBar", StringComparison.Ordinal))
        {
            return "status";
        }

        if (typeName.EndsWith(".Flyout", StringComparison.Ordinal) ||
            typeName.EndsWith(".MenuFlyout", StringComparison.Ordinal) ||
            typeName.EndsWith(".CommandBarFlyout", StringComparison.Ordinal))
        {
            return "popup";
        }

        if (typeName.EndsWith(".ContentDialog", StringComparison.Ordinal))
        {
            return "dialog";
        }

        if (typeName.EndsWith(".ToolTip", StringComparison.Ordinal) ||
            typeName.EndsWith(".TeachingTip", StringComparison.Ordinal))
        {
            return "tooltip";
        }

        if (typeName.EndsWith(".MenuFlyoutItem", StringComparison.Ordinal))
        {
            return "menuitem";
        }

        if (typeName.EndsWith(".CommandBar", StringComparison.Ordinal))
        {
            return "toolbar";
        }

        if (typeName.EndsWith(".NavigationViewItem", StringComparison.Ordinal))
        {
            return "navigation-item";
        }

        if (typeName.EndsWith(".Frame", StringComparison.Ordinal))
        {
            return "frame";
        }

        return "group";
    }

    private static string? ReadString(IReadOnlyDictionary<string, object?> properties, string key)
    {
        return properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> properties, string key)
    {
        return properties.TryGetValue(key, out var value) && value is bool boolean && boolean;
    }

    private static bool? ReadNullableBool(IReadOnlyDictionary<string, object?> properties, string key)
    {
        if (!properties.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return bool.TryParse(value.ToString(), out var boolean) ? boolean : null;
    }
}
