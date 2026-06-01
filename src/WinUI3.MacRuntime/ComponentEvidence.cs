namespace WinUI3.MacRuntime;

public sealed record ComponentDiffMetrics(
    double ChangedPixelPercentage,
    double MeanAbsoluteError,
    double RootMeanSquaredError);

public sealed record ComponentEvidenceDocument(
    string SchemaVersion,
    string FixtureName,
    string ScenarioName,
    IReadOnlyList<ComponentEvidenceEntry> Components,
    IReadOnlyList<SourceFeatureEvidenceEntry> SourceFeatures,
    string Status);

public sealed record ComponentEvidenceEntry(
    string Component,
    string Kind,
    string? Target,
    UiLayoutBox? LayoutRegion,
    string CatalogStatus,
    string Presence,
    string InteractionStatus,
    string VisualGrade,
    double? ChangedPixelPercentage,
    double? MeanAbsoluteError,
    double? RootMeanSquaredError,
    IReadOnlyList<string> KnownGaps);

public sealed record SourceFeatureEvidenceEntry(
    string Feature,
    string Kind,
    string? Target,
    string CatalogStatus,
    string Presence,
    IReadOnlyList<string> KnownGaps);

public static class ComponentEvidenceBuilder
{
    private static readonly IReadOnlyDictionary<string, int> VisualGradeRank = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["not-rendered"] = 0,
        ["poor"] = 1,
        ["weak"] = 2,
        ["usable"] = 3,
        ["good"] = 4
    };

    public static ComponentEvidenceDocument Build(
        VisualScenario scenario,
        UiTreeDocument tree,
        InteractionReport? interactions,
        ComponentDiffMetrics? metrics)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(tree);

        var components = scenario.Requirements
            .Select(requirement => BuildComponentEvidence(requirement, tree, interactions, metrics))
            .ToArray();
        var sourceFeatures = scenario.SourceFeatures
            .Select(requirement => BuildSourceFeatureEvidence(requirement, tree))
            .ToArray();
        var status = components.Zip(scenario.Requirements, ComponentFailed).Any(failed => failed) ||
            sourceFeatures.Any(SourceFeatureFailed)
            ? "failed"
            : "passed";

        return new ComponentEvidenceDocument(
            ArtifactSchemas.ComponentEvidence,
            scenario.FixtureName,
            scenario.Name,
            components,
            sourceFeatures,
            status);
    }

    private static ComponentEvidenceEntry BuildComponentEvidence(
        VisualRequirement requirement,
        UiTreeDocument tree,
        InteractionReport? interactions,
        ComponentDiffMetrics? metrics)
    {
        var targetNode = FindByName(tree.Root, requirement.Target);
        var presence = targetNode is null ? "missing" : "present";
        var interactionStatus = InteractionStatus(requirement.Target, interactions);
        var grade = requirement.VisualGrade ?? requirement.MinimumVisualGrade;
        var knownGaps = requirement.KnownGaps.Count == 0 && IsDiagnosticOnly(grade)
            ? new[] { "Diagnostic-only inventory row; no renderer support is claimed yet." }
            : requirement.KnownGaps;

        return new ComponentEvidenceEntry(
            requirement.Component,
            requirement.Kind,
            requirement.Target,
            targetNode?.Layout,
            requirement.ExpectedStatus,
            presence,
            interactionStatus,
            grade,
            metrics?.ChangedPixelPercentage,
            metrics?.MeanAbsoluteError,
            metrics?.RootMeanSquaredError,
            knownGaps);
    }

    private static SourceFeatureEvidenceEntry BuildSourceFeatureEvidence(
        SourceFeatureRequirement requirement,
        UiTreeDocument tree)
    {
        var targetNode = FindByName(tree.Root, requirement.Target);
        var presence = targetNode is null ? "missing" : "present";
        return new SourceFeatureEvidenceEntry(
            requirement.Feature,
            requirement.Kind,
            requirement.Target,
            requirement.ExpectedStatus,
            requirement.Presence == "catalog-only" ? "catalog-only" : presence,
            requirement.KnownGaps);
    }

    private static bool ComponentFailed(ComponentEvidenceEntry entry, VisualRequirement requirement)
    {
        if (entry.CatalogStatus is "planned" or "windows-only" or "not supported" or "unknown")
        {
            return false;
        }

        if (entry.Presence != "present")
        {
            return true;
        }

        if (entry.InteractionStatus == "failed")
        {
            return true;
        }

        return !MeetsMinimumVisualGrade(entry.VisualGrade, requirement.MinimumVisualGrade);
    }

    private static bool SourceFeatureFailed(SourceFeatureEvidenceEntry entry)
    {
        if (entry.CatalogStatus is "planned" or "windows-only" or "not supported" or "unknown")
        {
            return false;
        }

        return entry.Presence != "present" && entry.Presence != "catalog-only";
    }

    public static bool MeetsMinimumVisualGrade(string actual, string minimum)
    {
        return VisualGradeRank.TryGetValue(actual, out var actualRank) &&
            VisualGradeRank.TryGetValue(minimum, out var minimumRank) &&
            actualRank >= minimumRank;
    }

    private static string InteractionStatus(string? target, InteractionReport? interactions)
    {
        if (string.IsNullOrWhiteSpace(target) || interactions is null)
        {
            return "not-applicable";
        }

        var targetSteps = interactions.Steps
            .Where(step => string.Equals(step.Target, target, StringComparison.Ordinal))
            .ToArray();
        if (targetSteps.Length == 0)
        {
            return "not-applicable";
        }

        return targetSteps.All(step => step.Status == "passed") ? "passed" : "failed";
    }

    private static bool IsDiagnosticOnly(string grade)
    {
        return string.Equals(grade, "not-rendered", StringComparison.Ordinal);
    }

    private static UiNode? FindByName(UiNode node, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (string.Equals(node.Name, name, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindByName(child, name);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
