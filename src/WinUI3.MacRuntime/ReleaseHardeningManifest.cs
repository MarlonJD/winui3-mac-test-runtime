namespace WinUI3.MacRuntime;

public sealed record ReleaseHardeningManifestDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Profile,
    string Status,
    string Policy,
    IReadOnlyList<ReleaseHardeningCategory> Categories,
    IReadOnlyList<ReleaseHardeningDemo> Demos,
    IReadOnlyList<string> RequiredDocs,
    IReadOnlyList<string> LocalCommands,
    IReadOnlyList<string> ExternalWorkflows,
    IReadOnlyList<string> BaselineArtifacts,
    IReadOnlyList<string> CompatibilityMatrices,
    IReadOnlyList<string> ArtifactRetentionDocs,
    IReadOnlyList<string> KnownGapDocs);

public sealed record ReleaseHardeningCategory(
    string Name,
    string Status,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ReleaseGate);

public sealed record ReleaseHardeningDemo(
    string Name,
    string ProjectPath,
    string Policy,
    IReadOnlyList<string> Commands,
    IReadOnlyList<string> ExpectedArtifacts);

public static class ReleaseHardeningManifestBuilder
{
    public const string DefaultArtifactPath = "docs/release/release-hardening-manifest.json";

    private const string Profile = "phase15-release-hardening";

    private const string Policy =
        "Phase 15 hardens the public source-level harness release package. It does not expand the support claim beyond the documented subset, does not claim native WinUI visual fidelity, and does not run Windows binaries, .exe, or .msix payloads on macOS.";

    public static ReleaseHardeningManifestDocument Build(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var requiredDocs = RequiredDocs();
        var categories = Categories();
        var missingRequiredDocs = requiredDocs
            .Where(relative => !File.Exists(Path.Combine(root, relative)))
            .ToArray();
        var missingWorkflowDocs = Workflows()
            .Where(relative => !File.Exists(Path.Combine(root, relative)))
            .ToArray();
        var status = missingRequiredDocs.Length == 0 && missingWorkflowDocs.Length == 0
            ? "ready-pending-external-evidence"
            : "blocked";

        return new ReleaseHardeningManifestDocument(
            ArtifactSchemas.ReleaseHardeningManifest,
            DateTimeOffset.UnixEpoch,
            Profile,
            status,
            Policy,
            categories,
            Demos(),
            requiredDocs,
            LocalCommands(),
            Workflows(),
            BaselineArtifacts(),
            CompatibilityMatrices(),
            ArtifactRetentionDocs(),
            KnownGapDocs());
    }

    public static ReleaseHardeningManifestDocument Write(string repositoryRoot, string? outputPath = null)
    {
        var document = Build(repositoryRoot);
        var resolvedOutputPath = Path.GetFullPath(outputPath ?? Path.Combine(repositoryRoot, DefaultArtifactPath));
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
        File.WriteAllText(resolvedOutputPath, System.Text.Json.JsonSerializer.Serialize(document, JsonDefaults.Options));
        return document;
    }

    private static IReadOnlyList<ReleaseHardeningCategory> Categories()
    {
        return new[]
        {
            Category(
                "CLI polish",
                "tools/winui3-mac-release-ready-local",
                "README.md",
                "release-candidate"),
            Category(
                "GitHub Action docs",
                "docs/release/sample-workflows.md",
                ".github/workflows/ci.yml",
                ".github/workflows/windows-native-screenshot.yml"),
            Category(
                "Sample projects",
                "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj",
                "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
                "fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj"),
            Category(
                "Known gaps",
                "docs/release/phase-15-release-hardening.md",
                "docs/release/production-readiness.md",
                "docs/visual-parity/broader-control-state-coverage.json"),
            Category(
                "Baseline management",
                "docs/compatibility/corpus-inventory.json",
                "docs/compatibility/corpus-unknown-apis.json",
                "docs/visual-parity/state-coverage-matrix.json"),
            Category(
                "Artifact retention",
                "docs/release/sample-workflows.md",
                "docs/release/release-gates.md",
                "artifacts/product-evidence/public-product"),
            Category(
                "Versioned compatibility matrix",
                "docs/compatibility/matrix.md",
                "docs/compatibility/compatibility-levels.json",
                "docs/release/release-hardening-manifest.json")
        };
    }

    private static ReleaseHardeningCategory Category(string name, params string[] evidence)
    {
        return new ReleaseHardeningCategory(
            name,
            "documented",
            evidence,
            new[] { "winui3-mac-runner release-candidate", "winui3-mac-release-ready-local" });
    }

    private static IReadOnlyList<ReleaseHardeningDemo> Demos()
    {
        return new[]
        {
            new ReleaseHardeningDemo(
                "no-app-source-change-supported-subset",
                "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj",
                "The portable-headless run inspects and renders source-level supported subset artifacts; it does not mutate app source and does not execute .exe or .msix output on macOS.",
                new[]
                {
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner run --project fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --output artifacts/portable-headless/public-admin-workbench-light",
                    "gh workflow run windows-native-screenshot.yml -f scenario=public-admin-workbench-light",
                    "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner portable-headless-dashboard --portable artifacts/portable-headless/public-admin-workbench-light --windows-reference artifacts/windows-reference/public-admin-workbench-light --output artifacts/comparison/public-admin-workbench-light"
                },
                new[]
                {
                    "run.json",
                    "tree.json",
                    "accessibility.json",
                    "visual/visual-run.json",
                    "visual/windows-reference.json",
                    "portable-headless-comparison-dashboard.json"
                })
        };
    }

    private static IReadOnlyList<string> RequiredDocs()
    {
        return new[]
        {
            "README.md",
            "docs/release/phase-15-release-hardening.md",
            "docs/release/sample-workflows.md",
            "docs/release/release-gates.md",
            "docs/release/support-policy.md",
            "docs/release/final-production-gate.md",
            "docs/release/production-evidence-view.md",
            "docs/compatibility/matrix.md",
            "docs/compatibility/component-support.md",
            "docs/architecture/ci-strategy.md",
            "docs/architecture/artifacts.md"
        };
    }

    private static IReadOnlyList<string> LocalCommands()
    {
        return new[]
        {
            "PATH=\"$PWD/tools:$PATH\" winui3-mac-release-ready-local",
            "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner release-hardening-manifest --check",
            "PATH=\"$PWD/tools:$PATH\" winui3-mac-runner release-candidate"
        };
    }

    private static IReadOnlyList<string> Workflows()
    {
        return new[]
        {
            ".github/workflows/ci.yml",
            ".github/workflows/windows-native-screenshot.yml"
        };
    }

    private static IReadOnlyList<string> BaselineArtifacts()
    {
        return new[]
        {
            "docs/compatibility/corpus-inventory.json",
            "docs/compatibility/corpus-unknown-apis.json",
            "docs/visual-parity/component-quality-dashboard.json",
            "docs/visual-parity/state-coverage-matrix.json",
            "docs/visual-parity/native-quality-family-tranches.json",
            "docs/visual-parity/broader-control-state-coverage.json"
        };
    }

    private static IReadOnlyList<string> CompatibilityMatrices()
    {
        return new[]
        {
            "docs/compatibility/matrix.md",
            "docs/compatibility/component-support.md",
            "docs/compatibility/compatibility-levels.json",
            "docs/compatibility/winui-api-compatibility.catalog.json"
        };
    }

    private static IReadOnlyList<string> ArtifactRetentionDocs()
    {
        return new[]
        {
            "docs/release/sample-workflows.md",
            "docs/release/release-gates.md",
            "docs/architecture/artifacts.md"
        };
    }

    private static IReadOnlyList<string> KnownGapDocs()
    {
        return new[]
        {
            "docs/release/phase-15-release-hardening.md",
            "docs/release/production-readiness.md",
            "docs/visual-parity/broader-control-state-coverage.json",
            "docs/visual-parity/native-quality-family-tranches.json"
        };
    }
}
