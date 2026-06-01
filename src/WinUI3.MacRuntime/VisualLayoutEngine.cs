using System.Globalization;
using Microsoft.UI.Xaml;
using WinUI3.MacCompat.Diagnostics;

namespace WinUI3.MacRuntime;

public sealed record UnsupportedVisualFeature(
    string Api,
    string Kind,
    string Status,
    string? FirstSeenIn,
    string Reason,
    string? NodeName);

public static class VisualLayoutEngine
{
    private static readonly UiThickness EmptyThickness = new(0, 0, 0, 0);
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.Ordinal)
    {
        "Window",
        "Page",
        "Grid",
        "StackPanel",
        "Border",
        "TextBlock",
        "Button",
        "TextBox",
        "Frame",
        "NavigationView",
        "NavigationViewItem",
        "ListView",
        "FontIcon",
        "Image",
        "String"
    };

    public static UiTreeDocument Arrange(
        UiTreeDocument tree,
        VisualRunSettings settings,
        out IReadOnlyList<UnsupportedVisualFeature> unsupportedFeatures)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(settings);

        var unsupported = new List<UnsupportedVisualFeature>();
        var root = ArrangeNode(
            tree.Root,
            new LayoutRect(0, 0, settings.Viewport.Width, settings.Viewport.Height),
            unsupported);

        unsupportedFeatures = unsupported.ToArray();
        foreach (var feature in unsupportedFeatures)
        {
            UnsupportedApiRegistry.Report(feature.Api, feature.Kind, feature.FirstSeenIn);
        }

        return tree with
        {
            SchemaVersion = ArtifactSchemas.VisualUiTree,
            GeneratedAt = DateTimeOffset.UnixEpoch,
            Root = root
        };
    }

    private static UiNode ArrangeNode(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported)
    {
        var simpleType = SimpleType(node);
        if (!SupportedTypes.Contains(simpleType))
        {
            unsupported.Add(new UnsupportedVisualFeature(
                Api: node.Type,
                Kind: "visual-renderer",
                Status: "unsupported",
                FirstSeenIn: "skia-v2",
                Reason: $"No skia-v2 painter exists for '{node.Type}'.",
                NodeName: node.Name));
        }

        var visibility = ReadString(node, "visibility") ?? Visibility.Visible.ToString();
        if (visibility == Visibility.Collapsed.ToString())
        {
            return WithLayout(node, rect with { Width = 0, Height = 0 }, EmptyThickness, EmptyThickness, visibility, Array.Empty<UiNode>());
        }

        return simpleType switch
        {
            "NavigationView" => ArrangeNavigationView(node, rect, unsupported),
            "StackPanel" => ArrangeStackPanel(node, rect, unsupported),
            "ListView" => ArrangeListView(node, rect, unsupported),
            "Border" => ArrangeSingleSlot(node, Inset(rect, 0), unsupported, PaddingFor(node), visibility),
            "Grid" or "Window" or "Page" or "Frame" => ArrangeOverlay(node, rect, unsupported, visibility),
            _ => WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, node.Children)
        };
    }

    private static UiNode ArrangeOverlay(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        string visibility)
    {
        var childRect = SimpleType(node) == "Window" && !ContainsType(node, "NavigationView")
            ? new LayoutRect(rect.X, rect.Y + 48, rect.Width, Math.Max(1, rect.Height - 48))
            : rect;
        var children = node.Children
            .Select(child => ArrangeNode(child, childRect, unsupported))
            .ToArray();
        return WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, children);
    }

    private static UiNode ArrangeSingleSlot(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        UiThickness padding,
        string visibility)
    {
        var contentRect = Inset(rect, padding);
        var children = node.Children
            .Select(child => ArrangeNode(child, contentRect, unsupported))
            .ToArray();
        return WithLayout(node, rect, EmptyThickness, padding, visibility, children);
    }

    private static UiNode ArrangeNavigationView(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported)
    {
        var paneWidth = Math.Clamp(ReadDouble(node, "openPaneLength", 248), 220, 320);
        const double titleBarHeight = 48;
        var menuTop = rect.Y + titleBarHeight + 58;
        var footerTop = rect.Y + rect.Height - 154;
        var contentRect = new LayoutRect(
            rect.X + paneWidth + 32,
            rect.Y + titleBarHeight + 36,
            Math.Max(1, rect.Width - paneWidth - 64),
            Math.Max(1, rect.Height - titleBarHeight - 68));

        var itemIndex = 0;
        var children = new List<UiNode>();
        foreach (var child in node.Children)
        {
            var childType = SimpleType(child);
            if (childType == "NavigationViewItem")
            {
                var itemRect = new LayoutRect(rect.X + 12, menuTop + itemIndex * 44, paneWidth - 24, 40);
                children.Add(ArrangeNode(child, itemRect, unsupported));
                itemIndex++;
                continue;
            }

            if (childType == "StackPanel")
            {
                children.Add(ArrangeNode(child, new LayoutRect(rect.X + 14, footerTop, paneWidth - 28, 130), unsupported));
                continue;
            }

            if (childType == "Frame")
            {
                children.Add(ArrangeNode(child, contentRect, unsupported));
                continue;
            }

            children.Add(ArrangeNode(child, contentRect, unsupported));
        }

        return WithLayout(node, rect, EmptyThickness, EmptyThickness, ReadString(node, "visibility") ?? "Visible", children);
    }

    private static UiNode ArrangeStackPanel(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported)
    {
        var padding = PaddingFor(node);
        var spacing = ReadDouble(node, "spacing", 0);
        var orientation = ReadString(node, "orientation") ?? "Vertical";
        var content = Inset(rect, padding);
        var visibleChildren = node.Children.ToArray();
        var arranged = new List<UiNode>(visibleChildren.Length);

        if (string.Equals(orientation, "Horizontal", StringComparison.Ordinal))
        {
            var x = content.X;
            var availableWidth = Math.Max(1, content.Width - Math.Max(0, visibleChildren.Length - 1) * spacing);
            var width = visibleChildren.Length == 0 ? availableWidth : availableWidth / visibleChildren.Length;
            foreach (var child in visibleChildren)
            {
                arranged.Add(ArrangeNode(child, new LayoutRect(x, content.Y, width, EstimateHeight(child, content.Height)), unsupported));
                x += width + spacing;
            }
        }
        else
        {
            var y = content.Y;
            foreach (var child in visibleChildren)
            {
                var height = Math.Min(content.Height, EstimateHeight(child, content.Height));
                arranged.Add(ArrangeNode(child, new LayoutRect(content.X, y, content.Width, height), unsupported));
                y += height + spacing;
            }
        }

        return WithLayout(node, rect, EmptyThickness, padding, ReadString(node, "visibility") ?? "Visible", arranged);
    }

    private static UiNode ArrangeListView(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported)
    {
        var y = rect.Y;
        var arranged = new List<UiNode>(node.Children.Count);
        foreach (var child in node.Children)
        {
            arranged.Add(ArrangeNode(child, new LayoutRect(rect.X + 12, y + 8, Math.Max(1, rect.Width - 24), 28), unsupported));
            y += 34;
        }

        return WithLayout(node, rect, EmptyThickness, EmptyThickness, ReadString(node, "visibility") ?? "Visible", arranged);
    }

    private static UiNode WithLayout(
        UiNode node,
        LayoutRect rect,
        UiThickness margin,
        UiThickness padding,
        string visibility,
        IReadOnlyList<UiNode> children)
    {
        var layout = new UiLayoutBox(
            X: Math.Round(rect.X, 3),
            Y: Math.Round(rect.Y, 3),
            Width: Math.Round(Math.Max(0, rect.Width), 3),
            Height: Math.Round(Math.Max(0, rect.Height), 3),
            DesiredWidth: Math.Round(Math.Max(0, rect.Width), 3),
            DesiredHeight: Math.Round(Math.Max(0, rect.Height), 3),
            Margin: margin,
            Padding: padding,
            HorizontalAlignment: ReadString(node, "horizontalAlignment") ?? "Stretch",
            VerticalAlignment: ReadString(node, "verticalAlignment") ?? "Stretch",
            Visibility: visibility);

        return node with { Children = children, Layout = layout };
    }

    private static double EstimateHeight(UiNode node, double fallback)
    {
        return SimpleType(node) switch
        {
            "TextBlock" or "String" => 28,
            "Button" => 40,
            "TextBox" => 36,
            "FontIcon" => 24,
            "Image" => 96,
            "Border" => Math.Min(86, fallback),
            "ListView" => Math.Max(64, 18 + ReadDouble(node, "itemCount", node.Children.Count) * 34),
            "Frame" => Math.Min(Math.Max(64, fallback), fallback),
            _ => Math.Min(Math.Max(40, fallback), fallback)
        };
    }

    private static LayoutRect Inset(LayoutRect rect, UiThickness thickness)
    {
        return new LayoutRect(
            rect.X + thickness.Left,
            rect.Y + thickness.Top,
            Math.Max(1, rect.Width - thickness.Left - thickness.Right),
            Math.Max(1, rect.Height - thickness.Top - thickness.Bottom));
    }

    private static LayoutRect Inset(LayoutRect rect, double amount)
    {
        return new LayoutRect(
            rect.X + amount,
            rect.Y + amount,
            Math.Max(1, rect.Width - amount * 2),
            Math.Max(1, rect.Height - amount * 2));
    }

    private static UiThickness PaddingFor(UiNode node)
    {
        return TryParseThickness(ReadString(node, "padding"), out var thickness) ? thickness : EmptyThickness;
    }

    private static bool TryParseThickness(string? value, out UiThickness thickness)
    {
        thickness = EmptyThickness;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN)
            .ToArray();

        if (parts.Any(double.IsNaN))
        {
            return false;
        }

        thickness = parts.Length switch
        {
            1 => new UiThickness(parts[0], parts[0], parts[0], parts[0]),
            2 => new UiThickness(parts[0], parts[1], parts[0], parts[1]),
            4 => new UiThickness(parts[0], parts[1], parts[2], parts[3]),
            _ => EmptyThickness
        };
        return parts.Length is 1 or 2 or 4;
    }

    private static string SimpleType(UiNode node)
    {
        var type = node.Type;
        var dot = type.LastIndexOf('.');
        return dot < 0 ? type : type[(dot + 1)..];
    }

    private static bool ContainsType(UiNode node, string simpleType)
    {
        return SimpleType(node) == simpleType || node.Children.Any(child => ContainsType(child, simpleType));
    }

    private static string? ReadString(UiNode node, string key)
    {
        return node.Properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static double ReadDouble(UiNode node, string key, double fallback)
    {
        if (!node.Properties.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : fallback;
    }

    private readonly record struct LayoutRect(double X, double Y, double Width, double Height);
}
