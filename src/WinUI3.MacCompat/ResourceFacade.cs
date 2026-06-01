using System.Reflection;

namespace Microsoft.UI.Xaml;

public sealed record ResourceLookupFailure(
    string Key,
    string TargetProperty,
    string Status);

public static class ResourceOperations
{
    private static readonly object Gate = new();
    private static readonly List<ResourceLookupFailure> Failures = new();

    public static IReadOnlyList<ResourceLookupFailure> CurrentFailures
    {
        get
        {
            lock (Gate)
            {
                return Failures.ToArray();
            }
        }
    }

    public static void ClearFailures()
    {
        lock (Gate)
        {
            Failures.Clear();
        }
    }

    public static object? Resolve(ResourceDictionary resources, string key, string targetProperty)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProperty);

        if (resources.TryGetValue(key, out var value))
        {
            return value;
        }

        ReportMissing(key, targetProperty);
        return key;
    }

    public static string? ResolveString(ResourceDictionary resources, string key, string targetProperty)
    {
        return Resolve(resources, key, targetProperty)?.ToString();
    }

    public static Style? ResolveStyle(ResourceDictionary resources, string key, string targetProperty)
    {
        return Resolve(resources, key, targetProperty) as Style;
    }

    private static void ReportMissing(string key, string targetProperty)
    {
        lock (Gate)
        {
            if (Failures.Any(failure =>
                    failure.Key == key &&
                    failure.TargetProperty == targetProperty))
            {
                return;
            }

            Failures.Add(new ResourceLookupFailure(key, targetProperty, "missing"));
        }
    }
}

public static class StyleOperations
{
    public static void Apply(FrameworkElement element, Style? style)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (style is null)
        {
            return;
        }

        foreach (var setter in style.Setters)
        {
            var property = element.GetType().GetProperty(setter.Property, BindingFlags.Instance | BindingFlags.Public);
            if (property is null || !property.CanWrite)
            {
                continue;
            }

            property.SetValue(element, ConvertValue(setter.Value, property.PropertyType));
        }
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
}
