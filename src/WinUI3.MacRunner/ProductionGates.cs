using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;
using WinUI3.MacXaml;

internal static class ProductionGatesCommand
{
    private static readonly string[] ExpectedPackageIds =
    {
        "MarlonJD.WinUI3.MacRunner",
        "MarlonJD.WinUI3.MacCompat",
        "MarlonJD.WinUI3.MacRuntime",
        "MarlonJD.WinUI3.MacXaml",
        "MarlonJD.WinUI3.MacRenderer.Skia",
        "MarlonJD.WinUI3.MacTest.Sdk"
    };

    public static async Task<int> RunBenchmarkAsync(string[] args)
    {
        var outputPath = Path.GetFullPath(ReadOption(args, "--output") ?? Path.Combine("artifacts", "production-gates", "benchmark.json"));
        var iterations = ReadPositiveInt(args, "--iterations", defaultValue: 3);
        var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
        var outputDirectory = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(outputDirectory);

        var metrics = new List<BenchmarkMetric>();
        var flakes = new List<FlakeMetric>();
        await MeasureAsync(metrics, "corpus-ingestion", 5000, () =>
        {
            _ = new CorpusInventoryService().Generate(Path.Combine(repositoryRoot, "fixtures", "corpus.json"), "Debug");
            return Task.CompletedTask;
        });
        await MeasureAsync(metrics, "xaml-compile-three-files", 2000, () =>
        {
            var compiler = new MacXamlCompiler();
            _ = compiler.CompileFile(Path.Combine(repositoryRoot, "fixtures", "SingleWindowApp.WinUI", "MainWindow.xaml"));
            _ = compiler.CompileFile(Path.Combine(repositoryRoot, "fixtures", "SettingsFormApp.WinUI", "MainWindow.xaml"));
            _ = compiler.CompileFile(Path.Combine(repositoryRoot, "fixtures", "ResourceCatalogApp.WinUI", "MainWindow.xaml"));
            return Task.CompletedTask;
        });
        await MeasureAsync(metrics, "interaction-script", 1000, () =>
        {
            var (_, window, script) = CreateInteractionFixture();
            _ = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>())).Run(window, script);
            return Task.CompletedTask;
        });
        await MeasureAsync(metrics, "skia-v2-render", 2500, async () =>
        {
            var tree = UiTreeBuilder.Build(new Window
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Name = "TitleText", Text = "Benchmark" },
                        new Button { Name = "PrimaryButton", Content = "Run" },
                        new InfoBar { Name = "StatusInfo", Title = "Ready", Message = "Benchmark artifact" }
                    }
                }
            });
            var settings = new VisualRunSettings(null, "benchmark-render", "skia-v2", new VisualViewport(900, 560), 1.0, "light", true, new VisualThresholds());
            var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
            _ = await new SkiaV2SnapshotRenderer().RenderAsync(
                arranged,
                Path.Combine(outputDirectory, "benchmark-render"),
                new SnapshotRenderOptions("skia-v2", "benchmark-render", settings.Viewport, settings.Scale, settings.Theme, settings.StrictVisual, "mac-runtime.png"));
        });
        await MeasureAsync(metrics, "artifact-generation", 5000, async () =>
        {
            var assemblyPath = Path.Combine(
                repositoryRoot,
                "fixtures",
                "InteractionBindingApp.MacTest",
                "bin",
                "Debug",
                "net10.0",
                "InteractionBindingApp.MacTest.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Interaction fixture assembly was not built.", assemblyPath);
            }

            _ = await new MacApplicationHost(new SkiaSnapshotRenderer()).RunAsync(new MacRunOptions(
                AssemblyPath: assemblyPath,
                ProjectPath: null,
                OutputDirectory: Path.Combine(outputDirectory, "artifact-generation")));
        });

        var flakeFailures = 0;
        var flakeStopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var (_, window, script) = CreateInteractionFixture();
            var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>())).Run(window, script);
            if (report.Steps.Any(step => step.Status != "passed"))
            {
                flakeFailures++;
            }
        }

        flakeStopwatch.Stop();
        flakes.Add(new FlakeMetric(
            Name: "interaction-script-repeatability",
            Iterations: iterations,
            Failures: flakeFailures,
            FlakeRate: iterations == 0 ? 1 : (double)flakeFailures / iterations,
            Threshold: 0,
            Status: flakeFailures == 0 ? "passed" : "failed",
            DurationMs: flakeStopwatch.ElapsedMilliseconds));

        GC.Collect();
        GC.WaitForPendingFinalizers();
        metrics.Add(new BenchmarkMetric(
            Name: "managed-memory-after-gates",
            ElapsedMs: 0,
            ThresholdMs: 0,
            Status: "informational",
            Unit: "mb",
            Value: Math.Round(GC.GetTotalMemory(forceFullCollection: true) / 1024d / 1024d, 2)));

        var status = metrics.Any(metric => metric.Status == "failed") || flakes.Any(metric => metric.Status == "failed")
            ? "failed"
            : "passed";
        var document = new BenchmarkGateDocument(
            SchemaVersion: "0.1",
            GeneratedAt: DateTimeOffset.UtcNow,
            Status: status,
            Host: Environment.OSVersion.ToString(),
            Iterations: iterations,
            Metrics: metrics,
            Flakes: flakes);

        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(document, JsonDefaults.Options));
        Console.WriteLine($"benchmark: {status}");
        Console.WriteLine($"benchmark.json: {outputPath}");
        foreach (var metric in metrics)
        {
            Console.WriteLine($"  {metric.Status} {metric.Name}: {metric.ElapsedMs}ms threshold={metric.ThresholdMs}ms");
        }

        foreach (var flake in flakes)
        {
            Console.WriteLine($"  {flake.Status} {flake.Name}: failures={flake.Failures}/{flake.Iterations} rate={flake.FlakeRate:0.###}");
        }

        return status == "passed" ? 0 : 1;
    }

    public static async Task<int> RunReleaseCheckAsync(string[] args)
    {
        var outputPath = Path.GetFullPath(ReadOption(args, "--output") ?? Path.Combine("artifacts", "production-gates", "release-readiness.json"));
        var packageDirectory = Path.GetFullPath(ReadOption(args, "--package-dir") ?? Path.Combine("artifacts", "packages"));
        var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var checks = new List<ReleaseCheck>
        {
            CheckFile(repositoryRoot, "docs/security/threat-model.md", "security-threat-model"),
            CheckFile(repositoryRoot, "docs/release/release-gates.md", "release-gates-doc"),
            CheckFile(repositoryRoot, "docs/release/level-7-release-readiness.md", "level-7-readiness-doc"),
            CheckFile(repositoryRoot, "docs/compatibility/contracts.md", "compatibility-contract"),
            CheckPackageVersion(repositoryRoot),
            CheckPackageDirectory(packageDirectory)
        };

        foreach (var packageId in ExpectedPackageIds)
        {
            checks.Add(CheckPackage(packageDirectory, packageId));
        }

        checks.Add(new ReleaseCheck(
            Name: "signing-provenance-dry-run",
            Status: checks.Any(check => check.Status == "failed") ? "failed" : "passed",
            Message: "Dry run requires package files, release/security docs, repository URL, license metadata, and workflow artifact provenance before publishing. Packages are not signed in CI; production publishing remains blocked unless signing evidence is attached.",
            Evidence: new Dictionary<string, string>
            {
                ["packageDirectory"] = packageDirectory,
                ["workflow"] = ".github/workflows/ci.yml",
                ["nativeReferenceWorkflow"] = ".github/workflows/windows-native-screenshot.yml"
            }));

        var status = checks.Any(check => check.Status == "failed") ? "failed" : "passed";
        var document = new ReleaseReadinessDocument(
            SchemaVersion: "0.1",
            GeneratedAt: DateTimeOffset.UtcNow,
            Status: status,
            PublishAllowed: false,
            Checks: checks);

        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(document, JsonDefaults.Options));
        Console.WriteLine($"release-check: {status}");
        Console.WriteLine($"release-readiness.json: {outputPath}");
        foreach (var check in checks)
        {
            Console.WriteLine($"  {check.Status} {check.Name}: {check.Message}");
        }

        return status == "passed" ? 0 : 1;
    }

    private static async Task MeasureAsync(ICollection<BenchmarkMetric> metrics, string name, long thresholdMs, Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action();
            stopwatch.Stop();
            metrics.Add(new BenchmarkMetric(name, stopwatch.ElapsedMilliseconds, thresholdMs, stopwatch.ElapsedMilliseconds <= thresholdMs ? "passed" : "failed", "ms", stopwatch.ElapsedMilliseconds));
        }
        catch (Exception error)
        {
            stopwatch.Stop();
            metrics.Add(new BenchmarkMetric(name, stopwatch.ElapsedMilliseconds, thresholdMs, "failed", "ms", stopwatch.ElapsedMilliseconds, error.Message));
        }
    }

    private static (StackPanel Root, Window Window, InteractionScript Script) CreateInteractionFixture()
    {
        var title = new TextBlock { Name = "TitleText" };
        var textBox = new TextBox { Name = "TitleBox" };
        var button = new Button { Name = "ApplyButton", Content = "Apply" };
        var listView = new ListView { Name = "TaskList" };
        listView.Items.Add("Open");
        listView.Items.Add("Closed");

        var state = new BenchmarkState();
        var root = new StackPanel { DataContext = state };
        root.Children.Add(title);
        root.Children.Add(textBox);
        root.Children.Add(button);
        root.Children.Add(listView);
        BindingOperations.SetBinding(title, nameof(TextBlock.Text), new Binding(nameof(BenchmarkState.Title)));
        BindingOperations.SetBinding(textBox, nameof(TextBox.Text), new Binding(nameof(BenchmarkState.Title), BindingMode.TwoWay));
        button.Click += (_, _) => state.Title = "Applied";
        var window = new Window { Content = root };
        BindingOperations.RefreshTree(window);

        var script = new InteractionScript(new[]
        {
            new InteractionAction("typeText", "TitleBox", null, null, null, "Updated"),
            new InteractionAction("selectItem", "TaskList", null, null, null, "Closed"),
            new InteractionAction("click", "ApplyButton", null, null, null, null),
            new InteractionAction("assertProperty", "TitleText", "Text", null, null, "Applied")
        });
        return (root, window, script);
    }

    private static ReleaseCheck CheckFile(string repositoryRoot, string relativePath, string name)
    {
        var path = Path.Combine(repositoryRoot, relativePath);
        return File.Exists(path)
            ? new ReleaseCheck(name, "passed", $"Found {relativePath}.", new Dictionary<string, string> { ["path"] = relativePath })
            : new ReleaseCheck(name, "failed", $"Missing {relativePath}.", new Dictionary<string, string> { ["path"] = relativePath });
    }

    private static ReleaseCheck CheckPackageVersion(string repositoryRoot)
    {
        var propsPath = Path.Combine(repositoryRoot, "Directory.Build.props");
        var text = File.ReadAllText(propsPath);
        var version = Regex.Match(text, "<PackageVersion>(?<version>[^<]+)</PackageVersion>").Groups["version"].Value;
        var hasRepository = text.Contains("<RepositoryUrl>https://github.com/MarlonJD/winui3-mac-test-runtime</RepositoryUrl>", StringComparison.Ordinal);
        var hasLicense = text.Contains("<PackageLicenseExpression>LGPL-3.0-or-later</PackageLicenseExpression>", StringComparison.Ordinal);
        var semver = Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$");
        var passed = semver && hasRepository && hasLicense;
        return new ReleaseCheck(
            "package-metadata",
            passed ? "passed" : "failed",
            passed ? $"Package metadata is complete for version {version}." : "Package version, repository URL, or license metadata is incomplete.",
            new Dictionary<string, string>
            {
                ["version"] = version,
                ["repositoryUrl"] = hasRepository.ToString(),
                ["license"] = hasLicense.ToString()
            });
    }

    private static ReleaseCheck CheckPackageDirectory(string packageDirectory)
    {
        return Directory.Exists(packageDirectory)
            ? new ReleaseCheck("package-directory", "passed", "Package output directory exists.", new Dictionary<string, string> { ["path"] = packageDirectory })
            : new ReleaseCheck("package-directory", "failed", "Package output directory is missing. Run dotnet pack before release-check.", new Dictionary<string, string> { ["path"] = packageDirectory });
    }

    private static ReleaseCheck CheckPackage(string packageDirectory, string packageId)
    {
        var files = Directory.Exists(packageDirectory)
            ? Directory.GetFiles(packageDirectory, packageId + ".*.nupkg")
            : Array.Empty<string>();
        return files.Length > 0
            ? new ReleaseCheck("package:" + packageId, "passed", "Package dry-run artifact exists.", new Dictionary<string, string> { ["file"] = Path.GetFileName(files[0]) })
            : new ReleaseCheck("package:" + packageId, "failed", "Package dry-run artifact is missing.", new Dictionary<string, string> { ["packageId"] = packageId });
    }

    private static int ReadPositiveInt(string[] args, string name, int defaultValue)
    {
        var value = ReadOption(args, name);
        return int.TryParse(value, out var number) && number > 0 ? number : defaultValue;
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

    private static string FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
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

    private sealed class BenchmarkState
    {
        public string Title { get; set; } = "Initial";
    }
}

internal sealed record BenchmarkGateDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Status,
    string Host,
    int Iterations,
    IReadOnlyList<BenchmarkMetric> Metrics,
    IReadOnlyList<FlakeMetric> Flakes);

internal sealed record BenchmarkMetric(
    string Name,
    long ElapsedMs,
    long ThresholdMs,
    string Status,
    string Unit,
    double Value,
    string? Message = null);

internal sealed record FlakeMetric(
    string Name,
    int Iterations,
    int Failures,
    double FlakeRate,
    double Threshold,
    string Status,
    long DurationMs);

internal sealed record ReleaseReadinessDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Status,
    bool PublishAllowed,
    IReadOnlyList<ReleaseCheck> Checks);

internal sealed record ReleaseCheck(
    string Name,
    string Status,
    string Message,
    IReadOnlyDictionary<string, string> Evidence);
