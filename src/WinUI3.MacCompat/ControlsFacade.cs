using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Microsoft.UI.Xaml.Controls;

public enum Orientation
{
    Vertical,
    Horizontal
}

public class Page : FrameworkElement
{
    public object? Content { get; set; }

    internal void RaiseNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
    {
        OnNavigatedTo(args);
    }

    protected virtual void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
    {
    }
}

public class UserControl : Control
{
    public object? Content { get; set; }
}

public class Control : FrameworkElement
{
    public bool IsEnabled { get; set; } = true;

    public IList<Microsoft.UI.Xaml.Input.KeyboardAccelerator> KeyboardAccelerators { get; } =
        new List<Microsoft.UI.Xaml.Input.KeyboardAccelerator>();
}

public class ContentControl : Control
{
    public object? Content { get; set; }
}

public class ContentPresenter : Control
{
    public object? Content { get; set; }
}

public class ItemsControl : Control
{
    public IList<object?> Items { get; } = new List<object?>();

    public object? ItemTemplate { get; set; }
}

public class Frame : Control
{
    public object? Content { get; set; }

    public Type? SourcePageType { get; private set; }

    public string? CurrentRoute { get; set; }

    public bool Navigate(Type sourcePageType)
    {
        return Navigate(sourcePageType, parameter: null);
    }

    public bool Navigate(Type sourcePageType, object? parameter)
    {
        ArgumentNullException.ThrowIfNull(sourcePageType);

        SourcePageType = sourcePageType;
        CurrentRoute = sourcePageType.Name;
        Content = Activator.CreateInstance(sourcePageType);
        if (Content is Page page)
        {
            page.RaiseNavigatedTo(new Microsoft.UI.Xaml.Navigation.NavigationEventArgs(sourcePageType, parameter));
        }

        return Content is not null;
    }
}

public class Button : Control
{
    public object? Content { get; set; }

    public object? Flyout { get; set; }

    public object? ContextFlyout { get; set; }

    public ICommand? Command { get; set; }

    public object? CommandParameter { get; set; }

    public event RoutedEventHandler? Click;

    public void PerformClick()
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        Click?.Invoke(this, new RoutedEventArgs());
    }
}

public class AppBarButton : Button
{
    public string? Label { get; set; }

    public object? Icon { get; set; }
}

public class ToggleButton : Button
{
    public bool? IsChecked { get; set; }
}

public class HyperlinkButton : Button
{
    public Uri? NavigateUri { get; set; }
}

public class DropDownButton : Button
{
}

public class SplitButton : Button
{
}

public class ToggleSplitButton : ToggleButton
{
}

public class CheckBox : ToggleButton
{
}

public class RadioButton : ToggleButton
{
    public string? GroupName { get; set; }
}

public class TextBlock : FrameworkElement
{
    public string? Text { get; set; }

    public TextWrapping TextWrapping { get; set; } = TextWrapping.NoWrap;

    public object? FontWeight { get; set; }
}

public class TextBox : Control
{
    public string? Text { get; set; }

    public string? PlaceholderText { get; set; }

    public TextWrapping TextWrapping { get; set; } = TextWrapping.NoWrap;

    public bool AcceptsReturn { get; set; }
}

public class PasswordBox : Control
{
    public string? Password { get; set; }

    public string? PlaceholderText { get; set; }

    public object? Header { get; set; }
}

public class Slider : Control
{
    public double Minimum { get; set; }

    public double Maximum { get; set; } = 100;

    public double Value { get; set; }
}

public class ToggleSwitch : Control
{
    public object? Header { get; set; }

    public bool IsOn { get; set; }
}

public class RatingControl : Control
{
    public int MaxRating { get; set; } = 5;

    public double Value { get; set; }
}

public enum Symbol
{
    Accept,
    Find,
    Link
}

public class SymbolIcon : Control
{
    public Symbol Symbol { get; set; }
}

public class Image : Control
{
    public object? Source { get; set; }
}

public class ListView : ItemsControl
{
    private int selectedIndex = -1;

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            selectedIndex = value;
            SelectedItem = value >= 0 && value < Items.Count ? Items[value] : null;
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs());
        }
    }

    public object? SelectedItem { get; set; }

    public bool IsItemClickEnabled { get; set; }

    public ListViewSelectionMode SelectionMode { get; set; } = ListViewSelectionMode.Single;

    public event SelectionChangedEventHandler? SelectionChanged;
}

public enum ListViewSelectionMode
{
    None,
    Single,
    Multiple,
    Extended
}

public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs args);

public sealed class SelectionChangedEventArgs : EventArgs
{
}

public class ComboBox : ItemsControl
{
    private int selectedIndex = -1;

    public string? PlaceholderText { get; set; }

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            selectedIndex = value;
            SelectedItem = value >= 0 && value < Items.Count ? Items[value] : null;
        }
    }

    public object? SelectedItem { get; set; }
}

public enum ScrollBarVisibility
{
    Disabled,
    Auto,
    Hidden,
    Visible
}

public class ScrollViewer : Control
{
    public object? Content { get; set; }

    public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; } = ScrollBarVisibility.Disabled;

    public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Auto;
}

public class ProgressRing : Control
{
    public bool IsActive { get; set; }
}

public class ProgressBar : Control
{
    public double Minimum { get; set; }

    public double Maximum { get; set; } = 100;

    public double Value { get; set; }

    public bool IsIndeterminate { get; set; }
}

public enum InfoBarSeverity
{
    Informational,
    Success,
    Warning,
    Error
}

public class InfoBar : Control
{
    public string? Title { get; set; }

    public string? Message { get; set; }

    public InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;

    public bool IsOpen { get; set; } = true;

    public bool IsClosable { get; set; } = true;
}

public class CommandBar : Control
{
    public object? Content { get; set; }

    public CommandBarDefaultLabelPosition DefaultLabelPosition { get; set; } = CommandBarDefaultLabelPosition.Bottom;

    public IList<object?> PrimaryCommands { get; } = new List<object?>();
}

public enum CommandBarDefaultLabelPosition
{
    Bottom,
    Right,
    Collapsed
}

public class AutoSuggestBox : Control
{
    public string? Text { get; set; }

    public object? QueryIcon { get; set; }

    public event RoutedEventHandler? QuerySubmitted;

    public event RoutedEventHandler? TextChanged;

    public void RaiseQuerySubmitted()
    {
        QuerySubmitted?.Invoke(this, new RoutedEventArgs());
    }

    public void RaiseTextChanged()
    {
        TextChanged?.Invoke(this, new RoutedEventArgs());
    }
}

public class Flyout : ContentControl
{
    public bool IsOpen { get; set; }
}

public class ToolTip : ContentControl
{
    public bool IsOpen { get; set; }
}

public class TeachingTip : ContentControl
{
    public string? Title { get; set; }

    public string? Subtitle { get; set; }

    public bool IsOpen { get; set; }
}

public class ContentDialog : ContentControl
{
    public object? Title { get; set; }

    public string? PrimaryButtonText { get; set; }

    public bool IsOpen { get; set; }

    public string? Result { get; private set; }

    public void Show()
    {
        IsOpen = true;
        Result = null;
    }

    public void Hide(string? result = null)
    {
        IsOpen = false;
        Result = result;
    }
}

public class MenuFlyout : ItemsControl
{
    public bool IsOpen { get; set; }

    public string? InvokedItem { get; set; }
}

public class CommandBarFlyout : Control
{
    public IList<object?> PrimaryCommands { get; } = new List<object?>();

    public IList<object?> SecondaryCommands { get; } = new List<object?>();

    public bool IsOpen { get; set; }

    public string? InvokedCommand { get; set; }
}

public class MenuFlyoutItem : Control
{
    public string? Text { get; set; }

    public event RoutedEventHandler? Click;

    public void PerformClick()
    {
        Click?.Invoke(this, new RoutedEventArgs());
    }
}

public class MenuBar : ItemsControl
{
}

public class MenuBarItem : ItemsControl
{
    public string? Title { get; set; }
}

public class Expander : Control
{
    public object? Header { get; set; }

    public object? Content { get; set; }

    public bool IsExpanded { get; set; }
}

public class AnnotatedScrollBar : Control
{
    public int MarkerCount { get; set; } = 3;
}

public class SemanticZoom : Control
{
    public object? ZoomedInView { get; set; }

    public object? ZoomedOutView { get; set; }
}

public class SplitView : Control
{
    public object? Pane { get; set; }

    public object? Content { get; set; }

    public bool IsPaneOpen { get; set; }
}

public class TwoPaneView : Control
{
    public object? Pane1 { get; set; }

    public object? Pane2 { get; set; }
}

public class StackPanel : FrameworkElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public string? Padding { get; set; }

    public double Spacing { get; set; }

    public IList<UIElement> Children { get; } = new List<UIElement>();
}

public class Grid : FrameworkElement
{
    private static readonly Dictionary<UIElement, int> Columns = new();
    private static readonly Dictionary<UIElement, int> Rows = new();
    private static readonly Dictionary<UIElement, int> ColumnSpans = new();

    public string? ColumnDefinitions { get; set; }

    public string? RowDefinitions { get; set; }

    public double ColumnSpacing { get; set; }

    public double RowSpacing { get; set; }

    public string? Padding { get; set; }

    public IList<UIElement> Children { get; } = new List<UIElement>();

    public static void SetColumn(UIElement element, int value)
    {
        ArgumentNullException.ThrowIfNull(element);

        Columns[element] = Math.Max(0, value);
    }

    public static int GetColumn(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return Columns.TryGetValue(element, out var value) ? value : 0;
    }

    public static void SetRow(UIElement element, int value)
    {
        ArgumentNullException.ThrowIfNull(element);

        Rows[element] = Math.Max(0, value);
    }

    public static int GetRow(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return Rows.TryGetValue(element, out var value) ? value : 0;
    }

    public static void SetColumnSpan(UIElement element, int value)
    {
        ArgumentNullException.ThrowIfNull(element);

        ColumnSpans[element] = Math.Max(1, value);
    }

    public static int GetColumnSpan(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return ColumnSpans.TryGetValue(element, out var value) ? value : 1;
    }
}

public class Border : FrameworkElement
{
    public object? Child { get; set; }

    public object? CornerRadius { get; set; }

    public string? Padding { get; set; }

    public object? BorderBrush { get; set; }

    public string? BorderThickness { get; set; }
}

public class FontIcon : Control
{
    public string? Glyph { get; set; }

    public double FontSize { get; set; } = 20;
}

public delegate void NavigationViewSelectionChangedEventHandler(
    NavigationView sender,
    NavigationViewSelectionChangedEventArgs args);

public sealed class NavigationViewSelectionChangedEventArgs : EventArgs
{
    public NavigationViewItem? SelectedItemContainer { get; init; }
}

public class NavigationView : Control
{
    public IList<object?> MenuItems { get; } = new List<object?>();

    public object? Content { get; set; }

    public object? PaneFooter { get; set; }

    public object? SelectedItem { get; set; }

    public double CompactPaneLength { get; set; }

    public double OpenPaneLength { get; set; }

    public bool IsSettingsVisible { get; set; } = true;

    public string? IsBackButtonVisible { get; set; }

    public string? PaneDisplayMode { get; set; }

    public event NavigationViewSelectionChangedEventHandler? SelectionChanged;

    public void Select(NavigationViewItem item)
    {
        foreach (var menuItem in MenuItems.OfType<NavigationViewItem>())
        {
            menuItem.IsSelected = ReferenceEquals(menuItem, item);
        }

        SelectedItem = item;
        SelectionChanged?.Invoke(this, new NavigationViewSelectionChangedEventArgs
        {
            SelectedItemContainer = item
        });
    }
}

public class NavigationViewItem : Control
{
    public object? Content { get; set; }

    public object? Icon { get; set; }

    public bool IsSelected { get; set; }
}
