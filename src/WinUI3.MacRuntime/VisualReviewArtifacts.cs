using System.Net;
using System.Text;
using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record VisualReviewDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string ScenarioName,
    string EvidencePath,
    string OutputDirectory,
    VisualReviewSummary Summary,
    IReadOnlyList<VisualReviewRow> Rows,
    string HtmlPath);

public sealed record VisualReviewSummary(
    int ComponentCount,
    int CompleteTriptychCount,
    int MissingNativeReferenceCrops,
    int MissingMacRuntimeCrops,
    int MissingDiffCrops,
    int MissingInspectionNotes);

public sealed record VisualReviewRow(
    string Component,
    string? Target,
    string CatalogStatus,
    string VisualGrade,
    string NativeQualityGrade,
    string CropStatus,
    string ReviewStatus,
    string? NativeReferenceCropPath,
    string? MacRuntimeCropPath,
    string? PixelDiffPath,
    string ReferenceSource,
    string NativeReferenceRunId,
    string ReferenceCommitSha,
    string ReferenceScenarioPath,
    double? ChangedPixelPercentage,
    double? MeanAbsoluteError,
    double? RootMeanSquaredError,
    string InspectionStatus,
    string Notes);

public static class VisualReviewArtifacts
{
    public static VisualReviewDocument Write(string componentEvidencePath, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentEvidencePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var evidencePath = Path.GetFullPath(componentEvidencePath);
        if (!File.Exists(evidencePath))
        {
            throw new FileNotFoundException("Component evidence was not found.", evidencePath);
        }

        var outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);

        var evidenceDirectory = Path.GetDirectoryName(evidencePath)!;
        using var stream = File.OpenRead(evidencePath);
        var evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(stream, JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Could not read component evidence from '{evidencePath}'.");

        var rows = evidence.Components
            .Select(component => BuildRow(component, evidenceDirectory, outputRoot))
            .ToArray();
        var summary = new VisualReviewSummary(
            ComponentCount: rows.Length,
            CompleteTriptychCount: rows.Count(row => row.ReviewStatus == "ready-for-manual-inspection"),
            MissingNativeReferenceCrops: rows.Count(row => string.IsNullOrWhiteSpace(row.NativeReferenceCropPath)),
            MissingMacRuntimeCrops: rows.Count(row => string.IsNullOrWhiteSpace(row.MacRuntimeCropPath)),
            MissingDiffCrops: rows.Count(row => string.IsNullOrWhiteSpace(row.PixelDiffPath)),
            MissingInspectionNotes: rows.Count(row => row.InspectionStatus != "present"));

        var htmlPath = Path.Combine(outputRoot, "visual-review.html");
        var document = new VisualReviewDocument(
            SchemaVersion: ArtifactSchemas.VisualReview,
            GeneratedAt: DateTimeOffset.UnixEpoch,
            ScenarioName: evidence.ScenarioName,
            EvidencePath: RelativePath(outputRoot, evidencePath),
            OutputDirectory: outputRoot,
            Summary: summary,
            Rows: rows,
            HtmlPath: htmlPath);

        File.WriteAllText(htmlPath, BuildHtml(document), Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(outputRoot, "visual-review.json"),
            JsonSerializer.Serialize(document, JsonDefaults.Options),
            Encoding.UTF8);

        return document;
    }

    private static VisualReviewRow BuildRow(
        ComponentEvidenceEntry component,
        string evidenceDirectory,
        string outputDirectory)
    {
        var nativeReferencePath = ExistingReviewPath(component.Crop?.NativeReferencePath, evidenceDirectory, outputDirectory);
        var macRuntimePath = ExistingReviewPath(component.Crop?.MacRuntimePath, evidenceDirectory, outputDirectory);
        var pixelDiffPath = ExistingReviewPath(component.Crop?.PixelDiffPath, evidenceDirectory, outputDirectory);
        var reviewStatus = nativeReferencePath is not null && macRuntimePath is not null && pixelDiffPath is not null
            ? "ready-for-manual-inspection"
            : "missing-crop-evidence";

        return new VisualReviewRow(
            Component: component.Component,
            Target: component.Target,
            CatalogStatus: component.CatalogStatus,
            VisualGrade: component.VisualGrade,
            NativeQualityGrade: component.NativeQualityGrade,
            CropStatus: component.Crop?.Status ?? "missing",
            ReviewStatus: reviewStatus,
            NativeReferenceCropPath: nativeReferencePath,
            MacRuntimeCropPath: macRuntimePath,
            PixelDiffPath: pixelDiffPath,
            ReferenceSource: component.Crop?.NativeReferenceProvenance?.ReferenceSource ?? "missing",
            NativeReferenceRunId: component.Crop?.NativeReferenceProvenance?.WorkflowRunId ?? "missing",
            ReferenceCommitSha: component.Crop?.NativeReferenceProvenance?.CommitSha ?? "missing",
            ReferenceScenarioPath: component.Crop?.NativeReferenceProvenance?.ScenarioPath ?? "missing",
            ChangedPixelPercentage: component.Crop?.ChangedPixelPercentage ?? component.ChangedPixelPercentage,
            MeanAbsoluteError: component.Crop?.MeanAbsoluteError ?? component.MeanAbsoluteError,
            RootMeanSquaredError: component.Crop?.RootMeanSquaredError ?? component.RootMeanSquaredError,
            InspectionStatus: component.Inspection is null ? "missing" : "present",
            Notes: NotesFor(component));
    }

    private static string? ExistingReviewPath(string? path, string evidenceDirectory, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolved = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(evidenceDirectory, path));
        return File.Exists(resolved)
            ? RelativePath(outputDirectory, resolved)
            : null;
    }

    private static string NotesFor(ComponentEvidenceEntry component)
    {
        if (component.Inspection is { } inspection && !string.IsNullOrWhiteSpace(inspection.Notes))
        {
            return inspection.Notes;
        }

        return component.KnownGaps.Count == 0
            ? "Manual inspection is pending."
            : string.Join(" ", component.KnownGaps);
    }

    private static string BuildHtml(VisualReviewDocument document)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine($"<title>{Escape(document.ScenarioName)} visual review</title>");
        html.AppendLine("""
<style>
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; margin: 24px; color: #1f1f1f; background: #fff; }
h1 { font-size: 24px; margin: 0 0 8px; }
h2 { font-size: 18px; margin: 0 0 12px; }
.summary { display: flex; flex-wrap: wrap; gap: 8px; margin: 16px 0 24px; }
.metric { border: 1px solid #d1d1d1; border-radius: 4px; padding: 8px 10px; min-width: 150px; }
.metric strong { display: block; font-size: 20px; }
.component { border-top: 1px solid #d1d1d1; padding: 20px 0 24px; }
.metadata { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 12px; color: #4b4b4b; font-size: 13px; }
.metadata span { border: 1px solid #ddd; border-radius: 4px; padding: 3px 6px; }
.triptych { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }
figure { margin: 0; border: 1px solid #d1d1d1; border-radius: 4px; background: #fafafa; min-height: 120px; }
figcaption { font-weight: 600; padding: 8px 10px; border-bottom: 1px solid #d1d1d1; background: #f3f3f3; }
img { display: block; max-width: 100%; height: auto; margin: 0 auto; }
.missing { display: grid; min-height: 96px; place-items: center; color: #7a1f1f; padding: 12px; text-align: center; }
.notes { margin-top: 10px; color: #3b3b3b; line-height: 1.4; }
@media (max-width: 760px) { .triptych { grid-template-columns: 1fr; } body { margin: 16px; } }
</style>
""");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"<h1>{Escape(document.ScenarioName)} visual review</h1>");
        html.AppendLine($"<p>Evidence: <code>{Escape(document.EvidencePath)}</code></p>");
        html.AppendLine("<div class=\"summary\">");
        AppendMetric(html, "Components", document.Summary.ComponentCount);
        AppendMetric(html, "Complete triptychs", document.Summary.CompleteTriptychCount);
        AppendMetric(html, "Missing native crops", document.Summary.MissingNativeReferenceCrops);
        AppendMetric(html, "Missing macOS crops", document.Summary.MissingMacRuntimeCrops);
        AppendMetric(html, "Missing diffs", document.Summary.MissingDiffCrops);
        AppendMetric(html, "Missing inspections", document.Summary.MissingInspectionNotes);
        html.AppendLine("</div>");

        foreach (var row in document.Rows)
        {
            html.AppendLine("<section class=\"component\">");
            html.AppendLine($"<h2>{Escape(row.Component)}{TargetSuffix(row.Target)}</h2>");
            html.AppendLine("<div class=\"metadata\">");
            AppendMetadata(html, "catalog", row.CatalogStatus);
            AppendMetadata(html, "visual", row.VisualGrade);
            AppendMetadata(html, "native quality", row.NativeQualityGrade);
            AppendMetadata(html, "crop", row.CropStatus);
            AppendMetadata(html, "review", row.ReviewStatus);
            AppendMetadata(html, "inspection", row.InspectionStatus);
            AppendMetadata(html, "reference source", row.ReferenceSource);
            AppendMetadata(html, "reference run", row.NativeReferenceRunId);
            AppendMetadata(html, "reference commit", ShortCommit(row.ReferenceCommitSha));
            if (row.ChangedPixelPercentage is { } changed)
            {
                AppendMetadata(html, "changed pixels", changed.ToString("0.###"));
            }
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"triptych\">");
            AppendFigure(html, "Native WinUI reference", row.NativeReferenceCropPath);
            AppendFigure(html, "macOS runtime", row.MacRuntimeCropPath);
            AppendFigure(html, "Pixel diff", row.PixelDiffPath);
            html.AppendLine("</div>");
            html.AppendLine($"<p class=\"notes\">{Escape(row.Notes)}</p>");
            html.AppendLine("</section>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static void AppendMetric(StringBuilder html, string label, int value)
    {
        html.AppendLine($"<div class=\"metric\"><strong>{value}</strong>{Escape(label)}</div>");
    }

    private static void AppendMetadata(StringBuilder html, string label, string value)
    {
        html.AppendLine($"<span>{Escape(label)}: <strong>{Escape(value)}</strong></span>");
    }

    private static void AppendFigure(StringBuilder html, string label, string? path)
    {
        html.AppendLine("<figure>");
        html.AppendLine($"<figcaption>{Escape(label)}</figcaption>");
        if (path is null)
        {
            html.AppendLine($"<div class=\"missing\">Missing {Escape(label)} crop</div>");
        }
        else
        {
            html.AppendLine($"<img alt=\"{Escape(label)} crop\" src=\"{Escape(path)}\">");
        }

        html.AppendLine("</figure>");
    }

    private static string TargetSuffix(string? target)
    {
        return string.IsNullOrWhiteSpace(target)
            ? string.Empty
            : $" <code>{Escape(target)}</code>";
    }

    private static string ShortCommit(string commitSha)
    {
        return commitSha.Length >= 7 ? commitSha[..7] : commitSha;
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }

    private static string Escape(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
