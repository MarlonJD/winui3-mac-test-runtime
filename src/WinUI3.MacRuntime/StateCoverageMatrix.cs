using System.Text.Json;

namespace WinUI3.MacRuntime;

public sealed record StateCoverageMatrixDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Policy,
    StateCoverageTotals Totals,
    IReadOnlyList<StateCoverageComponentRow> Components,
    IReadOnlyList<StateCoverageRequirementRow> Requirements,
    string Status);

public sealed record StateCoverageTotals(
    int ComponentCount,
    int RequirementCount,
    int EvidenceBackedRequirementCount,
    int MissingEvidenceRequirementCount,
    int DefaultOnlyComponentCount,
    int StateBackedComponentCount,
    int MissingDefaultEvidenceComponentCount,
    IReadOnlyDictionary<string, int> CoverageStatusCounts);

public sealed record StateCoverageComponentRow(
    string Component,
    string OwnerFamily,
    IReadOnlyList<string> RequiredStates,
    IReadOnlyList<string> CoveredStates,
    IReadOnlyList<string> MissingStates,
    bool HasDefaultEvidence,
    bool HasInteractionEvidence,
    bool HasAccessibilityEvidence,
    string CoverageStatus,
    string ProductionReadiness);

public sealed record StateCoverageRequirementRow(
    string Component,
    string OwnerFamily,
    string ProductionPriority,
    string State,
    string ScenarioName,
    string ScenarioPath,
    bool ScenarioExists,
    string? EvidencePath,
    string EvidenceStatus,
    string? VisualGrade,
    string InteractionRequirement,
    string InteractionEvidenceStatus,
    string AccessibilityRequirement,
    string AccessibilityEvidenceStatus,
    string MinimumVisualGrade,
    string CoverageStatus,
    string ReleaseEvidenceProfile,
    string ReleaseEvidenceStatus,
    string ReleaseComponentEvidencePath,
    string ReleaseAccessibilityEvidencePath,
    string ReleaseVisualRunPath,
    IReadOnlyList<string> KnownGaps);

public static class StateCoverageMatrixBuilder
{
    public const string DefaultArtifactPath = "docs/visual-parity/state-coverage-matrix.json";

    private const string Policy =
        "Default-state component evidence is not production-ready state coverage. Native-quality promotion requires visible state, interaction, and accessibility coverage by component family; missing state evidence is tracked explicitly instead of inferred from default screenshots.";

    private static readonly HashSet<string> StateScenarioTokens = new(StringComparer.Ordinal)
    {
        "checked",
        "command-invoked",
        "dark",
        "deferred",
        "disabled",
        "error",
        "focused",
        "high-contrast",
        "invalid",
        "loading",
        "open-popup",
        "selected",
        "success"
    };

    private static readonly IReadOnlyDictionary<string, int> VisualGradeRank = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["not-rendered"] = 0,
        ["poor"] = 1,
        ["weak"] = 2,
        ["usable"] = 3,
        ["good"] = 4,
        ["production-ready"] = 5
    };

    public static StateCoverageMatrixDocument Build(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var requirements = LoadRequirements(root);
        var evidenceRows = LoadEvidenceRows(root);
        var defaultEvidence = evidenceRows.Values
            .Where(row => IsDefaultScenario(row.ScenarioName))
            .GroupBy(row => row.Component, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var requirementRows = requirements
            .SelectMany(requirement => requirement.Components.Select(component =>
                BuildRequirementRow(root, requirement, component, evidenceRows)))
            .OrderBy(row => row.OwnerFamily, StringComparer.Ordinal)
            .ThenBy(row => row.Component, StringComparer.Ordinal)
            .ThenBy(row => row.State, StringComparer.Ordinal)
            .ThenBy(row => row.ScenarioName, StringComparer.Ordinal)
            .ToArray();

        var components = BuildComponentRows(requirementRows, defaultEvidence);
        var statusCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var component in components)
        {
            statusCounts[component.CoverageStatus] = statusCounts.TryGetValue(component.CoverageStatus, out var count)
                ? count + 1
                : 1;
        }

        var totals = new StateCoverageTotals(
            components.Count,
            requirementRows.Length,
            requirementRows.Count(row => row.EvidenceStatus == "evidence-backed"),
            requirementRows.Count(row => row.EvidenceStatus != "evidence-backed"),
            components.Count(row => row.CoverageStatus == "default-only"),
            components.Count(row => row.CoverageStatus is "partial-state-coverage" or "state-covered"),
            components.Count(row => row.CoverageStatus == "missing-default-evidence"),
            statusCounts);

        return new StateCoverageMatrixDocument(
            ArtifactSchemas.StateCoverageMatrix,
            DateTimeOffset.UnixEpoch,
            Policy,
            totals,
            components,
            requirementRows,
            requirementRows.Any(row => row.EvidenceStatus != "evidence-backed")
                ? "tracked-with-gaps"
                : "passed");
    }

    public static StateCoverageMatrixDocument Write(string repositoryRoot, string? outputPath = null)
    {
        var document = Build(repositoryRoot);
        var resolvedOutputPath = Path.GetFullPath(outputPath ?? Path.Combine(repositoryRoot, DefaultArtifactPath));
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
        File.WriteAllText(resolvedOutputPath, JsonSerializer.Serialize(document, JsonDefaults.Options));
        return document;
    }

    private static IReadOnlyList<StateCoverageRequirement> LoadRequirements(string repositoryRoot)
    {
        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        if (!File.Exists(inventoryPath))
        {
            throw new FileNotFoundException("The component inventory is required to build the state coverage matrix.", inventoryPath);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var root = document.RootElement;
        if (!root.TryGetProperty("productionStateCoverage", out var coverage) ||
            coverage.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<StateCoverageRequirement>();
        }

        var requirements = new List<StateCoverageRequirement>();
        foreach (var item in coverage.EnumerateArray())
        {
            var components = item.GetProperty("components")
                .EnumerateArray()
                .Select(component => component.GetString())
                .Where(component => !string.IsNullOrWhiteSpace(component))
                .Select(component => component!)
                .ToArray();
            requirements.Add(new StateCoverageRequirement(
                ReadRequiredString(item, "productionPriority"),
                ReadRequiredString(item, "state"),
                ReadRequiredString(item, "scenario"),
                ReadRequiredString(item, "path"),
                components,
                ReadRequiredString(item, "interactionRequirement"),
                ReadRequiredString(item, "accessibilityRequirement"),
                ReadRequiredString(item, "minimumVisualGrade"),
                ReadStringArray(item, "knownGaps")));
        }

        return requirements;
    }

    private static IReadOnlyDictionary<string, EvidenceRow> LoadEvidenceRows(string repositoryRoot)
    {
        var rows = new Dictionary<string, EvidenceRow>(StringComparer.Ordinal);
        foreach (var evidencePath in PublicEvidenceDiscovery.FindCanonicalEvidenceFiles(repositoryRoot))
        {
            var relativePath = RelativePath(repositoryRoot, evidencePath);
            var evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(File.ReadAllText(evidencePath), JsonDefaults.Options)
                ?? throw new InvalidOperationException($"Could not read component evidence from '{evidencePath}'.");
            foreach (var component in evidence.Components)
            {
                rows[EvidenceKey(evidence.ScenarioName, component.Component)] = new EvidenceRow(
                    evidence.ScenarioName,
                    relativePath,
                    component.Component,
                    component.VisualGrade,
                    component.InteractionStatus);
            }
        }

        return rows;
    }

    private static StateCoverageRequirementRow BuildRequirementRow(
        string repositoryRoot,
        StateCoverageRequirement requirement,
        string component,
        IReadOnlyDictionary<string, EvidenceRow> evidenceRows)
    {
        var scenarioPath = Path.Combine(repositoryRoot, requirement.Path);
        var scenarioExists = File.Exists(scenarioPath);
        evidenceRows.TryGetValue(EvidenceKey(requirement.Scenario, component), out var evidence);
        var evidenceStatus = EvidenceStatus(repositoryRoot, requirement, component, evidence, scenarioExists);
        var interactionEvidenceStatus = evidence is null
            ? evidenceStatus
            : evidence.InteractionStatus;
        var coverageStatus = evidenceStatus == "evidence-backed" &&
            MeetsMinimumVisualGrade(evidence!.VisualGrade, requirement.MinimumVisualGrade)
                ? "evidence-backed"
                : evidenceStatus == "evidence-backed"
                    ? "below-minimum-visual-grade"
                    : evidenceStatus;

        return new StateCoverageRequirementRow(
            component,
            OwnerFamilyForScenario(requirement.Scenario),
            requirement.ProductionPriority,
            requirement.State,
            requirement.Scenario,
            requirement.Path,
            scenarioExists,
            evidence?.EvidencePath,
            evidenceStatus,
            evidence?.VisualGrade,
            requirement.InteractionRequirement,
            interactionEvidenceStatus,
            requirement.AccessibilityRequirement,
            evidence is null ? evidenceStatus : "not-attached",
            requirement.MinimumVisualGrade,
            coverageStatus,
            "strict-scenario-sweep",
            "required-via-public-product",
            StrictSweepPath(requirement.Scenario, "visual/component-evidence.json"),
            StrictSweepPath(requirement.Scenario, "accessibility.json"),
            StrictSweepPath(requirement.Scenario, "visual/visual-run.json"),
            requirement.KnownGaps);
    }

    private static IReadOnlyList<StateCoverageComponentRow> BuildComponentRows(
        IReadOnlyList<StateCoverageRequirementRow> requirementRows,
        IReadOnlyDictionary<string, EvidenceRow[]> defaultEvidence)
    {
        return requirementRows
            .GroupBy(row => row.Component, StringComparer.Ordinal)
            .Select(group =>
            {
                var component = group.Key;
                var hasDefaultEvidence = defaultEvidence.ContainsKey(component);
                var coveredStateRows = group
                    .Where(row => row.CoverageStatus == "evidence-backed")
                    .ToArray();
                var requiredStates = new[] { "default" }
                    .Concat(group.Select(row => row.State))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(state => state, StringComparer.Ordinal)
                    .ToArray();
                var coveredStates = (hasDefaultEvidence ? new[] { "default" } : Array.Empty<string>())
                    .Concat(coveredStateRows.Select(row => row.State))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(state => state, StringComparer.Ordinal)
                    .ToArray();
                var missingStates = requiredStates
                    .Except(coveredStates, StringComparer.Ordinal)
                    .OrderBy(state => state, StringComparer.Ordinal)
                    .ToArray();
                var hasStateEvidence = coveredStateRows.Length > 0;
                var ownerFamilies = group.Select(row => row.OwnerFamily)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(family => family, StringComparer.Ordinal)
                    .ToArray();
                var status = ComponentCoverageStatus(hasDefaultEvidence, hasStateEvidence, missingStates);
                return new StateCoverageComponentRow(
                    component,
                    ownerFamilies.Length == 1 ? ownerFamilies[0] : string.Join("; ", ownerFamilies),
                    requiredStates,
                    coveredStates,
                    missingStates,
                    hasDefaultEvidence,
                    defaultEvidence.TryGetValue(component, out var defaults) &&
                        defaults.Any(row => row.InteractionStatus == "passed") ||
                        coveredStateRows.Any(row => row.InteractionEvidenceStatus == "passed"),
                    HasAccessibilityEvidence: false,
                    status,
                    ProductionReadinessFor(status));
            })
            .OrderBy(row => row.OwnerFamily, StringComparer.Ordinal)
            .ThenBy(row => row.Component, StringComparer.Ordinal)
            .ToArray();
    }

    private static string EvidenceStatus(
        string repositoryRoot,
        StateCoverageRequirement requirement,
        string component,
        EvidenceRow? evidence,
        bool scenarioExists)
    {
        if (!scenarioExists)
        {
            return "missing-scenario";
        }

        if (evidence is null)
        {
            var scenarioEvidencePath = Path.Combine("docs", "visual-parity", "examples", requirement.Scenario, "component-evidence.json");
            return File.Exists(Path.Combine(repositoryRoot, scenarioEvidencePath))
                ? $"missing-component-row:{component}"
                : "missing-state-evidence";
        }

        return "evidence-backed";
    }

    private static bool MeetsMinimumVisualGrade(string actual, string minimum)
    {
        return VisualGradeRank.TryGetValue(actual, out var actualRank) &&
            VisualGradeRank.TryGetValue(minimum, out var minimumRank) &&
            actualRank >= minimumRank;
    }

    private static string ComponentCoverageStatus(
        bool hasDefaultEvidence,
        bool hasStateEvidence,
        IReadOnlyCollection<string> missingStates)
    {
        if (!hasDefaultEvidence)
        {
            return "missing-default-evidence";
        }

        if (!hasStateEvidence)
        {
            return "default-only";
        }

        return missingStates.Count == 0
            ? "state-covered"
            : "partial-state-coverage";
    }

    private static string ProductionReadinessFor(string coverageStatus)
    {
        return coverageStatus switch
        {
            "state-covered" => "state-matrix-covered-review-accessibility-before-promotion",
            "partial-state-coverage" => "not-production-ready-state-gaps",
            "default-only" => "not-production-ready-default-only",
            "missing-default-evidence" => "not-production-ready-missing-default-evidence",
            _ => "not-production-ready-state-gaps"
        };
    }

    private static bool IsDefaultScenario(string scenarioName)
    {
        return !StateScenarioTokens.Any(token => scenarioName.Contains(token, StringComparison.Ordinal));
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

        if (scenarioName.Contains("layout-media", StringComparison.Ordinal))
        {
            return "Theming and resources";
        }

        return "Unassigned visual family";
    }

    private static string EvidenceKey(string scenarioName, string component)
    {
        return $"{scenarioName}\u001f{component}";
    }

    private static string StrictSweepPath(string scenarioName, string artifactPath)
    {
        return Path.Combine(
                "artifacts",
                "product-evidence",
                "strict-scenario-sweep",
                scenarioName,
                artifactPath)
            .Replace('\\', '/');
    }

    private static string ReadRequiredString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()!
                : throw new InvalidOperationException($"productionStateCoverage row is missing '{property}'.");
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return array.EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    private static string RelativePath(string repositoryRoot, string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private sealed record StateCoverageRequirement(
        string ProductionPriority,
        string State,
        string Scenario,
        string Path,
        IReadOnlyList<string> Components,
        string InteractionRequirement,
        string AccessibilityRequirement,
        string MinimumVisualGrade,
        IReadOnlyList<string> KnownGaps);

    private sealed record EvidenceRow(
        string ScenarioName,
        string EvidencePath,
        string Component,
        string VisualGrade,
        string InteractionStatus);
}
