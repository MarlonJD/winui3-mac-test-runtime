using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;

namespace WinUI3.MacRuntime;

public sealed record MacRunOptions(
    string AssemblyPath,
    string? ProjectPath,
    string OutputDirectory);

public sealed record MacRunResult(
    RunReport Run,
    UiTreeDocument Tree,
    string RunJsonPath,
    string TreeJsonPath);

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

        var app = CreateApplication(assemblyPath);
        app.Launch();

        if (app.MainWindow is null)
        {
            throw new InvalidOperationException("The app launched without assigning Application.MainWindow.");
        }

        app.MainWindow.Activate();
        var tree = UiTreeBuilder.Build(app.MainWindow);
        stopwatch.Stop();

        var endedAt = DateTimeOffset.UtcNow;
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
            Diagnostics: Array.Empty<string>());

        Directory.CreateDirectory(outputDirectory);
        var runJsonPath = Path.Combine(outputDirectory, "run.json");
        var treeJsonPath = Path.Combine(outputDirectory, "tree.json");
        await File.WriteAllTextAsync(runJsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options), cancellationToken);
        await File.WriteAllTextAsync(treeJsonPath, JsonSerializer.Serialize(tree, JsonDefaults.Options), cancellationToken);

        return new MacRunResult(report, tree, runJsonPath, treeJsonPath);
    }

    private static Application CreateApplication(string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
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
