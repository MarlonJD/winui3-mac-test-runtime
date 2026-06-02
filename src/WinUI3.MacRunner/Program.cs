using System.Globalization;
using System.Text.Json;
using WinUI3.MacCompatibility;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;
using WinUI3.MacXaml;

return await Cli.RunAsync(args);

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        var invokedName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        if (invokedName.Contains("doctor", StringComparison.OrdinalIgnoreCase))
        {
            return RunDoctor(args);
        }

        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        return args[0] switch
        {
            "doctor" => RunDoctor(args[1..]),
            "run" => await RunProjectAsync(args[1..]),
            "ingest" => RunIngest(args[1..]),
            "benchmark" => await ProductionGatesCommand.RunBenchmarkAsync(args[1..]),
            "release-check" => await ProductionGatesCommand.RunReleaseCheckAsync(args[1..]),
            "release-candidate" => await ReleaseCandidateCommand.RunAsync(args[1..]),
            "catalog-audit" => RunCatalogAudit(args[1..]),
            "component-quality-dashboard" => RunComponentQualityDashboard(args[1..]),
            "component-inspection-template" => RunComponentInspectionTemplate(args[1..]),
            "component-inspection-apply" => RunComponentInspectionApply(args[1..]),
            "native-reference-import" => RunNativeReferenceImport(args[1..]),
            "visual-review" => await RunVisualReviewAsync(args[1..]),
            "visual-review-index" => RunVisualReviewIndex(args[1..]),
            "xaml" => RunXaml(args[1..]),
            _ => UnknownCommand(args[0])
        };
    }

    private static int RunIngest(string[] args)
    {
        var manifestPath = ReadOption(args, "--manifest");
        if (manifestPath is null)
        {
            Console.Error.WriteLine("Missing required option: --manifest <path>");
            return 2;
        }

        var configuration = ReadOption(args, "--configuration") ?? "Debug";
        var outputDirectory = ReadOption(args, "--output");
        var baselineDirectoryOption = ReadOption(args, "--baseline-dir");
        var check = HasOption(args, "--check");
        var writeBaseline = HasOption(args, "--write-baseline");

        try
        {
            var result = new CorpusInventoryService().Generate(manifestPath, configuration);
            var inventoryJson = JsonSerializer.Serialize(result.Inventory, JsonDefaults.Options);
            var unknownJson = JsonSerializer.Serialize(result.Unknown, JsonDefaults.Options);
            var summaryJson = JsonSerializer.Serialize(result.Summary, JsonDefaults.Options);

            if (outputDirectory is not null)
            {
                var outputRoot = Path.GetFullPath(outputDirectory);
                Directory.CreateDirectory(outputRoot);
                File.WriteAllText(Path.Combine(outputRoot, "corpus-inventory.json"), inventoryJson);
                File.WriteAllText(Path.Combine(outputRoot, "corpus-unknown-apis.json"), unknownJson);
                File.WriteAllText(Path.Combine(outputRoot, "corpus-summary.json"), summaryJson);
            }

            Console.WriteLine($"Corpus apps: {result.Summary.AppCount}");
            Console.WriteLine($"Discovered surfaces: {result.Summary.EntryCount}");
            Console.WriteLine($"Unknown surfaces: {result.Summary.UnknownCount}");
            foreach (var status in result.Summary.StatusCounts)
            {
                Console.WriteLine($"  status {status.Key}: {status.Value}");
            }

            foreach (var app in result.Summary.Apps)
            {
                Console.WriteLine(
                    $"  [{app.IngestionStatus}] {app.Id} ({app.Category}) tfm={app.TargetFramework} xaml={app.XamlFileCount} assets={app.Assets.Count}");
            }

            var baselineDirectory = baselineDirectoryOption is not null
                ? Path.GetFullPath(baselineDirectoryOption)
                : Path.Combine(FindRepositoryRoot(manifestPath), "docs", "compatibility");
            var inventoryBaseline = Path.Combine(baselineDirectory, "corpus-inventory.json");
            var unknownBaseline = Path.Combine(baselineDirectory, "corpus-unknown-apis.json");

            if (writeBaseline)
            {
                Directory.CreateDirectory(baselineDirectory);
                File.WriteAllText(inventoryBaseline, inventoryJson + Environment.NewLine);
                File.WriteAllText(unknownBaseline, unknownJson + Environment.NewLine);
                Console.WriteLine($"Wrote baseline: {inventoryBaseline}");
                Console.WriteLine($"Wrote baseline: {unknownBaseline}");
            }

            var failedApps = result.Summary.Apps
                .Where(app => app.IngestionStatus != "passed")
                .Select(app => app.Id)
                .ToArray();
            if (failedApps.Length > 0)
            {
                Console.Error.WriteLine($"Corpus ingestion failed for: {string.Join(", ", failedApps)}");
                return 1;
            }

            if (check)
            {
                var problems = new List<string>();
                if (result.Summary.UnknownCount > 0)
                {
                    problems.Add($"{result.Summary.UnknownCount} discovered surface(s) are not classified in the compatibility catalog.");
                }

                problems.AddRange(CompareBaseline("corpus-inventory.json", inventoryBaseline, inventoryJson));
                problems.AddRange(CompareBaseline("corpus-unknown-apis.json", unknownBaseline, unknownJson));
                if (problems.Count > 0)
                {
                    foreach (var problem in problems)
                    {
                        Console.Error.WriteLine($"corpus --check: {problem}");
                    }

                    Console.Error.WriteLine("Run `ingest --write-baseline` after reviewing the corpus surface change.");
                    return 1;
                }

                Console.WriteLine("corpus --check: inventory and unknown report match the tracked baseline.");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static IEnumerable<string> CompareBaseline(string name, string baselinePath, string generatedJson)
    {
        if (!File.Exists(baselinePath))
        {
            return new[] { $"{name} baseline is missing at {baselinePath}." };
        }

        var committed = NormalizeJson(File.ReadAllText(baselinePath));
        var generated = NormalizeJson(generatedJson);
        return string.Equals(committed, generated, StringComparison.Ordinal)
            ? Array.Empty<string>()
            : new[] { $"{name} drifted from the tracked baseline." };
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

    private static int RunDoctor(string[] args)
    {
        var json = args.Contains("--json", StringComparer.Ordinal);
        var report = MacDoctor.Check();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, JsonDefaults.Options));
            return 0;
        }

        Console.WriteLine("winui3-mac-doctor");
        Console.WriteLine($"Status: {report.Status}");
        Console.WriteLine($"Host: {report.Host}");
        Console.WriteLine($"Primary path requires Wine: {report.PrimaryPathRequiresWine}");
        Console.WriteLine($"Wine: {report.Wine.Status}, required={report.Wine.Required}, found={report.Wine.Found}");
        Console.WriteLine($".NET: {report.DotNetVersion}");
        Console.WriteLine($"OS: {report.OsDescription} ({report.Architecture})");
        return 0;
    }

    private static async Task<int> RunProjectAsync(string[] args)
    {
        var projectPath = ReadOption(args, "--project");
        if (projectPath is null)
        {
            Console.Error.WriteLine("Missing required option: --project <path>");
            return 2;
        }

        var configuration = ReadOption(args, "--configuration") ?? "Debug";
        var explicitOutputDirectory = ReadOption(args, "--output");
        var scriptPath = ReadOption(args, "--script");
        var rendererName = ReadOption(args, "--renderer") ?? "svg";
        var scenarioPath = ReadOption(args, "--scenario");
        var viewportOption = ReadOption(args, "--viewport");
        var scaleOption = ReadOption(args, "--scale");
        var themeOption = ReadOption(args, "--theme");
        var strictVisual = HasOption(args, "--strict-visual");
        var referencePath = ReadOption(args, "--reference");
        var diffOutputOption = ReadOption(args, "--diff-output");

        try
        {
            var scenario = string.IsNullOrWhiteSpace(scenarioPath)
                ? null
                : await VisualScenario.LoadAsync(scenarioPath);
            var repositoryRoot = FindRepositoryRoot(projectPath);
            var resolvedReferencePath = NativeReferenceImporter.ResolveReferenceImagePath(
                repositoryRoot,
                referencePath,
                scenario?.Name,
                scenarioPath);
            var visualSettings = CreateVisualSettings(
                scenario,
                rendererName,
                viewportOption,
                scaleOption,
                themeOption,
                strictVisual,
                referencePath,
                diffOutputOption);
            var outputDirectory = explicitOutputDirectory
                ?? DefaultOutputDirectory(visualSettings);
            var diffOutputDirectory = diffOutputOption
                ?? (visualSettings is null ? null : Path.Combine(outputDirectory, "visual"));

            var runner = new MacProjectRunner(CreateSnapshotRenderer(rendererName));
            var result = await runner.RunProjectAsync(projectPath, outputDirectory, configuration, scriptPath, visualSettings);
            Console.WriteLine($"Status: {result.Run.Status}");
            Console.WriteLine($"run.json: {result.RunJsonPath}");
            Console.WriteLine($"tree.json: {result.TreeJsonPath}");
            Console.WriteLine($"accessibility.json: {result.AccessibilityJsonPath}");
            Console.WriteLine($"unsupported-apis.json: {result.UnsupportedApisJsonPath}");
            Console.WriteLine($"snapshot.json: {result.SnapshotJsonPath}");
            if (visualSettings is not null && diffOutputDirectory is not null)
            {
                var visualPassed = await VisualArtifacts.WriteAsync(result, visualSettings, resolvedReferencePath, diffOutputDirectory);
                Console.WriteLine($"visual-run.json: {Path.Combine(Path.GetFullPath(diffOutputDirectory), "visual-run.json")}");
                Console.WriteLine($"mac-runtime.png: {Path.Combine(Path.GetFullPath(diffOutputDirectory), "mac-runtime.png")}");
                // Surface the strict-visual gate result explicitly. The base run can
                // pass while the visual gate fails, so reporting only the run status
                // above would hide a gate failure that still sets exit code 1.
                Console.WriteLine($"visual-status: {(visualPassed ? "passed" : "failed")}");
                if (!visualPassed)
                {
                    return 1;
                }
            }

            return result.Run.Status == "passed" ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static ISnapshotRenderer CreateSnapshotRenderer(string rendererName)
    {
        return rendererName.ToLowerInvariant() switch
        {
            "svg" => new SnapshotRenderer(),
            "skia" => new SkiaSnapshotRenderer(),
            "skia-v2" => new SkiaV2SnapshotRenderer(),
            _ => throw new ArgumentException($"Unknown renderer '{rendererName}'. Expected 'svg', 'skia', or 'skia-v2'.")
        };
    }

    private static VisualRunSettings? CreateVisualSettings(
        VisualScenario? scenario,
        string rendererName,
        string? viewportOption,
        string? scaleOption,
        string? themeOption,
        bool strictVisual,
        string? referencePath,
        string? diffOutputOption)
    {
        var visualRequested =
            scenario is not null ||
            string.Equals(rendererName, "skia-v2", StringComparison.OrdinalIgnoreCase) ||
            viewportOption is not null ||
            scaleOption is not null ||
            themeOption is not null ||
            strictVisual ||
            referencePath is not null ||
            diffOutputOption is not null;

        if (!visualRequested)
        {
            return null;
        }

        var viewport = viewportOption is null
            ? scenario?.Viewport ?? new VisualViewport(1280, 800)
            : VisualViewport.Parse(viewportOption);
        var scale = scaleOption is null
            ? scenario?.Scale ?? 1.0
            : ParsePositiveDouble(scaleOption, "--scale");
        var rawTheme = themeOption ?? scenario?.Theme ?? "light";
        if (!VisualTheme.IsSupported(rawTheme))
        {
            throw new ArgumentException($"Unsupported theme '{rawTheme}'. Expected light, dark, or high-contrast.");
        }

        var theme = VisualTheme.Normalize(rawTheme);

        return new VisualRunSettings(
            Scenario: scenario,
            ScenarioName: scenario?.Name ?? "ad-hoc",
            Renderer: rendererName.ToLowerInvariant(),
            Viewport: viewport,
            Scale: scale,
            Theme: theme,
            StrictVisual: strictVisual || scenario?.StrictVisual == true,
            Thresholds: scenario?.Thresholds ?? new VisualThresholds());
    }

    private static string DefaultOutputDirectory(VisualRunSettings? visualSettings)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "artifacts", "winui3-mac");
        return visualSettings is null
            ? root
            : Path.Combine(root, SanitizePathSegment(visualSettings.ScenarioName));
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(character => invalid.Contains(character) ? '-' : character).ToArray();
        return new string(chars);
    }

    private static int RunCatalogAudit(string[] args)
    {
        var repositoryRoot = FindRepositoryRoot(Path.Combine(Environment.CurrentDirectory, "catalog-audit"));
        var defaultPath = Path.Combine(repositoryRoot, "docs", "compatibility", "all-catalog-readiness-audit.json");
        var outputPath = Path.GetFullPath(ReadOption(args, "--output") ?? defaultPath);
        var check = HasOption(args, "--check");

        var audit = CatalogReadinessAudit.BuildFromCurrentCatalog();
        var json = JsonSerializer.Serialize(audit, JsonDefaults.Options);

        Console.WriteLine($"catalog-audit: {audit.AccountedEntries} entries accounted, {audit.UnassignedDispositionCount} unassigned.");
        foreach (var disposition in audit.DispositionCounts)
        {
            Console.WriteLine($"  disposition {disposition.Key}: {disposition.Value}");
        }

        if (audit.UnassignedDispositionCount != 0)
        {
            Console.Error.WriteLine($"catalog-audit failed: {audit.UnassignedDispositionCount} catalog entries have no production disposition.");
            return 1;
        }

        if (check)
        {
            if (!File.Exists(outputPath))
            {
                Console.Error.WriteLine($"catalog-audit --check failed: missing {outputPath}. Regenerate with 'winui3-mac-runner catalog-audit'.");
                return 1;
            }

            if (NormalizeJson(File.ReadAllText(outputPath)) != NormalizeJson(json))
            {
                Console.Error.WriteLine($"catalog-audit --check failed: {outputPath} is out of date. Regenerate with 'winui3-mac-runner catalog-audit'.");
                return 1;
            }

            Console.WriteLine($"catalog-audit --check passed: {outputPath} is up to date.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"all-catalog-readiness-audit.json: {outputPath}");
        return 0;
    }

    private static int RunComponentQualityDashboard(string[] args)
    {
        var repositoryRoot = FindRepositoryRoot(Path.Combine(Environment.CurrentDirectory, "component-quality-dashboard"));
        var defaultPath = Path.Combine(repositoryRoot, "docs", "visual-parity", "component-quality-dashboard.json");
        var outputPath = Path.GetFullPath(ReadOption(args, "--output") ?? defaultPath);
        var check = HasOption(args, "--check");

        var dashboard = ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot);
        var json = JsonSerializer.Serialize(dashboard, JsonDefaults.Options);

        Console.WriteLine(
            $"component-quality-dashboard: {dashboard.Totals.ComponentCount} rows, {dashboard.Totals.BlockingRowCount} blocker rows.");

        if (check)
        {
            if (!File.Exists(outputPath))
            {
                Console.Error.WriteLine($"component-quality-dashboard --check failed: missing {outputPath}. Regenerate with 'winui3-mac-runner component-quality-dashboard'.");
                return 1;
            }

            if (NormalizeJson(File.ReadAllText(outputPath)) != NormalizeJson(json))
            {
                Console.Error.WriteLine($"component-quality-dashboard --check failed: {outputPath} is out of date. Regenerate with 'winui3-mac-runner component-quality-dashboard'.");
                return 1;
            }

            Console.WriteLine($"component-quality-dashboard --check passed: {outputPath} is up to date.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"component-quality-dashboard.json: {outputPath}");
        return dashboard.Status == "passed" ? 0 : 1;
    }

    private static async Task<int> RunVisualReviewAsync(string[] args)
    {
        var scenarioPath = ReadOption(args, "--scenario");
        var evidencePath = ReadOption(args, "--evidence");
        var referenceDirectory = ReadOption(args, "--reference");
        var outputDirectory = ReadOption(args, "--output");

        if (string.IsNullOrWhiteSpace(evidencePath) && string.IsNullOrWhiteSpace(scenarioPath))
        {
            Console.Error.WriteLine("Missing required option: --scenario <path> or --evidence <component-evidence.json>.");
            return 2;
        }

        try
        {
            var repositoryRoot = FindRepositoryRoot(Path.Combine(Environment.CurrentDirectory, "visual-review"));
            VisualScenario? scenario = null;
            if (!string.IsNullOrWhiteSpace(scenarioPath))
            {
                scenario = await VisualScenario.LoadAsync(scenarioPath);
            }

            var resolvedEvidencePath = !string.IsNullOrWhiteSpace(evidencePath)
                ? Path.GetFullPath(evidencePath)
                : FindComponentEvidence(repositoryRoot, scenario!, referenceDirectory);
            if (resolvedEvidencePath is null)
            {
                Console.Error.WriteLine(
                    "visual-review failed: component-evidence.json was not found. Run 'winui3-mac-runner run --renderer skia-v2 --strict-visual --scenario <path> --reference <windows-reference.png>' first, or pass --evidence.");
                return 1;
            }

            var reviewOutputDirectory = outputDirectory is null
                ? Path.GetDirectoryName(resolvedEvidencePath)!
                : Path.GetFullPath(outputDirectory);
            var review = VisualReviewArtifacts.Write(resolvedEvidencePath, reviewOutputDirectory);
            Console.WriteLine($"visual-review: {review.Summary.ComponentCount} components, {review.Summary.CompleteTriptychCount} complete crop triptychs.");
            Console.WriteLine($"visual-review.html: {review.HtmlPath}");
            Console.WriteLine($"visual-review.json: {Path.Combine(reviewOutputDirectory, "visual-review.json")}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int RunVisualReviewIndex(string[] args)
    {
        var repositoryRoot = FindRepositoryRoot(Path.Combine(Environment.CurrentDirectory, "visual-review-index"));
        var outputDirectory = Path.GetFullPath(ReadOption(args, "--output") ?? Path.Combine(repositoryRoot, "docs", "visual-parity"));
        var check = HasOption(args, "--check");
        var document = VisualReviewIndexArtifacts.Build(repositoryRoot, outputDirectory);
        var jsonPath = Path.Combine(outputDirectory, VisualReviewIndexArtifacts.JsonFileName);
        var htmlPath = Path.Combine(outputDirectory, VisualReviewIndexArtifacts.HtmlFileName);
        var json = JsonSerializer.Serialize(document, JsonDefaults.Options);
        var html = VisualReviewIndexArtifacts.BuildHtml(document);

        Console.WriteLine(
            $"visual-review-index: {document.Summary.ComponentCount} rows, {document.Summary.CompleteTriptychCount} complete triptychs, {document.Summary.BlockingRowCount} blocker rows.");

        if (check)
        {
            if (!File.Exists(jsonPath) || !File.Exists(htmlPath))
            {
                Console.Error.WriteLine($"visual-review-index --check failed: missing {jsonPath} or {htmlPath}. Regenerate with 'winui3-mac-runner visual-review-index'.");
                return 1;
            }

            if (NormalizeJson(File.ReadAllText(jsonPath)) != NormalizeJson(json) ||
                NormalizeJson(File.ReadAllText(htmlPath)) != NormalizeJson(html))
            {
                Console.Error.WriteLine($"visual-review-index --check failed: {jsonPath} or {htmlPath} is out of date. Regenerate with 'winui3-mac-runner visual-review-index'.");
                return 1;
            }

            Console.WriteLine($"visual-review-index --check passed: {jsonPath} and {htmlPath} are up to date.");
            return 0;
        }

        VisualReviewIndexArtifacts.Write(repositoryRoot, outputDirectory);
        Console.WriteLine($"visual-review-index.html: {htmlPath}");
        Console.WriteLine($"visual-review-index.json: {jsonPath}");
        return document.Status == "passed" ? 0 : 1;
    }

    private static int RunComponentInspectionApply(string[] args)
    {
        var evidencePath = ReadOption(args, "--evidence");
        var inspectionPath = ReadOption(args, "--inspection");
        var outputPath = ReadOption(args, "--output");
        var check = HasOption(args, "--check");

        if (string.IsNullOrWhiteSpace(evidencePath))
        {
            Console.Error.WriteLine("Missing required option: --evidence <component-evidence.json>.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(inspectionPath))
        {
            Console.Error.WriteLine("Missing required option: --inspection <component-inspection.json>.");
            return 2;
        }

        try
        {
            var resolvedEvidencePath = Path.GetFullPath(evidencePath);
            var applied = ComponentInspectionApplier.Apply(resolvedEvidencePath, Path.GetFullPath(inspectionPath));
            var json = JsonSerializer.Serialize(applied, JsonDefaults.Options);
            var resolvedOutputPath = Path.GetFullPath(outputPath ?? resolvedEvidencePath);
            Console.WriteLine($"component-inspection-apply: {applied.Components.Count(component => component.Inspection is not null)} inspected component row(s).");

            if (check)
            {
                Console.WriteLine("component-inspection-apply --check passed.");
                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
            File.WriteAllText(resolvedOutputPath, json);
            Console.WriteLine($"component-evidence.json: {resolvedOutputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"component-inspection-apply failed: {ex.Message}");
            return 1;
        }
    }

    private static int RunComponentInspectionTemplate(string[] args)
    {
        var evidencePath = ReadOption(args, "--evidence");
        var outputPath = ReadOption(args, "--output");
        var check = HasOption(args, "--check");

        if (string.IsNullOrWhiteSpace(evidencePath))
        {
            Console.Error.WriteLine("Missing required option: --evidence <component-evidence.json>.");
            return 2;
        }

        try
        {
            var resolvedEvidencePath = Path.GetFullPath(evidencePath);
            var document = ComponentInspectionTemplate.Build(resolvedEvidencePath);
            var json = JsonSerializer.Serialize(document, JsonDefaults.Options);
            var resolvedOutputPath = Path.GetFullPath(outputPath ?? Path.Combine(
                Path.GetDirectoryName(resolvedEvidencePath)!,
                ComponentInspectionTemplate.DefaultFileName));

            Console.WriteLine($"component-inspection-template: {document.Rows.Count} pending inspection row(s).");

            if (check)
            {
                if (!File.Exists(resolvedOutputPath))
                {
                    Console.Error.WriteLine($"component-inspection-template --check failed: missing {resolvedOutputPath}.");
                    return 1;
                }

                if (NormalizeJson(File.ReadAllText(resolvedOutputPath)) != NormalizeJson(json))
                {
                    Console.Error.WriteLine($"component-inspection-template --check failed: {resolvedOutputPath} is out of date.");
                    return 1;
                }

                Console.WriteLine("component-inspection-template --check passed.");
                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
            File.WriteAllText(resolvedOutputPath, json);
            Console.WriteLine($"component-inspection-template.json: {resolvedOutputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"component-inspection-template failed: {ex.Message}");
            return 1;
        }
    }

    private static int RunNativeReferenceImport(string[] args)
    {
        var sourceRoot = ReadOption(args, "--source");
        if (sourceRoot is null)
        {
            Console.Error.WriteLine("Missing required option: --source <windows-reference-screenshots-dir>.");
            return 2;
        }

        var repositoryRoot = FindRepositoryRoot(Path.Combine(Environment.CurrentDirectory, "native-reference-import"));
        var outputRoot = Path.GetFullPath(ReadOption(args, "--output") ?? Path.Combine(repositoryRoot, "artifacts", "native-reference-import"));
        try
        {
            var import = NativeReferenceImporter.Import(repositoryRoot, sourceRoot, outputRoot);
            Console.WriteLine($"native-reference-import: {import.ImportedReferenceCount} references imported, {import.MissingComponentScenarioPaths.Count} component scenario references missing.");
            Console.WriteLine($"native-reference-import.json: {Path.Combine(outputRoot, "native-reference-import.json")}");
            if (import.Problems.Count > 0)
            {
                foreach (var problem in import.Problems)
                {
                    Console.Error.WriteLine($"native-reference-import: {problem}");
                }
            }

            return import.Status == "passed" ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int RunXaml(string[] args)
    {
        if (args.Length == 0 || args[0] != "compile")
        {
            Console.Error.WriteLine("Expected xaml compile.");
            return 2;
        }

        var outputPath = ReadOption(args, "--output");
        if (outputPath is null)
        {
            Console.Error.WriteLine("Missing required option: --output <path>");
            return 2;
        }

        var inputFiles = ReadPositionalInputs(args[1..]);
        if (inputFiles.Count == 0)
        {
            Console.Error.WriteLine("At least one XAML input file is required.");
            return 2;
        }

        var compiler = new MacXamlCompiler();
        var generatedSources = new List<string>();
        var diagnostics = new List<XamlDiagnostic>();

        foreach (var inputFile in inputFiles)
        {
            var result = compiler.CompileFile(inputFile);
            diagnostics.AddRange(result.Diagnostics);
            if (!string.IsNullOrWhiteSpace(result.GeneratedSource))
            {
                generatedSources.Add(result.GeneratedSource);
            }
        }

        var diagnosticsPath = Path.ChangeExtension(outputPath, ".diagnostics.json");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(diagnosticsPath, JsonSerializer.Serialize(diagnostics, JsonDefaults.Options));

        if (diagnostics.Any(diagnostic => diagnostic.Severity == "Error"))
        {
            Console.Error.WriteLine($"XAML compilation failed. Diagnostics: {diagnosticsPath}");
            return 1;
        }

        File.WriteAllText(outputPath, string.Join(Environment.NewLine, generatedSources));
        Console.WriteLine($"Generated XAML source: {outputPath}");
        return 0;
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

    private static bool HasOption(string[] args, string name)
    {
        return args.Contains(name, StringComparer.Ordinal);
    }

    private static double ParsePositiveDouble(string value, string option)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number <= 0)
        {
            throw new FormatException($"{option} must be a positive number.");
        }

        return number;
    }

    private static IReadOnlyList<string> ReadPositionalInputs(string[] args)
    {
        var values = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            var value = args[index];
            if (value.StartsWith("--", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            values.Add(value);
        }

        return values;
    }

    private static string? FindComponentEvidence(
        string repositoryRoot,
        VisualScenario scenario,
        string? referenceDirectory)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(referenceDirectory))
        {
            var referenceRoot = Path.GetFullPath(referenceDirectory);
            candidates.Add(Path.Combine(referenceRoot, "component-evidence.json"));
            candidates.Add(Path.Combine(referenceRoot, "visual", "component-evidence.json"));
            candidates.Add(Path.Combine(referenceRoot, scenario.Name, "component-evidence.json"));
            candidates.Add(Path.Combine(referenceRoot, scenario.Name, "visual", "component-evidence.json"));
        }

        candidates.Add(Path.Combine(repositoryRoot, "docs", "visual-parity", "examples", scenario.Name, "component-evidence.json"));
        candidates.Add(Path.Combine(repositoryRoot, "artifacts", "winui3-mac", scenario.Name, "visual", "component-evidence.json"));

        return candidates.FirstOrDefault(File.Exists);
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("WinUI3 Mac Test Runtime");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  doctor [--json]");
        Console.WriteLine("  run --project <path> [--configuration Debug] [--output <path>] [--script <path>] [--renderer svg|skia|skia-v2]");
        Console.WriteLine("      [--scenario <path>] [--viewport <width>x<height>] [--scale <number>] [--theme light|dark]");
        Console.WriteLine("      [--strict-visual] [--reference <png-or-native-reference-dir>] [--diff-output <dir>]");
        Console.WriteLine("  benchmark [--output <path>] [--iterations <count>]");
        Console.WriteLine("  release-check [--package-dir <dir>] [--output <path>]");
        Console.WriteLine("  release-candidate [--package-dir <dir>] [--output <path>] [--skip-private-name-scan]");
        Console.WriteLine("  catalog-audit [--output <path>] [--check]");
        Console.WriteLine("  component-quality-dashboard [--output <path>] [--check]");
        Console.WriteLine("  component-inspection-template --evidence <component-evidence.json> [--output <path>] [--check]");
        Console.WriteLine("  component-inspection-apply --evidence <component-evidence.json> --inspection <component-inspection.json> [--output <path>] [--check]");
        Console.WriteLine("  native-reference-import --source <windows-reference-screenshots-dir> [--output <dir>]");
        Console.WriteLine("  visual-review --scenario <path> --reference <dir> [--evidence <component-evidence.json>] [--output <dir>]");
        Console.WriteLine("  visual-review-index [--output <dir>] [--check]");
        Console.WriteLine("  ingest --manifest <path> [--configuration Debug] [--output <dir>] [--baseline-dir <dir>]");
        Console.WriteLine("      [--check] [--write-baseline]");
        Console.WriteLine("  xaml compile --output <path> <xaml-file> [...]");
    }
}
