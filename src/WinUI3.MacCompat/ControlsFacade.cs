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
}

public class Control : FrameworkElement
{
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

public class StackPanel : FrameworkElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public IList<UIElement> Children { get; } = new List<UIElement>();
}
