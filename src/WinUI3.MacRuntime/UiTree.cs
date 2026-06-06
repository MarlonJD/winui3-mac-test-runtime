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
            SchemaVersion: ArtifactSchemas.UiTree,
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
            if (!string.IsNullOrWhiteSpace(frameworkElement.Uid))
            {
                properties["uid"] = frameworkElement.Uid;
            }

            properties["visibility"] = frameworkElement.Visibility.ToString();
            properties["horizontalAlignment"] = frameworkElement.HorizontalAlignment.ToString();
            properties["verticalAlignment"] = frameworkElement.VerticalAlignment.ToString();
            properties["isFocused"] = frameworkElement.IsFocused;
            if (!double.IsNaN(frameworkElement.Width))
            {
                properties["width"] = frameworkElement.Width;
            }

            if (!double.IsNaN(frameworkElement.Height))
            {
                properties["height"] = frameworkElement.Height;
            }

            if (frameworkElement.MinWidth > 0)
            {
                properties["minWidth"] = frameworkElement.MinWidth;
            }

            if (frameworkElement.MinHeight > 0)
            {
                properties["minHeight"] = frameworkElement.MinHeight;
            }

            if (!double.IsPositiveInfinity(frameworkElement.MaxWidth))
            {
                properties["maxWidth"] = frameworkElement.MaxWidth;
            }

            if (!double.IsPositiveInfinity(frameworkElement.MaxHeight))
            {
                properties["maxHeight"] = frameworkElement.MaxHeight;
            }

            if (Grid.GetColumn(frameworkElement) is var gridColumn and > 0)
            {
                properties["gridColumn"] = gridColumn;
            }

            if (Grid.GetRow(frameworkElement) is var gridRow and > 0)
            {
                properties["gridRow"] = gridRow;
            }

            if (Grid.GetColumnSpan(frameworkElement) is var gridColumnSpan and > 1)
            {
                properties["gridColumnSpan"] = gridColumnSpan;
            }

            AddObjectProperty(properties, "background", frameworkElement.Background);
            AddObjectProperty(properties, "foreground", frameworkElement.Foreground);
            AddObjectProperty(properties, "style", frameworkElement.Style);
            var automationName = AutomationProperties.GetName(frameworkElement);
            if (!string.IsNullOrWhiteSpace(automationName))
            {
                properties["automationName"] = automationName;
            }

            var automationId = AutomationProperties.GetAutomationId(frameworkElement);
            if (!string.IsNullOrWhiteSpace(automationId))
            {
                properties["automationId"] = automationId;
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
            properties["isFocusable"] = control.IsEnabled;
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
            case ContentDialog contentDialog:
                properties["title"] = contentDialog.Title?.ToString();
                properties["primaryButtonText"] = contentDialog.PrimaryButtonText;
                properties["isOpen"] = contentDialog.IsOpen;
                properties["result"] = contentDialog.Result;
                AddChild(contentDialog.Content, children);
                break;
            case TeachingTip teachingTip:
                properties["title"] = teachingTip.Title;
                properties["subtitle"] = teachingTip.Subtitle;
                properties["isOpen"] = teachingTip.IsOpen;
                AddChild(teachingTip.Content, children);
                break;
            case Flyout flyout:
                properties["isOpen"] = flyout.IsOpen;
                AddChild(flyout.Content, children);
                break;
            case ToolTip toolTip:
                properties["isOpen"] = toolTip.IsOpen;
                AddChild(toolTip.Content, children);
                break;
            case ContentControl contentControl:
                AddChild(contentControl.Content, children);
                break;
            case ScrollViewer scrollViewer:
                properties["horizontalScrollBarVisibility"] = scrollViewer.HorizontalScrollBarVisibility.ToString();
                properties["verticalScrollBarVisibility"] = scrollViewer.VerticalScrollBarVisibility.ToString();
                AddChild(scrollViewer.Content, children);
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
                properties["rowDefinitions"] = grid.RowDefinitions;
                if (!string.IsNullOrWhiteSpace(grid.ColumnDefinitions))
                {
                    properties["columnDefinitionWidths"] = grid.ColumnDefinitions
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                }

                if (!string.IsNullOrWhiteSpace(grid.RowDefinitions))
                {
                    properties["rowDefinitionHeights"] = grid.RowDefinitions
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                }

                properties["columnSpacing"] = grid.ColumnSpacing;
                properties["rowSpacing"] = grid.RowSpacing;
                properties["padding"] = grid.Padding;
                foreach (var child in grid.Children)
                {
                    AddChild(child, children);
                }

                break;
            case Border border:
                AddObjectProperty(properties, "cornerRadius", border.CornerRadius);
                properties["padding"] = border.Padding;
                AddObjectProperty(properties, "borderBrush", border.BorderBrush);
                AddObjectProperty(properties, "borderThickness", border.BorderThickness);
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
            case CommandBar commandBar:
                properties["primaryCommandCount"] = commandBar.PrimaryCommands.Count;
                properties["defaultLabelPosition"] = commandBar.DefaultLabelPosition.ToString();
                properties["content"] = commandBar.Content is UIElement ? null : commandBar.Content?.ToString();
                AddChild(commandBar.Content, children);
                foreach (var command in commandBar.PrimaryCommands)
                {
                    AddChild(command, children);
                }

                break;
            case CommandBarFlyout commandBarFlyout:
                properties["isOpen"] = commandBarFlyout.IsOpen;
                properties["primaryCommandCount"] = commandBarFlyout.PrimaryCommands.Count;
                properties["secondaryCommandCount"] = commandBarFlyout.SecondaryCommands.Count;
                properties["invokedCommand"] = commandBarFlyout.InvokedCommand;
                foreach (var command in commandBarFlyout.PrimaryCommands.Concat(commandBarFlyout.SecondaryCommands))
                {
                    AddChild(command, children);
                }

                break;
            case MenuFlyout menuFlyout:
                properties["isOpen"] = menuFlyout.IsOpen;
                properties["itemCount"] = menuFlyout.Items.Count;
                properties["invokedItem"] = menuFlyout.InvokedItem;
                foreach (var item in menuFlyout.Items)
                {
                    AddChild(item, children);
                }

                break;
            case MenuFlyoutItem menuFlyoutItem:
                properties["text"] = menuFlyoutItem.Text;
                break;
            case MenuBar menuBar:
                properties["itemCount"] = menuBar.Items.Count;
                foreach (var item in menuBar.Items)
                {
                    AddChild(item, children);
                }

                break;
            case MenuBarItem menuBarItem:
                properties["title"] = menuBarItem.Title;
                properties["itemCount"] = menuBarItem.Items.Count;
                foreach (var item in menuBarItem.Items)
                {
                    AddChild(item, children);
                }

                break;
            case Expander expander:
                properties["header"] = expander.Header is UIElement ? null : expander.Header?.ToString();
                properties["isExpanded"] = expander.IsExpanded;
                if (expander.Header is UIElement expanderHeader)
                {
                    AddChild(expanderHeader, children);
                }

                AddChild(expander.Content, children);
                break;
            case AnnotatedScrollBar annotatedScrollBar:
                properties["markerCount"] = annotatedScrollBar.MarkerCount;
                break;
            case SemanticZoom semanticZoom:
                AddChild(semanticZoom.ZoomedInView, children);
                AddChild(semanticZoom.ZoomedOutView, children);
                break;
            case SplitView splitView:
                properties["isPaneOpen"] = splitView.IsPaneOpen;
                AddChild(splitView.Pane, children);
                AddChild(splitView.Content, children);
                break;
            case TwoPaneView twoPaneView:
                AddChild(twoPaneView.Pane1, children);
                AddChild(twoPaneView.Pane2, children);
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
            case AppBarButton appBarButton:
                properties["label"] = appBarButton.Label;
                properties["content"] = appBarButton.Content is UIElement ? null : appBarButton.Content?.ToString();
                if (appBarButton.Command is not null)
                {
                    properties["commandCanExecute"] = appBarButton.Command.CanExecute(appBarButton.CommandParameter);
                }

                AddChild(appBarButton.Icon, children);
                AddChild(appBarButton.Content, children);
                break;
            case RadioButton radioButton:
                properties["isChecked"] = radioButton.IsChecked;
                properties["groupName"] = radioButton.GroupName;
                properties["content"] = radioButton.Content is UIElement ? null : radioButton.Content?.ToString();
                AddChild(radioButton.Content, children);
                break;
            case CheckBox checkBox:
                properties["isChecked"] = checkBox.IsChecked;
                properties["content"] = checkBox.Content is UIElement ? null : checkBox.Content?.ToString();
                AddChild(checkBox.Content, children);
                break;
            case ToggleButton toggleButton:
                properties["isChecked"] = toggleButton.IsChecked;
                properties["content"] = toggleButton.Content is UIElement ? null : toggleButton.Content?.ToString();
                AddChild(toggleButton.Content, children);
                break;
            case Slider slider:
                properties["minimum"] = slider.Minimum;
                properties["maximum"] = slider.Maximum;
                properties["value"] = slider.Value;
                break;
            case ToggleSwitch toggleSwitch:
                properties["header"] = toggleSwitch.Header?.ToString();
                properties["isOn"] = toggleSwitch.IsOn;
                break;
            case RatingControl ratingControl:
                properties["maxRating"] = ratingControl.MaxRating;
                properties["value"] = ratingControl.Value;
                break;
            case SymbolIcon symbolIcon:
                properties["symbol"] = symbolIcon.Symbol.ToString();
                break;
            case AutoSuggestBox autoSuggestBox:
                properties["text"] = autoSuggestBox.Text;
                properties["minWidth"] = autoSuggestBox.MinWidth;
                if (!double.IsPositiveInfinity(autoSuggestBox.MaxWidth))
                {
                    properties["maxWidth"] = autoSuggestBox.MaxWidth;
                }

                AddChild(autoSuggestBox.QueryIcon, children);
                break;
            case TextBlock textBlock:
                properties["text"] = textBlock.Text;
                properties["textWrapping"] = textBlock.TextWrapping.ToString();
                AddObjectProperty(properties, "fontWeight", textBlock.FontWeight);
                break;
            case TextBox textBox:
                properties["text"] = textBox.Text;
                properties["textWrapping"] = textBox.TextWrapping.ToString();
                properties["acceptsReturn"] = textBox.AcceptsReturn;
                break;
            case PasswordBox passwordBox:
                properties["isPassword"] = true;
                properties["passwordLength"] = passwordBox.Password?.Length ?? 0;
                properties["placeholderText"] = passwordBox.PlaceholderText;
                properties["header"] = passwordBox.Header?.ToString();
                break;
            case ComboBox comboBox:
                properties["itemCount"] = comboBox.Items.Count;
                properties["placeholderText"] = comboBox.PlaceholderText;
                properties["selectedIndex"] = comboBox.SelectedIndex;
                properties["selectedItem"] = comboBox.SelectedItem?.ToString();
                foreach (var item in comboBox.Items)
                {
                    AddChild(item, children);
                }

                break;
            case Button button:
                properties["content"] = button.Content is UIElement ? null : button.Content?.ToString();
                if (button.Command is not null)
                {
                    properties["commandCanExecute"] = button.Command.CanExecute(button.CommandParameter);
                }

                if (button.Content is UIElement buttonContent)
                {
                    AddChild(buttonContent, children);
                }

                AddChild(button.Flyout, children);
                AddChild(button.ContextFlyout, children);
                break;
            case Image image:
                properties["source"] = image.Source?.ToString();
                break;
            case ListView listView:
                properties["itemCount"] = listView.Items.Count;
                properties["selectedIndex"] = listView.SelectedIndex;
                properties["selectedItem"] = listView.SelectedItem?.ToString();
                properties["isItemClickEnabled"] = listView.IsItemClickEnabled;
                properties["selectionMode"] = listView.SelectionMode.ToString();
                foreach (var item in listView.Items)
                {
                    AddChild(item, children);
                }

                break;
            case ProgressRing progressRing:
                properties["isActive"] = progressRing.IsActive;
                break;
            case ProgressBar progressBar:
                properties["minimum"] = progressBar.Minimum;
                properties["maximum"] = progressBar.Maximum;
                properties["value"] = progressBar.Value;
                properties["isIndeterminate"] = progressBar.IsIndeterminate;
                break;
            case InfoBar infoBar:
                properties["title"] = infoBar.Title;
                properties["message"] = infoBar.Message;
                properties["severity"] = infoBar.Severity.ToString();
                properties["isOpen"] = infoBar.IsOpen;
                properties["isClosable"] = infoBar.IsClosable;
                break;
            case ItemsControl itemsControl:
                properties["itemCount"] = itemsControl.Items.Count;
                foreach (var item in itemsControl.Items)
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
