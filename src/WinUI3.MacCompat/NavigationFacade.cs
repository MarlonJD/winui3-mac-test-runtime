namespace Microsoft.UI.Xaml.Navigation;

public sealed class NavigationEventArgs : EventArgs
{
    public NavigationEventArgs(Type sourcePageType, object? parameter)
    {
        SourcePageType = sourcePageType;
        Parameter = parameter;
    }

    public Type SourcePageType { get; }

    public object? Parameter { get; }
}
