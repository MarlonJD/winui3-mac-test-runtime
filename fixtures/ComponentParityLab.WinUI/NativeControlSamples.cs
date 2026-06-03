using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Reflection;
#endif

namespace ComponentParityLab.WinUI;

internal static class NativeControlSamples
{
    public static void PopulateBasicInput(
        ContentControl repeatButtonHost,
        ContentControl hyperlinkButtonHost,
        ContentControl dropDownButtonHost,
        ContentControl splitButtonHost,
        ContentControl toggleSplitButtonHost,
        ContentControl sliderHost,
        ContentControl toggleSwitchHost,
        ContentControl ratingControlHost)
    {
#if WINDOWS
        SetNativeControl(repeatButtonHost, "RepeatButton", ["Microsoft.UI.Xaml.Controls.Primitives.RepeatButton"], control =>
        {
            Set(control, "Content", "Repeat action");
        });
        SetNativeControl(hyperlinkButtonHost, "HyperlinkButton", ["Microsoft.UI.Xaml.Controls.HyperlinkButton"], control =>
        {
            Set(control, "Content", "Open public link");
        });
        SetNativeControl(dropDownButtonHost, "DropDownButton", ["Microsoft.UI.Xaml.Controls.DropDownButton"], control =>
        {
            Set(control, "Content", "Choose action");
        });
        SetNativeControl(splitButtonHost, "SplitButton", ["Microsoft.UI.Xaml.Controls.SplitButton"], control =>
        {
            Set(control, "Content", "Split action");
        });
        SetNativeControl(toggleSplitButtonHost, "ToggleSplitButton", ["Microsoft.UI.Xaml.Controls.ToggleSplitButton"], control =>
        {
            Set(control, "Content", "Toggle split");
            Set(control, "IsChecked", true);
        });
        SetNativeControl(sliderHost, "Slider", ["Microsoft.UI.Xaml.Controls.Slider"], control =>
        {
            Set(control, "Minimum", 0.0);
            Set(control, "Maximum", 100.0);
            Set(control, "Value", 64.0);
            Set(control, "Width", 180.0);
        });
        SetNativeControl(toggleSwitchHost, "ToggleSwitch", ["Microsoft.UI.Xaml.Controls.ToggleSwitch"], control =>
        {
            Set(control, "Header", "Enabled");
            Set(control, "IsOn", true);
        });
        SetNativeControl(ratingControlHost, "RatingControl", ["Microsoft.UI.Xaml.Controls.RatingControl"], control =>
        {
            Set(control, "MaxRating", 5);
            Set(control, "Value", 4.0);
        });
#else
        repeatButtonHost.Content = Labeled("RepeatButton", new Microsoft.UI.Xaml.Controls.Primitives.RepeatButton { Content = "Repeat action" });
        hyperlinkButtonHost.Content = Labeled("HyperlinkButton", new HyperlinkButton { Content = "Open public link" });
        dropDownButtonHost.Content = Labeled("DropDownButton", new DropDownButton { Content = "Choose action" });
        splitButtonHost.Content = Labeled("SplitButton", new SplitButton { Content = "Split action" });
        toggleSplitButtonHost.Content = Labeled("ToggleSplitButton", new ToggleSplitButton { Content = "Toggle split", IsChecked = true });
        sliderHost.Content = Labeled("Slider", new Slider { Minimum = 0, Maximum = 100, Value = 64 });
        toggleSwitchHost.Content = Labeled("ToggleSwitch", new ToggleSwitch { Header = "Enabled", IsOn = true });
        ratingControlHost.Content = Labeled("RatingControl", new RatingControl { MaxRating = 5, Value = 4 });
#endif
    }

    public static void PopulateTextForms(
        ContentControl richTextBlockHost,
        ContentControl richEditBoxHost,
        ContentControl passwordBoxHost,
        ContentControl numberBoxHost,
        ContentControl autoSuggestBoxHost,
        ContentControl autoSuggestBoxQueryIconHost,
        ContentControl formsPatternHost)
    {
#if WINDOWS
        SetNativeControl(richTextBlockHost, "RichTextBlock", ["Microsoft.UI.Xaml.Controls.RichTextBlock"], control =>
        {
            AddRichText(control, "Rich text block content");
            Set(control, "TextWrapping", EnumValue("Microsoft.UI.Xaml.TextWrapping", "Wrap"));
        });
        SetNativeControl(richEditBoxHost, "RichEditBox", ["Microsoft.UI.Xaml.Controls.RichEditBox"], control =>
        {
            Set(control, "PlaceholderText", "Rich edit notes");
            Set(control, "Width", 240.0);
            Set(control, "Height", 48.0);
        });
        SetNativeControl(passwordBoxHost, "PasswordBox", ["Microsoft.UI.Xaml.Controls.PasswordBox"], control =>
        {
            Set(control, "Password", "public");
            Set(control, "Width", 180.0);
        });
        SetNativeControl(numberBoxHost, "NumberBox", ["Microsoft.UI.Xaml.Controls.NumberBox"], control =>
        {
            Set(control, "Header", "Count");
            Set(control, "Value", 42.0);
            Set(control, "Width", 160.0);
        });
        SetNativeControl(autoSuggestBoxHost, "AutoSuggestBox", ["Microsoft.UI.Xaml.Controls.AutoSuggestBox"], control =>
        {
            Set(control, "PlaceholderText", "Search records");
            Set(control, "Text", "Public");
            Set(control, "Width", 220.0);
        });
        SetNativeControl(autoSuggestBoxQueryIconHost, "AutoSuggestBox.QueryIcon", ["Microsoft.UI.Xaml.Controls.AutoSuggestBox"], control =>
        {
            Set(control, "PlaceholderText", "Query icon");
            Set(control, "QueryIcon", SymbolIcon("Find"));
            Set(control, "Width", 220.0);
        });
        SetNativeElement(formsPatternHost, "Labels and forms", LabeledFormSample());
#endif
    }

    public static void PopulateCollections(
        ContentControl dataTemplateHost,
        ContentControl listViewItemTemplateHost,
        ContentControl itemsControlItemTemplateHost,
        ContentControl itemsViewHost,
        ContentControl gridViewHost,
        ContentControl flipViewHost,
        ContentControl pipsPagerHost,
        ContentControl treeViewHost,
        ContentControl itemsRepeaterHost,
        ContentControl swipePatternHost,
        ContentControl pullToRefreshPatternHost)
    {
#if WINDOWS
        SetNativeElement(dataTemplateHost, "DataTemplate", TemplatePreview("DataTemplate item"));
        SetNativeElement(listViewItemTemplateHost, "ListView.ItemTemplate", TemplatePreview("ListView template row"));
        SetNativeElement(itemsControlItemTemplateHost, "ItemsControl.ItemTemplate", TemplatePreview("ItemsControl template row"));
        SetNativeControl(itemsViewHost, "ItemsView", ["Microsoft.UI.Xaml.Controls.ItemsView"], control =>
        {
            Set(control, "ItemsSource", new[] { "ItemsView item one", "ItemsView item two" });
            Set(control, "Width", 260.0);
            Set(control, "Height", 86.0);
        });
        SetNativeControl(gridViewHost, "GridView", ["Microsoft.UI.Xaml.Controls.GridView"], control =>
        {
            AddItems(control, "Alpha", "Beta", "Gamma");
            Set(control, "Width", 260.0);
            Set(control, "Height", 86.0);
        });
        SetNativeControl(flipViewHost, "FlipView", ["Microsoft.UI.Xaml.Controls.FlipView"], control =>
        {
            AddItems(control, "First", "Second", "Third");
            Set(control, "SelectedIndex", 1);
            Set(control, "Width", 220.0);
            Set(control, "Height", 76.0);
        });
        SetNativeControl(pipsPagerHost, "PipsPager", ["Microsoft.UI.Xaml.Controls.PipsPager"], control =>
        {
            Set(control, "NumberOfPages", 5);
            Set(control, "SelectedPageIndex", 2);
        });
        SetNativeControl(treeViewHost, "TreeView", ["Microsoft.UI.Xaml.Controls.TreeView"], control =>
        {
            AddItems(control, "Root item", "Child item");
            Set(control, "Width", 240.0);
            Set(control, "Height", 96.0);
        });
        SetNativeControl(itemsRepeaterHost, "ItemsRepeater", ["Microsoft.UI.Xaml.Controls.ItemsRepeater"], control =>
        {
            Set(control, "ItemsSource", new[] { "Repeater one", "Repeater two" });
            Set(control, "Width", 260.0);
            Set(control, "Height", 72.0);
        });
        SetNativeControl(swipePatternHost, "SwipeControl", ["Microsoft.UI.Xaml.Controls.SwipeControl"], control =>
        {
            Set(control, "Content", new Button { Content = "Swipe row" });
        });
        SetNativeControl(pullToRefreshPatternHost, "RefreshContainer", ["Microsoft.UI.Xaml.Controls.RefreshContainer"], control =>
        {
            Set(control, "Content", new TextBlock { Text = "Pull-to-refresh region" });
        });
#endif
    }

    public static void PopulateDialogsAndFlyouts(
        ContentControl contentDialogHost,
        ContentControl flyoutHost,
        ContentControl teachingTipHost,
        ContentControl toolTipHost,
        ContentControl toolTipServiceHost)
    {
#if WINDOWS
        SetNativeControl(contentDialogHost, "ContentDialog", ["Microsoft.UI.Xaml.Controls.ContentDialog"], control =>
        {
            Set(control, "Title", "Public dialog");
            Set(control, "Content", "Review public fixture state");
            Set(control, "PrimaryButtonText", "OK");
        });
        SetNativeElement(flyoutHost, "Flyout", ButtonWithFlyout("Open Flyout", "Microsoft.UI.Xaml.Controls.Flyout"));
        SetNativeControl(teachingTipHost, "TeachingTip", ["Microsoft.UI.Xaml.Controls.TeachingTip"], control =>
        {
            Set(control, "Title", "Teaching tip");
            Set(control, "Subtitle", "Native WinUI tip sample");
            Set(control, "IsOpen", true);
        });
        SetNativeControl(toolTipHost, "ToolTip", ["Microsoft.UI.Xaml.Controls.ToolTip"], control =>
        {
            Set(control, "Content", "Native tooltip content");
        });
        SetNativeElement(toolTipServiceHost, "ToolTipService.SetToolTip", ButtonWithToolTip());
#endif
    }

    public static void PopulateCommandsAndMenus(
        ContentControl commandBarContentHost,
        ContentControl commandBarFlyoutHost,
        ContentControl menuFlyoutHost,
        ContentControl menuBarHost,
        ContentControl contextMenuPatternHost)
    {
#if WINDOWS
        SetNativeControl(commandBarContentHost, "CommandBar.Content", ["Microsoft.UI.Xaml.Controls.CommandBar"], control =>
        {
            Set(control, "Content", new TextBlock { Text = "Inline command content" });
            AddToPropertyCollection(control, "PrimaryCommands", AppBarButton("Accept"));
        });
        SetNativeElement(commandBarFlyoutHost, "CommandBarFlyout", ButtonWithCommandBarFlyout());
        SetNativeElement(menuFlyoutHost, "MenuFlyout", ButtonWithMenuFlyout());
        SetNativeControl(menuBarHost, "MenuBar", ["Microsoft.UI.Xaml.Controls.MenuBar"], control =>
        {
            var menuBarItem = Create("Microsoft.UI.Xaml.Controls.MenuBarItem");
            if (menuBarItem is not null)
            {
                Set(menuBarItem, "Title", "File");
                AddToPropertyCollection(menuBarItem, "Items", MenuFlyoutItem("Open"));
                AddToPropertyCollection(menuBarItem, "Items", MenuFlyoutItem("Save"));
                AddToPropertyCollection(control, "Items", menuBarItem);
            }
        });
        SetNativeElement(contextMenuPatternHost, "Context menu pattern", ButtonWithContextMenu());
#else
        commandBarContentHost.Content = new TextBlock { Text = "CommandBar.Content", Width = 120, Height = 120 };
        menuBarHost.Content = new MenuBar
        {
            Items =
            {
                new MenuBarItem
                {
                    Title = "File",
                    Items =
                    {
                        new MenuFlyoutItem { Text = "Open" },
                        new MenuFlyoutItem { Text = "Save" }
                    }
                }
            }
        };
#endif
    }

    public static void PopulateNavigation(
        ContentControl breadcrumbBarHost,
        ContentControl pivotHost,
        ContentControl selectorBarHost,
        ContentControl tabViewHost)
    {
#if WINDOWS
        SetNativeControl(breadcrumbBarHost, "BreadcrumbBar", ["Microsoft.UI.Xaml.Controls.BreadcrumbBar"], control =>
        {
            Set(control, "ItemsSource", new[] { "Home", "Queue", "Item" });
            Set(control, "Width", 260.0);
        });
        SetNativeControl(pivotHost, "Pivot", ["Microsoft.UI.Xaml.Controls.Pivot"], control =>
        {
            AddToPropertyCollection(control, "Items", PivotItem("Overview"));
            AddToPropertyCollection(control, "Items", PivotItem("History"));
        });
        SetNativeControl(selectorBarHost, "SelectorBar", ["Microsoft.UI.Xaml.Controls.SelectorBar"], control =>
        {
            AddToPropertyCollection(control, "Items", SelectorBarItem("Open"));
            AddToPropertyCollection(control, "Items", SelectorBarItem("Closed"));
        });
        SetNativeControl(tabViewHost, "TabView", ["Microsoft.UI.Xaml.Controls.TabView"], control =>
        {
            AddToPropertyCollection(control, "TabItems", TabViewItem("Summary"));
            AddToPropertyCollection(control, "TabItems", TabViewItem("Details"));
            Set(control, "Width", 360.0);
        });
#endif
    }

    public static void PopulateStatusAndPickers(
        ContentControl infoBadgeHost,
        ContentControl personPictureHost,
        ContentControl colorPickerHost,
        ContentControl calendarDatePickerHost,
        ContentControl calendarViewHost,
        ContentControl datePickerHost,
        ContentControl timePickerHost)
    {
#if WINDOWS
        SetNativeControl(infoBadgeHost, "InfoBadge", ["Microsoft.UI.Xaml.Controls.InfoBadge"], control =>
        {
            Set(control, "Value", 7);
        });
        SetNativeControl(personPictureHost, "PersonPicture", ["Microsoft.UI.Xaml.Controls.PersonPicture"], control =>
        {
            Set(control, "DisplayName", "Public Fixture");
            Set(control, "Width", 56.0);
            Set(control, "Height", 56.0);
        });
        SetNativeControl(colorPickerHost, "ColorPicker", ["Microsoft.UI.Xaml.Controls.ColorPicker"], control =>
        {
            Set(control, "Width", 260.0);
            Set(control, "Height", 120.0);
        });
        SetNativeControl(calendarDatePickerHost, "CalendarDatePicker", ["Microsoft.UI.Xaml.Controls.CalendarDatePicker"], control =>
        {
            Set(control, "PlaceholderText", "Pick date");
            Set(control, "Date", new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        });
        SetNativeControl(calendarViewHost, "CalendarView", ["Microsoft.UI.Xaml.Controls.CalendarView"], control =>
        {
            Set(control, "Width", 260.0);
            Set(control, "Height", 160.0);
        });
        SetNativeControl(datePickerHost, "DatePicker", ["Microsoft.UI.Xaml.Controls.DatePicker"], control =>
        {
            Set(control, "Date", new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        });
        SetNativeControl(timePickerHost, "TimePicker", ["Microsoft.UI.Xaml.Controls.TimePicker"], control =>
        {
            Set(control, "Time", new TimeSpan(9, 30, 0));
        });
#endif
    }

    public static void PopulateLayoutAndMedia(
        ContentControl symbolIconHost,
        ContentControl xamlControlsResourcesHost,
        ContentControl themeDictionariesHost,
        ContentControl colorHost,
        ContentControl solidColorBrushHost,
        ContentControl cornerRadiusHost,
        ContentControl expanderHost,
        ContentControl annotatedScrollBarHost,
        ContentControl semanticZoomHost,
        ContentControl splitViewHost,
        ContentControl twoPaneViewHost,
        ContentControl animatedIconHost,
        ContentControl shapesHost,
        ContentControl mediaPlayerElementHost,
        ContentControl webView2Host,
        ContentControl inkControlsHost,
        ContentControl titleBarCustomizationHost,
        ContentControl systemBackdropHost)
    {
#if WINDOWS
        SetNativeControl(symbolIconHost, "SymbolIcon", ["Microsoft.UI.Xaml.Controls.SymbolIcon"], control =>
        {
            Set(control, "Symbol", EnumValue("Microsoft.UI.Xaml.Controls.Symbol", "Link"));
        });
        SetNativeElement(xamlControlsResourcesHost, "XamlControlsResources", ResourcePreview("XamlControlsResources loaded"));
        SetNativeElement(themeDictionariesHost, "ResourceDictionary.ThemeDictionaries", ResourcePreview("Theme dictionary sample"));
        SetNativeElement(colorHost, "Color", ColorSwatch("Color resource", Colors.DodgerBlue));
        SetNativeElement(solidColorBrushHost, "SolidColorBrush", ColorSwatch("Brush resource", Colors.SeaGreen));
        SetNativeElement(cornerRadiusHost, "CornerRadius", CornerRadiusPreview());
        SetNativeControl(expanderHost, "Expander", ["Microsoft.UI.Xaml.Controls.Expander"], control =>
        {
            Set(control, "Header", "More details");
            Set(control, "Content", "Expanded public content");
            Set(control, "IsExpanded", true);
        });
        SetNativeControl(annotatedScrollBarHost, "AnnotatedScrollBar", ["Microsoft.UI.Xaml.Controls.AnnotatedScrollBar"]);
        SetNativeControl(semanticZoomHost, "SemanticZoom", ["Microsoft.UI.Xaml.Controls.SemanticZoom"], control =>
        {
            Set(control, "ZoomedInView", new ListView { Items = { "Detailed item" } });
            Set(control, "ZoomedOutView", new GridView { Items = { "Group" } });
            Set(control, "Width", 280.0);
            Set(control, "Height", 96.0);
        });
        SetNativeControl(splitViewHost, "SplitView", ["Microsoft.UI.Xaml.Controls.SplitView"], control =>
        {
            Set(control, "Pane", new TextBlock { Text = "Pane" });
            Set(control, "Content", new TextBlock { Text = "Content" });
            Set(control, "IsPaneOpen", true);
            Set(control, "Width", 260.0);
            Set(control, "Height", 96.0);
        });
        SetNativeControl(twoPaneViewHost, "TwoPaneView", ["Microsoft.UI.Xaml.Controls.TwoPaneView"], control =>
        {
            Set(control, "Pane1", new TextBlock { Text = "Pane 1" });
            Set(control, "Pane2", new TextBlock { Text = "Pane 2" });
            Set(control, "Width", 300.0);
            Set(control, "Height", 72.0);
        });
        SetNativeControl(animatedIconHost, "AnimatedIcon", ["Microsoft.UI.Xaml.Controls.AnimatedIcon"]);
        SetNativeElement(shapesHost, "Shapes", ShapesPreview());
        SetNativeControl(mediaPlayerElementHost, "MediaPlayerElement", ["Microsoft.UI.Xaml.Controls.MediaPlayerElement"], control =>
        {
            Set(control, "Width", 220.0);
            Set(control, "Height", 80.0);
        });
        SetNativeControl(webView2Host, "WebView2", ["Microsoft.UI.Xaml.Controls.WebView2"], control =>
        {
            Set(control, "Width", 220.0);
            Set(control, "Height", 80.0);
        });
        SetNativeElement(inkControlsHost, "InkCanvas / InkToolbar", InkPreview());
        SetNativeElement(titleBarCustomizationHost, "Title bar customization", ResourcePreview("ExtendsContentIntoTitleBar sample"));
        SetNativeElement(systemBackdropHost, "Window.SystemBackdrop / MicaBackdrop", ResourcePreview("MicaBackdrop assigned"));
#else
        symbolIconHost.Content = Labeled("SymbolIcon", new Microsoft.UI.Xaml.Controls.SymbolIcon { Symbol = Symbol.Link });
        xamlControlsResourcesHost.Content = Labeled("XamlControlsResources", MacResourcePreview("XamlControlsResources loaded"));
        themeDictionariesHost.Content = Labeled("ResourceDictionary.ThemeDictionaries", new TextBlock { Text = "Theme dictionary sample" });
        colorHost.Content = Labeled("Color", MacColorSwatch("Color resource", "#1E90FF"));
        solidColorBrushHost.Content = Labeled("SolidColorBrush", MacColorSwatch("Brush resource", "#2E8B57"));
        cornerRadiusHost.Content = Labeled("CornerRadius", new Border { CornerRadius = 10, Child = new TextBlock { Text = "Rounded border" } });
        expanderHost.Content = Labeled("Expander", new Expander { Header = "More details", Content = "Expanded public content", IsExpanded = true, Width = 260, Height = 92 });
        annotatedScrollBarHost.Content = Labeled("AnnotatedScrollBar", new AnnotatedScrollBar { Width = 180, Height = 92, MarkerCount = 3 });
        semanticZoomHost.Content = Labeled("SemanticZoom", new SemanticZoom { ZoomedInView = new TextBlock { Text = "Detailed item" }, ZoomedOutView = new TextBlock { Text = "Group" }, Width = 260, Height = 96 });
        splitViewHost.Content = Labeled("SplitView", new SplitView { Pane = new TextBlock { Text = "Pane" }, Content = new TextBlock { Text = "Content" }, IsPaneOpen = true, Width = 260, Height = 96 });
        twoPaneViewHost.Content = Labeled("TwoPaneView", new TwoPaneView { Pane1 = new TextBlock { Text = "Pane 1" }, Pane2 = new TextBlock { Text = "Pane 2" }, Width = 300, Height = 72 });
        animatedIconHost.Content = Labeled("AnimatedIcon", new TextBlock { Text = "Animated icon" });
        shapesHost.Content = Labeled("Shapes", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "Rectangle" }, new TextBlock { Text = "Ellipse" }, new TextBlock { Text = "Line" } } });
        mediaPlayerElementHost.Content = Labeled("MediaPlayerElement", new TextBlock { Text = "Media player surface" });
        webView2Host.Content = Labeled("WebView2", new TextBlock { Text = "WebView2 surface" });
        inkControlsHost.Content = Labeled("InkCanvas / InkToolbar", new StackPanel { Spacing = 4, Children = { new TextBlock { Text = "InkToolbar" }, new TextBlock { Text = "InkCanvas" } } });
        titleBarCustomizationHost.Content = Labeled("Title bar customization", new TextBlock { Text = "ExtendsContentIntoTitleBar sample" });
        systemBackdropHost.Content = Labeled("Window.SystemBackdrop / MicaBackdrop", new TextBlock { Text = "MicaBackdrop assigned" });
#endif
    }

    private static StackPanel Labeled(string label, UIElement element)
    {
        if (element is FrameworkElement frameworkElement)
        {
            frameworkElement.MinWidth = Math.Max(frameworkElement.MinWidth, 120);
        }

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Width = 180,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(element);
        return panel;
    }

#if !WINDOWS
    private static UIElement MacResourcePreview(string text)
    {
        return new Border
        {
            Width = 220,
            Height = 34,
            CornerRadius = Radius(6),
            Background = "#FAFAFA",
            Child = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
        };
    }

    private static UIElement MacColorSwatch(string text, string color)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new Border
                {
                    Width = 40,
                    Height = 20,
                    CornerRadius = Radius(4),
                    Background = color
                },
                new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }

    private static double Radius(double value) => value;
#endif

#if WINDOWS
    private static void SetNativeControl(
        ContentControl host,
        string label,
        string[] typeNames,
        Action<object>? configure = null)
    {
        var control = Create(typeNames);
        if (control is null)
        {
            SetUnavailable(host, label);
            return;
        }

        configure?.Invoke(control);
        if (control is UIElement element)
        {
            host.Content = Labeled(label, element);
            return;
        }

        SetUnavailable(host, label);
    }

    private static void SetNativeElement(ContentControl host, string label, UIElement element)
    {
        host.Content = Labeled(label, element);
    }

    private static void SetUnavailable(ContentControl host, string label)
    {
        host.Content = Labeled(label, new TextBlock
        {
            Text = label + " unavailable in this Windows App SDK"
        });
    }

    private static object? Create(params string[] typeNames)
    {
        var assembly = typeof(Button).Assembly;
        foreach (var typeName in typeNames)
        {
            var type = assembly.GetType(typeName, throwOnError: false) ?? Type.GetType(typeName, throwOnError: false);
            if (type is null)
            {
                continue;
            }

            try
            {
                return Activator.CreateInstance(type);
            }
            catch (MissingMethodException)
            {
                return null;
            }
        }

        return null;
    }

    private static void Set(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            return;
        }

        try
        {
            property.SetValue(target, value);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetInvocationException)
        {
        }
    }

    private static void AddItems(object target, params object[] items)
    {
        foreach (var item in items)
        {
            AddToPropertyCollection(target, "Items", item);
        }
    }

    private static void AddToPropertyCollection(object target, string propertyName, object? item)
    {
        if (item is null)
        {
            return;
        }

        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        var collection = property?.GetValue(target);
        if (collection is null)
        {
            return;
        }

        var addMethod = collection.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method => method.Name == "Add" && method.GetParameters().Length == 1);
        try
        {
            addMethod?.Invoke(collection, [item]);
        }
        catch (ArgumentException)
        {
        }
        catch (TargetInvocationException)
        {
        }
    }

    private static object? EnumValue(string enumTypeName, string name)
    {
        var enumType = typeof(Button).Assembly.GetType(enumTypeName, throwOnError: false);
        if (enumType is null || !enumType.IsEnum)
        {
            return null;
        }

        return Enum.Parse(enumType, name);
    }

    private static object? SymbolIcon(string symbol)
    {
        var icon = Create("Microsoft.UI.Xaml.Controls.SymbolIcon");
        if (icon is not null)
        {
            Set(icon, "Symbol", EnumValue("Microsoft.UI.Xaml.Controls.Symbol", symbol));
        }

        return icon;
    }

    private static object? AppBarButton(string label)
    {
        var button = Create("Microsoft.UI.Xaml.Controls.AppBarButton");
        if (button is not null)
        {
            Set(button, "Label", label);
            Set(button, "Icon", SymbolIcon("Accept"));
        }

        return button;
    }

    private static object? MenuFlyoutItem(string text)
    {
        var item = Create("Microsoft.UI.Xaml.Controls.MenuFlyoutItem");
        if (item is not null)
        {
            Set(item, "Text", text);
        }

        return item;
    }

    private static object? PivotItem(string header)
    {
        var item = Create("Microsoft.UI.Xaml.Controls.PivotItem");
        if (item is not null)
        {
            Set(item, "Header", header);
            Set(item, "Content", new TextBlock { Text = header + " content" });
        }

        return item;
    }

    private static object? SelectorBarItem(string text)
    {
        var item = Create("Microsoft.UI.Xaml.Controls.SelectorBarItem");
        if (item is not null)
        {
            Set(item, "Text", text);
        }

        return item;
    }

    private static object? TabViewItem(string header)
    {
        var item = Create("Microsoft.UI.Xaml.Controls.TabViewItem");
        if (item is not null)
        {
            Set(item, "Header", header);
            Set(item, "Content", new TextBlock { Text = header + " tab" });
        }

        return item;
    }

    private static UIElement ButtonWithFlyout(string label, string flyoutTypeName)
    {
        var button = new Button { Content = label };
        var flyout = Create(flyoutTypeName);
        if (flyout is not null)
        {
            Set(flyout, "Content", new TextBlock { Text = label + " content" });
            Set(button, "Flyout", flyout);
        }

        return button;
    }

    private static UIElement ButtonWithCommandBarFlyout()
    {
        var button = new Button { Content = "Open command flyout" };
        var flyout = Create("Microsoft.UI.Xaml.Controls.CommandBarFlyout");
        if (flyout is not null)
        {
            AddToPropertyCollection(flyout, "PrimaryCommands", AppBarButton("Pin"));
            AddToPropertyCollection(flyout, "SecondaryCommands", AppBarButton("Archive"));
            Set(button, "Flyout", flyout);
        }

        return button;
    }

    private static UIElement ButtonWithMenuFlyout()
    {
        var button = new Button { Content = "Open menu flyout" };
        var flyout = Create("Microsoft.UI.Xaml.Controls.MenuFlyout");
        if (flyout is not null)
        {
            AddToPropertyCollection(flyout, "Items", MenuFlyoutItem("Approve"));
            AddToPropertyCollection(flyout, "Items", MenuFlyoutItem("Defer"));
            Set(button, "Flyout", flyout);
        }

        return button;
    }

    private static UIElement ButtonWithContextMenu()
    {
        var button = new Button { Content = "Right-click menu target" };
        var flyout = Create("Microsoft.UI.Xaml.Controls.MenuFlyout");
        if (flyout is not null)
        {
            AddToPropertyCollection(flyout, "Items", MenuFlyoutItem("Context action"));
            Set(button, "ContextFlyout", flyout);
        }

        return button;
    }

    private static UIElement ButtonWithToolTip()
    {
        var button = new Button { Content = "Hover tooltip target" };
        var toolTip = Create("Microsoft.UI.Xaml.Controls.ToolTip");
        if (toolTip is not null)
        {
            Set(toolTip, "Content", "Native ToolTipService sample");
            var service = typeof(Button).Assembly.GetType("Microsoft.UI.Xaml.Controls.ToolTipService", throwOnError: false);
            service?.GetMethod("SetToolTip", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, [button, toolTip]);
        }

        return button;
    }

    private static UIElement LabeledFormSample()
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = "Name" });
        panel.Children.Add(new TextBox { Text = "Public Fixture" });
        return panel;
    }

    private static UIElement TemplatePreview(string text)
    {
        return new Border
        {
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Colors.AliceBlue),
            Child = new TextBlock { Text = text }
        };
    }

    private static void AddRichText(object richTextBlock, string text)
    {
        var paragraph = Create("Microsoft.UI.Xaml.Documents.Paragraph");
        var run = Create("Microsoft.UI.Xaml.Documents.Run");
        if (paragraph is null || run is null)
        {
            return;
        }

        Set(run, "Text", text);
        AddToPropertyCollection(paragraph, "Inlines", run);
        AddToPropertyCollection(richTextBlock, "Blocks", paragraph);
    }

    private static UIElement ResourcePreview(string text)
    {
        return new Border
        {
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Colors.GhostWhite),
            Child = new TextBlock { Text = text }
        };
    }

    private static UIElement ColorSwatch(string text, Windows.UI.Color color)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new Border
                {
                    Width = 40,
                    Height = 20,
                    Background = new SolidColorBrush(color),
                    CornerRadius = new CornerRadius(4)
                },
                new TextBlock { Text = text }
            }
        };
    }

    private static UIElement CornerRadiusPreview()
    {
        return new Border
        {
            Width = 140,
            Height = 34,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Colors.LightSteelBlue),
            Child = new TextBlock
            {
                Text = "Rounded border",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }

    private static UIElement ShapesPreview()
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new Rectangle { Width = 44, Height = 24, Fill = new SolidColorBrush(Colors.DodgerBlue) },
                new Ellipse { Width = 28, Height = 28, Fill = new SolidColorBrush(Colors.SeaGreen) },
                new Line { X1 = 0, Y1 = 24, X2 = 48, Y2 = 0, StrokeThickness = 3, Stroke = new SolidColorBrush(Colors.OrangeRed) }
            }
        };
    }

    private static UIElement InkPreview()
    {
        var panel = new StackPanel { Spacing = 4 };
        var toolbar = Create("Microsoft.UI.Xaml.Controls.InkToolbar");
        var canvas = Create("Microsoft.UI.Xaml.Controls.InkCanvas");
        panel.Children.Add(toolbar as UIElement ?? new TextBlock { Text = "InkToolbar unavailable" });
        panel.Children.Add(canvas as UIElement ?? new TextBlock { Text = "InkCanvas unavailable" });
        return panel;
    }
#endif
}
