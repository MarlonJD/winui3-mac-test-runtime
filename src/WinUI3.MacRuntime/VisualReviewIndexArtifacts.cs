using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record VisualReviewIndexDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string EvidenceRoot,
    string DashboardPath,
    VisualReviewIndexSummary Summary,
    IReadOnlyList<VisualReviewIndexScenario> Scenarios,
    IReadOnlyList<VisualReviewIndexRow> Rows,
    string HtmlPath,
    string Status);

public sealed record VisualReviewIndexSummary(
    int ScenarioCount,
    int ComponentCount,
    int CompleteTriptychCount,
    int MissingReviewFiles,
    int MissingNativeReferenceCrops,
    int MissingMacRuntimeCrops,
    int MissingDiffCrops,
    int MissingInspectionNotes,
    int BlockingRowCount);

public sealed record VisualReviewIndexScenario(
    string ScenarioName,
    string EvidencePath,
    string? ReviewJsonPath,
    string? ReviewHtmlPath,
    int ComponentCount,
    int CompleteTriptychCount,
    int MissingInspectionNotes,
    int BlockingRowCount,
    string Status);

public sealed record VisualReviewIndexRow(
    string ScenarioName,
    string Component,
    string? Target,
    string CatalogStatus,
    string VisualGrade,
    string NativeQualityGrade,
    string ReviewStatus,
    string CropStatus,
    string InspectionStatus,
    string? ReviewHtmlPath,
    string? NativeReferenceCropPath,
    string? MacRuntimeCropPath,
    string? PixelDiffPath,
    double? ChangedPixelPercentage,
    double? MeanAbsoluteError,
    double? RootMeanSquaredError,
    string RemainingBlocker);

public static class VisualReviewIndexArtifacts
{
    public const string JsonFileName = "public-visual-review-index.json";
    public const string HtmlFileName = "public-visual-review-index.html";

    public static VisualReviewIndexDocument Build(string repositoryRoot, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var root = Path.GetFullPath(repositoryRoot);
        var outputRoot = Path.GetFullPath(outputDirectory);
        var dashboard = ComponentQualityDashboard.BuildFromPublicEvidence(root);
        var blockerMap = dashboard.Rows.ToDictionary(
            row => RowKey(row.ScenarioName, row.Component, row.Target),
            row => row.RemainingBlocker,
            StringComparer.Ordinal);
        var blockerCountMap = dashboard.Blockers
            .GroupBy(blocker => blocker.ScenarioName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var scenarios = new List<VisualReviewIndexScenario>();
        var rows = new List<VisualReviewIndexRow>();
        var missingReviewFiles = 0;

        foreach (var scenario in dashboard.Scenarios)
        {
            var evidencePath = Path.Combine(root, scenario.EvidencePath);
            var scenarioDirectory = Path.GetDirectoryName(evidencePath)!;
            var reviewJsonPath = Path.Combine(scenarioDirectory, "visual-review.json");
            var reviewHtmlPath = Path.Combine(scenarioDirectory, "visual-review.html");
            var reviewJsonExists = File.Exists(reviewJsonPath);
            var reviewHtmlExists = File.Exists(reviewHtmlPath);
            VisualReviewDocument? review = null;
            if (reviewJsonExists)
            {
                using var stream = File.OpenRead(reviewJsonPath);
                review = JsonSerializer.Deserialize<VisualReviewDocument>(stream, JsonDefaults.Options)
                    ?? throw new InvalidOperationException($"Could not read visual review from '{reviewJsonPath}'.");
            }
            else
            {
                missingReviewFiles++;
            }

            scenarios.Add(new VisualReviewIndexScenario(
                ScenarioName: scenario.ScenarioName,
                EvidencePath: RelativePath(outputRoot, evidencePath),
                ReviewJsonPath: reviewJsonExists ? RelativePath(outputRoot, reviewJsonPath) : null,
                ReviewHtmlPath: reviewHtmlExists ? RelativePath(outputRoot, reviewHtmlPath) : null,
                ComponentCount: scenario.ComponentCount,
                CompleteTriptychCount: review?.Summary.CompleteTriptychCount ?? 0,
                MissingInspectionNotes: review?.Summary.MissingInspectionNotes ?? scenario.ComponentCount,
                BlockingRowCount: blockerCountMap.TryGetValue(scenario.ScenarioName, out var blockers) ? blockers : 0,
                Status: review is null ? "missing-review" : scenario.Status));

            if (review is null)
            {
                continue;
            }

            foreach (var row in review.Rows)
            {
                rows.Add(new VisualReviewIndexRow(
                    ScenarioName: scenario.ScenarioName,
                    Component: row.Component,
                    Target: row.Target,
                    CatalogStatus: row.CatalogStatus,
                    VisualGrade: row.VisualGrade,
                    NativeQualityGrade: row.NativeQualityGrade,
                    ReviewStatus: row.ReviewStatus,
                    CropStatus: row.CropStatus,
                    InspectionStatus: row.InspectionStatus,
                    ReviewHtmlPath: reviewHtmlExists ? RelativePath(outputRoot, reviewHtmlPath) : null,
                    NativeReferenceCropPath: ReviewCropPath(outputRoot, scenarioDirectory, row.NativeReferenceCropPath),
                    MacRuntimeCropPath: ReviewCropPath(outputRoot, scenarioDirectory, row.MacRuntimeCropPath),
                    PixelDiffPath: ReviewCropPath(outputRoot, scenarioDirectory, row.PixelDiffPath),
                    ChangedPixelPercentage: row.ChangedPixelPercentage,
                    MeanAbsoluteError: row.MeanAbsoluteError,
                    RootMeanSquaredError: row.RootMeanSquaredError,
                    RemainingBlocker: blockerMap.TryGetValue(RowKey(scenario.ScenarioName, row.Component, row.Target), out var blocker)
                        ? blocker
                        : "none"));
            }
        }

        var summary = new VisualReviewIndexSummary(
            ScenarioCount: dashboard.Totals.ScenarioCount,
            ComponentCount: dashboard.Totals.ComponentCount,
            CompleteTriptychCount: scenarios.Sum(scenario => scenario.CompleteTriptychCount),
            MissingReviewFiles: missingReviewFiles,
            MissingNativeReferenceCrops: dashboard.Totals.MissingNativeReferenceCrops,
            MissingMacRuntimeCrops: dashboard.Totals.MissingMacRuntimeCrops,
            MissingDiffCrops: dashboard.Totals.MissingComponentDiffs,
            MissingInspectionNotes: dashboard.Totals.MissingInspectionNotes,
            BlockingRowCount: dashboard.Totals.BlockingRowCount);

        return new VisualReviewIndexDocument(
            SchemaVersion: ArtifactSchemas.VisualReviewIndex,
            GeneratedAt: DateTimeOffset.UnixEpoch,
            EvidenceRoot: RelativePath(outputRoot, Path.Combine(root, "docs", "visual-parity", "examples")),
            DashboardPath: RelativePath(outputRoot, Path.Combine(root, "docs", "visual-parity", "component-quality-dashboard.json")),
            Summary: summary,
            Scenarios: scenarios,
            Rows: rows,
            HtmlPath: HtmlFileName,
            Status: summary.MissingReviewFiles == 0 && dashboard.Status == "passed" ? "passed" : "blocked");
    }

    public static VisualReviewIndexDocument Write(string repositoryRoot, string outputDirectory)
    {
        var outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);
        var document = Build(repositoryRoot, outputRoot);
        File.WriteAllText(Path.Combine(outputRoot, JsonFileName), JsonSerializer.Serialize(document, JsonDefaults.Options), Encoding.UTF8);
        File.WriteAllText(Path.Combine(outputRoot, HtmlFileName), BuildHtml(document), Encoding.UTF8);
        return document;
    }

    public static string BuildHtml(VisualReviewIndexDocument document)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>Public visual review index</title>");
        html.AppendLine("""
<style>
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; margin: 24px; color: #1f1f1f; background: #fff; }
h1 { font-size: 24px; margin: 0 0 8px; }
h2 { font-size: 18px; margin: 28px 0 12px; }
a { color: #005a9e; }
.summary { display: flex; flex-wrap: wrap; gap: 8px; margin: 16px 0 24px; }
.metric { border: 1px solid #d1d1d1; border-radius: 4px; padding: 8px 10px; min-width: 150px; }
.metric strong { display: block; font-size: 20px; }
table { border-collapse: collapse; width: 100%; font-size: 13px; }
th, td { border: 1px solid #d1d1d1; padding: 7px 8px; vertical-align: top; text-align: left; }
th { background: #f3f3f3; }
.blocked { color: #7a1f1f; font-weight: 600; }
.ready { color: #0f6c0f; font-weight: 600; }
.blocker { max-width: 420px; line-height: 1.35; }
.metrics { white-space: nowrap; }
.triptych-preview { display: grid; grid-template-columns: repeat(3, 84px); gap: 6px; min-width: 270px; }
.triptych-preview figure { margin: 0; border: 1px solid #d1d1d1; background: #fafafa; }
.triptych-preview figcaption { padding: 3px 4px; font-size: 11px; font-weight: 600; background: #f3f3f3; border-bottom: 1px solid #d1d1d1; }
.triptych-preview img { display: block; width: 84px; height: 52px; object-fit: contain; background: #fff; }
@media (max-width: 900px) { body { margin: 16px; } table { font-size: 12px; } }
</style>
""");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<h1>Public visual review index</h1>");
        html.AppendLine($"<p>Dashboard: <a href=\"{Escape(document.DashboardPath)}\">{Escape(document.DashboardPath)}</a></p>");
        html.AppendLine("<div class=\"summary\">");
        AppendMetric(html, "Scenarios", document.Summary.ScenarioCount);
        AppendMetric(html, "Components", document.Summary.ComponentCount);
        AppendMetric(html, "Complete triptychs", document.Summary.CompleteTriptychCount);
        AppendMetric(html, "Missing reviews", document.Summary.MissingReviewFiles);
        AppendMetric(html, "Missing inspections", document.Summary.MissingInspectionNotes);
        AppendMetric(html, "Blocker rows", document.Summary.BlockingRowCount);
        html.AppendLine("</div>");

        html.AppendLine("<h2>Scenarios</h2>");
        html.AppendLine("<table><thead><tr><th>Scenario</th><th>Review</th><th>Triptychs</th><th>Missing inspections</th><th>Blockers</th><th>Status</th></tr></thead><tbody>");
        foreach (var scenario in document.Scenarios)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{Escape(scenario.ScenarioName)}</td>");
            html.AppendLine($"<td>{LinkOrMissing(scenario.ReviewHtmlPath)}</td>");
            html.AppendLine($"<td>{scenario.CompleteTriptychCount}/{scenario.ComponentCount}</td>");
            html.AppendLine($"<td>{scenario.MissingInspectionNotes}</td>");
            html.AppendLine($"<td>{scenario.BlockingRowCount}</td>");
            html.AppendLine($"<td class=\"{StatusClass(scenario.Status)}\">{Escape(scenario.Status)}</td>");
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody></table>");

        html.AppendLine("<h2>Components</h2>");
        html.AppendLine("<table><thead><tr><th>Scenario</th><th>Component</th><th>Visual</th><th>Native quality</th><th>Review</th><th>Inspection</th><th>Metrics</th><th>Triptych preview</th><th>Crops</th><th>Remaining blocker</th></tr></thead><tbody>");
        foreach (var row in document.Rows)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{Escape(row.ScenarioName)}</td>");
            html.AppendLine($"<td>{Escape(row.Component)}{TargetSuffix(row.Target)}</td>");
            html.AppendLine($"<td>{Escape(row.VisualGrade)}</td>");
            html.AppendLine($"<td>{Escape(row.NativeQualityGrade)}</td>");
            html.AppendLine($"<td class=\"{StatusClass(row.ReviewStatus)}\">{Escape(row.ReviewStatus)}</td>");
            html.AppendLine($"<td>{Escape(row.InspectionStatus)}</td>");
            html.AppendLine($"<td class=\"metrics\">{MetricSummary(row)}</td>");
            html.AppendLine($"<td>{TriptychPreview(row)}</td>");
            html.AppendLine($"<td>{CropLinks(row)}</td>");
            html.AppendLine($"<td class=\"blocker\">{Escape(row.RemainingBlocker)}</td>");
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody></table>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static string? ReviewCropPath(string outputRoot, string scenarioDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolved = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(scenarioDirectory, path));
        return RelativePath(outputRoot, resolved);
    }

    private static string RowKey(string scenarioName, string component, string? target)
    {
        return $"{scenarioName}\u001f{component}\u001f{target ?? string.Empty}";
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }

    private static void AppendMetric(StringBuilder html, string label, int value)
    {
        html.AppendLine($"<div class=\"metric\"><strong>{value}</strong>{Escape(label)}</div>");
    }

    private static string CropLinks(VisualReviewIndexRow row)
    {
        var links = new List<string>();
        AddLink(links, "native", row.NativeReferenceCropPath);
        AddLink(links, "macOS", row.MacRuntimeCropPath);
        AddLink(links, "diff", row.PixelDiffPath);
        AddLink(links, "review", row.ReviewHtmlPath);
        return links.Count == 0 ? "missing" : string.Join(" | ", links);
    }

    private static string MetricSummary(VisualReviewIndexRow row)
    {
        var metrics = new List<string>();
        AddMetric(metrics, "changed", row.ChangedPixelPercentage, suffix: "%");
        AddMetric(metrics, "MAE", row.MeanAbsoluteError);
        AddMetric(metrics, "RMS", row.RootMeanSquaredError);
        return metrics.Count == 0 ? "missing" : string.Join("<br>", metrics);
    }

    private static void AddMetric(List<string> metrics, string label, double? value, string suffix = "")
    {
        if (value is { } metric)
        {
            metrics.Add($"{Escape(label)}: {metric.ToString("0.###", CultureInfo.InvariantCulture)}{Escape(suffix)}");
        }
    }

    private static string TriptychPreview(VisualReviewIndexRow row)
    {
        var html = new StringBuilder();
        html.AppendLine("<div class=\"triptych-preview\">");
        AppendPreviewFigure(html, "native", row.NativeReferenceCropPath);
        AppendPreviewFigure(html, "macOS", row.MacRuntimeCropPath);
        AppendPreviewFigure(html, "diff", row.PixelDiffPath);
        html.AppendLine("</div>");
        return html.ToString();
    }

    private static void AppendPreviewFigure(StringBuilder html, string label, string? path)
    {
        html.AppendLine("<figure>");
        html.AppendLine($"<figcaption>{Escape(label)}</figcaption>");
        if (string.IsNullOrWhiteSpace(path))
        {
            html.AppendLine("<span>missing</span>");
        }
        else
        {
            html.AppendLine($"<a href=\"{Escape(path)}\"><img alt=\"{Escape(label)} crop\" src=\"{Escape(path)}\"></a>");
        }

        html.AppendLine("</figure>");
    }

    private static void AddLink(List<string> links, string label, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            links.Add($"<a href=\"{Escape(path)}\">{Escape(label)}</a>");
        }
    }

    private static string LinkOrMissing(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? "missing"
            : $"<a href=\"{Escape(path)}\">{Escape(path)}</a>";
    }

    private static string TargetSuffix(string? target)
    {
        return string.IsNullOrWhiteSpace(target) ? string.Empty : $" <span>({Escape(target)})</span>";
    }

    private static string StatusClass(string status)
    {
        return status.Contains("ready", StringComparison.Ordinal) || status == "passed"
            ? "ready"
            : "blocked";
    }

    private static string Escape(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
