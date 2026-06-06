using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Data;

public enum BindingMode
{
    OneWay,
    TwoWay
}

public sealed record Binding(string Path, BindingMode Mode = BindingMode.OneWay);

public sealed record BindingFailure(
    string ElementName,
    string ElementType,
    string PropertyName,
    string Path,
    string Message);

public static class BindingOperations
{
    private static readonly Dictionary<FrameworkElement, List<ElementBinding>> Bindings = new();
    private static readonly Dictionary<FrameworkElement, object?> LastDataContexts = new();
    private static readonly HashSet<string> PropertySubscriptions = new(StringComparer.Ordinal);
    private static readonly HashSet<string> CollectionSubscriptions = new(StringComparer.Ordinal);
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

        if (element is FrameworkElement frameworkElementWithContext)
        {
            LastDataContexts[frameworkElementWithContext] = dataContext;
        }

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

        SubscribePropertyChanged(element, binding, dataContext);

        var property = element.GetType().GetProperty(binding.PropertyName, BindingFlags.Instance | BindingFlags.Public);
        if (element is ItemsControl itemsControl && binding.PropertyName == nameof(ItemsControl.Items))
        {
            ApplyItemsBinding(itemsControl, binding, value.Value);
            return;
        }

        if (property is null || !property.CanWrite)
        {
            Failures.Add(CreateFailure(element, binding, $"Property '{binding.PropertyName}' is not writable."));
            return;
        }

        property.SetValue(element, ConvertValue(value.Value, property.PropertyType));
    }

    public static void UpdateSource(FrameworkElement element, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (!Bindings.TryGetValue(element, out var bindings))
        {
            return;
        }

        var binding = bindings.FirstOrDefault(candidate =>
            candidate.PropertyName == propertyName &&
            candidate.Binding.Mode == BindingMode.TwoWay);
        if (binding is null || !LastDataContexts.TryGetValue(element, out var dataContext) || dataContext is null)
        {
            return;
        }

        var property = element.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanRead)
        {
            Failures.Add(CreateFailure(element, binding, $"Property '{propertyName}' is not readable."));
            return;
        }

        if (!SetPathValue(dataContext, binding.Binding.Path, property.GetValue(element)))
        {
            Failures.Add(CreateFailure(element, binding, $"Path '{binding.Binding.Path}' is not writable."));
        }
    }

    private static void ApplyItemsBinding(ItemsControl itemsControl, ElementBinding binding, object? value)
    {
        itemsControl.Items.Clear();
        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (var item in enumerable)
            {
                itemsControl.Items.Add(item);
            }
        }
        else if (value is not null)
        {
            itemsControl.Items.Add(value);
        }

        if (value is INotifyCollectionChanged collectionChanged)
        {
            SubscribeCollectionChanged(itemsControl, binding, collectionChanged, value);
        }
    }

    private static void SubscribePropertyChanged(FrameworkElement element, ElementBinding binding, object dataContext)
    {
        if (dataContext is not INotifyPropertyChanged propertyChanged)
        {
            return;
        }

        var key = $"{RuntimeHelpers.GetHashCode(dataContext)}:{RuntimeHelpers.GetHashCode(element)}:{binding.PropertyName}:{binding.Binding.Path}";
        if (!PropertySubscriptions.Add(key))
        {
            return;
        }

        propertyChanged.PropertyChanged += (_, args) =>
        {
            var firstSegment = binding.Binding.Path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(args.PropertyName) || args.PropertyName == firstSegment)
            {
                ApplyBinding(element, binding, dataContext);
            }
        };
    }

    private static void SubscribeCollectionChanged(ItemsControl itemsControl, ElementBinding binding, INotifyCollectionChanged collectionChanged, object source)
    {
        var key = $"{RuntimeHelpers.GetHashCode(source)}:{RuntimeHelpers.GetHashCode(itemsControl)}:{binding.PropertyName}:{binding.Binding.Path}";
        if (!CollectionSubscriptions.Add(key))
        {
            return;
        }

        collectionChanged.CollectionChanged += (_, _) => ApplyItemsBinding(itemsControl, binding, source);
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

    private static bool SetPathValue(object source, string path, object? value)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        object? current = source;
        foreach (var segment in segments[..^1])
        {
            if (current is null)
            {
                return false;
            }

            var nested = current.GetType().GetProperty(segment, BindingFlags.Instance | BindingFlags.Public);
            if (nested is null)
            {
                return false;
            }

            current = nested.GetValue(current);
        }

        if (current is null)
        {
            return false;
        }

        var property = current.GetType().GetProperty(segments[^1], BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            return false;
        }

        property.SetValue(current, ConvertValue(value, property.PropertyType));
        return true;
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
            ContentControl contentControl => One(contentControl.Content),
            ScrollViewer scrollViewer => One(scrollViewer.Content),
            Border border => One(border.Child),
            Button button => One(button.Content),
            Frame frame => One(frame.Content),
            StackPanel stackPanel => stackPanel.Children,
            Grid grid => grid.Children,
            NavigationView navigationView => navigationView.MenuItems.Concat(One(navigationView.PaneFooter)).Concat(One(navigationView.Content)),
            NavigationViewItem item => One(item.Content).Concat(One(item.Icon)),
            CommandBar commandBar => One(commandBar.Content).Concat(commandBar.PrimaryCommands),
            ItemsControl itemsControl => itemsControl.Items,
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
