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
}

public class Window
{
    public string? Title { get; set; }

    public object? Content { get; set; }

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
