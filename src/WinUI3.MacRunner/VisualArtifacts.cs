using System.Text.Json;
using WinUI3.MacCompat.Diagnostics;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;

internal sealed record VisualRunReport(
    string SchemaVersion,
    string FixtureName,
    string ScenarioName,
    string RunnerOs,
    string OsImage,
    string Renderer,
    string RendererVersion,
    VisualViewport Viewport,
    double Scale,
    string Theme,
    string? ReferenceImagePath,
    string RuntimeImagePath,
    string? DiffImagePath,
    VisualThresholds Thresholds,
    object? Comparison,
    IReadOnlyList<UnsupportedApiEntry> UnsupportedVisualFeatures,
    string Status);

internal sealed record SkippedPixelDiff(
    string SchemaVersion,
    string Status,
    string Reason,
    VisualThresholds Thresholds);

internal static class VisualArtifacts
{
    public static async Task<bool> WriteAsync(
        MacRunResult result,
        VisualRunSettings settings,
        string? referencePath,
        string diffOutputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(diffOutputDirectory);

        var outputDirectory = Path.GetFullPath(diffOutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var runtimePath = Path.Combine(outputDirectory, "mac-runtime.png");
        File.Copy(result.Snapshot.FilePath, runtimePath, overwrite: true);

        string? copiedReferencePath = null;
        string? diffPath = null;
        object? comparison;
        if (!string.IsNullOrWhiteSpace(referencePath))
        {
            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Reference image was not found.", referencePath);
            }

            copiedReferencePath = Path.Combine(outputDirectory, "windows-reference.png");
            File.Copy(referencePath, copiedReferencePath, overwrite: true);
            diffPath = Path.Combine(outputDirectory, "pixel-diff.png");
            var diff = PixelDiff.Compare(copiedReferencePath, runtimePath, diffPath, settings.Thresholds);
            comparison = diff;
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "pixel-diff.json"),
                JsonSerializer.Serialize(diff, JsonDefaults.Options),
                cancellationToken);
        }
        else
        {
            comparison = new SkippedPixelDiff(
                SchemaVersion: ArtifactSchemas.PixelDiff,
                Status: "skipped",
                Reason: "No Windows reference image was provided.",
                Thresholds: settings.Thresholds);
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "pixel-diff.json"),
                JsonSerializer.Serialize(comparison, JsonDefaults.Options),
                cancellationToken);
        }

        var unsupportedVisualFeatures = result.Run.Diagnostics
            .Where(diagnostic => diagnostic.StartsWith("unsupported-api:", StringComparison.Ordinal))
            .Select(diagnostic => diagnostic["unsupported-api:".Length..])
            .Where(diagnostic => diagnostic.Contains(":unsupported", StringComparison.Ordinal))
            .Select(diagnostic => new UnsupportedApiEntry(
                Api: diagnostic[..diagnostic.LastIndexOf(":unsupported", StringComparison.Ordinal)],
                Kind: "visual-or-compat-api",
                Status: "unsupported",
                FirstSeenIn: settings.Renderer))
            .ToArray();

        var diffFailed = comparison is PixelDiffResult { Status: "failed" };
        var status = settings.StrictVisual && (result.Run.Status != "passed" || diffFailed)
            ? "failed"
            : "passed";

        var report = new VisualRunReport(
            SchemaVersion: ArtifactSchemas.VisualRun,
            FixtureName: settings.Scenario?.FixtureName ?? "fixture",
            ScenarioName: settings.ScenarioName,
            RunnerOs: Environment.OSVersion.Platform.ToString(),
            OsImage: Environment.GetEnvironmentVariable("ImageOS") ??
                Environment.GetEnvironmentVariable("ImageVersion") ??
                Environment.OSVersion.VersionString,
            Renderer: settings.Renderer,
            RendererVersion: result.Snapshot.SchemaVersion,
            Viewport: settings.Viewport,
            Scale: settings.Scale,
            Theme: settings.Theme,
            ReferenceImagePath: copiedReferencePath,
            RuntimeImagePath: runtimePath,
            DiffImagePath: diffPath,
            Thresholds: settings.Thresholds,
            Comparison: comparison,
            UnsupportedVisualFeatures: unsupportedVisualFeatures,
            Status: status);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "visual-run.json"),
            JsonSerializer.Serialize(report, JsonDefaults.Options),
            cancellationToken);

        return status == "passed";
    }
}
