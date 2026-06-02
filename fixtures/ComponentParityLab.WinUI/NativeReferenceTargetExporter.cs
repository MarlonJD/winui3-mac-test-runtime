using Microsoft.UI.Xaml;

#if WINDOWS
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
#endif

namespace ComponentParityLab.WinUI;

internal static class NativeReferenceTargetExporter
{
    private const string OutputEnvironmentVariable = "WINUI3_MAC_NATIVE_REFERENCE_TARGETS_OUTPUT";

    public static void ExportIfRequested(FrameworkElement root, NativeLaunchOptions launchOptions)
    {
#if WINDOWS
        var outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        _ = root.DispatcherQueue.TryEnqueue(() =>
        {
            root.UpdateLayout();
            _ = root.DispatcherQueue.TryEnqueue(() =>
            {
                root.UpdateLayout();
                WriteExport(outputPath, root, launchOptions);
            });
        });
#endif
    }

#if WINDOWS
    private static void WriteExport(string outputPath, FrameworkElement root, NativeLaunchOptions launchOptions)
    {
        var scenarioTargets = LoadScenarioTargets(launchOptions.ScenarioPath);
        var candidates = new Dictionary<string, TargetCandidate>(StringComparer.Ordinal);
        CollectTargets(root, root, scenarioTargets, candidates);

        var document = new NativeReferenceTargetDocument
        {
            ScenarioName = launchOptions.ScenarioName,
            ScenarioPath = NormalizePath(launchOptions.ScenarioPath),
            FixtureProjectPath = "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            Theme = launchOptions.Theme,
            Viewport = new SizeRecord(launchOptions.ViewportWidth, launchOptions.ViewportHeight),
            Scale = launchOptions.Scale,
            RootBounds = new BoundsRecord(0, 0, root.ActualWidth, root.ActualHeight),
            Targets = candidates.Values
                .OrderBy(candidate => candidate.Export.Target, StringComparer.Ordinal)
                .Select(candidate => candidate.Export)
                .ToList()
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(
                document,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
    }

    private static IReadOnlyDictionary<string, string> LoadScenarioTargets(string? scenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        if (!document.RootElement.TryGetProperty("requirements", out var requirements) ||
            requirements.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var targets = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var requirement in requirements.EnumerateArray())
        {
            var target = ReadString(requirement, "target");
            var component = ReadString(requirement, "component");
            if (!string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(component))
            {
                targets[target] = component;
            }
        }

        return targets;
    }

    private static void CollectTargets(
        DependencyObject node,
        FrameworkElement root,
        IReadOnlyDictionary<string, string> scenarioTargets,
        IDictionary<string, TargetCandidate> candidates)
    {
        if (node is FrameworkElement element)
        {
            var automationId = AutomationProperties.GetAutomationId(element);
            TryAddCandidate(automationId, "automation-id", 0, element, root, scenarioTargets, candidates);
            TryAddCandidate(element.Name, "x:Name", 1, element, root, scenarioTargets, candidates);
        }

        var childCount = VisualTreeHelper.GetChildrenCount(node);
        for (var index = 0; index < childCount; index++)
        {
            CollectTargets(VisualTreeHelper.GetChild(node, index), root, scenarioTargets, candidates);
        }
    }

    private static void TryAddCandidate(
        string? target,
        string identitySource,
        int rank,
        FrameworkElement element,
        FrameworkElement root,
        IReadOnlyDictionary<string, string> scenarioTargets,
        IDictionary<string, TargetCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(target) ||
            element.ActualWidth <= 0 ||
            element.ActualHeight <= 0 ||
            element.Visibility != Visibility.Visible)
        {
            return;
        }

        var bounds = element
            .TransformToVisual(root)
            .TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        scenarioTargets.TryGetValue(target, out var component);
        var candidate = new TargetCandidate(
            target,
            rank,
            bounds.Width * bounds.Height,
            new TargetRecord(
                component,
                target,
                identitySource,
                AutomationProperties.GetAutomationId(element),
                element.Name,
                element.GetType().FullName ?? element.GetType().Name,
                new BoundsRecord(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                new SizeRecord(element.ActualWidth, element.ActualHeight),
                element.Visibility.ToString()));

        if (!candidates.TryGetValue(target, out var existing) ||
            candidate.Rank < existing.Rank ||
            candidate.Rank == existing.Rank && candidate.Area < existing.Area)
        {
            candidates[target] = candidate;
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalized = path.Replace('\\', '/');
        const string marker = "fixtures/ComponentParityLab.WinUI/scenarios/";
        var markerIndex = normalized.IndexOf(marker, StringComparison.Ordinal);
        return markerIndex >= 0 ? normalized[markerIndex..] : normalized;
    }

    private sealed class NativeReferenceTargetDocument
    {
        public string SchemaVersion { get; init; } = "0.1";
        public string ReferenceSource { get; init; } = "native-winui-element-bounds";
        public string CoordinateSpace { get; init; } = "client-area";
        public string ScenarioName { get; init; } = string.Empty;
        public string? ScenarioPath { get; init; }
        public string? FixtureProjectPath { get; init; }
        public string Theme { get; init; } = string.Empty;
        public SizeRecord Viewport { get; init; } = new(0, 0);
        public double Scale { get; init; }
        public BoundsRecord RootBounds { get; init; } = new(0, 0, 0, 0);
        public string CapturedAt { get; init; } = DateTimeOffset.UtcNow.ToString("O");
        public List<TargetRecord> Targets { get; init; } = [];
    }

    private sealed record TargetCandidate(string Target, int Rank, double Area, TargetRecord Export);

    private sealed record TargetRecord(
        string? Component,
        string Target,
        string IdentitySource,
        string? AutomationId,
        string? Name,
        string ElementType,
        BoundsRecord Bounds,
        SizeRecord ActualSize,
        string Visibility);

    private sealed record BoundsRecord(double X, double Y, double Width, double Height);

    private sealed record SizeRecord(double Width, double Height);
#endif
}
