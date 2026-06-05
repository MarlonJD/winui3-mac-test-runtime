using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WinUI3.MacCompatibility;

namespace WinUI3.MacRuntime;

public sealed record ProductEvidencePlan(
    string Profile,
    string OutputRoot,
    IReadOnlyList<ProductEvidenceStep> Steps)
{
    public static ProductEvidencePlan Create(string profile, string outputRoot, string? repositoryRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        if (string.Equals(profile, "public-product", StringComparison.Ordinal))
        {
            return CreatePublicProductPlan(outputRoot);
        }

        if (string.Equals(profile, "strict-scenario-sweep", StringComparison.Ordinal))
        {
            return CreateStrictScenarioSweepPlan(outputRoot, repositoryRoot);
        }

        throw new ArgumentException($"Unknown product evidence profile '{profile}'. Expected 'public-product' or 'strict-scenario-sweep'.", nameof(profile));
    }

    private static ProductEvidencePlan CreatePublicProductPlan(string outputRoot)
    {
        return new ProductEvidencePlan(
            "public-product",
            outputRoot.Replace('\\', '/'),
            new[]
            {
                Local(
                    "catalog-audit",
                    "Catalog readiness audit",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner catalog-audit --check",
                    "source-level-release",
                    "docs/compatibility/all-catalog-readiness-audit.json"),
                Local(
                    "component-quality-dashboard",
                    "Component-quality dashboard freshness",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner component-quality-dashboard --check",
                    "source-level-release",
                    "docs/visual-parity/component-quality-dashboard.json"),
                Local(
                    "state-coverage-matrix",
                    "State, interaction, and accessibility matrix freshness",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner state-coverage-matrix --check",
                    "state-interaction-accessibility",
                    "docs/visual-parity/state-coverage-matrix.json"),
                Local(
                    "native-quality-family-tranches",
                    "Native-quality family tranche freshness",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner native-quality-family-tranches --check",
                    "native-quality-promotion",
                    "docs/visual-parity/native-quality-family-tranches.json"),
                Local(
                    "native-reference-readiness",
                    "Native reference readiness freshness",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner native-reference-readiness --check",
                    "source-level-release",
                    "docs/visual-parity/native-reference-readiness.json"),
                Local(
                    "visual-drift-dashboard-freshness",
                    "Visual drift dashboard freshness",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner visual-drift-dashboard --check",
                    "product-polish",
                    "docs/visual-parity/visual-drift-dashboard.json"),
                Local(
                    "visual-review-index",
                    "Public visual review index freshness",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner visual-review-index --check",
                    "source-level-release",
                    "docs/visual-parity/public-visual-review-index.json",
                    "docs/visual-parity/public-visual-review-index.html"),
                External(
                    "strict-scenario-sweep",
                    "Full strict scenario sweep",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner product-evidence --profile strict-scenario-sweep --output artifacts/product-evidence/strict-scenario-sweep",
                    "product-polish",
                    "artifacts/product-evidence/strict-scenario-sweep"),
                External(
                    "public-admin-workbench",
                    "Public admin workbench strict visual",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner product-evidence --profile strict-scenario-sweep --output artifacts/product-evidence/strict-scenario-sweep",
                    "product-polish",
                    "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json"),
                External(
                    "production-e2e-workbench",
                    "Production E2E workbench strict visual",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner product-evidence --profile strict-scenario-sweep --output artifacts/product-evidence/strict-scenario-sweep",
                    "product-polish",
                    "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json"),
                Local(
                    "release-candidate-dry-run",
                    "Release candidate local dry run scaffold",
                    "internal: verify release-candidate inputs and package dry-run evidence path",
                    "package-publication",
                    "artifacts/production-gates/release-candidate.json")
            });
    }

    private static ProductEvidencePlan CreateStrictScenarioSweepPlan(string outputRoot, string? repositoryRoot)
    {
        var root = Path.GetFullPath(repositoryRoot ?? Environment.CurrentDirectory);
        var stableOutputRoot = outputRoot.Replace('\\', '/');
        var steps = DiscoverStrictScenarioSweepScenarios(root)
            .Select(scenario =>
            {
                var scenarioName = Path.GetFileNameWithoutExtension(scenario.ScenarioPath);
                var outputDirectory = $"{stableOutputRoot.TrimEnd('/')}/{scenarioName}";
                return Scenario(
                    scenarioName,
                    $"Strict visual scenario: {scenarioName}",
                    $"PATH=\"$PWD/tools:$PATH\" winui3-mac-runner run --project ./{scenario.ProjectPath} --renderer skia-v2 --scenario ./{scenario.ScenarioPath} --strict-visual --output {outputDirectory}",
                    "strict-scenario-sweep",
                    scenario.ProjectPath,
                    scenario.ScenarioPath,
                    outputDirectory,
                    $"{outputDirectory}/run.json",
                    $"{outputDirectory}/tree.json",
                    $"{outputDirectory}/accessibility.json",
                    $"{outputDirectory}/visual/visual-run.json",
                    $"{outputDirectory}/visual/mac-runtime.png");
            })
            .ToArray();

        return new ProductEvidencePlan(
            "strict-scenario-sweep",
            stableOutputRoot,
            steps);
    }

    private static ProductEvidenceStep Local(
        string name,
        string title,
        string command,
        string blockingScope,
        params string[] artifactPaths)
    {
        return new ProductEvidenceStep(name, title, command, "local-check", blockingScope, artifactPaths);
    }

    private static ProductEvidenceStep External(
        string name,
        string title,
        string command,
        string blockingScope,
        params string[] artifactPaths)
    {
        return new ProductEvidenceStep(name, title, command, "external-evidence-required", blockingScope, artifactPaths);
    }

    private static ProductEvidenceStep Scenario(
        string name,
        string title,
        string command,
        string blockingScope,
        string projectPath,
        string scenarioPath,
        string outputDirectory,
        params string[] artifactPaths)
    {
        return new ProductEvidenceStep(
            name,
            title,
            command,
            "local-check",
            blockingScope,
            artifactPaths,
            projectPath,
            scenarioPath,
            outputDirectory);
    }

    private static IReadOnlyList<StrictScenarioSweepScenario> DiscoverStrictScenarioSweepScenarios(string repositoryRoot)
    {
        var fixturesRoot = Path.Combine(repositoryRoot, "fixtures");
        if (!Directory.Exists(fixturesRoot))
        {
            return Array.Empty<StrictScenarioSweepScenario>();
        }

        return Directory
            .EnumerateFiles(fixturesRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains("scenarios", StringComparer.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new StrictScenarioSweepScenario(ProjectPathForScenario(path), path))
            .ToArray();
    }

    private static string ProjectPathForScenario(string scenarioPath)
    {
        if (scenarioPath.StartsWith("fixtures/ComponentParityLab.WinUI/", StringComparison.Ordinal))
        {
            return "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/ControlGallery.MacTest/", StringComparison.Ordinal))
        {
            return "fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/InteractionBindingApp.MacTest/", StringComparison.Ordinal))
        {
            return "fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/ProductionSmoke.WinUI/", StringComparison.Ordinal))
        {
            return "fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/PublicAdminWorkbench.WinUI/", StringComparison.Ordinal))
        {
            return "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/ResourceCatalogApp.WinUI/", StringComparison.Ordinal))
        {
            return "fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/SampleAdminShell.MacTest/", StringComparison.Ordinal))
        {
            return "fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/SettingsFormApp.WinUI/", StringComparison.Ordinal))
        {
            return "fixtures/SettingsFormApp.WinUI/SettingsFormApp.WinUI.csproj";
        }

        if (scenarioPath.StartsWith("fixtures/SingleWindowApp.WinUI/", StringComparison.Ordinal))
        {
            return "fixtures/SingleWindowApp.WinUI/SingleWindowApp.WinUI.csproj";
        }

        throw new InvalidOperationException($"No project mapping is registered for scenario '{scenarioPath}'.");
    }

    private sealed record StrictScenarioSweepScenario(string ProjectPath, string ScenarioPath);
}

public sealed record ProductEvidenceStep(
    string Name,
    string Title,
    string Command,
    string ExecutionMode,
    string BlockingScope,
    IReadOnlyList<string> ArtifactPaths,
    string? ProjectPath = null,
    string? ScenarioPath = null,
    string? OutputDirectory = null);

public sealed record ProductEvidenceStepOutcome(
    string Status,
    string Message,
    string? FailureReason,
    IReadOnlyList<string> ArtifactPaths)
{
    public static ProductEvidenceStepOutcome Passed(string message, IReadOnlyList<string> artifactPaths)
    {
        return new ProductEvidenceStepOutcome("passed", message, null, artifactPaths);
    }

    public static ProductEvidenceStepOutcome Failed(string failureReason, IReadOnlyList<string> artifactPaths)
    {
        return new ProductEvidenceStepOutcome("failed", failureReason, failureReason, artifactPaths);
    }

    public static ProductEvidenceStepOutcome ExternalEvidenceRequired(string message, IReadOnlyList<string> artifactPaths)
    {
        return new ProductEvidenceStepOutcome("external-evidence-required", message, null, artifactPaths);
    }
}

public sealed record ProductEvidenceDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Profile,
    string OutputRoot,
    string Status,
    ProductEvidenceSummary Summary,
    IReadOnlyList<ProductEvidenceStepResult> Steps);

public sealed record ProductEvidenceSummary(
    int TotalSteps,
    int PassedSteps,
    int FailedSteps,
    int ExternalEvidenceSteps,
    IReadOnlyDictionary<string, int> BlockingScopeCounts);

public sealed record ProductEvidenceStepResult(
    string Name,
    string Title,
    string Status,
    string ExecutionMode,
    string BlockingScope,
    string Command,
    long ElapsedMilliseconds,
    IReadOnlyList<string> ArtifactPaths,
    string? FailureReason,
    string? ProjectPath = null,
    string? ScenarioPath = null,
    string? OutputDirectory = null);

public static class ProductEvidenceRunner
{
    public const string SchemaVersion = "0.1";

    private static readonly IReadOnlyDictionary<string, int> VisualGradeRank = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["not-rendered"] = 0,
        ["poor"] = 1,
        ["weak"] = 2,
        ["usable"] = 3,
        ["good"] = 4,
        ["production-ready"] = 5
    };

    public static async Task<ProductEvidenceDocument> RunAsync(
        string repositoryRoot,
        string profile,
        string outputRoot,
        Func<ProductEvidenceStep, Task<ProductEvidenceStepOutcome>>? executeStep = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        var plan = ProductEvidencePlan.Create(profile, StableOutputRoot(outputRoot), repositoryRoot);
        var results = new List<ProductEvidenceStepResult>();

        foreach (var step in plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();
            var outcome = step.ExecutionMode == "external-evidence-required"
                ? CheckAttachedExternalEvidence(repositoryRoot, step)
                : executeStep is null
                    ? RunBuiltInStep(repositoryRoot, step)
                    : await executeStep(step);
            stopwatch.Stop();

            results.Add(new ProductEvidenceStepResult(
                step.Name,
                step.Title,
                outcome.Status,
                step.ExecutionMode,
                step.BlockingScope,
                step.Command,
                stopwatch.ElapsedMilliseconds,
                outcome.ArtifactPaths,
                outcome.FailureReason,
                step.ProjectPath,
                step.ScenarioPath,
                step.OutputDirectory));
        }

        var failedSteps = results.Count(step => step.Status == "failed");
        var externalSteps = results.Count(step => step.Status == "external-evidence-required");
        var status = failedSteps > 0
            ? "blocked"
            : externalSteps > 0
                ? "ready-pending-external-evidence"
                : "passed";
        var summary = new ProductEvidenceSummary(
            results.Count,
            results.Count(step => step.Status == "passed"),
            failedSteps,
            externalSteps,
            results
                .GroupBy(step => step.BlockingScope, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal));

        var document = new ProductEvidenceDocument(
            SchemaVersion,
            DateTimeOffset.UtcNow,
            plan.Profile,
            plan.OutputRoot,
            status,
            summary,
            results);

        Directory.CreateDirectory(outputRoot);
        await File.WriteAllTextAsync(
            Path.Combine(outputRoot, "product-evidence.json"),
            JsonSerializer.Serialize(document, JsonDefaults.Options),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(outputRoot, "product-evidence.md"),
            BuildMarkdown(document),
            cancellationToken);

        return document;
    }

    private static ProductEvidenceStepOutcome RunBuiltInStep(string repositoryRoot, ProductEvidenceStep step)
    {
        return step.Name switch
        {
            "catalog-audit" => CheckCatalogAudit(repositoryRoot, step),
            "component-quality-dashboard" => OutcomeFromFreshness(EvidenceFreshness.CheckComponentQualityDashboard(repositoryRoot), step),
            "state-coverage-matrix" => OutcomeFromFreshness(EvidenceFreshness.CheckStateCoverageMatrix(repositoryRoot), step),
            "native-quality-family-tranches" => OutcomeFromFreshness(EvidenceFreshness.CheckNativeQualityFamilyTranches(repositoryRoot), step),
            "native-reference-readiness" => CheckNativeReferenceReadiness(repositoryRoot, step),
            "visual-drift-dashboard-freshness" => OutcomeFromFreshness(EvidenceFreshness.CheckVisualDriftDashboard(repositoryRoot), step),
            "visual-review-index" => CheckVisualReviewIndex(repositoryRoot, step),
            "release-candidate-dry-run" => CheckReleaseCandidateScaffold(repositoryRoot, step),
            _ => ProductEvidenceStepOutcome.Failed($"No built-in product-evidence executor exists for step '{step.Name}'.", step.ArtifactPaths)
        };
    }

    private static ProductEvidenceStepOutcome CheckAttachedExternalEvidence(string repositoryRoot, ProductEvidenceStep step)
    {
        return step.Name switch
        {
            "strict-scenario-sweep" => CheckStrictScenarioSweepEvidence(repositoryRoot, step),
            "public-admin-workbench" => CheckScenarioVisualEvidence(repositoryRoot, step, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light"),
            "production-e2e-workbench" => CheckScenarioVisualEvidence(repositoryRoot, step, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light"),
            _ => ProductEvidenceStepOutcome.ExternalEvidenceRequired(
                "External workflow or long-running scenario evidence is required; this dry run records the command without claiming it ran.",
                step.ArtifactPaths)
        };
    }

    private static ProductEvidenceStepOutcome CheckStrictScenarioSweepEvidence(string repositoryRoot, ProductEvidenceStep step)
    {
        var sweepRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "strict-scenario-sweep");
        var reportPath = Path.Combine(sweepRoot, "product-evidence.json");
        if (!File.Exists(reportPath))
        {
            return ProductEvidenceStepOutcome.ExternalEvidenceRequired(
                "Strict scenario sweep product-evidence.json is not attached yet.",
                step.ArtifactPaths);
        }

        ProductEvidenceDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ProductEvidenceDocument>(File.ReadAllText(reportPath), JsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            return ProductEvidenceStepOutcome.Failed(
                $"Strict scenario sweep product-evidence.json could not be parsed: {ex.Message}",
                step.ArtifactPaths);
        }

        if (document is null)
        {
            return ProductEvidenceStepOutcome.Failed(
                "Strict scenario sweep product-evidence.json is empty.",
                step.ArtifactPaths);
        }

        var expectedPlan = ProductEvidencePlan.Create(
            "strict-scenario-sweep",
            "artifacts/product-evidence/strict-scenario-sweep",
            repositoryRoot);
        if (expectedPlan.Steps.Count == 0)
        {
            return ProductEvidenceStepOutcome.Failed(
                "Strict scenario sweep has no current fixture scenarios to validate.",
                step.ArtifactPaths);
        }

        var expectedNames = expectedPlan.Steps
            .Select(expectedStep => expectedStep.Name)
            .ToArray();
        var actualNames = document.Steps
            .Select(result => result.Name)
            .ToArray();
        var duplicateActualNames = actualNames
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateActualNames.Length > 0)
        {
            return ProductEvidenceStepOutcome.Failed(
                $"Strict scenario sweep report contains duplicate step names: {string.Join(", ", duplicateActualNames)}.",
                step.ArtifactPaths);
        }

        var actualNameSet = actualNames.ToHashSet(StringComparer.Ordinal);
        var expectedNameSet = expectedNames.ToHashSet(StringComparer.Ordinal);
        var missingNames = expectedNames
            .Where(name => !actualNameSet.Contains(name))
            .ToArray();
        var unexpectedNames = actualNames
            .Where(name => !expectedNameSet.Contains(name))
            .ToArray();
        if (missingNames.Length > 0 || unexpectedNames.Length > 0)
        {
            var coverageProblems = new List<string>();
            if (missingNames.Length > 0)
            {
                coverageProblems.Add($"missing {string.Join(", ", missingNames)}");
            }

            if (unexpectedNames.Length > 0)
            {
                coverageProblems.Add($"unexpected {string.Join(", ", unexpectedNames)}");
            }

            return ProductEvidenceStepOutcome.Failed(
                $"Strict scenario sweep report does not match the current scenario set: {string.Join("; ", coverageProblems)}.",
                step.ArtifactPaths);
        }

        var failedSteps = document.Steps
            .Where(result => result.Status != "passed")
            .Select(result => $"{result.Name}={result.Status}")
            .ToArray();
        if (document.Status != "passed" || failedSteps.Length > 0)
        {
            return ProductEvidenceStepOutcome.Failed(
                $"Strict scenario sweep report status is '{document.Status}': {string.Join(", ", failedSteps)}.",
                step.ArtifactPaths);
        }

        var missingVisualRuns = new List<string>();
        var failedVisualRuns = new List<string>();
        foreach (var expectedStep in expectedPlan.Steps)
        {
            var visualRunPath = expectedStep.ArtifactPaths
                .FirstOrDefault(path => path.EndsWith("/visual/visual-run.json", StringComparison.Ordinal));
            if (visualRunPath is null)
            {
                missingVisualRuns.Add($"{expectedStep.Name}=missing-artifact-path");
                continue;
            }

            var resolvedVisualRunPath = ResolveArtifactPath(repositoryRoot, visualRunPath);
            if (!File.Exists(resolvedVisualRunPath))
            {
                missingVisualRuns.Add(expectedStep.Name);
                continue;
            }

            var status = ReadVisualRunStatus(resolvedVisualRunPath);
            if (status != "passed")
            {
                failedVisualRuns.Add($"{expectedStep.Name}={status ?? "missing-status"}");
            }
        }

        if (missingVisualRuns.Count > 0)
        {
            return ProductEvidenceStepOutcome.Failed(
                $"Strict scenario sweep report passed but visual-run artifacts are missing: {string.Join(", ", missingVisualRuns)}.",
                step.ArtifactPaths);
        }

        var stateCoverageProblems = CheckStrictScenarioSweepStateCoverage(repositoryRoot);
        if (stateCoverageProblems.Count > 0)
        {
            return ProductEvidenceStepOutcome.Failed(
                $"Strict scenario sweep state coverage evidence is incomplete: {string.Join(", ", stateCoverageProblems)}.",
                step.ArtifactPaths);
        }

        return failedVisualRuns.Count == 0
            ? ProductEvidenceStepOutcome.Passed(
                $"attached strict scenario sweep evidence passed ({expectedPlan.Steps.Count}/{expectedPlan.Steps.Count} visual runs).",
                step.ArtifactPaths)
            : ProductEvidenceStepOutcome.Failed(
                $"attached strict scenario sweep evidence has failed visual runs: {string.Join(", ", failedVisualRuns)}.",
                step.ArtifactPaths);
    }

    private static IReadOnlyList<string> CheckStrictScenarioSweepStateCoverage(string repositoryRoot)
    {
        var requirements = LoadProductionStateCoverageRequirements(repositoryRoot);
        if (requirements.Count == 0)
        {
            return Array.Empty<string>();
        }

        var problems = new List<string>();
        foreach (var requirement in requirements)
        {
            AddReleaseEvidencePathProblems(requirement, problems);

            var visualRunPath = ResolveArtifactPath(repositoryRoot, requirement.VisualRunPath);
            if (!File.Exists(visualRunPath))
            {
                problems.Add($"{requirement.Scenario}=missing-release-visual-run");
            }
            else
            {
                var status = ReadVisualRunStatus(visualRunPath);
                if (status != "passed")
                {
                    problems.Add($"{requirement.Scenario}=release-visual-run:{status ?? "missing-status"}");
                }
            }

            var accessibilityPath = ResolveArtifactPath(repositoryRoot, requirement.AccessibilityPath);
            AccessibilityDocument? accessibility = null;
            if (!File.Exists(accessibilityPath))
            {
                problems.Add($"{requirement.Scenario}=missing-accessibility");
            }
            else
            {
                try
                {
                    accessibility = JsonSerializer.Deserialize<AccessibilityDocument>(File.ReadAllText(accessibilityPath), JsonDefaults.Options);
                }
                catch (JsonException ex)
                {
                    problems.Add($"{requirement.Scenario}=invalid-accessibility:{ex.Message}");
                }

                if (accessibility is null)
                {
                    problems.Add($"{requirement.Scenario}=empty-accessibility");
                }
            }

            var evidencePath = ResolveArtifactPath(repositoryRoot, requirement.ComponentEvidencePath);
            if (!File.Exists(evidencePath))
            {
                problems.AddRange(requirement.Components.Select(component => $"{requirement.Scenario}/{component}=missing-component-evidence"));
                continue;
            }

            ComponentEvidenceDocument? evidence;
            try
            {
                evidence = JsonSerializer.Deserialize<ComponentEvidenceDocument>(File.ReadAllText(evidencePath), JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                problems.Add($"{requirement.Scenario}=invalid-component-evidence:{ex.Message}");
                continue;
            }

            if (evidence is null)
            {
                problems.Add($"{requirement.Scenario}=empty-component-evidence");
                continue;
            }

            var accessibilityNodes = new List<AccessibilityNode>();
            foreach (var component in requirement.Components)
            {
                var componentEvidence = evidence.Components.FirstOrDefault(entry =>
                    string.Equals(entry.Component, component, StringComparison.Ordinal));
                if (componentEvidence is null)
                {
                    problems.Add($"{requirement.Scenario}/{component}=missing-component-row");
                    continue;
                }

                if (componentEvidence.Presence != "present")
                {
                    problems.Add($"{requirement.Scenario}/{component}=presence:{componentEvidence.Presence}");
                }

                if (componentEvidence.InteractionStatus == "failed")
                {
                    problems.Add($"{requirement.Scenario}/{component}=interaction:failed");
                }

                if (!MeetsMinimumVisualGrade(componentEvidence.VisualGrade, requirement.MinimumVisualGrade))
                {
                    problems.Add($"{requirement.Scenario}/{component}=visual-grade:{componentEvidence.VisualGrade}<required:{requirement.MinimumVisualGrade}");
                }

                if (accessibility is not null)
                {
                    var target = componentEvidence.Target;
                    if (string.IsNullOrWhiteSpace(target))
                    {
                        problems.Add($"{requirement.Scenario}/{component}=missing-accessibility-target");
                        continue;
                    }

                    var node = FindAccessibilityNode(accessibility.Root, target);
                    if (node is null)
                    {
                        problems.Add($"{requirement.Scenario}/{component}=missing-accessibility-node:{target}");
                        continue;
                    }

                    accessibilityNodes.Add(node);
                    var stateProblem = ComponentAccessibilityStateProblem(requirement.State, node);
                    if (stateProblem is not null)
                    {
                        problems.Add($"{requirement.Scenario}/{component}=accessibility-state:{stateProblem}");
                    }
                }
            }

            if (accessibilityNodes.Count > 0)
            {
                var scenarioStateProblem = ScenarioAccessibilityStateProblem(requirement.State, accessibilityNodes);
                if (scenarioStateProblem is not null)
                {
                    problems.Add($"{requirement.Scenario}=accessibility-state:{scenarioStateProblem}");
                }
            }
        }

        return problems;
    }

    private static AccessibilityNode? FindAccessibilityNode(AccessibilityNode node, string target)
    {
        if (string.Equals(node.Name, target, StringComparison.Ordinal) ||
            string.Equals(node.AutomationId, target, StringComparison.Ordinal) ||
            string.Equals(node.Label, target, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindAccessibilityNode(child, target);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string? ComponentAccessibilityStateProblem(string state, AccessibilityNode node)
    {
        return state switch
        {
            "checked" when node.IsChecked != true => "checked",
            _ => null
        };
    }

    private static string? ScenarioAccessibilityStateProblem(string state, IReadOnlyList<AccessibilityNode> nodes)
    {
        return state switch
        {
            "disabled" when !nodes.Any(node => node.IsEnabled == false) => "disabled",
            "focused" when !nodes.Any(node => node.IsFocused) => "focused",
            "selected" when !nodes.Any(node => node.IsSelected == true) => "selected",
            _ => null
        };
    }

    private static IReadOnlyList<ProductionStateCoverageRequirement> LoadProductionStateCoverageRequirements(string repositoryRoot)
    {
        var matrixPath = Path.Combine(repositoryRoot, "docs", "visual-parity", "state-coverage-matrix.json");
        if (File.Exists(matrixPath))
        {
            var matrixRequirements = LoadStateCoverageMatrixRequirements(matrixPath);
            if (matrixRequirements.Count > 0)
            {
                return matrixRequirements;
            }
        }

        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        if (!File.Exists(inventoryPath))
        {
            return Array.Empty<ProductionStateCoverageRequirement>();
        }

        using var document = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        if (!document.RootElement.TryGetProperty("productionStateCoverage", out var coverage) ||
            coverage.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ProductionStateCoverageRequirement>();
        }

        var requirements = new List<ProductionStateCoverageRequirement>();
        foreach (var row in coverage.EnumerateArray())
        {
            if (!TryReadString(row, "scenario", out var scenario) ||
                !TryReadString(row, "state", out var state) ||
                !TryReadString(row, "minimumVisualGrade", out var minimumVisualGrade) ||
                !row.TryGetProperty("components", out var componentsElement) ||
                componentsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var components = componentsElement
                .EnumerateArray()
                .Select(component => component.GetString())
                .Where(component => !string.IsNullOrWhiteSpace(component))
                .Select(component => component!)
                .ToArray();
            if (components.Length == 0)
            {
                continue;
            }

            requirements.Add(new ProductionStateCoverageRequirement(
                scenario,
                state,
                components,
                minimumVisualGrade,
                StrictSweepArtifactPath(scenario, "visual/component-evidence.json"),
                StrictSweepArtifactPath(scenario, "accessibility.json"),
                StrictSweepArtifactPath(scenario, "visual/visual-run.json")));
        }

        return requirements;
    }

    private static IReadOnlyList<ProductionStateCoverageRequirement> LoadStateCoverageMatrixRequirements(string matrixPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(matrixPath));
        if (!document.RootElement.TryGetProperty("requirements", out var requirementsElement) ||
            requirementsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ProductionStateCoverageRequirement>();
        }

        var rows = new List<StateCoverageMatrixRequirementSourceRow>();
        foreach (var row in requirementsElement.EnumerateArray())
        {
            if (!TryReadString(row, "scenarioName", out var scenario) ||
                !TryReadString(row, "state", out var state) ||
                !TryReadString(row, "component", out var component) ||
                !TryReadString(row, "minimumVisualGrade", out var minimumVisualGrade))
            {
                continue;
            }

            rows.Add(new StateCoverageMatrixRequirementSourceRow(
                scenario,
                state,
                component,
                minimumVisualGrade,
                ReadOptionalString(row, "releaseComponentEvidencePath"),
                ReadOptionalString(row, "releaseAccessibilityEvidencePath"),
                ReadOptionalString(row, "releaseVisualRunPath")));
        }

        return rows
            .GroupBy(row => new
            {
                row.Scenario,
                row.State,
                row.MinimumVisualGrade,
                row.ComponentEvidencePath,
                row.AccessibilityPath,
                row.VisualRunPath
            })
            .Select(group => new ProductionStateCoverageRequirement(
                group.Key.Scenario,
                group.Key.State,
                group.Select(row => row.Component)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(component => component, StringComparer.Ordinal)
                    .ToArray(),
                group.Key.MinimumVisualGrade,
                group.Key.ComponentEvidencePath,
                group.Key.AccessibilityPath,
                group.Key.VisualRunPath))
            .ToArray();
    }

    private static void AddReleaseEvidencePathProblems(
        ProductionStateCoverageRequirement requirement,
        ICollection<string> problems)
    {
        AddReleaseEvidencePathProblem(
            requirement,
            problems,
            "release-component-evidence-path",
            requirement.ComponentEvidencePath,
            StrictSweepArtifactPath(requirement.Scenario, "visual/component-evidence.json"));
        AddReleaseEvidencePathProblem(
            requirement,
            problems,
            "release-accessibility-evidence-path",
            requirement.AccessibilityPath,
            StrictSweepArtifactPath(requirement.Scenario, "accessibility.json"));
        AddReleaseEvidencePathProblem(
            requirement,
            problems,
            "release-visual-run-path",
            requirement.VisualRunPath,
            StrictSweepArtifactPath(requirement.Scenario, "visual/visual-run.json"));
    }

    private static void AddReleaseEvidencePathProblem(
        ProductionStateCoverageRequirement requirement,
        ICollection<string> problems,
        string problem,
        string actualPath,
        string expectedPath)
    {
        if (string.Equals(actualPath, expectedPath, StringComparison.Ordinal))
        {
            return;
        }

        foreach (var component in requirement.Components)
        {
            problems.Add($"{requirement.Scenario}/{component}={problem}:{actualPath}<expected:{expectedPath}");
        }
    }

    private static bool TryReadString(JsonElement element, string property, out string value)
    {
        if (element.TryGetProperty(property, out var propertyValue) &&
            propertyValue.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(propertyValue.GetString()))
        {
            value = propertyValue.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string ReadOptionalString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var propertyValue) &&
            propertyValue.ValueKind == JsonValueKind.String
                ? propertyValue.GetString() ?? string.Empty
                : string.Empty;
    }

    private static bool MeetsMinimumVisualGrade(string actual, string minimum)
    {
        return VisualGradeRank.TryGetValue(actual, out var actualRank) &&
            VisualGradeRank.TryGetValue(minimum, out var minimumRank) &&
            actualRank >= minimumRank;
    }

    private static ProductEvidenceStepOutcome CheckScenarioVisualEvidence(
        string repositoryRoot,
        ProductEvidenceStep step,
        string artifactRoot)
    {
        var visualRunPath = Path.Combine(repositoryRoot, artifactRoot, "visual", "visual-run.json");
        if (!File.Exists(visualRunPath))
        {
            return ProductEvidenceStepOutcome.ExternalEvidenceRequired(
                $"{artifactRoot}/visual/visual-run.json is not attached yet.",
                step.ArtifactPaths);
        }

        var status = ReadVisualRunStatus(visualRunPath);
        return status == "passed"
            ? ProductEvidenceStepOutcome.Passed($"{artifactRoot} visual evidence is attached and passed.", step.ArtifactPaths)
            : ProductEvidenceStepOutcome.Failed($"{artifactRoot}/visual/visual-run.json status is '{status ?? "missing-status"}'.", step.ArtifactPaths);
    }

    private static ProductEvidenceStepOutcome CheckCatalogAudit(string repositoryRoot, ProductEvidenceStep step)
    {
        var path = Path.Combine(repositoryRoot, "docs", "compatibility", "all-catalog-readiness-audit.json");
        if (!File.Exists(path))
        {
            return ProductEvidenceStepOutcome.Failed("all-catalog-readiness-audit.json is missing.", step.ArtifactPaths);
        }

        var expected = JsonSerializer.Serialize(CatalogReadinessAudit.BuildFromCurrentCatalog(), JsonDefaults.Options);
        return NormalizeJson(File.ReadAllText(path)) == NormalizeJson(expected)
            ? ProductEvidenceStepOutcome.Passed("catalog readiness audit is current.", step.ArtifactPaths)
            : ProductEvidenceStepOutcome.Failed("all-catalog-readiness-audit.json is out of date.", step.ArtifactPaths);
    }

    private static ProductEvidenceStepOutcome CheckNativeReferenceReadiness(string repositoryRoot, ProductEvidenceStep step)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json");
        if (!File.Exists(path))
        {
            return ProductEvidenceStepOutcome.Failed("native-reference-readiness.json is missing.", step.ArtifactPaths);
        }

        var expected = JsonSerializer.Serialize(NativeReferenceReadinessBuilder.BuildFromPublicEvidence(repositoryRoot), JsonDefaults.Options);
        return NormalizeJson(File.ReadAllText(path)) == NormalizeJson(expected)
            ? ProductEvidenceStepOutcome.Passed("native reference readiness is current.", step.ArtifactPaths)
            : ProductEvidenceStepOutcome.Failed("native-reference-readiness.json is out of date.", step.ArtifactPaths);
    }

    private static ProductEvidenceStepOutcome CheckVisualReviewIndex(string repositoryRoot, ProductEvidenceStep step)
    {
        var outputDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity");
        var jsonPath = Path.Combine(outputDirectory, VisualReviewIndexArtifacts.JsonFileName);
        var htmlPath = Path.Combine(outputDirectory, VisualReviewIndexArtifacts.HtmlFileName);
        if (!File.Exists(jsonPath) || !File.Exists(htmlPath))
        {
            return ProductEvidenceStepOutcome.Failed("public visual review index JSON or HTML is missing.", step.ArtifactPaths);
        }

        var expected = VisualReviewIndexArtifacts.Build(repositoryRoot, outputDirectory);
        var expectedJson = JsonSerializer.Serialize(expected, JsonDefaults.Options);
        var expectedHtml = VisualReviewIndexArtifacts.BuildHtml(expected);
        return NormalizeJson(File.ReadAllText(jsonPath)) == NormalizeJson(expectedJson) &&
            NormalizeJson(File.ReadAllText(htmlPath)) == NormalizeJson(expectedHtml)
                ? ProductEvidenceStepOutcome.Passed("public visual review index is current.", step.ArtifactPaths)
                : ProductEvidenceStepOutcome.Failed("public visual review index is out of date.", step.ArtifactPaths);
    }

    private static ProductEvidenceStepOutcome CheckReleaseCandidateScaffold(string repositoryRoot, ProductEvidenceStep step)
    {
        string[] requiredDocs =
        {
            "README.md",
            "docs/release/production-evidence-view.md",
            "docs/release/support-policy.md",
            "docs/release/final-production-gate.md",
            "docs/compatibility/compatibility-levels.json",
            "docs/compatibility/compatibility-levels.md"
        };
        var missing = requiredDocs.Where(relative => !File.Exists(Path.Combine(repositoryRoot, relative))).ToArray();
        return missing.Length == 0
            ? ProductEvidenceStepOutcome.Passed("release candidate dry-run inputs are present; package dry-run remains separately gated.", step.ArtifactPaths)
            : ProductEvidenceStepOutcome.Failed($"release candidate dry-run inputs are missing: {string.Join(", ", missing)}.", step.ArtifactPaths);
    }

    private static ProductEvidenceStepOutcome OutcomeFromFreshness(EvidenceFreshnessResult freshness, ProductEvidenceStep step)
    {
        return freshness.Passed
            ? ProductEvidenceStepOutcome.Passed($"{freshness.Name} is current.", freshness.ArtifactPaths)
            : ProductEvidenceStepOutcome.Failed(string.Join(" ", freshness.Problems), freshness.ArtifactPaths);
    }

    private static string? ReadVisualRunStatus(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String
            ? status.GetString()
            : null;
    }

    private static string ResolveArtifactPath(string repositoryRoot, string path)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repositoryRoot, path));
    }

    private static string StrictSweepArtifactPath(string scenarioName, string artifactPath)
    {
        return Path.Combine(
                "artifacts",
                "product-evidence",
                "strict-scenario-sweep",
                scenarioName,
                artifactPath)
            .Replace('\\', '/');
    }

    private static string BuildMarkdown(ProductEvidenceDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Product Evidence");
        builder.AppendLine();
        builder.AppendLine($"Profile: `{document.Profile}`");
        builder.AppendLine($"Status: `{document.Status}`");
        builder.AppendLine($"Generated: `{document.GeneratedAt:O}`");
        builder.AppendLine();
        builder.AppendLine("| Step | Status | Blocking scope | Evidence |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var step in document.Steps)
        {
            var evidence = step.ArtifactPaths.Count == 0
                ? "none"
                : string.Join("<br>", step.ArtifactPaths.Select(path => $"`{path}`"));
            var reason = string.IsNullOrWhiteSpace(step.FailureReason)
                ? string.Empty
                : $" {step.FailureReason}";
            builder.AppendLine($"| {step.Name} | {step.Status}{reason} | {step.BlockingScope} | {evidence} |");
        }

        return builder.ToString();
    }

    private static string StableOutputRoot(string outputRoot)
    {
        return outputRoot.Replace('\\', '/');
    }

    private static string NormalizeJson(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
    }

    private sealed record ProductionStateCoverageRequirement(
        string Scenario,
        string State,
        IReadOnlyList<string> Components,
        string MinimumVisualGrade,
        string ComponentEvidencePath,
        string AccessibilityPath,
        string VisualRunPath);

    private sealed record StateCoverageMatrixRequirementSourceRow(
        string Scenario,
        string State,
        string Component,
        string MinimumVisualGrade,
        string ComponentEvidencePath,
        string AccessibilityPath,
        string VisualRunPath);
}
