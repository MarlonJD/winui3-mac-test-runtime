using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record ComponentQualityDashboardDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string EvidenceRoot,
    ComponentQualityTotals Totals,
    IReadOnlyList<ComponentQualityScenario> Scenarios,
    IReadOnlyList<ComponentQualityRow> Rows,
    IReadOnlyList<ComponentQualityBlocker> Blockers,
    string Status);

public sealed record ComponentQualityTotals(
    int ScenarioCount,
    int ComponentCount,
    IReadOnlyDictionary<string, int> VisualGradeCounts,
    IReadOnlyDictionary<string, int> NativeQualityGradeCounts,
    int MissingMacRuntimeCrops,
    int MissingNativeReferenceCrops,
    int MissingNativeReferenceProvenance,
    int MissingComponentDiffs,
    int MissingInspectionNotes,
    int BlockingRowCount,
    IReadOnlyDictionary<string, int> NativeReferenceReadinessCounts,
    int InvalidNativeReferenceRows,
    int UntrustedNativeReferenceRows,
    int ReferenceIntegrityBlockingRowCount);

public sealed record ComponentQualityScenario(
    string ScenarioName,
    string EvidencePath,
    int ComponentCount,
    int BlockingRowCount,
    string Status);

public sealed record ComponentQualityRow(
    string ScenarioName,
    string EvidencePath,
    string Component,
    string? Target,
    string OwnerFamily,
    string CatalogStatus,
    string VisualGrade,
    string NativeQualityGrade,
    string TargetGrade,
    IReadOnlyList<string> RequiredStates,
    IReadOnlyList<string> RequiredScenarios,
    string RemainingBlocker,
    bool NativeReferenceCropExists,
    bool NativeReferenceCropValid,
    bool NativeReferenceHasWindowsNativeElementBounds,
    bool NativeReferenceDiagnosticOrPlaceholder,
    string NativeReferenceReadiness,
    string NativeReferenceBoundsSource,
    string NativeReferenceStatus,
    string NativeReferenceRequiredAction,
    string NativeReferenceIntegrityBlockerReason);

public sealed record ComponentQualityBlocker(
    string ScenarioName,
    string EvidencePath,
    string Component,
    string? Target,
    string CatalogStatus,
    string VisualGrade,
    string NativeQualityGrade,
    IReadOnlyList<string> Reasons);

public static class ComponentQualityDashboard
{
    private static readonly HashSet<string> FinalVisualGrades = new(StringComparer.Ordinal)
    {
        "good",
        "production-ready"
    };

    private static readonly HashSet<string> BlockingVisualGrades = new(StringComparer.Ordinal)
    {
        "not-rendered",
        "poor",
        "weak",
        "usable"
    };

    public static ComponentQualityDashboardDocument BuildFromPublicEvidence(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var evidenceRoot = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples");
        var evidenceFiles = Directory.Exists(evidenceRoot)
            ? Directory.EnumerateFiles(evidenceRoot, "component-evidence.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : Array.Empty<string>();

        var scenarios = new List<ComponentQualityScenario>();
        var rows = new List<ComponentQualityRow>();
        var blockers = new List<ComponentQualityBlocker>();
        var visualGradeCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var nativeQualityGradeCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var nativeReferenceReadinessCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var readinessOverrides = LoadNativeReferenceReadiness(repositoryRoot);
        var componentCount = 0;
        var missingMacRuntimeCrops = 0;
        var missingNativeReferenceCrops = 0;
        var missingNativeReferenceProvenance = 0;
        var missingComponentDiffs = 0;
        var missingInspectionNotes = 0;
        var invalidNativeReferenceRows = 0;
        var untrustedNativeReferenceRows = 0;
        var referenceIntegrityBlockingRows = 0;

        foreach (var evidenceFile in evidenceFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(evidenceFile));
            var root = document.RootElement;
            var scenarioName = ReadString(root, "scenarioName") ?? Path.GetFileName(Path.GetDirectoryName(evidenceFile)) ?? "unknown";
            var scenarioBlockers = 0;
            var scenarioComponents = root.GetProperty("components").EnumerateArray().ToArray();
            componentCount += scenarioComponents.Length;

            foreach (var component in scenarioComponents)
            {
                var visualGrade = ReadString(component, "visualGrade") ?? "missing";
                var nativeQualityGrade = ReadString(component, "nativeQualityGrade") ?? "missing";
                Increment(visualGradeCounts, visualGrade);
                Increment(nativeQualityGradeCounts, nativeQualityGrade);
                var componentName = ReadString(component, "component") ?? "unknown";
                var target = ReadString(component, "target");
                var scenarioPath = RelativePath(repositoryRoot, evidenceFile);
                var nativeReadiness = NativeReferenceReadinessFor(component, scenarioName, componentName, target, readinessOverrides);
                Increment(nativeReferenceReadinessCounts, nativeReadiness.Status);
                if (!nativeReadiness.ValidForPromotion)
                {
                    invalidNativeReferenceRows++;
                }

                if (!nativeReadiness.TrustedForPromotion)
                {
                    untrustedNativeReferenceRows++;
                }

                if (nativeReadiness.BlocksPromotion)
                {
                    referenceIntegrityBlockingRows++;
                }

                var reasons = BlockingReasons(component, visualGrade, nativeQualityGrade, nativeReadiness);
                rows.Add(new ComponentQualityRow(
                    scenarioName,
                    scenarioPath,
                    componentName,
                    target,
                    OwnerFamilyForScenario(scenarioName),
                    ReadString(component, "catalogStatus") ?? "unknown",
                    visualGrade,
                    nativeQualityGrade,
                    "good-or-production-ready",
                    RequiredStatesForScenario(scenarioName),
                    new[] { scenarioName },
                    reasons.Count == 0 ? "none" : string.Join(" ", reasons),
                    nativeReadiness.CropExists,
                    nativeReadiness.ValidForPromotion,
                    nativeReadiness.HasWindowsNativeElementBounds,
                    nativeReadiness.IsDiagnosticOrPlaceholder,
                    nativeReadiness.Status,
                    nativeReadiness.BoundsSource,
                    nativeReadiness.Status,
                    nativeReadiness.RequiredAction,
                    nativeReadiness.IntegrityBlockerReason));
                if (!HasMacRuntimeCrop(component))
                {
                    missingMacRuntimeCrops++;
                }

                if (!HasNativeReferenceCrop(component))
                {
                    missingNativeReferenceCrops++;
                }

                if (!HasNativeReferenceProvenance(component))
                {
                    missingNativeReferenceProvenance++;
                }

                if (!HasComponentDiff(component))
                {
                    missingComponentDiffs++;
                }

                if (!HasInspection(component))
                {
                    missingInspectionNotes++;
                }

                if (reasons.Count == 0)
                {
                    continue;
                }

                scenarioBlockers++;
                blockers.Add(new ComponentQualityBlocker(
                    scenarioName,
                    scenarioPath,
                    ReadString(component, "component") ?? "unknown",
                    ReadString(component, "target"),
                    ReadString(component, "catalogStatus") ?? "unknown",
                    visualGrade,
                    nativeQualityGrade,
                    reasons));
            }

            scenarios.Add(new ComponentQualityScenario(
                scenarioName,
                RelativePath(repositoryRoot, evidenceFile),
                scenarioComponents.Length,
                scenarioBlockers,
                scenarioBlockers == 0 ? "passed" : "blocked"));
        }

        var totals = new ComponentQualityTotals(
            evidenceFiles.Length,
            componentCount,
            visualGradeCounts,
            nativeQualityGradeCounts,
            missingMacRuntimeCrops,
            missingNativeReferenceCrops,
            missingNativeReferenceProvenance,
            missingComponentDiffs,
            missingInspectionNotes,
            blockers.Count,
            nativeReferenceReadinessCounts,
            invalidNativeReferenceRows,
            untrustedNativeReferenceRows,
            referenceIntegrityBlockingRows);

        return new ComponentQualityDashboardDocument(
            ArtifactSchemas.ComponentQualityDashboard,
            DateTimeOffset.UnixEpoch,
            RelativePath(repositoryRoot, evidenceRoot),
            totals,
            scenarios,
            rows,
            blockers,
            blockers.Count == 0 ? "passed" : "blocked");
    }

    private static IReadOnlyList<string> BlockingReasons(
        JsonElement component,
        string visualGrade,
        string nativeQualityGrade,
        NativeReferenceReadiness nativeReadiness)
    {
        var reasons = new List<string>();

        if (BlockingVisualGrades.Contains(visualGrade))
        {
            reasons.Add($"visualGrade is '{visualGrade}', below the native-quality target.");
        }

        if (!FinalVisualGrades.Contains(nativeQualityGrade))
        {
            reasons.Add($"nativeQualityGrade is '{nativeQualityGrade}', not good or production-ready.");
        }

        if (!HasMacRuntimeCrop(component))
        {
            reasons.Add("Missing macOS component crop evidence.");
        }

        if (!HasNativeReferenceCrop(component))
        {
            reasons.Add("Missing native WinUI reference crop evidence.");
        }

        if (!HasNativeReferenceProvenance(component))
        {
            reasons.Add("Missing native WinUI reference provenance.");
        }

        if (nativeReadiness.BlocksPromotion)
        {
            reasons.Add($"Native reference status is '{nativeReadiness.Status}': {nativeReadiness.Reason}");
        }

        if (!HasComponentDiff(component))
        {
            reasons.Add("Missing component-level diff metrics.");
        }

        if (!HasInspection(component))
        {
            reasons.Add("Missing manual screenshot inspection metadata.");
        }

        return reasons;
    }

    private static NativeReferenceReadiness NativeReferenceReadinessFor(
        JsonElement component,
        string scenarioName,
        string componentName,
        string? target,
        IReadOnlyDictionary<string, NativeReferenceReadinessOverride> overrides)
    {
        var cropExists = HasNativeReferenceCrop(component);
        var hasWindowsNativeElementBounds = TryGetCrop(component, out var crop) &&
            crop.TryGetProperty("nativeReferenceBounds", out var nativeBounds) &&
            nativeBounds.ValueKind is JsonValueKind.Object &&
            string.Equals(ReadString(crop, "nativeReferenceBoundsSource"), "windows-native-element-bounds", StringComparison.Ordinal);
        var boundsSource = TryGetCrop(component, out crop)
            ? ReadString(crop, "nativeReferenceBoundsSource") ?? "missing"
            : "missing";
        var validForPromotion = TryGetCrop(component, out crop) &&
            crop.TryGetProperty("nativeReferenceBoundsValidForPromotion", out var validProperty) &&
            validProperty.ValueKind == JsonValueKind.True &&
            hasWindowsNativeElementBounds;
        var status = TryGetCrop(component, out crop)
            ? ReadNativeReferenceReadinessString(crop, "status") ??
                ReadString(crop, "nativeReferenceReadinessStatus") ??
                "needs-native-crop-bounds"
            : "missing-native-reference-crop";
        var reason = TryGetCrop(component, out crop)
            ? ReadNativeReferenceReadinessString(crop, "reason") ??
                ReadString(crop, "nativeReferenceReadinessReason") ??
                "Native reference crop integrity is not proven."
            : "No native reference crop is available.";
        var requiredAction = TryGetCrop(component, out crop)
            ? ReadNativeReferenceReadinessString(crop, "requiredAction") ??
                ReadString(crop, "nativeReferenceRequiredAction") ??
                "Capture Windows native element bounds and regenerate evidence."
            : "Capture Windows native reference crop evidence.";
        var integrityBlockerReason = TryGetCrop(component, out crop)
            ? ReadNativeReferenceReadinessString(crop, "blockerReason") ??
                ReadString(crop, "nativeReferenceIntegrityBlockerReason") ??
                reason
            : "No native reference crop is available.";

        if (overrides.TryGetValue(RowKey(scenarioName, componentName, target), out var readinessOverride))
        {
            status = readinessOverride.Status;
            reason = readinessOverride.Reason;
            requiredAction = readinessOverride.RequiredAction;
            integrityBlockerReason = readinessOverride.Reason;
            validForPromotion = status is "ready" or "verified" && validForPromotion;
        }

        var diagnosticOrPlaceholder = status.Contains("diagnostic", StringComparison.Ordinal) ||
            status.Contains("placeholder", StringComparison.Ordinal) ||
            status.Contains("offscreen", StringComparison.Ordinal) ||
            status.Contains("state-reference-incomplete", StringComparison.Ordinal) ||
            status.Contains("native-not-rendered", StringComparison.Ordinal) ||
            status.Contains("native-unavailable", StringComparison.Ordinal);
        var readinessAllowsPromotion = status is "ready" or "verified";
        var trustedForPromotion = cropExists &&
            validForPromotion &&
            readinessAllowsPromotion &&
            hasWindowsNativeElementBounds &&
            HasNativeReferenceProvenance(component);
        return new NativeReferenceReadiness(
            status,
            cropExists,
            validForPromotion && readinessAllowsPromotion,
            trustedForPromotion,
            hasWindowsNativeElementBounds,
            diagnosticOrPlaceholder,
            !validForPromotion || !readinessAllowsPromotion,
            reason,
            requiredAction,
            boundsSource,
            integrityBlockerReason);
    }

    private static bool HasMacRuntimeCrop(JsonElement component)
    {
        return TryGetCrop(component, out var crop) &&
            !string.IsNullOrWhiteSpace(ReadString(crop, "macRuntimePath"));
    }

    private static bool HasNativeReferenceCrop(JsonElement component)
    {
        return TryGetCrop(component, out var crop) &&
            !string.IsNullOrWhiteSpace(ReadString(crop, "nativeReferencePath"));
    }

    private static bool HasNativeReferenceProvenance(JsonElement component)
    {
        if (!TryGetCrop(component, out var crop) ||
            !crop.TryGetProperty("nativeReferenceProvenance", out var provenance) ||
            provenance.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ReadString(provenance, "referenceSource")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "fixtureProjectPath")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "scenarioPath")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "commitSha")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "workflowRunId")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "runnerImage")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "theme")) &&
            !string.IsNullOrWhiteSpace(ReadString(provenance, "captureMode")) &&
            provenance.TryGetProperty("viewport", out var viewport) &&
            viewport.ValueKind == JsonValueKind.Object &&
            provenance.TryGetProperty("scale", out var scale) &&
            scale.ValueKind == JsonValueKind.Number;
    }

    private static bool HasComponentDiff(JsonElement component)
    {
        return TryGetCrop(component, out var crop) &&
            crop.TryGetProperty("changedPixelPercentage", out var changed) &&
            changed.ValueKind == JsonValueKind.Number &&
            crop.TryGetProperty("meanAbsoluteError", out var mean) &&
            mean.ValueKind == JsonValueKind.Number &&
            crop.TryGetProperty("rootMeanSquaredError", out var rms) &&
            rms.ValueKind == JsonValueKind.Number &&
            !string.IsNullOrWhiteSpace(ReadString(crop, "pixelDiffPath"));
    }

    private static bool HasInspection(JsonElement component)
    {
        if (!component.TryGetProperty("inspection", out var inspection) ||
            inspection.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ReadString(inspection, "inspectedBy")) &&
            !string.IsNullOrWhiteSpace(ReadString(inspection, "inspectedDate")) &&
            !string.IsNullOrWhiteSpace(ReadString(inspection, "nativeReferenceRunId")) &&
            !string.IsNullOrWhiteSpace(ReadString(inspection, "notes")) &&
            inspection.TryGetProperty("comparisonArtifactPaths", out var paths) &&
            paths.ValueKind == JsonValueKind.Array &&
            paths.GetArrayLength() > 0;
    }

    private static IReadOnlyDictionary<string, NativeReferenceReadinessOverride> LoadNativeReferenceReadiness(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, NativeReferenceReadinessOverride>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, NativeReferenceReadinessOverride>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, NativeReferenceReadinessOverride>(StringComparer.Ordinal);
        foreach (var row in rows.EnumerateArray())
        {
            var scenarioName = ReadString(row, "scenarioName");
            var component = ReadString(row, "component");
            if (string.IsNullOrWhiteSpace(scenarioName) || string.IsNullOrWhiteSpace(component))
            {
                continue;
            }

            var target = ReadString(row, "target");
            var status = ReadString(row, "nativeReferenceStatus") ?? "needs-native-crop-bounds";
            var reason = ReadString(row, "reason") ?? "Native reference source readiness is not proven.";
            var requiredAction = ReadString(row, "requiredAction") ?? "Capture Windows native element bounds and regenerate evidence.";
            result[RowKey(scenarioName, component, target)] = new NativeReferenceReadinessOverride(status, reason, requiredAction);
        }

        return result;
    }

    private static bool TryGetCrop(JsonElement component, out JsonElement crop)
    {
        return component.TryGetProperty("crop", out crop) && crop.ValueKind == JsonValueKind.Object;
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? ReadNativeReferenceReadinessString(JsonElement crop, string property)
    {
        return crop.TryGetProperty("nativeReferenceReadiness", out var readiness) &&
            readiness.ValueKind == JsonValueKind.Object
            ? ReadString(readiness, property)
            : null;
    }

    private static void Increment(IDictionary<string, int> counts, string value)
    {
        counts[value] = counts.TryGetValue(value, out var current) ? current + 1 : 1;
    }

    private static string RowKey(string scenarioName, string component, string? target)
    {
        return $"{scenarioName}\u001f{component}\u001f{target ?? string.Empty}";
    }

    private static string RelativePath(string repositoryRoot, string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static string OwnerFamilyForScenario(string scenarioName)
    {
        if (scenarioName.Contains("basic-input", StringComparison.Ordinal))
        {
            return "Basic input";
        }

        if (scenarioName.Contains("commands-menus", StringComparison.Ordinal))
        {
            return "Commands and menus";
        }

        if (scenarioName.Contains("layout-media", StringComparison.Ordinal))
        {
            return "Layout, media, visuals, and platform";
        }

        if (scenarioName.Contains("text-forms", StringComparison.Ordinal))
        {
            return "Text and forms";
        }

        if (scenarioName.Contains("collections", StringComparison.Ordinal))
        {
            return "Collections and templates";
        }

        if (scenarioName.Contains("dialogs-flyouts", StringComparison.Ordinal))
        {
            return "Dialogs and flyouts";
        }

        if (scenarioName.Contains("navigation", StringComparison.Ordinal) ||
            scenarioName.Contains("workbench", StringComparison.Ordinal))
        {
            return "Navigation and workbench";
        }

        if (scenarioName.Contains("status-pickers", StringComparison.Ordinal))
        {
            return "Status, pickers, and people";
        }

        return "Unassigned visual family";
    }

    private static IReadOnlyList<string> RequiredStatesForScenario(string scenarioName)
    {
        var states = new List<string> { "default" };
        foreach (var state in new[] { "focused", "disabled", "selected", "checked", "open-popup", "loading", "success", "error", "invalid", "dark", "high-contrast" })
        {
            if (scenarioName.Contains(state, StringComparison.Ordinal))
            {
                states.Add(state);
            }
        }

        return states.Distinct(StringComparer.Ordinal).ToArray();
    }

    private sealed record NativeReferenceReadiness(
        string Status,
        bool CropExists,
        bool ValidForPromotion,
        bool TrustedForPromotion,
        bool HasWindowsNativeElementBounds,
        bool IsDiagnosticOrPlaceholder,
        bool BlocksPromotion,
        string Reason,
        string RequiredAction,
        string BoundsSource,
        string IntegrityBlockerReason);

    private sealed record NativeReferenceReadinessOverride(
        string Status,
        string Reason,
        string RequiredAction);
}
