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
    SnapshotFontDiagnostics? FontDiagnostics,
    VisualViewport Viewport,
    double Scale,
    string Theme,
    string? ReferenceImagePath,
    string? ReferenceMetadataPath,
    string? ReferenceSource,
    JsonElement? ReferenceProvenance,
    string RuntimeImagePath,
    string? DiffImagePath,
    string? ComponentEvidencePath,
    string? ComponentCropDirectory,
    string? VisualReviewPath,
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
        string? copiedReferenceMetadataPath = null;
        JsonElement? referenceProvenance = null;
        NativeReferenceProvenance? nativeReferenceProvenance = null;
        NativeReferenceTargetDocument? nativeReferenceTargets = null;
        string? referenceSource = null;
        string? diffPath = null;
        object? comparison;
        ComponentDiffMetrics? componentMetrics = null;
        if (!string.IsNullOrWhiteSpace(referencePath))
        {
            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Reference image was not found.", referencePath);
            }

            copiedReferencePath = Path.Combine(outputDirectory, "windows-reference.png");
            File.Copy(referencePath, copiedReferencePath, overwrite: true);
            var sourceReferenceMetadataPath = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(referencePath))!,
                "windows-reference.json");
            if (File.Exists(sourceReferenceMetadataPath))
            {
                copiedReferenceMetadataPath = Path.Combine(outputDirectory, "windows-reference.json");
                File.Copy(sourceReferenceMetadataPath, copiedReferenceMetadataPath, overwrite: true);
                referenceProvenance = await ReadReferenceProvenanceAsync(sourceReferenceMetadataPath, cancellationToken);
                nativeReferenceProvenance = await ReadNativeReferenceProvenanceAsync(sourceReferenceMetadataPath, cancellationToken);
                referenceSource = ReadReferenceSource(referenceProvenance);
                var sourceReferenceTargetsPath = ResolveNativeReferenceTargetsPath(
                    sourceReferenceMetadataPath,
                    nativeReferenceProvenance?.NativeReferenceTargetsPath);
                if (sourceReferenceTargetsPath is not null)
                {
                    var copiedReferenceTargetsPath = Path.Combine(outputDirectory, "native-reference-targets.json");
                    File.Copy(sourceReferenceTargetsPath, copiedReferenceTargetsPath, overwrite: true);
                    nativeReferenceTargets = await ReadNativeReferenceTargetsAsync(copiedReferenceTargetsPath, cancellationToken);
                }
            }

            diffPath = Path.Combine(outputDirectory, "pixel-diff.png");
            var diff = PixelDiff.Compare(copiedReferencePath, runtimePath, diffPath, settings.Thresholds);
            comparison = diff;
            componentMetrics = new ComponentDiffMetrics(
                diff.ChangedPixelPercentage,
                diff.MeanAbsoluteError,
                diff.RootMeanSquaredError);
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
            .Select(diagnostic => ParseUnsupportedApiDiagnostic(diagnostic, settings.Renderer))
            .ToArray();

        ComponentEvidenceDocument? componentEvidence = null;
        string? visualReviewPath = null;
        if (settings.Scenario is not null &&
            (settings.Scenario.Requirements.Count > 0 || settings.Scenario.SourceFeatures.Count > 0))
        {
            componentEvidence = ComponentEvidenceBuilder.Build(
                settings.Scenario,
                result.Tree,
                await ReadInteractionReportAsync(result.InteractionJsonPath, cancellationToken),
                componentMetrics,
                result.Snapshot.FontDiagnostics);
            componentEvidence = ComponentCropper.WriteCrops(
                componentEvidence,
                runtimePath,
                copiedReferencePath,
                outputDirectory,
                settings.Scale,
                settings.Thresholds,
                nativeReferenceProvenance,
                useRelativePaths: true,
                nativeReferenceTargets: nativeReferenceTargets);
            var componentEvidencePath = Path.Combine(outputDirectory, "component-evidence.json");
            await File.WriteAllTextAsync(
                componentEvidencePath,
                JsonSerializer.Serialize(componentEvidence, JsonDefaults.Options),
                cancellationToken);
            visualReviewPath = VisualReviewArtifacts.Write(componentEvidencePath, outputDirectory).HtmlPath;
        }

        var diffFailed = comparison is PixelDiffResult { Status: "failed" };
        var evidenceFailed = componentEvidence?.Status == "failed";
        var status = settings.StrictVisual && (result.Run.Status != "passed" || diffFailed || evidenceFailed)
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
            FontDiagnostics: result.Snapshot.FontDiagnostics,
            Viewport: settings.Viewport,
            Scale: settings.Scale,
            Theme: settings.Theme,
            ReferenceImagePath: ArtifactPath(outputDirectory, copiedReferencePath),
            ReferenceMetadataPath: ArtifactPath(outputDirectory, copiedReferenceMetadataPath),
            ReferenceSource: referenceSource,
            ReferenceProvenance: referenceProvenance,
            RuntimeImagePath: ArtifactPath(outputDirectory, runtimePath)!,
            DiffImagePath: ArtifactPath(outputDirectory, diffPath),
            ComponentEvidencePath: componentEvidence is null ? null : ArtifactPath(outputDirectory, Path.Combine(outputDirectory, "component-evidence.json")),
            ComponentCropDirectory: componentEvidence is null ? null : ArtifactPath(outputDirectory, Path.Combine(outputDirectory, "components")),
            VisualReviewPath: ArtifactPath(outputDirectory, visualReviewPath),
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

    private static string? ArtifactPath(string outputDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetRelativePath(outputDirectory, path).Replace('\\', '/');
    }

    private static async Task<JsonElement?> ReadReferenceProvenanceAsync(
        string metadataPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(metadataPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static async Task<NativeReferenceProvenance?> ReadNativeReferenceProvenanceAsync(
        string metadataPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(metadataPath);
        return await JsonSerializer.DeserializeAsync<NativeReferenceProvenance>(
            stream,
            JsonDefaults.Options,
            cancellationToken);
    }

    private static async Task<NativeReferenceTargetDocument?> ReadNativeReferenceTargetsAsync(
        string targetsPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(targetsPath);
        return await JsonSerializer.DeserializeAsync<NativeReferenceTargetDocument>(
            stream,
            JsonDefaults.Options,
            cancellationToken);
    }

    private static string? ResolveNativeReferenceTargetsPath(
        string metadataPath,
        string? declaredTargetsPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(metadataPath))!;
        if (!string.IsNullOrWhiteSpace(declaredTargetsPath))
        {
            var candidate = Path.IsPathRooted(declaredTargetsPath)
                ? Path.GetFullPath(declaredTargetsPath)
                : Path.GetFullPath(Path.Combine(directory, declaredTargetsPath));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var sibling = Path.Combine(directory, "native-reference-targets.json");
        return File.Exists(sibling) ? sibling : null;
    }

    private static string? ReadReferenceSource(JsonElement? provenance)
    {
        if (provenance is { } value &&
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("referenceSource", out var referenceSource) &&
            referenceSource.ValueKind == JsonValueKind.String)
        {
            return referenceSource.GetString();
        }

        return null;
    }

    private static async Task<InteractionReport?> ReadInteractionReportAsync(
        string? interactionJsonPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(interactionJsonPath) || !File.Exists(interactionJsonPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(interactionJsonPath);
        return await JsonSerializer.DeserializeAsync<InteractionReport>(stream, JsonDefaults.Options, cancellationToken);
    }

    private static UnsupportedApiEntry ParseUnsupportedApiDiagnostic(string diagnostic, string renderer)
    {
        var statusSeparator = diagnostic.LastIndexOf(':');
        if (statusSeparator <= 0)
        {
            return new UnsupportedApiEntry(
                Api: diagnostic,
                Kind: "visual-or-compat-api",
                Status: "unknown",
                FirstSeenIn: renderer);
        }

        return new UnsupportedApiEntry(
            Api: diagnostic[..statusSeparator],
            Kind: "visual-or-compat-api",
            Status: diagnostic[(statusSeparator + 1)..],
            FirstSeenIn: renderer);
    }
}
