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
    private static string currentTheme = "Light";

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

    public static string CurrentTheme
    {
        get
        {
            lock (Gate)
            {
                return currentTheme;
            }
        }
    }

    public static void SetTheme(string? theme)
    {
        lock (Gate)
        {
            currentTheme = ThemeDictionaryKey(theme);
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

        if (TryResolveValue(resources, key, out var value))
        {
            return value;
        }

        var applicationResources = Application.Current?.Resources;
        if (applicationResources is not null &&
            !ReferenceEquals(applicationResources, resources) &&
            TryResolveValue(applicationResources, key, out var applicationValue))
        {
            return applicationValue;
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

    private static bool TryResolveValue(ResourceDictionary resources, string key, out object? value)
    {
        if (resources.ThemeDictionaries.TryGetValue(CurrentTheme, out var themedResources) &&
            themedResources.TryGetValue(key, out value))
        {
            return true;
        }

        if (resources.ThemeDictionaries.TryGetValue("Default", out var defaultResources) &&
            defaultResources.TryGetValue(key, out value))
        {
            return true;
        }

        if (resources.TryGetValue(key, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    private static string ThemeDictionaryKey(string? theme)
    {
        if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return "Dark";
        }

        if (string.Equals(theme, "high-contrast", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(theme, "HighContrast", StringComparison.OrdinalIgnoreCase))
        {
            return "HighContrast";
        }

        return "Light";
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

        if (value is string resourceReference &&
            TryReadResourceReference(resourceReference, out var resourceKey) &&
            Application.Current?.Resources is { } applicationResources)
        {
            value = ResourceOperations.Resolve(applicationResources, resourceKey, "Style.Setter");
            if (value is null)
            {
                return null;
            }

            if (value is not null && nonNullableTarget.IsInstanceOfType(value))
            {
                return value;
            }
        }

        if (nonNullableTarget == typeof(string))
        {
            return value?.ToString();
        }

        if (nonNullableTarget.IsEnum && value is string enumValue)
        {
            return Enum.Parse(nonNullableTarget, enumValue, ignoreCase: true);
        }

        return Convert.ChangeType(value, nonNullableTarget);
    }

    private static bool TryReadResourceReference(string value, out string key)
    {
        if ((value.StartsWith("{StaticResource ", StringComparison.Ordinal) ||
                value.StartsWith("{ThemeResource ", StringComparison.Ordinal)) &&
            value.EndsWith('}'))
        {
            var markerLength = value.StartsWith("{StaticResource ", StringComparison.Ordinal)
                ? "{StaticResource ".Length
                : "{ThemeResource ".Length;
            key = value[markerLength..^1].Trim();
            return !string.IsNullOrWhiteSpace(key);
        }

        key = string.Empty;
        return false;
    }
}
