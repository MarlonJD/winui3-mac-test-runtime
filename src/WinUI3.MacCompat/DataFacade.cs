using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Data;

public sealed record Binding(string Path);

public sealed record BindingFailure(
    string ElementName,
    string ElementType,
    string PropertyName,
    string Path,
    string Message);

public static class BindingOperations
{
    private static readonly Dictionary<FrameworkElement, List<ElementBinding>> Bindings = new();
    private static readonly List<BindingFailure> Failures = new();

    public static IReadOnlyList<BindingFailure> CurrentFailures => Failures;

    public static void ClearFailures()
    {
        Failures.Clear();
    }

    public static void SetBinding(FrameworkElement element, string propertyName, Binding binding)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(binding);

        if (!Bindings.TryGetValue(element, out var bindings))
        {
            bindings = new List<ElementBinding>();
            Bindings[element] = bindings;
        }

        bindings.RemoveAll(existing => existing.PropertyName == propertyName);
        bindings.Add(new ElementBinding(propertyName, binding));
    }

    public static void RefreshTree(object root)
    {
        ClearFailures();
        RefreshElement(root, inheritedDataContext: null);
    }

    private static void RefreshElement(object? element, object? inheritedDataContext)
    {
        if (element is null)
        {
            return;
        }

        var dataContext = element switch
        {
            Window window => window.DataContext ?? inheritedDataContext,
            FrameworkElement frameworkElement => frameworkElement.DataContext ?? inheritedDataContext,
            _ => inheritedDataContext
        };

        if (element is FrameworkElement boundElement && Bindings.TryGetValue(boundElement, out var bindings))
        {
            foreach (var binding in bindings)
            {
                ApplyBinding(boundElement, binding, dataContext);
            }
        }

        foreach (var child in EnumerateChildren(element))
        {
            RefreshElement(child, dataContext);
        }
    }

    private static void ApplyBinding(FrameworkElement element, ElementBinding binding, object? dataContext)
    {
        if (dataContext is null)
        {
            Failures.Add(CreateFailure(element, binding, "DataContext is null."));
            return;
        }

        var value = ResolvePath(dataContext, binding.Binding.Path);
        if (!value.Found)
        {
            Failures.Add(CreateFailure(element, binding, $"Path '{binding.Binding.Path}' was not found."));
            return;
        }

        var property = element.GetType().GetProperty(binding.PropertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            Failures.Add(CreateFailure(element, binding, $"Property '{binding.PropertyName}' is not writable."));
            return;
        }

        property.SetValue(element, ConvertValue(value.Value, property.PropertyType));
    }

    private static (bool Found, object? Value) ResolvePath(object source, string path)
    {
        object? current = source;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is null)
            {
                return (false, null);
            }

            var property = current.GetType().GetProperty(segment, BindingFlags.Instance | BindingFlags.Public);
            if (property is null)
            {
                return (false, null);
            }

            current = property.GetValue(current);
        }

        return (true, current);
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var nonNullableTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (nonNullableTarget.IsInstanceOfType(value))
        {
            return value;
        }

        if (nonNullableTarget == typeof(string))
        {
            return value.ToString();
        }

        if (nonNullableTarget.IsEnum && value is string enumValue)
        {
            return Enum.Parse(nonNullableTarget, enumValue, ignoreCase: true);
        }

        return Convert.ChangeType(value, nonNullableTarget);
    }

    private static BindingFailure CreateFailure(FrameworkElement element, ElementBinding binding, string message)
    {
        return new BindingFailure(
            ElementName: element.Name ?? string.Empty,
            ElementType: element.GetType().FullName ?? element.GetType().Name,
            PropertyName: binding.PropertyName,
            Path: binding.Binding.Path,
            Message: message);
    }

    private static IEnumerable<object?> EnumerateChildren(object element)
    {
        return element switch
        {
            Window window => One(window.Content),
            Page page => One(page.Content),
            UserControl userControl => One(userControl.Content),
            Border border => One(border.Child),
            Button button => One(button.Content),
            Frame frame => One(frame.Content),
            StackPanel stackPanel => stackPanel.Children,
            Grid grid => grid.Children,
            NavigationView navigationView => navigationView.MenuItems.Concat(One(navigationView.PaneFooter)).Concat(One(navigationView.Content)),
            NavigationViewItem item => One(item.Content).Concat(One(item.Icon)),
            ListView listView => listView.Items,
            _ => Array.Empty<object?>()
        };
    }

    private static IEnumerable<object?> One(object? value)
    {
        if (value is not null)
        {
            yield return value;
        }
    }

    private sealed record ElementBinding(string PropertyName, Binding Binding);
}
