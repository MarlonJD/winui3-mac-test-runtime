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
        "ScrollViewer",
        "ContentControl",
        "ItemsControl",
        "TextBlock",
        "Button",
        "RepeatButton",
        "HyperlinkButton",
        "DropDownButton",
        "SplitButton",
        "ToggleSplitButton",
        "AppBarButton",
        "ToggleButton",
        "CheckBox",
        "RadioButton",
        "TextBox",
        "Slider",
        "ToggleSwitch",
        "RatingControl",
        "SymbolIcon",
        "ComboBox",
        "Frame",
        "NavigationView",
        "NavigationViewItem",
        "ListView",
        "ProgressRing",
        "ProgressBar",
        "InfoBar",
        "CommandBar",
        "CommandBarFlyout",
        "Flyout",
        "MenuFlyout",
        "MenuFlyoutItem",
        "MenuBar",
        "MenuBarItem",
        "ContentDialog",
        "TeachingTip",
        "ToolTip",
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
            "ListView" or "ItemsControl" or "MenuFlyout" => ArrangeListView(node, rect, unsupported),
            "MenuBar" => ArrangeMenuBar(node, rect, unsupported, visibility),
            "CommandBar" => ArrangeCommandBar(node, rect, unsupported),
            "CommandBarFlyout" => ArrangeCommandBar(node, rect, unsupported),
            "AppBarButton" => ArrangeAppBarButton(node, rect, unsupported, visibility),
            "Button" or "RepeatButton" or "HyperlinkButton" or "DropDownButton" or "SplitButton" or "ToggleSplitButton" => ArrangeButton(node, rect, unsupported, visibility),
            "Flyout" or "ContentDialog" or "TeachingTip" or "ToolTip" => ArrangeSingleSlot(node, Inset(rect, 0), unsupported, PaddingFor(node), visibility),
            "Border" or "ScrollViewer" or "ContentControl" => ArrangeSingleSlot(node, Inset(rect, 0), unsupported, PaddingFor(node), visibility),
            "Grid" => ArrangeGrid(node, rect, unsupported, visibility),
            "Window" or "Page" or "Frame" => ArrangeOverlay(node, rect, unsupported, visibility),
            _ => WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, node.Children)
        };
    }

    private static UiNode ArrangeGrid(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        string visibility)
    {
        var columnSpacing = ReadDouble(node, "columnSpacing", 0);
        var maxColumn = node.Children
            .Select(child => Math.Max(0, (int)Math.Round(ReadDouble(child, "gridColumn", 0))))
            .DefaultIfEmpty(0)
            .Max();
        var columns = ResolveColumnWidths(node, rect.Width, columnSpacing, maxColumn + 1);
        var arranged = new List<UiNode>(node.Children.Count);
        foreach (var child in node.Children)
        {
            var column = Math.Clamp((int)Math.Round(ReadDouble(child, "gridColumn", 0)), 0, columns.Length - 1);
            var x = rect.X + columns.Take(column).Sum() + column * columnSpacing;
            arranged.Add(ArrangeNode(child, new LayoutRect(x, rect.Y, Math.Max(1, columns[column]), rect.Height), unsupported));
        }

        return WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, arranged);
    }

    private static UiNode ArrangeOverlay(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        string visibility)
    {
        var children = node.Children
            .Select(child => ArrangeNode(child, rect, unsupported))
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
        var menuTop = rect.Y + 48;
        var contentRect = new LayoutRect(
            rect.X + paneWidth,
            rect.Y,
            Math.Max(1, rect.Width - paneWidth),
            Math.Max(1, rect.Height));

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
                var footerHeight = Math.Clamp(EstimateHeight(child, 130), 48, 130);
                var footerTop = rect.Y + rect.Height - footerHeight - 12;
                children.Add(ArrangeNode(child, new LayoutRect(rect.X + 12, footerTop, paneWidth - 24, footerHeight), unsupported));
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
            foreach (var child in visibleChildren)
            {
                var width = Math.Min(availableWidth, EstimateWidth(child, availableWidth));
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
                var width = Math.Min(content.Width, EstimateWidth(child, content.Width));
                arranged.Add(ArrangeNode(child, new LayoutRect(content.X, y, width, height), unsupported));
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

    private static UiNode ArrangeMenuBar(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        string visibility)
    {
        var itemWidth = Math.Max(64, rect.Width / Math.Max(1, node.Children.Count));
        var x = rect.X + 4;
        var arranged = new List<UiNode>(node.Children.Count);
        foreach (var child in node.Children)
        {
            arranged.Add(ArrangeNode(child, new LayoutRect(x, rect.Y + 4, Math.Min(itemWidth, rect.Width - 8), 28), unsupported));
            x += itemWidth;
        }

        return WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, arranged);
    }

    private static UiNode ArrangeCommandBar(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported)
    {
        if (SimpleType(node) == "CommandBar")
        {
            const double commandWidth = 104;
            const double commandHeight = 32;
            const double commandSpacing = 64;
            const double overflowReserve = 40;
            var startX = rect.X + rect.Width - overflowReserve - node.Children.Count * commandSpacing;
            var commandY = rect.Y + Math.Max(0, (rect.Height - commandHeight) / 2);
            var commandChildren = new List<UiNode>(node.Children.Count);
            for (var index = 0; index < node.Children.Count; index++)
            {
                var childX = startX + index * commandSpacing;
                var arrangedChild = ArrangeNode(
                    node.Children[index],
                    new LayoutRect(childX, commandY, commandWidth, commandHeight),
                    unsupported);
                commandChildren.Add(WithProperty(arrangedChild, "commandBarCompact", true));
            }

            return WithLayout(node, rect, EmptyThickness, EmptyThickness, ReadString(node, "visibility") ?? "Visible", commandChildren);
        }

        var x = rect.X + 8;
        var arranged = new List<UiNode>(node.Children.Count);
        foreach (var child in node.Children)
        {
            arranged.Add(ArrangeNode(child, new LayoutRect(x, rect.Y + 6, 104, Math.Max(1, rect.Height - 12)), unsupported));
            x += 112;
        }

        return WithLayout(node, rect, EmptyThickness, EmptyThickness, ReadString(node, "visibility") ?? "Visible", arranged);
    }

    private static UiNode ArrangeButton(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        string visibility)
    {
        var children = new List<UiNode>(node.Children.Count);
        var popupY = rect.Y + rect.Height + 8;
        foreach (var child in node.Children)
        {
            var childType = SimpleType(child);
            var childRect = childType switch
            {
                "MenuFlyout" => new LayoutRect(rect.X, popupY, Math.Max(180, rect.Width), 96),
                "CommandBarFlyout" => new LayoutRect(rect.X, popupY, Math.Max(240, rect.Width), 48),
                "ContentDialog" => new LayoutRect(rect.X, popupY, Math.Max(280, rect.Width), 128),
                "Flyout" or "ToolTip" or "TeachingTip" => new LayoutRect(rect.X, popupY, Math.Max(220, rect.Width), 72),
                _ => rect
            };
            children.Add(ArrangeNode(child, childRect, unsupported));
            popupY += childRect.Height + 8;
        }

        return WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, children);
    }

    private static UiNode ArrangeAppBarButton(
        UiNode node,
        LayoutRect rect,
        ICollection<UnsupportedVisualFeature> unsupported,
        string visibility)
    {
        var children = node.Children
            .Select(child => ArrangeNode(child, new LayoutRect(rect.X + 10, rect.Y + 10, 16, 16), unsupported))
            .ToArray();
        return WithLayout(node, rect, EmptyThickness, EmptyThickness, visibility, children);
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

    private static UiNode WithProperty(UiNode node, string key, object? value)
    {
        var properties = new Dictionary<string, object?>(node.Properties, StringComparer.Ordinal)
        {
            [key] = value
        };
        return node with { Properties = properties };
    }

    private static double EstimateHeight(UiNode node, double fallback)
    {
        var natural = SimpleType(node) switch
        {
            "TextBlock" or "String" => 24,
            "Button" or "RepeatButton" or "HyperlinkButton" or "DropDownButton" or "SplitButton" or "ToggleSplitButton" or "AppBarButton" or "ToggleButton" or "CheckBox" or "RadioButton" or "ComboBox" => 32,
            "TextBox" => 32,
            "Slider" => 32,
            "ToggleSwitch" => 44,
            "RatingControl" => 32,
            "SymbolIcon" => 32,
            "FontIcon" => 24,
            "Image" => 96,
            "ProgressBar" => 28,
            "ProgressRing" => 32,
            "InfoBar" => 74,
            "CommandBar" => 48,
            "CommandBarFlyout" => 54,
            "MenuFlyout" => Math.Max(72, 18 + ReadDouble(node, "itemCount", node.Children.Count) * 34),
            "MenuBar" => 32,
            "MenuBarItem" => 28,
            "ContentDialog" => 128,
            "Flyout" or "ToolTip" or "TeachingTip" => 72,
            "Border" => Math.Min(86, fallback),
            "ListView" or "ItemsControl" => Math.Max(64, 18 + ReadDouble(node, "itemCount", node.Children.Count) * 34),
            "ScrollViewer" => Math.Min(120, Math.Max(64, fallback)),
            "ContentControl" => EstimateContentControlHeight(node, fallback),
            "StackPanel" => EstimateStackHeight(node, fallback),
            "Grid" => EstimateGridHeight(node, fallback),
            "Frame" => Math.Min(Math.Max(64, fallback), fallback),
            _ => Math.Min(Math.Max(40, fallback), fallback)
        };
        return ApplyHeightConstraints(node, fallback, natural);
    }

    private static double EstimateWidth(UiNode node, double fallback)
    {
        var natural = SimpleType(node) switch
        {
            "TextBlock" or "String" => Math.Min(fallback, Math.Max(24, EstimateTextWidth(ReadText(node) ?? node.Name ?? string.Empty))),
            "Button" or "RepeatButton" or "ToggleButton" => Math.Min(fallback, Math.Max(64, EstimateTextWidth(ReadControlText(node, "Button")) + 28)),
            "HyperlinkButton" => Math.Min(fallback, Math.Max(96, EstimateTextWidth(ReadControlText(node, "Link")) + 8)),
            "DropDownButton" or "SplitButton" => Math.Min(fallback, Math.Max(124, EstimateTextWidth(ReadControlText(node, "Action")) + 48)),
            "ToggleSplitButton" => Math.Min(fallback, Math.Max(120, EstimateTextWidth(ReadControlText(node, "Toggle")) + 42)),
            "AppBarButton" => Math.Min(fallback, Math.Max(96, EstimateTextWidth(ReadString(node, "label") ?? ReadControlText(node, "Command")) + 42)),
            "CheckBox" or "RadioButton" => Math.Min(fallback, Math.Max(96, EstimateTextWidth(ReadControlText(node, "Option")) + 34)),
            "ComboBox" => Math.Min(fallback, Math.Max(92, EstimateTextWidth(ReadString(node, "selectedItem") ?? ReadString(node, "placeholderText") ?? "Select") + 48)),
            "TextBox" => Math.Min(fallback, Math.Max(180, EstimateTextWidth(ReadText(node) ?? string.Empty) + 28)),
            "Slider" => Math.Min(fallback, 180),
            "ToggleSwitch" => Math.Min(fallback, Math.Max(120, EstimateTextWidth(ReadString(node, "header") ?? string.Empty) + 74)),
            "RatingControl" => Math.Min(fallback, Math.Max(112, ReadDouble(node, "maxRating", 5) * 22)),
            "SymbolIcon" => Math.Min(fallback, 32),
            "ProgressBar" => Math.Min(fallback, 180),
            "ProgressRing" => Math.Min(fallback, 32),
            "FontIcon" => Math.Min(fallback, 24),
            "Image" => Math.Min(fallback, 128),
            "InfoBar" => Math.Min(fallback, Math.Max(280, fallback)),
            "CommandBar" or "CommandBarFlyout" or "MenuFlyout" or "ContentDialog" or "Flyout" or "ToolTip" or "TeachingTip" => fallback,
            "MenuBar" => Math.Min(fallback, Math.Max(96, node.Children.Sum(child => EstimateWidth(child, fallback)) + 8)),
            "MenuBarItem" => Math.Min(fallback, Math.Max(64, EstimateTextWidth(ReadString(node, "title") ?? "Menu") + 28)),
            "Border" or "ScrollViewer" or "Frame" or "Page" or "Window" or "Grid" => fallback,
            "ContentControl" => EstimateContentControlWidth(node, fallback),
            "StackPanel" => EstimateStackWidth(node, fallback),
            "ListView" or "ItemsControl" => fallback,
            _ => Math.Min(Math.Max(40, fallback), fallback)
        };
        return ApplyWidthConstraints(node, fallback, natural);
    }

    private static double ApplyWidthConstraints(UiNode node, double fallback, double natural)
    {
        var explicitWidth = ReadDouble(node, "width", double.NaN);
        if (!double.IsNaN(explicitWidth) && explicitWidth > 0)
        {
            return Math.Min(fallback, explicitWidth);
        }

        var minWidth = ReadDouble(node, "minWidth", 0);
        return Math.Min(fallback, Math.Max(minWidth, natural));
    }

    private static double ApplyHeightConstraints(UiNode node, double fallback, double natural)
    {
        var explicitHeight = ReadDouble(node, "height", double.NaN);
        if (!double.IsNaN(explicitHeight) && explicitHeight > 0)
        {
            return Math.Min(fallback, explicitHeight);
        }

        var minHeight = ReadDouble(node, "minHeight", 0);
        return Math.Min(fallback, Math.Max(minHeight, natural));
    }

    private static double EstimateStackWidth(UiNode node, double fallback)
    {
        var padding = PaddingFor(node);
        var horizontalPadding = padding.Left + padding.Right;
        if (node.Children.Count == 0)
        {
            return Math.Min(fallback, horizontalPadding);
        }

        var available = Math.Max(1, fallback - horizontalPadding);
        var spacing = ReadDouble(node, "spacing", 0);
        double content = string.Equals(ReadString(node, "orientation"), "Horizontal", StringComparison.Ordinal)
            ? node.Children.Sum(child => EstimateWidth(child, available)) + Math.Max(0, node.Children.Count - 1) * spacing
            : node.Children.Max(child => EstimateWidth(child, available));

        return Math.Min(fallback, content + horizontalPadding);
    }

    private static double EstimateContentControlWidth(UiNode node, double fallback)
    {
        if (node.Children.Count == 0)
        {
            return Math.Min(120, Math.Max(64, fallback));
        }

        return Math.Min(fallback, Math.Max(1, node.Children.Max(child => EstimateWidth(child, fallback))));
    }

    private static double EstimateTextWidth(string text)
    {
        return Math.Ceiling(text.Length * 7.2);
    }

    private static string ReadControlText(UiNode node, string fallback)
    {
        return ReadString(node, "content") ?? ReadText(node) ?? node.Name ?? fallback;
    }

    // A panel stacked inside another panel must size to its content along the
    // stacking axis, not claim the whole available height. Returning the full
    // fallback made the first stacked row consume the column and pushed every
    // later row out of the viewport, blanking their component crops.
    private static double EstimateStackHeight(UiNode node, double fallback)
    {
        var padding = PaddingFor(node);
        var verticalPadding = padding.Top + padding.Bottom;
        if (node.Children.Count == 0)
        {
            return Math.Min(fallback, verticalPadding);
        }

        var available = Math.Max(1, fallback - verticalPadding);
        double content;
        if (string.Equals(ReadString(node, "orientation"), "Horizontal", StringComparison.Ordinal))
        {
            content = node.Children.Max(child => EstimateHeight(child, available));
        }
        else
        {
            var spacing = ReadDouble(node, "spacing", 0);
            content = node.Children.Sum(child => EstimateHeight(child, available))
                + Math.Max(0, node.Children.Count - 1) * spacing;
        }

        return Math.Min(fallback, content + verticalPadding);
    }

    // ArrangeGrid lays children across columns within a single row band, so the
    // grid's natural height is the tallest child rather than the full fallback.
    private static double EstimateGridHeight(UiNode node, double fallback)
    {
        if (node.Children.Count == 0)
        {
            return Math.Min(fallback, 40);
        }

        return Math.Min(fallback, node.Children.Max(child => EstimateHeight(child, fallback)));
    }

    private static double EstimateContentControlHeight(UiNode node, double fallback)
    {
        if (node.Children.Count == 0)
        {
            return Math.Min(120, Math.Max(64, fallback));
        }

        return Math.Min(fallback, Math.Max(1, node.Children.Max(child => EstimateHeight(child, fallback))));
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

    private static string? ReadText(UiNode node)
    {
        return ReadString(node, "text") ?? ReadString(node, "content");
    }

    private static double[] ResolveColumnWidths(UiNode node, double availableWidth, double spacing, int minimumColumnCount)
    {
        var definitions = ReadStringArray(node, "columnDefinitionWidths").ToArray();
        var columnCount = Math.Max(Math.Max(1, minimumColumnCount), definitions.Length);
        var totalSpacing = Math.Max(0, columnCount - 1) * spacing;
        var widthBudget = Math.Max(1, availableWidth - totalSpacing);
        if (definitions.Length == 0 && columnCount == 2)
        {
            var first = Math.Round(widthBudget * 0.38, 3);
            return new[] { first, Math.Max(1, widthBudget - first) };
        }

        var widths = new double[columnCount];
        var starColumns = new List<int>();
        var fixedTotal = 0d;
        for (var index = 0; index < columnCount; index++)
        {
            var definition = index < definitions.Length ? definitions[index] : "*";
            if (TryParseFixedGridLength(definition, out var fixedWidth))
            {
                widths[index] = fixedWidth;
                fixedTotal += fixedWidth;
            }
            else
            {
                starColumns.Add(index);
            }
        }

        var remaining = Math.Max(1, widthBudget - fixedTotal);
        var starWidth = starColumns.Count == 0 ? 0 : remaining / starColumns.Count;
        foreach (var index in starColumns)
        {
            widths[index] = starWidth;
        }

        return widths;
    }

    private static bool TryParseFixedGridLength(string? value, out double width)
    {
        width = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return !normalized.Contains('*', StringComparison.Ordinal) &&
            !string.Equals(normalized, "Auto", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out width) &&
            width > 0;
    }

    private static IEnumerable<string> ReadStringArray(UiNode node, string key)
    {
        if (!node.Properties.TryGetValue(key, out var value) || value is null)
        {
            yield break;
        }

        if (value is IEnumerable<string> strings)
        {
            foreach (var item in strings)
            {
                yield return item;
            }

            yield break;
        }

        if (value is System.Collections.IEnumerable values and not string)
        {
            foreach (var item in values)
            {
                if (item is not null)
                {
                    yield return item.ToString() ?? string.Empty;
                }
            }
        }
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
