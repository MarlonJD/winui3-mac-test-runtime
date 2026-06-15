using System.Diagnostics;
using System.Xml.Linq;

namespace WinUI3.MacRuntime;

public sealed record ProjectBuildResult(
    string ProjectPath,
    string AssemblyPath,
    string Configuration,
    string TargetFramework,
    IReadOnlyList<string> BuildOutput,
    ProjectIngestionReport? ProjectIngestion,
    string? ProjectIngestionJsonPath);

public sealed class ProjectBuildService
{
    private readonly ProjectIngestionService ingestionService = new();

    public async Task<ProjectBuildResult> BuildAsync(
        string projectPath,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        return await BuildAsync(projectPath, artifactsDirectory: null, configuration, cancellationToken);
    }

    public async Task<ProjectBuildResult> BuildAsync(
        string projectPath,
        string? artifactsDirectory,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        return await BuildAsync(projectPath, artifactsDirectory, configuration, scenario: null, cancellationToken);
    }

    public async Task<ProjectBuildResult> BuildAsync(
        string projectPath,
        string? artifactsDirectory,
        string configuration,
        VisualScenario? scenario,
        CancellationToken cancellationToken = default)
    {
        var resolvedProject = Path.GetFullPath(projectPath);
        if (!File.Exists(resolvedProject))
        {
            throw new FileNotFoundException("Project file was not found.", resolvedProject);
        }

        var buildPlan = await ingestionService.PrepareAsync(resolvedProject, artifactsDirectory, configuration, scenario, cancellationToken);
        var output = await RunDotNetBuildAsync(buildPlan.BuildProjectPath, configuration, buildPlan.RequiresRestore, cancellationToken);
        var projectInfo = ProjectInfo.Read(buildPlan.BuildProjectPath);
        var assemblyPath = Path.Combine(
            Path.GetDirectoryName(buildPlan.BuildProjectPath)!,
            "bin",
            configuration,
            projectInfo.TargetFramework,
            projectInfo.AssemblyName + ".dll");

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("Build completed but the app assembly was not found.", assemblyPath);
        }

        return new ProjectBuildResult(
            ProjectPath: resolvedProject,
            AssemblyPath: assemblyPath,
            Configuration: configuration,
            TargetFramework: projectInfo.TargetFramework,
            BuildOutput: output,
            ProjectIngestion: buildPlan.Report,
            ProjectIngestionJsonPath: buildPlan.ReportPath);
    }

    private static async Task<IReadOnlyList<string>> RunDotNetBuildAsync(
        string projectPath,
        string configuration,
        bool restore,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        if (!restore)
        {
            startInfo.ArgumentList.Add("--no-restore");
        }

        startInfo.ArgumentList.Add("--disable-build-servers");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("/m:1");
        startInfo.ArgumentList.Add("/p:UseSharedCompilation=false");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet build.");

        var output = new List<string>();
        process.OutputDataReceived += (_, args) => AddLine(args.Data, output);
        process.ErrorDataReceived += (_, args) => AddLine(args.Data, output);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "dotnet build failed for the fixture project." + Environment.NewLine + string.Join(Environment.NewLine, output));
        }

        return output;
    }

    private static void AddLine(string? line, ICollection<string> output)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            output.Add(line);
        }
    }

    private sealed record ProjectInfo(string AssemblyName, string TargetFramework)
    {
        public static ProjectInfo Read(string projectPath)
        {
            var document = XDocument.Load(projectPath);
            var assemblyName = ReadProperty(document, "AssemblyName") ?? Path.GetFileNameWithoutExtension(projectPath);
            var targetFramework = ReadProperty(document, "TargetFramework") ?? "net10.0";
            return new ProjectInfo(assemblyName, targetFramework);
        }

        private static string? ReadProperty(XDocument document, string propertyName)
        {
            return document
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == propertyName)
                ?.Value
                .Trim();
        }
    }
}
