namespace Microsoft.UI.Xaml;

public delegate void RoutedEventHandler(object sender, RoutedEventArgs args);

public class RoutedEventArgs : EventArgs
{
}

public sealed class ApplicationInitializationCallbackParams
{
}

public sealed class LaunchActivatedEventArgs : EventArgs
{
    public string? Arguments { get; init; }
}

public enum Visibility
{
    Visible,
    Collapsed
}

public enum HorizontalAlignment
{
    Left,
    Center,
    Right,
    Stretch
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom,
    Stretch
}

public enum TextWrapping
{
    NoWrap,
    Wrap
}

public abstract class DependencyObject
{
}

public class UIElement : DependencyObject
{
}

public class FrameworkElement : UIElement
{
    public string? Name { get; set; }

    public string? Uid { get; set; }

    public object? DataContext { get; set; }

    public object? Tag { get; set; }

    public ResourceDictionary Resources { get; set; } = new();

    public Visibility Visibility { get; set; } = Visibility.Visible;

    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;

    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;

    public double Width { get; set; } = double.NaN;

    public double Height { get; set; } = double.NaN;

    public double MinWidth { get; set; }

    public double MinHeight { get; set; }

    public double MaxWidth { get; set; } = double.PositiveInfinity;

    public double MaxHeight { get; set; } = double.PositiveInfinity;

    public object? Background { get; set; }

    public object? Foreground { get; set; }

    public object? Style { get; set; }

    public bool IsFocused { get; private set; }

    public event RoutedEventHandler? SizeChanged;

    public void Focus(FocusState focusState)
    {
        IsFocused = true;
    }

    public void RaiseSizeChanged()
    {
        SizeChanged?.Invoke(this, new RoutedEventArgs());
    }
}

public class DataTemplate : DependencyObject
{
    public object? Content { get; set; }
}

public class Window
{
    public string? Title { get; set; }

    public object? Content { get; set; }

    public object? DataContext { get; set; }

    public ResourceDictionary Resources { get; set; } = new();

    public object? SystemBackdrop { get; set; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }
}

public abstract class Application
{
    protected Application()
    {
        Current = this;
    }

    public static Application? Current { get; private set; }

    public Window? MainWindow { get; protected set; }

    public ResourceDictionary Resources { get; } = new();

    public static void Start(Func<ApplicationInitializationCallbackParams, Application> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var app = callback(new ApplicationInitializationCallbackParams());
        app.Launch();
    }

    public void Launch(string? arguments = null)
    {
        OnLaunched(new LaunchActivatedEventArgs { Arguments = arguments });
    }

    protected virtual void OnLaunched(LaunchActivatedEventArgs args)
    {
    }
}

public class ResourceDictionary : Dictionary<string, object?>
{
    public IDictionary<string, ResourceDictionary> ThemeDictionaries { get; } =
        new Dictionary<string, ResourceDictionary>(StringComparer.OrdinalIgnoreCase);
}

public sealed class Style
{
    public string? TargetType { get; set; }

    public IList<Setter> Setters { get; } = new List<Setter>();

    public override string ToString()
    {
        return TargetType is null ? "Style" : $"Style({TargetType})";
    }
}

public sealed record Setter(string Property, object? Value);

public enum FocusState
{
    Programmatic
}
