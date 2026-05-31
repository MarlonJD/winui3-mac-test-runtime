namespace WinUI3.MacRuntime;

public sealed record AccessibilityDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    AccessibilityNode Root);

public sealed record AccessibilityNode(
    string Role,
    string? Name,
    string? Label,
    IReadOnlyList<AccessibilityNode> Children);

public static class AccessibilityTreeBuilder
{
    public static AccessibilityDocument Build(UiTreeDocument tree)
    {
        return new AccessibilityDocument(
            "0.1",
            DateTimeOffset.UtcNow,
            BuildNode(tree.Root));
    }

    private static AccessibilityNode BuildNode(UiNode node)
    {
        var label = ReadString(node.Properties, "text") ??
            ReadString(node.Properties, "content") ??
            node.Name;
        return new AccessibilityNode(
            Role: MapRole(node.Type),
            Name: node.Name,
            Label: label,
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
}
