using System.Globalization;
using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record NativeQualityFamilyTrancheDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Policy,
    NativeQualityFamilyTrancheTotals Totals,
    IReadOnlyList<NativeQualityFamilyTranche> Families,
    IReadOnlyList<NativeQualityFamilyTrancheRow> Rows,
    string Status);

public sealed record NativeQualityFamilyTrancheTotals(
    int FamilyCount,
    int RowCount,
    int NativeQualityReadyRowCount,
    int NotEvaluatedRowCount,
    int DefaultOnlyComponentCount,
    int MissingDefaultEvidenceComponentCount,
    int BlockedFamilyCount,
    int ReadyFamilyCount,
    int OutOfMilestoneScopeRowCount);

public sealed record NativeQualityFamilyTranche(
    string FamilyId,
    string Name,
    IReadOnlyList<string> Components,
    int RowCount,
    int NativeQualityReadyRowCount,
    int NotEvaluatedRowCount,
    int DefaultOnlyComponentCount,
    int MissingDefaultEvidenceComponentCount,
    int StateBackedComponentCount,
    int StateRequirementCount,
    int MissingStateRequirementCount,
    IReadOnlyList<string> StateRequirementStates,
    IReadOnlyList<string> StateRequirementScenarios,
    string Status,
    IReadOnlyList<string> EvidencePaths,
    IReadOnlyList<string> NextActions);

public sealed record NativeQualityFamilyTrancheRow(
    string FamilyId,
    string FamilyName,
    string ScenarioName,
    string EvidencePath,
    string Component,
    string? Target,
    string CatalogStatus,
    string VisualGrade,
    string NativeQualityGrade,
    string StateCoverageStatus,
    IReadOnlyList<string> RequiredStates,
    IReadOnlyList<string> MissingStates,
    string RemainingBlocker,
    string NativeReferenceStatus,
    string NextAction);

public static class NativeQualityFamilyTrancheBuilder
{
    public const string DefaultArtifactPath = "docs/visual-parity/native-quality-family-tranches.json";

    private const string Policy =
        "Milestone C native-quality work is tracked by control family, not by ad hoc component closure. A family can only move toward native-quality promotion when its rows have native-quality evidence and the state matrix is broader than default-only evidence.";

    private static readonly IReadOnlyList<FamilyDefinition> FamilyDefinitions = new[]
    {
        new FamilyDefinition(
            "selection-controls",
            "Selection controls",
            new[] { "CheckBox", "RadioButton", "ToggleButton", "ToggleSwitch" }),
        new FamilyDefinition(
            "button-link",
            "Button and link controls",
            new[] { "Button", "RepeatButton", "HyperlinkButton", "AppBarButton", "CommandBar" }),
        new FamilyDefinition(
            "dropdown-menu",
            "Dropdown and menu controls",
            new[] { "ComboBox", "DropDownButton", "SplitButton", "ToggleSplitButton", "MenuBar", "MenuFlyout", "CommandBarFlyout", "Flyout", "ContentDialog", "ToolTip", "TeachingTip" }),
        new FamilyDefinition(
            "text-forms",
            "Text and forms",
            new[] { "TextBlock", "TextBox", "PasswordBox", "Labels/forms pattern" }),
        new FamilyDefinition(
            "navigation-list",
            "Navigation and list controls",
            new[] { "NavigationView", "NavigationViewItem", "Frame", "ListView", "ItemsControl" }),
        new FamilyDefinition(
            "status-progress",
            "Status and progress controls",
            new[] { "InfoBar", "ProgressBar", "ProgressRing", "RatingControl", "PersonPicture" })
    };

    public static NativeQualityFamilyTrancheDocument Build(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var dashboard = ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot);
        var stateMatrix = StateCoverageMatrixBuilder.Build(repositoryRoot);
        var stateByComponent = stateMatrix.Components
            .GroupBy(component => component.Component, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var nativeQualityBlockers = LoadNativeQualityEvidenceBlockers(repositoryRoot, dashboard.Rows);
        var rows = new List<NativeQualityFamilyTrancheRow>();
        var families = new List<NativeQualityFamilyTranche>();
        var inScopeComponents = FamilyDefinitions
            .SelectMany(family => family.Components)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var family in FamilyDefinitions)
        {
            var familyRows = dashboard.Rows
                .Where(row => family.Components.Contains(row.Component, StringComparer.Ordinal))
                .OrderBy(row => row.Component, StringComparer.Ordinal)
                .ThenBy(row => row.ScenarioName, StringComparer.Ordinal)
                .ThenBy(row => row.Target, StringComparer.Ordinal)
                .ToArray();
            var rowDocuments = familyRows
                .Select(row => BuildRow(family, row, stateByComponent, nativeQualityBlockers))
                .ToArray();
            rows.AddRange(rowDocuments);

            var stateRows = family.Components
                .Where(stateByComponent.ContainsKey)
                .Select(component => stateByComponent[component])
                .ToArray();
            var stateRequirements = stateMatrix.Requirements
                .Where(requirement => family.Components.Contains(requirement.Component, StringComparer.Ordinal))
                .OrderBy(requirement => requirement.Component, StringComparer.Ordinal)
                .ThenBy(requirement => requirement.State, StringComparer.Ordinal)
                .ThenBy(requirement => requirement.ScenarioName, StringComparer.Ordinal)
                .ToArray();
            var notEvaluatedRows = rowDocuments.Count(row => row.NativeQualityGrade == "not-evaluated");
            var nativeReadyRows = rowDocuments.Length - notEvaluatedRows;
            var defaultOnlyComponents = stateRows.Count(row => row.CoverageStatus == "default-only");
            var missingDefaultEvidenceComponents = stateRows.Count(row => row.CoverageStatus == "missing-default-evidence");
            var stateBackedComponents = stateRows.Count(row => row.CoverageStatus is "partial-state-coverage" or "state-covered");
            var nextActions = NextActions(rowDocuments, stateRows, familyRows.Length == 0);
            var status = familyRows.Length == 0
                ? "missing-family-evidence"
                : notEvaluatedRows == 0 && defaultOnlyComponents == 0 && missingDefaultEvidenceComponents == 0
                    ? "native-quality-ready"
                    : "native-quality-blocked";

            families.Add(new NativeQualityFamilyTranche(
                family.FamilyId,
                family.Name,
                rowDocuments
                    .Select(row => row.Component)
                    .Concat(stateRows.Select(row => row.Component))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(component => component, StringComparer.Ordinal)
                    .ToArray(),
                rowDocuments.Length,
                nativeReadyRows,
                notEvaluatedRows,
                defaultOnlyComponents,
                missingDefaultEvidenceComponents,
                stateBackedComponents,
                stateRequirements.Length,
                stateRequirements.Count(requirement => requirement.EvidenceStatus != "evidence-backed"),
                stateRequirements
                    .Select(requirement => requirement.State)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(state => state, StringComparer.Ordinal)
                    .ToArray(),
                stateRequirements
                    .Select(requirement => requirement.ScenarioName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(scenario => scenario, StringComparer.Ordinal)
                    .ToArray(),
                status,
                rowDocuments
                    .Select(row => row.EvidencePath)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray(),
                nextActions));
        }

        var rowCount = rows.Count;
        var readyRows = rows.Count(row => row.NativeQualityGrade is "good" or "production-ready");
        var notEvaluated = rows.Count(row => row.NativeQualityGrade == "not-evaluated");
        var blockedFamilies = families.Count(family => family.Status != "native-quality-ready");
        var outOfScopeRows = dashboard.Rows.Count(row => !inScopeComponents.Contains(row.Component));
        var totals = new NativeQualityFamilyTrancheTotals(
            families.Count,
            rowCount,
            readyRows,
            notEvaluated,
            families.Sum(family => family.DefaultOnlyComponentCount),
            families.Sum(family => family.MissingDefaultEvidenceComponentCount),
            blockedFamilies,
            families.Count - blockedFamilies,
            outOfScopeRows);

        return new NativeQualityFamilyTrancheDocument(
            ArtifactSchemas.NativeQualityFamilyTranches,
            DateTimeOffset.UnixEpoch,
            Policy,
            totals,
            families,
            rows
                .OrderBy(row => row.FamilyId, StringComparer.Ordinal)
                .ThenBy(row => row.Component, StringComparer.Ordinal)
                .ThenBy(row => row.ScenarioName, StringComparer.Ordinal)
                .ThenBy(row => row.Target, StringComparer.Ordinal)
                .ToArray(),
            blockedFamilies == 0 ? "native-quality-ready" : "tracked-with-native-quality-gaps");
    }

    public static NativeQualityFamilyTrancheDocument Write(string repositoryRoot, string? outputPath = null)
    {
        var document = Build(repositoryRoot);
        var resolvedOutputPath = Path.GetFullPath(outputPath ?? Path.Combine(repositoryRoot, DefaultArtifactPath));
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
        File.WriteAllText(resolvedOutputPath, JsonSerializer.Serialize(document, JsonDefaults.Options));
        return document;
    }

    private static NativeQualityFamilyTrancheRow BuildRow(
        FamilyDefinition family,
        ComponentQualityRow row,
        IReadOnlyDictionary<string, StateCoverageComponentRow> stateByComponent,
        IReadOnlyDictionary<string, string> nativeQualityBlockers)
    {
        stateByComponent.TryGetValue(row.Component, out var state);
        var stateCoverageStatus = state?.CoverageStatus ?? "not-tracked";
        var remainingBlocker = row.RemainingBlocker == "none" &&
            nativeQualityBlockers.TryGetValue(RowKey(row.ScenarioName, row.Component, row.Target), out var nativeQualityBlocker)
                ? nativeQualityBlocker
                : row.RemainingBlocker;
        var nextAction = NextAction(row, stateCoverageStatus, remainingBlocker);
        return new NativeQualityFamilyTrancheRow(
            family.FamilyId,
            family.Name,
            row.ScenarioName,
            row.EvidencePath,
            row.Component,
            row.Target,
            row.CatalogStatus,
            row.VisualGrade,
            row.NativeQualityGrade,
            stateCoverageStatus,
            state?.RequiredStates ?? row.RequiredStates,
            state?.MissingStates ?? Array.Empty<string>(),
            remainingBlocker,
            row.NativeReferenceStatus,
            nextAction);
    }

    private static IReadOnlyList<string> NextActions(
        IReadOnlyList<NativeQualityFamilyTrancheRow> rows,
        IReadOnlyList<StateCoverageComponentRow> stateRows,
        bool missingFamilyEvidence)
    {
        var actions = new List<string>();
        if (missingFamilyEvidence)
        {
            actions.Add("Add canonical component evidence before attempting family promotion.");
        }

        if (rows.Any(row => row.NativeQualityGrade == "not-evaluated"))
        {
            actions.Add("Complete native-quality inspection and promotion evidence for not-evaluated rows.");
        }

        if (stateRows.Any(row => row.CoverageStatus is "default-only" or "missing-default-evidence"))
        {
            actions.Add("Close state coverage gaps before treating the family as production-ready native quality.");
        }

        if (rows.Any(row => row.RemainingBlocker != "none"))
        {
            actions.Add("Resolve source-level component quality blockers before native-quality promotion.");
        }

        if (actions.Count == 0)
        {
            actions.Add("Ready for native-quality family review.");
        }

        return actions;
    }

    private static string NextAction(ComponentQualityRow row, string stateCoverageStatus, string remainingBlocker)
    {
        if (remainingBlocker != "none")
        {
            return remainingBlocker;
        }

        if (stateCoverageStatus is "default-only" or "missing-default-evidence")
        {
            return "Close visible state, interaction, and accessibility coverage gaps for this component.";
        }

        if (row.NativeQualityGrade == "not-evaluated")
        {
            return "Attach native-quality inspection evidence or keep this row explicitly not-evaluated.";
        }

        return "Candidate for family-level native-quality review.";
    }

    private static IReadOnlyDictionary<string, string> LoadNativeQualityEvidenceBlockers(
        string repositoryRoot,
        IReadOnlyList<ComponentQualityRow> rows)
    {
        var blockers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var evidencePath in rows
            .Select(row => row.EvidencePath)
            .Distinct(StringComparer.Ordinal))
        {
            var resolvedPath = Path.Combine(repositoryRoot, evidencePath);
            if (!File.Exists(resolvedPath))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(resolvedPath));
            var root = document.RootElement;
            var scenarioName = ReadString(root, "scenarioName") ??
                Path.GetFileName(Path.GetDirectoryName(resolvedPath)) ??
                "unknown";
            if (!root.TryGetProperty("components", out var components) ||
                components.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var component in components.EnumerateArray())
            {
                var nativeQualityGrade = ReadString(component, "nativeQualityGrade") ?? "missing";
                if (nativeQualityGrade != "not-evaluated" ||
                    !component.TryGetProperty("crop", out var crop) ||
                    crop.ValueKind != JsonValueKind.Object ||
                    ReadString(crop, "status") != "failed")
                {
                    continue;
                }

                var componentName = ReadString(component, "component");
                if (string.IsNullOrWhiteSpace(componentName))
                {
                    continue;
                }

                var reasons = FailedCropThresholdReasons(crop).ToList();
                if (reasons.Count == 0)
                {
                    reasons.Add("component crop failed; inspect threshold metrics and triptych evidence.");
                }

                blockers[RowKey(scenarioName, componentName, ReadString(component, "target"))] =
                    $"Native-quality crop status is 'failed'; {string.Join(" ", reasons)}";
            }
        }

        return blockers;
    }

    private static IReadOnlyList<string> FailedCropThresholdReasons(JsonElement crop)
    {
        if (!crop.TryGetProperty("thresholds", out var thresholds) ||
            thresholds.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<string>();
        }

        var reasons = new List<string>();
        AddExceededThresholdReason(crop, thresholds, "changedPixelPercentage", reasons);
        AddExceededThresholdReason(crop, thresholds, "meanAbsoluteError", reasons);
        AddExceededThresholdReason(crop, thresholds, "rootMeanSquaredError", reasons);
        return reasons;
    }

    private static void AddExceededThresholdReason(
        JsonElement crop,
        JsonElement thresholds,
        string metric,
        ICollection<string> reasons)
    {
        if (!TryReadNumber(crop, metric, out var actual) ||
            !TryReadNumber(thresholds, metric, out var threshold) ||
            actual <= threshold)
        {
            return;
        }

        reasons.Add($"{metric} {FormatNumber(actual)} exceeds threshold {FormatNumber(threshold)}.");
    }

    private static bool TryReadNumber(JsonElement element, string property, out double value)
    {
        if (element.TryGetProperty(property, out var propertyValue) &&
            propertyValue.ValueKind == JsonValueKind.Number &&
            propertyValue.TryGetDouble(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string RowKey(string scenarioName, string component, string? target)
    {
        return $"{scenarioName}\u001f{component}\u001f{target ?? string.Empty}";
    }

    private sealed record FamilyDefinition(
        string FamilyId,
        string Name,
        IReadOnlyList<string> Components);
}
