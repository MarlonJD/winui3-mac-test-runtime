using System.Text;
using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record VisualComparisonReport(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string BeforeRoot,
    string AfterRoot,
    string Status,
    VisualComparisonSummary Summary,
    IReadOnlyList<VisualComparisonRow> Rows)
{
    private const double MetricTolerance = 0.000001;

    public static VisualComparisonReport Create(string beforeRoot, string afterRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(beforeRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(afterRoot);

        var beforeRows = LoadRows(beforeRoot);
        var afterRows = LoadRows(afterRoot);
        var keys = beforeRows.Keys
            .Concat(afterRows.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var rows = keys
            .Select(key => CompareRows(beforeRows.GetValueOrDefault(key), afterRows.GetValueOrDefault(key)))
            .ToArray();
        var summary = new VisualComparisonSummary(
            rows.Length,
            rows.Count(row => row.Status == "improved"),
            rows.Count(row => row.Status == "regressed"),
            rows.Count(row => row.Status == "unchanged"),
            rows.Count(row => row.Status == "newly-passing"),
            rows.Count(row => row.Status == "newly-failing"),
            rows.Count(row => row.Status == "newly-added"),
            rows.Count(row => row.Status == "removed"));
        var status = summary.RegressedRows > 0 || summary.NewlyFailingRows > 0 || summary.RemovedRows > 0
            ? "failed"
            : "passed";

        return new VisualComparisonReport(
            ArtifactSchemas.VisualComparison,
            DateTimeOffset.UtcNow,
            StablePath(beforeRoot),
            StablePath(afterRoot),
            status,
            summary,
            rows);
    }

    public static VisualComparisonReport Write(string beforeRoot, string afterRoot, string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var report = Create(beforeRoot, afterRoot);
        Directory.CreateDirectory(outputRoot);
        File.WriteAllText(
            Path.Combine(outputRoot, "visual-compare.json"),
            JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(
            Path.Combine(outputRoot, "visual-compare.md"),
            BuildMarkdown(report));
        return report;
    }

    private static IReadOnlyDictionary<string, ComponentSnapshot> LoadRows(string root)
    {
        if (!Directory.Exists(root))
        {
            return new Dictionary<string, ComponentSnapshot>(StringComparer.Ordinal);
        }

        var rows = new Dictionary<string, ComponentSnapshot>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(root, "component-evidence.json", SearchOption.AllDirectories))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var scenarioName = ReadString(document.RootElement, "scenarioName") ?? ScenarioFromPath(root, path);
            if (!document.RootElement.TryGetProperty("components", out var components) ||
                components.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var component in components.EnumerateArray())
            {
                var snapshot = ReadComponentSnapshot(scenarioName, component);
                rows[Key(snapshot.ScenarioName, snapshot.Component, snapshot.Target)] = snapshot;
            }
        }

        return rows;
    }

    private static ComponentSnapshot ReadComponentSnapshot(string scenarioName, JsonElement component)
    {
        var crop = component.TryGetProperty("crop", out var cropElement) && cropElement.ValueKind == JsonValueKind.Object
            ? cropElement
            : default;
        return new ComponentSnapshot(
            scenarioName,
            ReadString(component, "component") ?? "unknown",
            ReadString(component, "target") ?? string.Empty,
            ReadString(crop, "status"),
            ReadDouble(component, "changedPixelPercentage") ?? ReadDouble(crop, "changedPixelPercentage"),
            ReadDouble(component, "meanAbsoluteError") ?? ReadDouble(crop, "meanAbsoluteError"),
            ReadDouble(component, "rootMeanSquaredError") ?? ReadDouble(crop, "rootMeanSquaredError"),
            ReadSize(crop, "nativeReferenceCropSize"),
            ReadSize(crop, "macRuntimeCropSize"));
    }

    private static VisualComparisonRow CompareRows(ComponentSnapshot? before, ComponentSnapshot? after)
    {
        if (before is null && after is null)
        {
            throw new InvalidOperationException("Cannot compare two missing rows.");
        }

        var row = after ?? before!;
        var status = Classify(before, after);
        return new VisualComparisonRow(
            row.ScenarioName,
            row.Component,
            row.Target,
            status,
            before?.CropStatus,
            after?.CropStatus,
            before?.ChangedPixelPercentage,
            after?.ChangedPixelPercentage,
            before?.MeanAbsoluteError,
            after?.MeanAbsoluteError,
            before?.RootMeanSquaredError,
            after?.RootMeanSquaredError,
            before?.NativeReferenceCropSize,
            after?.NativeReferenceCropSize,
            before?.MacRuntimeCropSize,
            after?.MacRuntimeCropSize);
    }

    private static string Classify(ComponentSnapshot? before, ComponentSnapshot? after)
    {
        if (before is null)
        {
            return "newly-added";
        }

        if (after is null)
        {
            return "removed";
        }

        if (IsFailed(before) && !IsFailed(after))
        {
            return "newly-passing";
        }

        if (!IsFailed(before) && IsFailed(after))
        {
            return "newly-failing";
        }

        if (before.ChangedPixelPercentage is double beforeMetric &&
            after.ChangedPixelPercentage is double afterMetric)
        {
            if (afterMetric < beforeMetric - MetricTolerance)
            {
                return "improved";
            }

            if (afterMetric > beforeMetric + MetricTolerance)
            {
                return "regressed";
            }
        }

        return "unchanged";
    }

    private static bool IsFailed(ComponentSnapshot row)
    {
        return row.CropStatus == "failed";
    }

    private static string BuildMarkdown(VisualComparisonReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Visual Compare");
        builder.AppendLine();
        builder.AppendLine($"Status: `{report.Status}`");
        builder.AppendLine($"Before: `{report.BeforeRoot}`");
        builder.AppendLine($"After: `{report.AfterRoot}`");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Component | Target | Status | Before % | After % |");
        builder.AppendLine("| --- | --- | --- | --- | ---: | ---: |");
        foreach (var row in report.Rows)
        {
            builder.AppendLine(
                $"| {row.ScenarioName} | {row.Component} | {row.Target} | {row.Status} | {FormatMetric(row.BeforeChangedPixelPercentage)} | {FormatMetric(row.AfterChangedPixelPercentage)} |");
        }

        return builder.ToString();
    }

    private static string FormatMetric(double? value)
    {
        return value is null ? "" : value.Value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Key(string scenarioName, string component, string target)
    {
        return $"{scenarioName}|{component}|{target}";
    }

    private static string ScenarioFromPath(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        var firstSlash = relative.IndexOf('/', StringComparison.Ordinal);
        return firstSlash < 0 ? "unknown" : relative[..firstSlash];
    }

    private static string StablePath(string path)
    {
        return path.Replace('\\', '/');
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

    private static VisualComparisonSize? ReadSize(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var width = ReadDouble(value, "width");
        var height = ReadDouble(value, "height");
        return width is null || height is null ? null : new VisualComparisonSize(width.Value, height.Value);
    }

    private sealed record ComponentSnapshot(
        string ScenarioName,
        string Component,
        string Target,
        string? CropStatus,
        double? ChangedPixelPercentage,
        double? MeanAbsoluteError,
        double? RootMeanSquaredError,
        VisualComparisonSize? NativeReferenceCropSize,
        VisualComparisonSize? MacRuntimeCropSize);
}

public sealed record VisualComparisonSummary(
    int TotalRows,
    int ImprovedRows,
    int RegressedRows,
    int UnchangedRows,
    int NewlyPassingRows,
    int NewlyFailingRows,
    int NewlyAddedRows,
    int RemovedRows);

public sealed record VisualComparisonRow(
    string ScenarioName,
    string Component,
    string Target,
    string Status,
    string? BeforeCropStatus,
    string? AfterCropStatus,
    double? BeforeChangedPixelPercentage,
    double? AfterChangedPixelPercentage,
    double? BeforeMeanAbsoluteError,
    double? AfterMeanAbsoluteError,
    double? BeforeRootMeanSquaredError,
    double? AfterRootMeanSquaredError,
    VisualComparisonSize? BeforeNativeReferenceCropSize,
    VisualComparisonSize? AfterNativeReferenceCropSize,
    VisualComparisonSize? BeforeMacRuntimeCropSize,
    VisualComparisonSize? AfterMacRuntimeCropSize);

public sealed record VisualComparisonSize(double Width, double Height);
