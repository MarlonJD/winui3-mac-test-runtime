using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record PortableHeadlessComparisonDashboard(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string PortableLane,
    string WindowsReferenceLane,
    string PortableRoot,
    string WindowsReferenceRoot,
    double BoundsTolerance,
    string Status,
    PortableHeadlessComparisonSummary Summary,
    IReadOnlyList<PortableHeadlessScenarioComparison> Scenarios)
{
    public static PortableHeadlessComparisonDashboard Create(
        string portableRoot,
        string windowsReferenceRoot,
        double boundsTolerance = 2.0d)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portableRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsReferenceRoot);

        var portableScenarios = DiscoverScenarios(portableRoot);
        var windowsScenarios = DiscoverScenarios(windowsReferenceRoot);
        var scenarioNames = portableScenarios.Keys
            .Concat(windowsScenarios.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var scenarios = scenarioNames
            .Select(name => CompareScenario(
                name,
                portableScenarios.GetValueOrDefault(name),
                windowsScenarios.GetValueOrDefault(name),
                boundsTolerance))
            .ToArray();
        var summary = new PortableHeadlessComparisonSummary(
            ScenarioCount: scenarios.Length,
            PassedScenarioCount: scenarios.Count(scenario => scenario.Status == "passed"),
            FailedScenarioCount: scenarios.Count(scenario => scenario.Status == "failed"),
            AutomationMismatchCount: scenarios.Sum(scenario => scenario.Automation.Mismatches.Count),
            VisualDifferenceCount: scenarios.Count(scenario => scenario.Visual.Status == "failed"),
            ActionableDiagnosticCount: scenarios.Sum(scenario => scenario.ActionableDiagnostics.Count));
        var status = summary.FailedScenarioCount == 0 ? "passed" : "failed";

        return new PortableHeadlessComparisonDashboard(
            ArtifactSchemas.PortableHeadlessComparisonDashboard,
            DateTimeOffset.UtcNow,
            "portable-headless",
            "windows-reference",
            StablePath(portableRoot),
            StablePath(windowsReferenceRoot),
            boundsTolerance,
            status,
            summary,
            scenarios);
    }

    public static PortableHeadlessComparisonDashboard Write(
        string portableRoot,
        string windowsReferenceRoot,
        string outputRoot,
        double boundsTolerance = 2.0d)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var dashboard = Create(portableRoot, windowsReferenceRoot, boundsTolerance);
        Directory.CreateDirectory(outputRoot);
        File.WriteAllText(
            Path.Combine(outputRoot, "portable-headless-comparison-dashboard.json"),
            JsonSerializer.Serialize(dashboard, JsonDefaults.Options));
        File.WriteAllText(
            Path.Combine(outputRoot, "portable-headless-comparison-dashboard.md"),
            BuildMarkdown(dashboard));
        return dashboard;
    }

    private static PortableHeadlessScenarioComparison CompareScenario(
        string scenarioName,
        ScenarioArtifacts? portable,
        ScenarioArtifacts? windows,
        double boundsTolerance)
    {
        var scenarioResult = CompareScenarioResult(portable, windows);
        var automation = CompareAutomation(portable, windows, boundsTolerance);
        var visual = CompareVisual(portable);
        var diagnostics = automation.Mismatches
            .Select(mismatch => $"automation:{mismatch.AutomationId}:{mismatch.Message}")
            .Concat(scenarioResult.Diagnostics)
            .ToArray();
        var status = scenarioResult.Status == "passed" &&
            automation.Status == "passed" &&
            visual.Status is "passed" or "skipped"
                ? "passed"
                : "failed";

        return new PortableHeadlessScenarioComparison(
            scenarioName,
            status,
            scenarioResult,
            automation,
            visual,
            diagnostics);
    }

    private static ScenarioResultComparison CompareScenarioResult(ScenarioArtifacts? portable, ScenarioArtifacts? windows)
    {
        var diagnostics = new List<string>();
        var portableStatus = ReadString(portable?.ScenarioResultPath, "status") ?? "missing";
        var windowsReferenceSource = ReadString(windows?.WindowsReferencePath, "referenceSource") ?? "missing";
        var windowsCaptureStatus = ReadNestedString(windows?.WindowsReferencePath, "capture", "status") ?? "unknown";

        if (portableStatus != "passed")
        {
            diagnostics.Add($"portable scenario result is {portableStatus}");
        }

        if (windowsReferenceSource != "native-winui")
        {
            diagnostics.Add($"windows reference source is {windowsReferenceSource}, expected native-winui");
        }

        var status = diagnostics.Count == 0 ? "passed" : "failed";
        return new ScenarioResultComparison(
            status,
            portableStatus,
            windowsReferenceSource,
            windowsCaptureStatus,
            diagnostics);
    }

    private static AutomationComparison CompareAutomation(
        ScenarioArtifacts? portable,
        ScenarioArtifacts? windows,
        double boundsTolerance)
    {
        var portableNodes = ReadAutomationNodes(portable?.AutomationPath);
        var windowsNodes = ReadAutomationNodes(windows?.NativeAutomationPath);
        var mismatches = new List<AutomationMismatch>();

        if (portableNodes.Count == 0)
        {
            mismatches.Add(new AutomationMismatch("portable-headless", "missing", "Portable automation tree was not found."));
        }

        if (windowsNodes.Count == 0)
        {
            mismatches.Add(new AutomationMismatch("windows-reference", "missing", "Windows native automation tree was not found."));
        }

        foreach (var portableNode in portableNodes.Values)
        {
            if (!windowsNodes.TryGetValue(portableNode.AutomationId, out var windowsNode))
            {
                mismatches.Add(new AutomationMismatch(
                    portableNode.AutomationId,
                    "missing-windows-reference",
                    "Portable automation node is missing from the Windows reference tree."));
                continue;
            }

            if (!string.Equals(portableNode.ControlType, windowsNode.ControlType, StringComparison.Ordinal))
            {
                mismatches.Add(new AutomationMismatch(
                    portableNode.AutomationId,
                    "controlType",
                    $"ControlType portable={portableNode.ControlType} windows={windowsNode.ControlType}."));
            }

            if (!string.Equals(portableNode.Name, windowsNode.Name, StringComparison.Ordinal))
            {
                mismatches.Add(new AutomationMismatch(
                    portableNode.AutomationId,
                    "name",
                    $"Name portable={portableNode.Name} windows={windowsNode.Name}."));
            }

            if (!portableNode.Patterns.SetEquals(windowsNode.Patterns))
            {
                mismatches.Add(new AutomationMismatch(
                    portableNode.AutomationId,
                    "patterns",
                    $"Patterns portable={string.Join(",", portableNode.Patterns.Order())} windows={string.Join(",", windowsNode.Patterns.Order())}."));
            }

            if (BoundsExceeded(portableNode.Bounds, windowsNode.Bounds, boundsTolerance))
            {
                mismatches.Add(new AutomationMismatch(
                    portableNode.AutomationId,
                    "bounds",
                    $"Bounds delta exceeded tolerance {boundsTolerance.ToString(CultureInfo.InvariantCulture)}."));
            }
        }

        foreach (var windowsNode in windowsNodes.Values)
        {
            if (!portableNodes.ContainsKey(windowsNode.AutomationId))
            {
                mismatches.Add(new AutomationMismatch(
                    windowsNode.AutomationId,
                    "missing-portable-headless",
                    "Windows reference automation node is missing from the portable tree."));
            }
        }

        return new AutomationComparison(
            mismatches.Count == 0 ? "passed" : "failed",
            portableNodes.Count,
            windowsNodes.Count,
            boundsTolerance,
            mismatches);
    }

    private static VisualComparison CompareVisual(ScenarioArtifacts? portable)
    {
        var path = portable?.PixelDiffPath;
        if (path is null || !File.Exists(path))
        {
            return new VisualComparison("skipped", null, null, null, "No portable pixel-diff.json was found.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var status = ReadString(root, "status") ?? "unknown";
        return new VisualComparison(
            status,
            ReadDouble(root, "changedPixelPercentage"),
            ReadDouble(root, "meanAbsoluteError"),
            ReadDouble(root, "rootMeanSquaredError"),
            status == "failed" ? "Visual diff exceeded configured tolerance." : null);
    }

    private static IReadOnlyDictionary<string, ScenarioArtifacts> DiscoverScenarios(string root)
    {
        if (!Directory.Exists(root))
        {
            return new Dictionary<string, ScenarioArtifacts>(StringComparer.Ordinal);
        }

        var scenarios = new Dictionary<string, ScenarioArtifacts>(StringComparer.Ordinal);
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            var artifacts = ScenarioArtifacts.FromDirectory(root, directory);
            if (artifacts is not null)
            {
                scenarios[artifacts.ScenarioName] = scenarios.TryGetValue(artifacts.ScenarioName, out var existing)
                    ? existing.Merge(artifacts)
                    : artifacts;
            }
        }

        var rootArtifacts = ScenarioArtifacts.FromDirectory(root, root);
        if (rootArtifacts is not null)
        {
            scenarios[rootArtifacts.ScenarioName] = scenarios.TryGetValue(rootArtifacts.ScenarioName, out var existing)
                ? existing.Merge(rootArtifacts)
                : rootArtifacts;
        }

        return scenarios;
    }

    private static IReadOnlyDictionary<string, AutomationSnapshot> ReadAutomationNodes(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return new Dictionary<string, AutomationSnapshot>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("root", out var root) ||
            root.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, AutomationSnapshot>(StringComparer.Ordinal);
        }

        var nodes = new Dictionary<string, AutomationSnapshot>(StringComparer.Ordinal);
        FlattenAutomationNode(root, nodes);
        return nodes;
    }

    private static void FlattenAutomationNode(JsonElement node, IDictionary<string, AutomationSnapshot> nodes)
    {
        var automationId = ReadString(node, "automationId");
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            nodes[automationId] = new AutomationSnapshot(
                automationId,
                ReadString(node, "name"),
                ReadString(node, "controlType") ?? ReadString(node, "role") ?? "unknown",
                ReadBounds(node),
                ReadStringSet(node, "patterns"));
        }

        if (node.TryGetProperty("children", out var children) &&
            children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
            {
                if (child.ValueKind == JsonValueKind.Object)
                {
                    FlattenAutomationNode(child, nodes);
                }
            }
        }
    }

    private static AutomationBoundsSnapshot? ReadBounds(JsonElement node)
    {
        if (!node.TryGetProperty("bounds", out var bounds) ||
            bounds.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new AutomationBoundsSnapshot(
            ReadDouble(bounds, "x") ?? 0,
            ReadDouble(bounds, "y") ?? 0,
            ReadDouble(bounds, "width") ?? 0,
            ReadDouble(bounds, "height") ?? 0);
    }

    private static ISet<string> ReadStringSet(JsonElement node, string property)
    {
        if (!node.TryGetProperty(property, out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal)!;
    }

    private static bool BoundsExceeded(AutomationBoundsSnapshot? portable, AutomationBoundsSnapshot? windows, double tolerance)
    {
        if (portable is null || windows is null)
        {
            return portable is not null || windows is not null;
        }

        return Math.Abs(portable.X - windows.X) > tolerance ||
            Math.Abs(portable.Y - windows.Y) > tolerance ||
            Math.Abs(portable.Width - windows.Width) > tolerance ||
            Math.Abs(portable.Height - windows.Height) > tolerance;
    }

    private static string BuildMarkdown(PortableHeadlessComparisonDashboard dashboard)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Portable Headless Comparison Dashboard");
        builder.AppendLine();
        builder.AppendLine($"Status: `{dashboard.Status}`");
        builder.AppendLine($"Portable lane: `{dashboard.PortableLane}`");
        builder.AppendLine($"Windows reference lane: `{dashboard.WindowsReferenceLane}`");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Status | Portable result | Windows source | Automation | Visual % |");
        builder.AppendLine("| --- | --- | --- | --- | --- | ---: |");
        foreach (var scenario in dashboard.Scenarios)
        {
            builder.AppendLine(
                $"| {scenario.ScenarioName} | {scenario.Status} | {scenario.ScenarioResult.PortableStatus} | {scenario.ScenarioResult.WindowsReferenceSource} | {scenario.Automation.Status} ({scenario.Automation.Mismatches.Count}) | {FormatMetric(scenario.Visual.ChangedPixelPercentage)} |");
            foreach (var mismatch in scenario.Automation.Mismatches)
            {
                builder.AppendLine($"| {scenario.ScenarioName} | diagnostic | | | {mismatch.AutomationId}: {mismatch.Kind} - {mismatch.Message} | |");
            }
        }

        return builder.ToString();
    }

    private static string FormatMetric(double? value)
    {
        return value is null ? "" : value.Value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string? ReadString(string? path, string property)
    {
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return ReadString(document.RootElement, property);
    }

    private static string? ReadNestedString(string? path, string parent, string property)
    {
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty(parent, out var parentElement)
            ? ReadString(parentElement, property)
            : null;
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private static double? ReadDouble(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.Number
                ? value.GetDouble()
                : null;
    }

    private static string StablePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/');
    }

    private sealed record ScenarioArtifacts(
        string ScenarioName,
        string Directory,
        string? ScenarioResultPath,
        string? AutomationPath,
        string? PixelDiffPath,
        string? WindowsReferencePath,
        string? NativeAutomationPath)
    {
        public static ScenarioArtifacts? FromDirectory(string root, string directory)
        {
            var scenarioResultPath = Existing(Path.Combine(directory, "scenario-result.json"));
            var automationPath = Existing(Path.Combine(directory, "automation.json")) ??
                Existing(Path.Combine(directory, "automation-core.json"));
            var pixelDiffPath = Existing(Path.Combine(directory, "visual", "pixel-diff.json")) ??
                Existing(Path.Combine(directory, "pixel-diff.json"));
            var windowsReferencePath = Existing(Path.Combine(directory, "windows-reference.json"));
            var nativeAutomationPath = Existing(Path.Combine(directory, "native-automation.json"));
            if (scenarioResultPath is null &&
                automationPath is null &&
                pixelDiffPath is null &&
                windowsReferencePath is null &&
                nativeAutomationPath is null)
            {
                return null;
            }

            return new ScenarioArtifacts(
                ScenarioName: ReadScenarioName(scenarioResultPath, windowsReferencePath) ?? ScenarioNameFromPath(root, directory),
                Directory: directory,
                ScenarioResultPath: scenarioResultPath,
                AutomationPath: automationPath,
                PixelDiffPath: pixelDiffPath,
                WindowsReferencePath: windowsReferencePath,
                NativeAutomationPath: nativeAutomationPath);
        }

        public ScenarioArtifacts Merge(ScenarioArtifacts other)
        {
            return this with
            {
                ScenarioResultPath = ScenarioResultPath ?? other.ScenarioResultPath,
                AutomationPath = AutomationPath ?? other.AutomationPath,
                PixelDiffPath = PixelDiffPath ?? other.PixelDiffPath,
                WindowsReferencePath = WindowsReferencePath ?? other.WindowsReferencePath,
                NativeAutomationPath = NativeAutomationPath ?? other.NativeAutomationPath
            };
        }

        private static string? Existing(string path)
        {
            return File.Exists(path) ? path : null;
        }

        private static string? ReadScenarioName(string? scenarioResultPath, string? windowsReferencePath)
        {
            return ReadString(scenarioResultPath, "name") ??
                ReadString(windowsReferencePath, "scenarioName");
        }

        private static string ScenarioNameFromPath(string root, string directory)
        {
            var relative = Path.GetRelativePath(root, directory).Replace('\\', '/');
            return relative == "." ? Path.GetFileName(Path.GetFullPath(root)) : relative.Split('/')[0];
        }
    }

    private sealed record AutomationSnapshot(
        string AutomationId,
        string? Name,
        string ControlType,
        AutomationBoundsSnapshot? Bounds,
        ISet<string> Patterns);

    private sealed record AutomationBoundsSnapshot(double X, double Y, double Width, double Height);
}

public sealed record PortableHeadlessComparisonSummary(
    int ScenarioCount,
    int PassedScenarioCount,
    int FailedScenarioCount,
    int AutomationMismatchCount,
    int VisualDifferenceCount,
    int ActionableDiagnosticCount);

public sealed record PortableHeadlessScenarioComparison(
    string ScenarioName,
    string Status,
    ScenarioResultComparison ScenarioResult,
    AutomationComparison Automation,
    VisualComparison Visual,
    IReadOnlyList<string> ActionableDiagnostics);

public sealed record ScenarioResultComparison(
    string Status,
    string PortableStatus,
    string WindowsReferenceSource,
    string WindowsCaptureStatus,
    IReadOnlyList<string> Diagnostics);

public sealed record AutomationComparison(
    string Status,
    int PortableNodeCount,
    int WindowsReferenceNodeCount,
    double BoundsTolerance,
    IReadOnlyList<AutomationMismatch> Mismatches);

public sealed record AutomationMismatch(
    string AutomationId,
    string Kind,
    string Message);

public sealed record VisualComparison(
    string Status,
    double? ChangedPixelPercentage,
    double? MeanAbsoluteError,
    double? RootMeanSquaredError,
    string? Diagnostic);
