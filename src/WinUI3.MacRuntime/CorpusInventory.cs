using System.Text.Json;
using System.Xml.Linq;
using WinUI3.MacCompatibility;

namespace WinUI3.MacRuntime;

/// <summary>
/// Deterministic, classification-only inventory of the public WinUI 3 application corpus.
/// Discovery never executes corpus apps; it parses project metadata and XAML and classifies every
/// discovered surface against the shared <see cref="CompatibilityCatalog"/>. Surfaces that the
/// catalog does not classify are reported as <c>unknown</c> so the corpus tracks new public surface.
/// </summary>
public sealed record CorpusManifestDocument(
    string SchemaVersion,
    string Description,
    IReadOnlyList<CorpusManifestApp> Apps);

public sealed record CorpusManifestApp(
    string Id,
    string Name,
    string Project,
    string Category,
    string Tier,
    string Provenance,
    string Owner,
    string ExitCriteria,
    string? Description);

public sealed record CorpusInventoryDocument(
    string SchemaVersion,
    string GeneratedFrom,
    IReadOnlyList<string> Apps,
    IReadOnlyList<CorpusInventoryEntry> Entries);

public sealed record CorpusInventoryEntry(
    string Kind,
    string Construct,
    string Status,
    string? CatalogId,
    IReadOnlyList<string> UsedBy);

public sealed record CorpusUnknownDocument(
    string SchemaVersion,
    string GeneratedFrom,
    IReadOnlyList<CorpusInventoryEntry> Entries);

public sealed record CorpusSummaryDocument(
    string SchemaVersion,
    string GeneratedFrom,
    int AppCount,
    int EntryCount,
    int UnknownCount,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyList<CorpusAppSummary> Apps);

public sealed record CorpusAppSummary(
    string Id,
    string Category,
    string Tier,
    string Provenance,
    string IngestionStatus,
    string? TargetFramework,
    int XamlFileCount,
    int SourceFileCount,
    IReadOnlyList<CorpusAsset> Assets,
    int XamlDiagnosticCount,
    int UnsupportedFeatureCount);

public sealed record CorpusAsset(string Path, string Extension);

public sealed record CorpusInventoryResult(
    CorpusInventoryDocument Inventory,
    CorpusUnknownDocument Unknown,
    CorpusSummaryDocument Summary);

public sealed class CorpusInventoryService
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    private readonly ProjectIngestionService ingestionService = new();

    public CorpusInventoryResult Generate(string manifestPath, string configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        var resolvedManifest = Path.GetFullPath(manifestPath);
        var manifestDirectory = Path.GetDirectoryName(resolvedManifest)!;
        var manifest = JsonSerializer.Deserialize<CorpusManifestDocument>(
            File.ReadAllText(resolvedManifest),
            JsonDefaults.Options) ?? throw new InvalidOperationException($"Corpus manifest could not be read: {resolvedManifest}");

        var generatedFrom = Path.GetFileName(resolvedManifest);
        var discoveries = new Dictionary<string, DiscoveredConstruct>(StringComparer.Ordinal);
        var summaries = new List<CorpusAppSummary>();

        foreach (var app in manifest.Apps.OrderBy(app => app.Id, StringComparer.Ordinal))
        {
            var projectPath = Path.GetFullPath(Path.Combine(manifestDirectory, app.Project));
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException($"Corpus app '{app.Id}' project was not found.", projectPath);
            }

            var report = ingestionService.Inspect(projectPath, configuration)
                ?? throw new InvalidOperationException(
                    $"Corpus app '{app.Id}' is not a Windows-targeted WinUI project: {projectPath}");

            var projectDirectory = Path.GetDirectoryName(report.SourceProjectPath)!;
            CollectProjectConstructs(app.Id, report, discoveries);
            var xamlFiles = report.IncludedFiles.Where(file => file.Kind == "xaml").ToArray();
            foreach (var xamlFile in xamlFiles)
            {
                ScanXaml(app.Id, Path.Combine(projectDirectory, xamlFile.Path), discoveries);
            }

            summaries.Add(new CorpusAppSummary(
                Id: app.Id,
                Category: app.Category,
                Tier: app.Tier,
                Provenance: app.Provenance,
                IngestionStatus: report.Status,
                TargetFramework: report.TargetFramework,
                XamlFileCount: xamlFiles.Length,
                SourceFileCount: report.IncludedFiles.Count(file => file.Kind == "compile"),
                Assets: CollectAssets(projectDirectory),
                XamlDiagnosticCount: report.XamlDiagnostics.Count,
                UnsupportedFeatureCount: report.UnsupportedFeatures.Count));
        }

        var entries = discoveries.Values
            .Select(discovery => new CorpusInventoryEntry(
                discovery.Kind,
                discovery.Construct,
                discovery.Status,
                discovery.CatalogId,
                discovery.UsedBy.OrderBy(id => id, StringComparer.Ordinal).ToArray()))
            .OrderBy(entry => entry.Kind, StringComparer.Ordinal)
            .ThenBy(entry => entry.Construct, StringComparer.Ordinal)
            .ToArray();
        var appIds = manifest.Apps
            .Select(app => app.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var statusCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            statusCounts[entry.Status] = statusCounts.GetValueOrDefault(entry.Status) + 1;
        }

        var inventory = new CorpusInventoryDocument(ArtifactSchemas.CorpusInventory, generatedFrom, appIds, entries);
        var unknown = new CorpusUnknownDocument(
            ArtifactSchemas.CorpusInventory,
            generatedFrom,
            entries.Where(entry => entry.Status == CompatibilityStatuses.Unknown).ToArray());
        var summary = new CorpusSummaryDocument(
            ArtifactSchemas.CorpusSummary,
            generatedFrom,
            AppCount: appIds.Length,
            EntryCount: entries.Length,
            UnknownCount: unknown.Entries.Count,
            StatusCounts: statusCounts,
            Apps: summaries.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray());

        return new CorpusInventoryResult(inventory, unknown, summary);
    }

    private static void CollectProjectConstructs(
        string appId,
        ProjectIngestionReport report,
        Dictionary<string, DiscoveredConstruct> discoveries)
    {
        foreach (var status in report.CatalogStatuses)
        {
            Record(discoveries, appId, status.Kind, status.Api, status.Status, status.Id);
        }

        foreach (var feature in report.UnsupportedFeatures)
        {
            Record(discoveries, appId, feature.Kind, feature.Api, feature.Status, feature.Id);
        }
    }

    private void ScanXaml(string appId, string xamlPath, Dictionary<string, DiscoveredConstruct> discoveries)
    {
        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"Corpus app '{appId}' XAML file was not found.", xamlPath);
        }

        var document = XDocument.Load(xamlPath);
        foreach (var element in document.Descendants())
        {
            var localName = element.Name.LocalName;
            if (localName.Contains('.', StringComparison.Ordinal))
            {
                ClassifyPropertyElement(appId, localName, discoveries);
            }
            else
            {
                ClassifyElement(appId, localName, discoveries);
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    continue;
                }

                ClassifyAttribute(appId, element.Name.LocalName, attribute, discoveries);
                ClassifyMarkup(appId, attribute.Value, discoveries);
            }
        }
    }

    private static void ClassifyElement(string appId, string localName, Dictionary<string, DiscoveredConstruct> discoveries)
    {
        var catalog = CompatibilityCatalog.Current;
        var element = catalog.FindXamlElement(localName);
        if (element is not null)
        {
            Record(discoveries, appId, element.Kind, localName, element.Status, element.Id);
            return;
        }

        var mappedId = localName switch
        {
            "Application" => "api:Microsoft.UI.Xaml.Application",
            "Style" or "Setter" => "xaml-resource:Style.Setter",
            "SolidColorBrush" => "xaml-resource:SolidColorBrush",
            "XamlControlsResources" => "xaml-resource:XamlControlsResources",
            _ => null
        };
        RecordById(discoveries, appId, mappedId, fallbackKind: "xaml-element", construct: localName);
    }

    private static void ClassifyPropertyElement(string appId, string localName, Dictionary<string, DiscoveredConstruct> discoveries)
    {
        var mappedId = localName switch
        {
            "ResourceDictionary.MergedDictionaries" => "xaml-resource:ResourceDictionary.MergedDictionaries",
            "ResourceDictionary.ThemeDictionaries" => "xaml-resource:ResourceDictionary.ThemeDictionaries",
            _ => null
        };
        if (mappedId is not null)
        {
            RecordById(discoveries, appId, mappedId, fallbackKind: "xaml-resource", construct: localName);
        }
    }

    private static void ClassifyAttribute(
        string appId,
        string elementLocalName,
        XAttribute attribute,
        Dictionary<string, DiscoveredConstruct> discoveries)
    {
        var catalog = CompatibilityCatalog.Current;
        var localName = attribute.Name.LocalName;
        if (attribute.Name.Namespace == XamlNamespace)
        {
            var directive = catalog.FindXamlDirective($"x:{localName}");
            RecordEntryOrUnknown(discoveries, appId, "xaml-directive", $"x:{localName}", directive);
            return;
        }

        if (localName.Contains('.', StringComparison.Ordinal))
        {
            var attached = catalog.FindXamlAttachedProperty(localName);
            RecordEntryOrUnknown(discoveries, appId, "xaml-attached-property", localName, attached);
            return;
        }

        var catalogEvent = catalog.FindXamlEvent(elementLocalName, localName);
        if (catalogEvent is not null)
        {
            Record(discoveries, appId, catalogEvent.Kind, $"{elementLocalName}.{localName}", catalogEvent.Status, catalogEvent.Id);
        }
    }

    private static void ClassifyMarkup(string appId, string value, Dictionary<string, DiscoveredConstruct> discoveries)
    {
        foreach (var (token, id) in MarkupExtensions)
        {
            if (value.Contains(token, StringComparison.Ordinal))
            {
                RecordById(discoveries, appId, id, fallbackKind: "xaml-markup", construct: token[1..]);
            }
        }
    }

    private static readonly (string Token, string Id)[] MarkupExtensions =
    {
        ("{StaticResource", "xaml-resource:StaticResource"),
        ("{ThemeResource", "xaml-resource:ThemeResource"),
        ("{Binding", "xaml-markup:Binding")
    };

    private static void RecordEntryOrUnknown(
        Dictionary<string, DiscoveredConstruct> discoveries,
        string appId,
        string kind,
        string construct,
        CompatibilityCatalogEntry? entry)
    {
        if (entry is not null)
        {
            Record(discoveries, appId, entry.Kind, construct, entry.Status, entry.Id);
        }
        else
        {
            Record(discoveries, appId, kind, construct, CompatibilityStatuses.Unknown, catalogId: null);
        }
    }

    private static void RecordById(
        Dictionary<string, DiscoveredConstruct> discoveries,
        string appId,
        string? catalogId,
        string fallbackKind,
        string construct)
    {
        if (catalogId is null)
        {
            Record(discoveries, appId, fallbackKind, construct, CompatibilityStatuses.Unknown, catalogId: null);
            return;
        }

        var entry = CompatibilityCatalog.Current.FindById(catalogId)
            ?? throw new InvalidOperationException($"Corpus inventory references missing catalog entry '{catalogId}'.");
        Record(discoveries, appId, entry.Kind, construct, entry.Status, entry.Id);
    }

    private static void Record(
        Dictionary<string, DiscoveredConstruct> discoveries,
        string appId,
        string kind,
        string construct,
        string status,
        string? catalogId)
    {
        var key = $"{kind} {construct}";
        if (!discoveries.TryGetValue(key, out var discovery))
        {
            discovery = new DiscoveredConstruct(kind, construct, status, catalogId);
            discoveries[key] = discovery;
        }

        discovery.UsedBy.Add(appId);
    }

    private static IReadOnlyList<CorpusAsset> CollectAssets(string projectDirectory)
    {
        var assets = new List<CorpusAsset>();
        foreach (var path in Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(projectDirectory, path).Replace('\\', '/');
            if (relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase) ||
                relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase) ||
                relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".cs" or ".xaml" or ".csproj" or ".user" or ".sln")
            {
                continue;
            }

            assets.Add(new CorpusAsset(relative, string.IsNullOrEmpty(extension) ? "(none)" : extension));
        }

        return assets
            .OrderBy(asset => asset.Path, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class DiscoveredConstruct
    {
        public DiscoveredConstruct(string kind, string construct, string status, string? catalogId)
        {
            Kind = kind;
            Construct = construct;
            Status = status;
            CatalogId = catalogId;
        }

        public string Kind { get; }

        public string Construct { get; }

        public string Status { get; }

        public string? CatalogId { get; }

        public HashSet<string> UsedBy { get; } = new(StringComparer.Ordinal);
    }
}
