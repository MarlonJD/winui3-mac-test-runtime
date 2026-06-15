namespace WinUI3.MacRuntime;

public sealed class MacProjectRunner
{
    private readonly ProjectBuildService buildService;
    private readonly MacApplicationHost host;

    public MacProjectRunner()
        : this(new ProjectBuildService(), new MacApplicationHost())
    {
    }

    public MacProjectRunner(ISnapshotRenderer snapshotRenderer)
        : this(new ProjectBuildService(), new MacApplicationHost(snapshotRenderer))
    {
    }

    public MacProjectRunner(ProjectBuildService buildService, MacApplicationHost host)
    {
        this.buildService = buildService;
        this.host = host;
    }

    public async Task<MacRunResult> RunProjectAsync(
        string projectPath,
        string outputDirectory,
        string configuration = "Debug",
        string? scriptPath = null,
        VisualRunSettings? visualSettings = null,
        CancellationToken cancellationToken = default)
    {
        var build = await buildService.BuildAsync(
            projectPath,
            outputDirectory,
            configuration,
            visualSettings?.Scenario,
            cancellationToken);
        return await host.RunAsync(
            new MacRunOptions(
                AssemblyPath: build.AssemblyPath,
                ProjectPath: build.ProjectPath,
                OutputDirectory: outputDirectory,
                ScriptPath: scriptPath,
                VisualSettings: visualSettings,
                ProjectIngestionJsonPath: build.ProjectIngestionJsonPath),
            cancellationToken);
    }
}
