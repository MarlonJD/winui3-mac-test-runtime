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

public abstract class DependencyObject
{
}

public class UIElement : DependencyObject
{
}

public class FrameworkElement : UIElement
{
    public string? Name { get; set; }

    public object? DataContext { get; set; }

    public object? Tag { get; set; }

    public ResourceDictionary Resources { get; set; } = new();

    public Visibility Visibility { get; set; } = Visibility.Visible;

    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;

    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;

    public object? Background { get; set; }

    public object? Foreground { get; set; }

    public object? Style { get; set; }

    public bool IsFocused { get; private set; }

    public void Focus(FocusState focusState)
    {
        IsFocused = true;
    }
}

public class Window
{
    public string? Title { get; set; }

    public object? Content { get; set; }

    public ResourceDictionary Resources { get; set; } = new();

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
}

public enum FocusState
{
    Programmatic
}
