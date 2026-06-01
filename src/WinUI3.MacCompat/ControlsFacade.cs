using Microsoft.UI.Xaml;

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

public class Frame : Control
{
    public object? Content { get; set; }

    public Type? SourcePageType { get; private set; }

    public bool Navigate(Type sourcePageType)
    {
        return Navigate(sourcePageType, parameter: null);
    }

    public bool Navigate(Type sourcePageType, object? parameter)
    {
        ArgumentNullException.ThrowIfNull(sourcePageType);

        SourcePageType = sourcePageType;
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

    public event RoutedEventHandler? Click;

    public void PerformClick()
    {
        Click?.Invoke(this, new RoutedEventArgs());
    }
}

public class TextBlock : FrameworkElement
{
    public string? Text { get; set; }
}

public class TextBox : Control
{
    public string? Text { get; set; }
}

public class Image : Control
{
    public object? Source { get; set; }
}

public class ListView : Control
{
    public IList<object?> Items { get; } = new List<object?>();
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

    public string? ColumnDefinitions { get; set; }

    public double ColumnSpacing { get; set; }

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
}

public class Border : FrameworkElement
{
    public object? Child { get; set; }
}

public class FontIcon : Control
{
    public string? Glyph { get; set; }

    public double FontSize { get; set; }
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
}
