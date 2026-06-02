using System.Diagnostics;
using System.Text.Json;
using WinUI3.MacCompatibility;
using WinUI3.MacRuntime;

/// <summary>
/// Phase 9 release candidate visual gate. It turns "ready" into a release
/// decision by aggregating the deterministic, locally verifiable release
/// requirements into a single artifact, and by listing the requirements that
/// can only be satisfied with external workflow evidence (native reference
/// capture, the full strict scenario sweep, and the signed package dry run).
/// </summary>
internal static class ReleaseCandidateCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var repositoryRoot = FindRepositoryRoot(Path.Combine(Environment.CurrentDirectory, "release-candidate"));
        var outputPath = Path.GetFullPath(
            ReadOption(args, "--output") ?? Path.Combine(repositoryRoot, "artifacts", "production-gates", "release-candidate.json"));
        var packageDirectory = ReadOption(args, "--package-dir");
        var skipPrivateNameScan = args.Contains("--skip-private-name-scan", StringComparer.Ordinal);

        var local = new List<ReleaseCheck>
        {
            CheckCatalogDispositions(),
            CheckCatalogAuditCurrent(repositoryRoot),
            CheckCatalogDocCounts(repositoryRoot),
            CheckZeroUnknownSurfaces(repositoryRoot),
            CheckBroaderControlHonesty(repositoryRoot),
            CheckNoOsCompositionClaim(repositoryRoot),
            CheckDriftGate(repositoryRoot),
            CheckComponentQualityDashboard(repositoryRoot),
            CheckNativeProvenancePolicy(repositoryRoot),
            CheckNativeReferenceReadiness(repositoryRoot),
            CheckReleaseDocsPresent(repositoryRoot),
        };

        if (!skipPrivateNameScan)
        {
            local.Add(CheckPrivateNameScan(repositoryRoot));
        }

        var external = new List<ReleaseCheck>
        {
            External("full-native-reference-workflow", "Capture native WinUI references for every claimed visual scenario via windows-native-screenshot.yml."),
            External("full-strict-scenario-sweep", "Run the full --renderer skia-v2 --strict-visual scenario sweep in CI."),
            CheckPackageDryRun(packageDirectory),
        };

        var failed = local.Count(check => check.Status == "failed");
        var passed = local.Count - failed;
        var status = failed == 0 ? "ready-pending-external-evidence" : "blocked";

        var document = new ReleaseCandidateDocument(
            "0.1",
            DateTimeOffset.UtcNow,
            status,
            ReleaseAllowed: false,
            passed,
            failed,
            local,
            external);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(document, JsonDefaults.Options));

        Console.WriteLine($"release-candidate: {status} ({passed} local checks passed, {failed} failed).");
        foreach (var check in local)
        {
            Console.WriteLine($"  [{check.Status}] {check.Name}: {check.Message}");
        }

        foreach (var check in external)
        {
            Console.WriteLine($"  [{check.Status}] {check.Name}: {check.Message}");
        }

        Console.WriteLine($"release-candidate.json: {outputPath}");
        return failed == 0 ? 0 : 1;
    }

    private static ReleaseCheck CheckCatalogDispositions()
    {
        var audit = CatalogReadinessAudit.BuildFromCurrentCatalog();
        return audit is { AccountedEntries: 126, UnassignedDispositionCount: 0 }
            ? Pass("catalog-dispositions", "126/126 catalog entries have a production disposition.", ("accounted", audit.AccountedEntries.ToString()))
            : Fail("catalog-dispositions", $"Catalog audit accounts for {audit.AccountedEntries} entries with {audit.UnassignedDispositionCount} unassigned.");
    }

    private static ReleaseCheck CheckCatalogAuditCurrent(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "compatibility", "all-catalog-readiness-audit.json");
        if (!File.Exists(path))
        {
            return Fail("catalog-audit-current", $"Missing {path}. Run 'winui3-mac-runner catalog-audit'.");
        }

        var expected = JsonSerializer.Serialize(CatalogReadinessAudit.BuildFromCurrentCatalog(), JsonDefaults.Options);
        return NormalizeJson(File.ReadAllText(path)) == NormalizeJson(expected)
            ? Pass("catalog-audit-current", "all-catalog-readiness-audit.json is up to date.")
            : Fail("catalog-audit-current", "all-catalog-readiness-audit.json is out of date. Run 'winui3-mac-runner catalog-audit'.");
    }

    private static ReleaseCheck CheckCatalogDocCounts(string repositoryRoot)
    {
        var catalogPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-api-compatibility.catalog.json");
        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "visual-readiness-inventory.json");
        if (!File.Exists(catalogPath) || !File.Exists(inventoryPath))
        {
            return Fail("catalog-docs-counts", "Catalog or visual readiness inventory file is missing.");
        }

        using var catalog = JsonDocument.Parse(File.ReadAllText(catalogPath));
        var statusCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var total = 0;
        foreach (var entry in catalog.RootElement.GetProperty("entries").EnumerateArray())
        {
            var entryStatus = entry.GetProperty("status").GetString() ?? string.Empty;
            statusCounts[entryStatus] = statusCounts.TryGetValue(entryStatus, out var current) ? current + 1 : 1;
            total++;
        }

        using var inventory = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var snapshot = inventory.RootElement.GetProperty("catalogSnapshot");
        if (snapshot.GetProperty("total").GetInt32() != total)
        {
            return Fail("catalog-docs-counts", $"Inventory total {snapshot.GetProperty("total").GetInt32()} does not match catalog total {total}.");
        }

        var inventoryStatus = snapshot.GetProperty("statusCounts");
        foreach (var (statusName, count) in statusCounts)
        {
            if (!inventoryStatus.TryGetProperty(statusName, out var value) || value.GetInt32() != count)
            {
                return Fail("catalog-docs-counts", $"Inventory status count for '{statusName}' does not match catalog.");
            }
        }

        return Pass("catalog-docs-counts", $"Catalog and inventory agree on {total} entries and all status counts.", ("total", total.ToString()));
    }

    private static ReleaseCheck CheckZeroUnknownSurfaces(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "compatibility", "corpus-unknown-apis.json");
        if (!File.Exists(path))
        {
            return Fail("zero-unknown-surfaces", $"Missing {path}.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var unknown = document.RootElement.GetProperty("entries").GetArrayLength();
        return unknown == 0
            ? Pass("zero-unknown-surfaces", "Corpus ingestion reports zero unknown public surfaces.")
            : Fail("zero-unknown-surfaces", $"Corpus ingestion reports {unknown} unknown public surfaces.");
    }

    private static ReleaseCheck CheckBroaderControlHonesty(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        if (!File.Exists(path))
        {
            return Fail("broader-control-honesty", $"Missing {path}.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var entries = root.GetProperty("entries").EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("component").GetString()!, entry => entry, StringComparer.Ordinal);

        foreach (var control in root.GetProperty("broaderControlInventory").GetProperty("controls").EnumerateArray())
        {
            var name = control.GetProperty("control").GetString()!;
            var grade = control.GetProperty("currentGrade").GetString();
            if (grade == "not-rendered")
            {
                continue;
            }

            if (!entries.TryGetValue(name, out var entry) ||
                entry.GetProperty("catalogStatus").GetString() is not ("supported" or "partial") ||
                entry.GetProperty("visualEvidence").GetString() == "not-rendered")
            {
                return Fail("broader-control-honesty", $"Broader control '{name}' claims grade '{grade}' without matching catalog status and visual evidence.");
            }
        }

        return Pass("broader-control-honesty", "No broader control claims a rendered grade without evidence.");
    }

    private static ReleaseCheck CheckNoOsCompositionClaim(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "compatibility", "material-motion-approximations.json");
        if (!File.Exists(path))
        {
            return Fail("no-os-composition-claim", $"Missing {path}.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.GetProperty("osCompositionClaim").GetBoolean())
        {
            return Fail("no-os-composition-claim", "Material/motion registry claims OS composition.");
        }

        foreach (var surface in root.GetProperty("surfaces").EnumerateArray())
        {
            if (surface.GetProperty("isOsComposition").GetBoolean())
            {
                return Fail("no-os-composition-claim", $"Surface '{surface.GetProperty("surface").GetString()}' claims OS composition.");
            }
        }

        return Pass("no-os-composition-claim", "No material/motion surface claims real Windows OS composition.");
    }

    private static ReleaseCheck CheckDriftGate(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "visual-drift-dashboard.json");
        if (!File.Exists(path))
        {
            return Fail("drift-gate", $"Missing {path}.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.GetProperty("gatedMetric").GetString() != "component-crop")
        {
            return Fail("drift-gate", "Visual drift dashboard must gate the component-crop metric.");
        }

        foreach (var family in root.GetProperty("families").EnumerateArray())
        {
            if (!family.GetProperty("componentCropDrift").GetProperty("gated").GetBoolean() ||
                family.GetProperty("wholeScreenDrift").GetProperty("gated").GetBoolean())
            {
                return Fail("drift-gate", $"Family '{family.GetProperty("family").GetString()}' must gate component-crop and keep whole-screen informational.");
            }
        }

        return Pass("drift-gate", "Component-crop drift is gated and whole-screen drift is informational.");
    }

    private static ReleaseCheck CheckComponentQualityDashboard(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "component-quality-dashboard.json");
        if (!File.Exists(path))
        {
            return Fail("component-quality-dashboard", $"Missing {path}. Run 'winui3-mac-runner component-quality-dashboard'.");
        }

        var expected = ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot);
        var expectedJson = JsonSerializer.Serialize(expected, JsonDefaults.Options);
        if (NormalizeJson(File.ReadAllText(path)) != NormalizeJson(expectedJson))
        {
            return Fail("component-quality-dashboard", "component-quality-dashboard.json is out of date. Run 'winui3-mac-runner component-quality-dashboard'.");
        }

        if (expected.Status != "passed")
        {
            return Fail(
                "component-quality-dashboard",
                $"{expected.Totals.BlockingRowCount} public component row(s) are not native-quality complete.");
        }

        return Pass(
            "component-quality-dashboard",
            $"All {expected.Totals.ComponentCount} public component rows have native-quality evidence.",
            ("rows", expected.Totals.ComponentCount.ToString()));
    }

    private static ReleaseCheck CheckNativeProvenancePolicy(string repositoryRoot)
    {
        var examplesRoot = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples");
        if (!Directory.Exists(examplesRoot))
        {
            return Fail("native-provenance-policy", $"Missing {examplesRoot}.");
        }

        var referenceFiles = Directory.EnumerateFiles(examplesRoot, "windows-reference.json", SearchOption.AllDirectories).ToArray();
        if (referenceFiles.Length == 0)
        {
            return Fail("native-provenance-policy", "No checked-in native reference provenance files were found.");
        }

        foreach (var referenceFile in referenceFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(referenceFile));
            if (document.RootElement.GetProperty("referenceSource").GetString() != "native-winui")
            {
                return Fail("native-provenance-policy", $"{Path.GetFileName(Path.GetDirectoryName(referenceFile))} reference is not native-winui.");
            }
        }

        return Pass("native-provenance-policy", $"All {referenceFiles.Length} checked-in references declare native-winui provenance.", ("references", referenceFiles.Length.ToString()));
    }

    private static ReleaseCheck CheckNativeReferenceReadiness(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json");
        if (!File.Exists(path))
        {
            return Fail("native-reference-readiness", $"Missing {path}.");
        }

        var expected = NativeReferenceReadinessBuilder.BuildFromPublicEvidence(repositoryRoot);
        var expectedJson = JsonSerializer.Serialize(expected, JsonDefaults.Options);
        if (NormalizeJson(File.ReadAllText(path)) != NormalizeJson(expectedJson))
        {
            return Fail(
                "native-reference-readiness",
                "native-reference-readiness.json is out of date. Run 'winui3-mac-runner native-reference-readiness'.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var rows = document.RootElement.GetProperty("rows").EnumerateArray().ToArray();
        var rowKeys = rows
            .Select(NativeReferenceReadinessKey)
            .ToHashSet(StringComparer.Ordinal);

        var dashboard = ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot);
        var dashboardKeys = dashboard.Rows
            .Select(row => row.ScenarioName + "|" + row.Component + "|" + (row.Target ?? string.Empty))
            .ToHashSet(StringComparer.Ordinal);
        if (!rowKeys.SetEquals(dashboardKeys))
        {
            return Fail(
                "native-reference-readiness",
                $"native-reference-readiness.json accounts for {rowKeys.Count} row(s), but the public dashboard has {dashboardKeys.Count} row(s).");
        }

        var blocking = rows
            .Where(row => row.GetProperty("nativeReferenceStatus").GetString() != "ready")
            .ToArray();
        if (blocking.Length > 0)
        {
            var statusCounts = blocking
                .Select(row => row.GetProperty("nativeReferenceStatus").GetString() ?? "missing")
                .GroupBy(status => status, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key}={group.Count()}");
            return Fail(
                "native-reference-readiness",
                $"{blocking.Length} public native reference row(s) are not source-ready: {string.Join(", ", statusCounts)}.");
        }

        return Pass("native-reference-readiness", $"All {rows.Length} public native reference rows are source-ready.", ("rows", rows.Length.ToString()));
    }

    private static ReleaseCheck CheckReleaseDocsPresent(string repositoryRoot)
    {
        string[] required =
        {
            "docs/release/final-production-gate.md",
            "docs/release/support-policy.md",
            "docs/release/level-7-release-readiness.md",
            "docs/release/production-evidence-view.md",
        };

        var missing = required.Where(relative => !File.Exists(Path.Combine(repositoryRoot, relative))).ToArray();
        return missing.Length == 0
            ? Pass("release-docs-present", "Release and support-policy documents are present.")
            : Fail("release-docs-present", $"Missing release documents: {string.Join(", ", missing)}.");
    }

    private static ReleaseCheck CheckPrivateNameScan(string repositoryRoot)
    {
        var scriptPath = Path.Combine(repositoryRoot, "tools", "private-name-denylist", "private-name-scan.sh");
        if (!File.Exists(scriptPath))
        {
            return Fail("private-name-scan", $"Missing {scriptPath}.");
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo("bash", $"\"{scriptPath}\"")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (process is null)
            {
                return Fail("private-name-scan", "Could not start the private-name scan.");
            }

            process.WaitForExit();
            return process.ExitCode == 0
                ? Pass("private-name-scan", "No denylisted private names found.")
                : Fail("private-name-scan", $"Private-name scan failed with exit code {process.ExitCode}.");
        }
        catch (Exception exception)
        {
            return Fail("private-name-scan", $"Private-name scan could not run: {exception.Message}.");
        }
    }

    private static ReleaseCheck CheckPackageDryRun(string? packageDirectory)
    {
        if (packageDirectory is null)
        {
            return External("package-dry-run-and-release-check", "Run dotnet pack and 'winui3-mac-runner release-check --package-dir <dir>' in CI, then pass --package-dir here to gate it.");
        }

        var resolved = Path.GetFullPath(packageDirectory);
        if (!Directory.Exists(resolved))
        {
            return Fail("package-dry-run-and-release-check", $"Package directory {resolved} does not exist. Run dotnet pack first.");
        }

        var packages = Directory.EnumerateFiles(resolved, "*.nupkg", SearchOption.TopDirectoryOnly).ToArray();
        return packages.Length > 0
            ? Pass("package-dry-run-and-release-check", $"Found {packages.Length} package dry-run artifacts.", ("packages", packages.Length.ToString()))
            : Fail("package-dry-run-and-release-check", $"No .nupkg artifacts in {resolved}. Run dotnet pack first.");
    }

    private static ReleaseCheck Pass(string name, string message, params (string Key, string Value)[] evidence)
    {
        return new ReleaseCheck(name, "passed", message, evidence.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
    }

    private static ReleaseCheck Fail(string name, string message)
    {
        return new ReleaseCheck(name, "failed", message, new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static ReleaseCheck External(string name, string message)
    {
        return new ReleaseCheck(name, "external-evidence-required", message, new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static string NativeReferenceReadinessKey(JsonElement row)
    {
        return row.GetProperty("scenarioName").GetString() + "|" +
            row.GetProperty("component").GetString() + "|" +
            (row.TryGetProperty("target", out var target) ? target.GetString() : string.Empty);
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] == name)
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string NormalizeJson(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(startPath))!);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinUI3.MacTestRuntime.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}

internal sealed record ReleaseCandidateDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Status,
    bool ReleaseAllowed,
    int LocalChecksPassed,
    int LocalChecksFailed,
    IReadOnlyList<ReleaseCheck> LocalChecks,
    IReadOnlyList<ReleaseCheck> ExternalEvidenceRequired);
