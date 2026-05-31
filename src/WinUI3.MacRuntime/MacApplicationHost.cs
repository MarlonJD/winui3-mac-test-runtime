using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinUI3.MacRuntime;

public sealed record MacRunOptions(
    string AssemblyPath,
    string? ProjectPath,
    string OutputDirectory,
    string? ScriptPath = null);

public sealed record MacRunResult(
    RunReport Run,
    UiTreeDocument Tree,
    AccessibilityDocument Accessibility,
    SnapshotResult Snapshot,
    string RunJsonPath,
    string TreeJsonPath,
    string AccessibilityJsonPath,
    string BindingFailuresJsonPath,
    string? InteractionJsonPath,
    string SnapshotJsonPath);

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
        var tree = UiTreeBuilder.Build(app.MainWindow);
        var accessibility = AccessibilityTreeBuilder.Build(tree);
        var snapshot = await new SnapshotRenderer().RenderAsync(
            tree,
            Path.Combine(outputDirectory, "screenshots"),
            cancellationToken);
        stopwatch.Stop();

        var endedAt = DateTimeOffset.UtcNow;
        var runJsonPath = Path.Combine(outputDirectory, "run.json");
        var treeJsonPath = Path.Combine(outputDirectory, "tree.json");
        var accessibilityJsonPath = Path.Combine(outputDirectory, "accessibility.json");
        var bindingFailuresJsonPath = Path.Combine(outputDirectory, "binding-failures.json");
        var interactionJsonPath = interactionReport is null ? null : Path.Combine(outputDirectory, "interactions.json");
        var snapshotJsonPath = Path.Combine(outputDirectory, "snapshot.json");
        var artifacts = new Dictionary<string, string>
        {
            ["run"] = runJsonPath,
            ["tree"] = treeJsonPath,
            ["accessibility"] = accessibilityJsonPath,
            ["bindingFailures"] = bindingFailuresJsonPath,
            ["snapshot"] = snapshotJsonPath,
            ["screenshot"] = snapshot.FilePath
        };
        if (interactionJsonPath is not null)
        {
            artifacts["interactions"] = interactionJsonPath;
        }

        var report = new RunReport(
            SchemaVersion: "0.1",
            Status: "passed",
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
            Diagnostics: Array.Empty<string>());

        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(runJsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(treeJsonPath, JsonSerializer.Serialize(tree, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(accessibilityJsonPath, JsonSerializer.Serialize(accessibility, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(bindingFailuresJsonPath, JsonSerializer.Serialize(BindingOperations.CurrentFailures, JsonDefaults.Options), cancellationToken);
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
            interactionJsonPath,
            snapshotJsonPath);
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
