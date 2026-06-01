using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using WinUI3.MacCompat.Diagnostics;

namespace WinUI3.MacRuntime;

public sealed record MacRunOptions(
    string AssemblyPath,
    string? ProjectPath,
    string OutputDirectory,
    string? ScriptPath = null,
    VisualRunSettings? VisualSettings = null,
    string? ProjectIngestionJsonPath = null);

public sealed record MacRunResult(
    RunReport Run,
    UiTreeDocument Tree,
    AccessibilityDocument Accessibility,
    SnapshotResult Snapshot,
    string RunJsonPath,
    string TreeJsonPath,
    string AccessibilityJsonPath,
    string BindingFailuresJsonPath,
    string ResourceFailuresJsonPath,
    string UnsupportedApisJsonPath,
    string DiagnosticsSarifPath,
    string? InteractionJsonPath,
    string SnapshotJsonPath,
    IReadOnlyList<UnsupportedVisualFeature> UnsupportedVisualFeatures);

public sealed record RunReport(
    string SchemaVersion,
    string Status,
    string Host,
    bool PrimaryPathRequiresWine,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    long DurationMs,
    string? ProjectPath,
    string AssemblyPath,
    string OutputDirectory,
    WineDependency Wine,
    IReadOnlyDictionary<string, string> Artifacts,
    IReadOnlyList<string> Diagnostics);

public sealed class MacApplicationHost
{
    private readonly ISnapshotRenderer snapshotRenderer;

    public MacApplicationHost()
        : this(new SnapshotRenderer())
    {
    }

    public MacApplicationHost(ISnapshotRenderer snapshotRenderer)
    {
        this.snapshotRenderer = snapshotRenderer;
    }

    public async Task<MacRunResult> RunAsync(MacRunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var assemblyPath = Path.GetFullPath(options.AssemblyPath);
        var outputDirectory = Path.GetFullPath(options.OutputDirectory);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("App assembly was not found.", assemblyPath);
        }

        ResourceOperations.ClearFailures();
        ResourceOperations.SetTheme(options.VisualSettings?.Theme);
        UnsupportedApiRegistry.Clear();

        var assembly = Assembly.LoadFrom(assemblyPath);
        var app = CreateApplication(assembly);
        app.Launch();

        if (app.MainWindow is null)
        {
            throw new InvalidOperationException("The app launched without assigning Application.MainWindow.");
        }

        app.MainWindow.Activate();
        BindingOperations.RefreshTree(app.MainWindow);
        InteractionReport? interactionReport = null;
        if (!string.IsNullOrWhiteSpace(options.ScriptPath))
        {
            var scriptRunner = new InteractionScriptRunner(new TypeResolver(assembly.GetTypes()));
            interactionReport = await scriptRunner.RunFileAsync(app.MainWindow, options.ScriptPath, cancellationToken);
        }

        BindingOperations.RefreshTree(app.MainWindow);
        var scenarioInteractionReport = RunScenarioInteractions(app.MainWindow, assembly, options.VisualSettings);
        interactionReport = MergeInteractions(interactionReport, scenarioInteractionReport);

        BindingOperations.RefreshTree(app.MainWindow);
        var tree = UiTreeBuilder.Build(app.MainWindow);
        IReadOnlyList<UnsupportedVisualFeature> unsupportedVisualFeatures = Array.Empty<UnsupportedVisualFeature>();
        if (options.VisualSettings is not null)
        {
            tree = VisualLayoutEngine.Arrange(tree, options.VisualSettings, out unsupportedVisualFeatures);
        }

        var accessibility = AccessibilityTreeBuilder.Build(tree);
        var snapshot = await snapshotRenderer.RenderAsync(
            tree,
            Path.Combine(outputDirectory, "screenshots"),
            options.VisualSettings is null
                ? null
                : new SnapshotRenderOptions(
                    Renderer: options.VisualSettings.Renderer,
                    ScenarioName: options.VisualSettings.ScenarioName,
                    Viewport: options.VisualSettings.Viewport,
                    Scale: options.VisualSettings.Scale,
                    Theme: options.VisualSettings.Theme,
                    StrictVisual: options.VisualSettings.StrictVisual,
                    PreferredFileName: options.VisualSettings.Renderer == "skia-v2" ? "mac-runtime.png" : null),
            cancellationToken);
        stopwatch.Stop();

        var endedAt = DateTimeOffset.UtcNow;
        var runJsonPath = Path.Combine(outputDirectory, "run.json");
        var treeJsonPath = Path.Combine(outputDirectory, "tree.json");
        var accessibilityJsonPath = Path.Combine(outputDirectory, "accessibility.json");
        var bindingFailuresJsonPath = Path.Combine(outputDirectory, "binding-failures.json");
        var resourceFailuresJsonPath = Path.Combine(outputDirectory, "resource-failures.json");
        var unsupportedApisJsonPath = Path.Combine(outputDirectory, "unsupported-apis.json");
        var diagnosticsSarifPath = Path.Combine(outputDirectory, "diagnostics.sarif");
        var interactionJsonPath = interactionReport is null ? null : Path.Combine(outputDirectory, "interactions.json");
        var snapshotJsonPath = Path.Combine(outputDirectory, "snapshot.json");
        var bindingFailures = BindingOperations.CurrentFailures;
        var resourceFailures = ResourceOperations.CurrentFailures;
        var unsupportedApis = UnsupportedApiRegistry.Current;
        var diagnostics = BuildDiagnostics(bindingFailures, resourceFailures, unsupportedApis);
        var status = BuildStatus(options.VisualSettings, bindingFailures, resourceFailures, unsupportedApis, interactionReport);
        var artifacts = new Dictionary<string, string>
        {
            ["run"] = runJsonPath,
            ["tree"] = treeJsonPath,
            ["accessibility"] = accessibilityJsonPath,
            ["bindingFailures"] = bindingFailuresJsonPath,
            ["resourceFailures"] = resourceFailuresJsonPath,
            ["unsupportedApis"] = unsupportedApisJsonPath,
            ["diagnostics"] = diagnosticsSarifPath,
            ["snapshot"] = snapshotJsonPath,
            ["screenshot"] = snapshot.FilePath
        };
        if (!string.IsNullOrWhiteSpace(options.ProjectIngestionJsonPath))
        {
            artifacts["projectIngestion"] = options.ProjectIngestionJsonPath;
        }

        if (interactionJsonPath is not null)
        {
            artifacts["interactions"] = interactionJsonPath;
        }

        var report = new RunReport(
            SchemaVersion: ArtifactSchemas.RunReport,
            Status: status,
            Host: "managed-macos-dotnet",
            PrimaryPathRequiresWine: false,
            StartedAt: startedAt,
            EndedAt: endedAt,
            DurationMs: stopwatch.ElapsedMilliseconds,
            ProjectPath: options.ProjectPath,
            AssemblyPath: assemblyPath,
            OutputDirectory: outputDirectory,
            Wine: MacDoctor.Check().Wine,
            Artifacts: artifacts,
            Diagnostics: diagnostics);

        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(runJsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(treeJsonPath, JsonSerializer.Serialize(tree, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(accessibilityJsonPath, JsonSerializer.Serialize(accessibility, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(
            bindingFailuresJsonPath,
            JsonSerializer.Serialize(new BindingFailureDocument(ArtifactSchemas.BindingFailures, bindingFailures), JsonDefaults.Options),
            cancellationToken);
        await File.WriteAllTextAsync(
            resourceFailuresJsonPath,
            JsonSerializer.Serialize(new ResourceFailureDocument(ArtifactSchemas.ResourceFailures, resourceFailures), JsonDefaults.Options),
            cancellationToken);
        await File.WriteAllTextAsync(
            unsupportedApisJsonPath,
            JsonSerializer.Serialize(new UnsupportedApiDocument(ArtifactSchemas.UnsupportedApis, unsupportedApis), JsonDefaults.Options),
            cancellationToken);
        await File.WriteAllTextAsync(diagnosticsSarifPath, JsonSerializer.Serialize(BuildSarif(diagnostics), JsonDefaults.Options), cancellationToken);
        if (interactionReport is not null && interactionJsonPath is not null)
        {
            await File.WriteAllTextAsync(interactionJsonPath, JsonSerializer.Serialize(interactionReport, JsonDefaults.Options), cancellationToken);
        }

        await File.WriteAllTextAsync(snapshotJsonPath, JsonSerializer.Serialize(snapshot, JsonDefaults.Options), cancellationToken);

        return new MacRunResult(
            report,
            tree,
            accessibility,
            snapshot,
            runJsonPath,
            treeJsonPath,
            accessibilityJsonPath,
            bindingFailuresJsonPath,
            resourceFailuresJsonPath,
            unsupportedApisJsonPath,
            diagnosticsSarifPath,
            interactionJsonPath,
            snapshotJsonPath,
            unsupportedVisualFeatures);
    }

    private static InteractionReport? RunScenarioInteractions(
        Window window,
        Assembly assembly,
        VisualRunSettings? visualSettings)
    {
        if (visualSettings?.Scenario is null || visualSettings.Scenario.Interactions.Count == 0)
        {
            return null;
        }

        var scriptRunner = new InteractionScriptRunner(new TypeResolver(assembly.GetTypes()));
        return scriptRunner.Run(window, new InteractionScript(visualSettings.Scenario.Interactions));
    }

    private static InteractionReport? MergeInteractions(InteractionReport? first, InteractionReport? second)
    {
        if (first is null)
        {
            return second;
        }

        if (second is null)
        {
            return first;
        }

        var steps = first.Steps
            .Concat(second.Steps.Select((step, index) => step with { Index = first.Steps.Count + index }))
            .ToArray();
        return new InteractionReport(ArtifactSchemas.InteractionReport, steps);
    }

    private static string BuildStatus(
        VisualRunSettings? visualSettings,
        IReadOnlyList<BindingFailure> bindingFailures,
        IReadOnlyList<ResourceLookupFailure> resourceFailures,
        IReadOnlyList<UnsupportedApiEntry> unsupportedApis,
        InteractionReport? interactionReport)
    {
        if (visualSettings?.StrictVisual != true)
        {
            return "passed";
        }

        if (bindingFailures.Count > 0 ||
            resourceFailures.Count > 0 ||
            unsupportedApis.Count > 0 ||
            interactionReport?.Steps.Any(step => step.Status != "passed") == true)
        {
            return "failed";
        }

        return "passed";
    }

    private static IReadOnlyList<string> BuildDiagnostics(
        IReadOnlyList<BindingFailure> bindingFailures,
        IReadOnlyList<ResourceLookupFailure> resourceFailures,
        IReadOnlyList<UnsupportedApiEntry> unsupportedApis)
    {
        return bindingFailures
            .Select(failure => $"binding:{failure.ElementName ?? failure.ElementType}.{failure.PropertyName}:{failure.Message}")
            .Concat(resourceFailures.Select(failure => $"resource:{failure.Key}:{failure.Status}"))
            .Concat(unsupportedApis.Select(entry => $"unsupported-api:{entry.Api}:{entry.Status}"))
            .ToArray();
    }

    private static object BuildSarif(IReadOnlyList<string> diagnostics)
    {
        return new
        {
            Version = "2.1.0",
            Runs = new[]
            {
                new
                {
                    Tool = new
                    {
                        Driver = new
                        {
                            Name = "WinUI3.MacTestRuntime",
                            Rules = new[]
                            {
                                SarifRule(DiagnosticRuleIds.BindingFailure, "Binding failure"),
                                SarifRule(DiagnosticRuleIds.ResourceFailure, "Resource lookup failure"),
                                SarifRule(DiagnosticRuleIds.UnsupportedApi, "Unavailable compatibility API")
                            }
                        }
                    },
                    Results = diagnostics.Select(diagnostic => new
                    {
                        RuleId = RuleIdFor(diagnostic),
                        Level = "warning",
                        Message = new
                        {
                            Text = diagnostic
                        }
                    }).ToArray()
                }
            }
        };
    }

    private static object SarifRule(string id, string description)
    {
        return new
        {
            Id = id,
            Name = id,
            ShortDescription = new
            {
                Text = description
            }
        };
    }

    private static string RuleIdFor(string diagnostic)
    {
        if (diagnostic.StartsWith("binding:", StringComparison.Ordinal))
        {
            return DiagnosticRuleIds.BindingFailure;
        }

        if (diagnostic.StartsWith("resource:", StringComparison.Ordinal))
        {
            return DiagnosticRuleIds.ResourceFailure;
        }

        if (diagnostic.StartsWith("unsupported-api:", StringComparison.Ordinal))
        {
            return DiagnosticRuleIds.UnsupportedApi;
        }

        return DiagnosticRuleIds.UnsupportedApi;
    }

    private static Application CreateApplication(Assembly assembly)
    {
        var appType = assembly
            .GetTypes()
            .FirstOrDefault(type =>
                !type.IsAbstract &&
                typeof(Application).IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) is not null);

        if (appType is null)
        {
            throw new InvalidOperationException("No concrete Microsoft.UI.Xaml.Application type with a default constructor was found.");
        }

        return (Application)Activator.CreateInstance(appType)!;
    }
}
