using Microsoft.UI.Xaml;

#if WINDOWS
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
#endif

namespace PublicAdminWorkbench.WinUI;

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
            FixtureProjectPath = "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj",
            CommitSha = Environment.GetEnvironmentVariable("GITHUB_SHA"),
            WorkflowRunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            Theme = launchOptions.Theme,
            Viewport = new SizeRecord(launchOptions.ViewportWidth, launchOptions.ViewportHeight),
            Scale = launchOptions.Scale,
            Dimensions = new SizeRecord((int)Math.Round(root.ActualWidth), (int)Math.Round(root.ActualHeight)),
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

        scenarioTargets.TryGetValue(target, out var component);
        var boundsElement = SelectBoundsElement(element, component);
        var boundsSource = ReferenceEquals(boundsElement, element)
            ? identitySource
            : identitySource + "+native-descendant";
        var bounds = boundsElement
            .TransformToVisual(root)
            .TransformBounds(new Windows.Foundation.Rect(0, 0, boundsElement.ActualWidth, boundsElement.ActualHeight));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var capturedAt = DateTimeOffset.UtcNow;
        var candidate = new TargetCandidate(
            target,
            rank,
            bounds.Width * bounds.Height,
            new TargetRecord(
                component,
                target,
                boundsSource,
                AutomationProperties.GetAutomationId(boundsElement),
                boundsElement.Name,
                boundsElement.GetType().FullName ?? boundsElement.GetType().Name,
                new BoundsRecord(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                new SizeRecord((int)Math.Round(boundsElement.ActualWidth), (int)Math.Round(boundsElement.ActualHeight)),
                boundsElement.Visibility.ToString(),
                boundsSource,
                capturedAt.ToString("O")));

        if (!candidates.TryGetValue(target, out var existing) ||
            candidate.Rank < existing.Rank ||
            candidate.Rank == existing.Rank && candidate.Area < existing.Area)
        {
            candidates[target] = candidate;
        }
    }

    private static FrameworkElement SelectBoundsElement(FrameworkElement element, string? component)
    {
        if (string.IsNullOrWhiteSpace(component))
        {
            return element;
        }

        return DescendantsAndSelf(element)
            .Where(candidate =>
                candidate.ActualWidth > 0 &&
                candidate.ActualHeight > 0 &&
                candidate.Visibility == Visibility.Visible)
            .OrderBy(candidate => CandidateRank(candidate, component))
            .ThenBy(candidate => candidate.ActualWidth * candidate.ActualHeight)
            .FirstOrDefault() ?? element;
    }

    private static IEnumerable<FrameworkElement> DescendantsAndSelf(DependencyObject node)
    {
        if (node is FrameworkElement element)
        {
            yield return element;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(node);
        for (var index = 0; index < childCount; index++)
        {
            foreach (var child in DescendantsAndSelf(VisualTreeHelper.GetChild(node, index)))
            {
                yield return child;
            }
        }
    }

    private static int CandidateRank(FrameworkElement element, string component)
    {
        var simpleType = element.GetType().Name;
        if (string.Equals(simpleType, component, StringComparison.Ordinal) ||
            component.EndsWith("." + simpleType, StringComparison.Ordinal))
        {
            return 0;
        }

        if (component.Contains(simpleType, StringComparison.Ordinal))
        {
            return 1;
        }

        if (element is TextBlock && !string.Equals(component, "TextBlock", StringComparison.Ordinal))
        {
            return 100;
        }

        if (element is ContentControl && component is not "ContentControl" and not "CommandBar.Content")
        {
            return 90;
        }

        if (element is StackPanel && component is not "StackPanel" and not "Labels and forms")
        {
            return 80;
        }

        return 10;
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
        const string marker = "fixtures/PublicAdminWorkbench.WinUI/scenarios/";
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
        public string? CommitSha { get; init; }
        public string? WorkflowRunId { get; init; }
        public string Theme { get; init; } = string.Empty;
        public SizeRecord Viewport { get; init; } = new(0, 0);
        public double Scale { get; init; }
        public SizeRecord Dimensions { get; init; } = new(0, 0);
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
        string Visibility,
        string BoundsSource,
        string CapturedAt);

    private sealed record BoundsRecord(double X, double Y, double Width, double Height);

    private sealed record SizeRecord(double Width, double Height);
#endif
}
