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
