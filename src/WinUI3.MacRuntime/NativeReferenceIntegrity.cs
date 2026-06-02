using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record NativeReferenceIntegrityAnnotationResult(
    int EvidenceFileCount,
    int ComponentCount,
    int AnnotatedComponentCount);

public static class NativeReferenceIntegrityAnnotator
{
    public static NativeReferenceIntegrityAnnotationResult AnnotatePublicEvidence(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var evidenceFiles = PublicEvidenceDiscovery.FindCanonicalEvidenceFiles(root);
        var readiness = LoadReadiness(root);
        var componentCount = 0;
        var annotatedCount = 0;

        foreach (var evidenceFile in evidenceFiles)
        {
            using var stream = File.OpenRead(evidenceFile);
            var evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(stream, JsonDefaults.Options)
                ?? throw new InvalidOperationException($"Could not read component evidence from '{evidenceFile}'.");
            var components = evidence.Components
                .Select(component =>
                {
                    componentCount++;
                    if (component.Crop is not { } crop)
                    {
                        return component;
                    }

                    var updatedCrop = AnnotateCrop(evidence.ScenarioName, component, crop, readiness);
                    if (!ReferenceEquals(crop, updatedCrop))
                    {
                        annotatedCount++;
                    }

                    return component with { Crop = updatedCrop };
                })
                .ToArray();

            var updatedEvidence = evidence with
            {
                SchemaVersion = ArtifactSchemas.ComponentEvidence,
                Components = components
            };
            File.WriteAllText(evidenceFile, JsonSerializer.Serialize(updatedEvidence, JsonDefaults.Options));
        }

        return new NativeReferenceIntegrityAnnotationResult(
            evidenceFiles.Count,
            componentCount,
            annotatedCount);
    }

    private static ComponentCropEvidence AnnotateCrop(
        string scenarioName,
        ComponentEvidenceEntry component,
        ComponentCropEvidence crop,
        IReadOnlyDictionary<string, NativeReferenceReadinessOverride> readiness)
    {
        var key = RowKey(scenarioName, component.Component, component.Target);
        readiness.TryGetValue(key, out var readinessOverride);
        if (crop.NativeReferenceBounds is not null &&
            crop.NativeReferenceBoundsValidForPromotion &&
            crop.NativeReferenceReadinessStatus is "ready" or "verified")
        {
            var reason = string.IsNullOrWhiteSpace(crop.NativeReferenceReadinessReason)
                ? "Native crop uses Windows native element bounds from native-reference-targets.json."
                : crop.NativeReferenceReadinessReason;
            var requiredAction = string.IsNullOrWhiteSpace(crop.NativeReferenceRequiredAction)
                ? "Keep the native target export with the Windows reference artifact."
                : crop.NativeReferenceRequiredAction;
            return crop with
            {
                NativeReferenceIntegrityBlockerReason = "none",
                NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                    crop.NativeReferenceReadinessStatus,
                    reason,
                    requiredAction,
                    ReadyForPromotion: true,
                    "none"),
                NativeReferenceCropSize = crop.NativeReferenceCropSize ?? CropSize(crop.NativeReferenceBounds),
                MacRuntimeCropSize = crop.MacRuntimeCropSize ?? (crop.Bounds is null ? null : CropSize(crop.Bounds)),
                NativeReferenceBoundsDelta = crop.NativeReferenceBoundsDelta ??
                    (crop.Bounds is null ? null : BoundsDelta(crop.NativeReferenceBounds, crop.Bounds))
            };
        }

        if (!string.IsNullOrWhiteSpace(crop.NativeReferencePath))
        {
            var hasWindowsNativeBounds = crop.NativeReferenceBounds is not null &&
                string.Equals(crop.NativeReferenceBoundsSource, "windows-native-element-bounds", StringComparison.Ordinal);
            var useCropReadiness = hasWindowsNativeBounds &&
                !string.IsNullOrWhiteSpace(crop.NativeReferenceReadinessStatus) &&
                !IsSemanticReadinessOverride(readinessOverride?.Status);
            var status = useCropReadiness
                ? crop.NativeReferenceReadinessStatus
                : readinessOverride?.Status ?? "needs-native-crop-bounds";
            var reason = useCropReadiness
                ? crop.NativeReferenceReadinessReason
                : readinessOverride?.Reason ??
                "Current native crop is legacy evidence without Windows native element bounds.";
            var requiredAction = useCropReadiness
                ? crop.NativeReferenceRequiredAction
                : readinessOverride?.RequiredAction ??
                "Re-run the Windows native reference workflow with native-reference-targets.json and regenerate component evidence.";
            return crop with
            {
                NativeReferenceReadinessStatus = status,
                NativeReferenceReadinessReason = reason,
                NativeReferenceRequiredAction = requiredAction,
                NativeReferenceBoundsSource = string.IsNullOrWhiteSpace(crop.NativeReferenceBoundsSource) ||
                    crop.NativeReferenceBoundsSource == "missing"
                        ? "mac-runtime-layout-bounds"
                        : crop.NativeReferenceBoundsSource,
                NativeReferenceBoundsValidForPromotion = false,
                NativeReferenceIntegrityBlockerReason = reason,
                NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                    status,
                    reason,
                    requiredAction,
                    ReadyForPromotion: false,
                    reason),
                NativeReferenceCropSize = crop.NativeReferenceCropSize ?? (crop.NativeReferenceBounds is null ? null : CropSize(crop.NativeReferenceBounds)),
                MacRuntimeCropSize = crop.MacRuntimeCropSize ?? (crop.Bounds is null ? null : CropSize(crop.Bounds)),
                NativeReferenceBoundsDelta = crop.NativeReferenceBoundsDelta ??
                    (crop.NativeReferenceBounds is null || crop.Bounds is null ? null : BoundsDelta(crop.NativeReferenceBounds, crop.Bounds))
            };
        }

        var missingStatus = readinessOverride?.Status ?? "missing-native-reference-crop";
        var missingReason = readinessOverride?.Reason ?? "No native reference crop is available.";
        var missingAction = readinessOverride?.RequiredAction ??
            "Capture a native Windows reference with exported target bounds.";
        return crop with
        {
            NativeReferenceReadinessStatus = missingStatus,
            NativeReferenceReadinessReason = missingReason,
            NativeReferenceRequiredAction = missingAction,
            NativeReferenceBoundsSource = "missing",
            NativeReferenceBoundsValidForPromotion = false,
            NativeReferenceIntegrityBlockerReason = missingReason,
            NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                missingStatus,
                missingReason,
                missingAction,
                ReadyForPromotion: false,
                missingReason),
            NativeReferenceCropSize = null,
            MacRuntimeCropSize = crop.Bounds is null ? null : CropSize(crop.Bounds),
            NativeReferenceBoundsDelta = null
        };
    }

    private static ReferenceImageDimensions CropSize(ComponentCropBounds bounds)
    {
        return new ReferenceImageDimensions(bounds.Width, bounds.Height);
    }

    private static ComponentCropBoundsDelta BoundsDelta(ComponentCropBounds nativeReferenceBounds, ComponentCropBounds macRuntimeBounds)
    {
        return new ComponentCropBoundsDelta(
            nativeReferenceBounds.X - macRuntimeBounds.X,
            nativeReferenceBounds.Y - macRuntimeBounds.Y,
            nativeReferenceBounds.Width - macRuntimeBounds.Width,
            nativeReferenceBounds.Height - macRuntimeBounds.Height);
    }

    private static IReadOnlyDictionary<string, NativeReferenceReadinessOverride> LoadReadiness(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, NativeReferenceReadinessOverride>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<string, NativeReferenceReadinessOverride>(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var row in rows.EnumerateArray())
        {
            var scenarioName = ReadString(row, "scenarioName");
            var component = ReadString(row, "component");
            if (string.IsNullOrWhiteSpace(scenarioName) || string.IsNullOrWhiteSpace(component))
            {
                continue;
            }

            result[RowKey(scenarioName, component, ReadString(row, "target"))] = new NativeReferenceReadinessOverride(
                ReadString(row, "nativeReferenceStatus") ?? "needs-native-crop-bounds",
                ReadString(row, "reason") ?? "Native reference source readiness is not proven.",
                ReadString(row, "requiredAction") ?? "Capture Windows native element bounds and regenerate evidence.");
        }

        return result;
    }

    private static bool IsSemanticReadinessOverride(string? status)
    {
        return status is
            "diagnostic-reference" or
            "diagnostic-reference-needs-crop-bounds" or
            "native-not-rendered" or
            "native-unavailable" or
            "offscreen-reference" or
            "placeholder-reference" or
            "state-reference-incomplete";
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string RowKey(string scenarioName, string component, string? target)
    {
        return $"{scenarioName}\u001f{component}\u001f{target ?? string.Empty}";
    }

    private sealed record NativeReferenceReadinessOverride(
        string Status,
        string Reason,
        string RequiredAction);
}
