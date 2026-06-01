using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace WinUI3.MacRuntime;

public sealed record UiTreeDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    UiNode Root);

public sealed record UiThickness(
    double Left,
    double Top,
    double Right,
    double Bottom);

public sealed record UiLayoutBox(
    double X,
    double Y,
    double Width,
    double Height,
    double DesiredWidth,
    double DesiredHeight,
    UiThickness Margin,
    UiThickness Padding,
    string HorizontalAlignment,
    string VerticalAlignment,
    string Visibility);

public sealed record UiNode(
    string Type,
    string? Name,
    IReadOnlyDictionary<string, object?> Properties,
    IReadOnlyList<UiNode> Children,
    UiLayoutBox? Layout = null);

public static class UiTreeBuilder
{
    public static UiTreeDocument Build(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return new UiTreeDocument(
            SchemaVersion: "0.1",
            GeneratedAt: DateTimeOffset.UtcNow,
            Root: BuildNode(window));
    }

    private static UiNode BuildNode(object element)
    {
        var properties = new Dictionary<string, object?>();
        var children = new List<UiNode>();
        string? name = null;

        if (element is FrameworkElement frameworkElement)
        {
            name = frameworkElement.Name;
            properties["visibility"] = frameworkElement.Visibility.ToString();
            properties["horizontalAlignment"] = frameworkElement.HorizontalAlignment.ToString();
            properties["verticalAlignment"] = frameworkElement.VerticalAlignment.ToString();
            properties["isFocused"] = frameworkElement.IsFocused;
            AddObjectProperty(properties, "background", frameworkElement.Background);
            AddObjectProperty(properties, "foreground", frameworkElement.Foreground);
            AddObjectProperty(properties, "style", frameworkElement.Style);
            var automationName = AutomationProperties.GetName(frameworkElement);
            if (!string.IsNullOrWhiteSpace(automationName))
            {
                properties["automationName"] = automationName;
            }

            var automationHelpText = AutomationProperties.GetHelpText(frameworkElement);
            if (!string.IsNullOrWhiteSpace(automationHelpText))
            {
                properties["automationHelpText"] = automationHelpText;
            }

            if (frameworkElement.Tag is not null)
            {
                properties["tag"] = frameworkElement.Tag.ToString();
            }
        }

        if (element is Control control)
        {
            properties["isEnabled"] = control.IsEnabled;
        }

        switch (element)
        {
            case Window window:
                properties["title"] = window.Title;
                properties["isActive"] = window.IsActive;
                AddChild(window.Content, children);
                break;
            case Page page:
                AddChild(page.Content, children);
                break;
            case StackPanel stackPanel:
                properties["orientation"] = stackPanel.Orientation.ToString();
                properties["childCount"] = stackPanel.Children.Count;
                properties["padding"] = stackPanel.Padding;
                properties["spacing"] = stackPanel.Spacing;
                foreach (var child in stackPanel.Children)
                {
                    AddChild(child, children);
                }

                break;
            case Grid grid:
                properties["childCount"] = grid.Children.Count;
                properties["columnDefinitions"] = grid.ColumnDefinitions;
                properties["columnSpacing"] = grid.ColumnSpacing;
                foreach (var child in grid.Children)
                {
                    AddChild(child, children);
                }

                break;
            case Border border:
                AddChild(border.Child, children);
                break;
            case FontIcon fontIcon:
                properties["glyph"] = fontIcon.Glyph;
                properties["fontSize"] = fontIcon.FontSize;
                break;
            case Frame frame:
                properties["sourcePageType"] = frame.SourcePageType?.FullName;
                AddChild(frame.Content, children);
                break;
            case NavigationView navigationView:
                properties["menuItemCount"] = navigationView.MenuItems.Count;
                properties["paneDisplayMode"] = navigationView.PaneDisplayMode;
                properties["selectedItem"] = (navigationView.SelectedItem as FrameworkElement)?.Name;
                properties["compactPaneLength"] = navigationView.CompactPaneLength;
                properties["openPaneLength"] = navigationView.OpenPaneLength;
                properties["isSettingsVisible"] = navigationView.IsSettingsVisible;
                foreach (var item in navigationView.MenuItems)
                {
                    AddChild(item, children);
                }

                AddChild(navigationView.PaneFooter, children);
                AddChild(navigationView.Content, children);
                break;
            case NavigationViewItem navigationViewItem:
                properties["content"] = navigationViewItem.Content is UIElement ? null : navigationViewItem.Content?.ToString();
                AddChild(navigationViewItem.Content, children);
                break;
            case TextBlock textBlock:
                properties["text"] = textBlock.Text;
                break;
            case TextBox textBox:
                properties["text"] = textBox.Text;
                break;
            case Button button:
                properties["content"] = button.Content is UIElement ? null : button.Content?.ToString();
                if (button.Content is UIElement buttonContent)
                {
                    AddChild(buttonContent, children);
                }

                break;
            case Image image:
                properties["source"] = image.Source?.ToString();
                break;
            case ListView listView:
                properties["itemCount"] = listView.Items.Count;
                foreach (var item in listView.Items)
                {
                    AddChild(item, children);
                }

                break;
        }

        return new UiNode(GetStableTypeName(element), name, properties, children);
    }

    private static void AddObjectProperty(Dictionary<string, object?> properties, string key, object? value)
    {
        if (value is not null)
        {
            properties[key] = value.ToString();
        }
    }

    private static void AddChild(object? child, ICollection<UiNode> children)
    {
        if (child is null)
        {
            return;
        }

        if (child is string text)
        {
            children.Add(new UiNode(
                Type: "System.String",
                Name: null,
                Properties: new Dictionary<string, object?> { ["text"] = text },
                Children: Array.Empty<UiNode>()));
            return;
        }

        children.Add(BuildNode(child));
    }

    private static string GetStableTypeName(object element)
    {
        if (element is Window)
        {
            return "Microsoft.UI.Xaml.Window";
        }

        if (element is Page)
        {
            return "Microsoft.UI.Xaml.Controls.Page";
        }

        var type = element.GetType();
        return type.FullName ?? type.Name;
    }
}
