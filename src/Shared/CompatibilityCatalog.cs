using System.Reflection;
using System.Text.Json;

namespace WinUI3.MacCompatibility;

public static class CompatibilityStatuses
{
    public const string Supported = "supported";
    public const string Partial = "partial";
    public const string Planned = "planned";
    public const string WindowsOnly = "windows-only";
    public const string NotSupported = "not supported";
    public const string Unknown = "unknown";

    public static bool IsAvailableOnMac(string status)
    {
        return status is Supported or Partial;
    }

    public static bool IsKnownStatus(string status)
    {
        return status is Supported or Partial or Planned or WindowsOnly or NotSupported;
    }
}

public sealed record CompatibilityCatalogDocument(
    string SchemaVersion,
    string GeneratedFrom,
    CompatibilityCatalogEntry[] Entries);

public sealed record CompatibilityCatalogEntry(
    string Id,
    string Kind,
    string Status,
    string Area,
    string Api,
    string? XamlElement,
    string? XamlProperty,
    string? ParityStage,
    string? Notes);

public sealed class CompatibilityCatalog
{
    private const string ResourceSuffix = "winui-api-compatibility.catalog.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<CompatibilityCatalog> CurrentCatalog = new(Load);

    private readonly Dictionary<string, CompatibilityCatalogEntry> entriesByApi;
    private readonly Dictionary<string, CompatibilityCatalogEntry> entriesById;

    private CompatibilityCatalog(CompatibilityCatalogDocument document)
    {
        Document = document;
        entriesByApi = document.Entries
            .GroupBy(entry => entry.Api, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        entriesById = document.Entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
    }

    public static CompatibilityCatalog Current => CurrentCatalog.Value;

    public CompatibilityCatalogDocument Document { get; }

    public IReadOnlyList<CompatibilityCatalogEntry> Entries => Document.Entries;

    public CompatibilityCatalogEntry? FindByApi(string api)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(api);

        return entriesByApi.TryGetValue(api, out var entry) ? entry : null;
    }

    public CompatibilityCatalogEntry? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return entriesById.TryGetValue(id, out var entry) ? entry : null;
    }

    public CompatibilityCatalogEntry? FindXamlAttachedProperty(string propertyName)
    {
        return FindFirst("xaml-attached-property", propertyName: propertyName);
    }

    public CompatibilityCatalogEntry? FindXamlDirective(string directiveName)
    {
        return Entries.FirstOrDefault(entry =>
            entry.Kind == "xaml-directive" &&
            string.Equals(entry.Api, directiveName, StringComparison.Ordinal));
    }

    public CompatibilityCatalogEntry? FindXamlElement(string elementName)
    {
        return FindFirst("xaml-element", elementName: elementName);
    }

    public CompatibilityCatalogEntry? FindXamlEvent(string elementName, string eventName)
    {
        return FindFirst("xaml-event", elementName, eventName);
    }

    public CompatibilityCatalogEntry? FindXamlProperty(string elementName, string propertyName)
    {
        return FindFirst("xaml-property", elementName, propertyName) ??
            FindFirst("xaml-property", propertyName: propertyName);
    }

    public CompatibilityCatalogEntry? FindXamlPropertyElement(string elementName, string propertyName)
    {
        return FindFirst("xaml-property-element", elementName, propertyName) ??
            FindFirst("xaml-property-element", propertyName: propertyName);
    }

    public string StatusForApi(string api)
    {
        return FindByApi(api)?.Status ?? CompatibilityStatuses.Unknown;
    }

    private static CompatibilityCatalog Load()
    {
        var assembly = typeof(CompatibilityCatalog).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));
        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded compatibility catalog resource '{ResourceSuffix}' was not found.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded compatibility catalog resource '{resourceName}' could not be opened.");
        var document = JsonSerializer.Deserialize<CompatibilityCatalogDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Compatibility catalog could not be deserialized.");
        Validate(document);
        return new CompatibilityCatalog(document);
    }

    private static void Validate(CompatibilityCatalogDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.SchemaVersion))
        {
            throw new InvalidOperationException("Compatibility catalog must declare schemaVersion.");
        }

        var duplicate = document.Entries
            .GroupBy(entry => entry.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Compatibility catalog contains duplicate id '{duplicate.Key}'.");
        }

        foreach (var entry in document.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) ||
                string.IsNullOrWhiteSpace(entry.Kind) ||
                string.IsNullOrWhiteSpace(entry.Area) ||
                string.IsNullOrWhiteSpace(entry.Api))
            {
                throw new InvalidOperationException("Compatibility catalog entries must declare id, kind, area, and api.");
            }

            if (!CompatibilityStatuses.IsKnownStatus(entry.Status))
            {
                throw new InvalidOperationException($"Compatibility catalog entry '{entry.Id}' uses unknown status '{entry.Status}'.");
            }
        }
    }

    private CompatibilityCatalogEntry? FindFirst(
        string kind,
        string? elementName = null,
        string? propertyName = null)
    {
        return Entries.FirstOrDefault(entry =>
            entry.Kind == kind &&
            (elementName is null || string.Equals(entry.XamlElement, elementName, StringComparison.Ordinal)) &&
            (propertyName is null || string.Equals(entry.XamlProperty, propertyName, StringComparison.Ordinal)));
    }
}
