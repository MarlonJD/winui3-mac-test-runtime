using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using WinUI3.MacCompatibility;

namespace WinUI3.MacRuntime;

public static class ProjectIngestionStatuses
{
    public const string Passed = "passed";
    public const string Failed = "failed";
}

public sealed record ProjectIngestionBuildPlan(
    string BuildProjectPath,
    ProjectIngestionReport? Report,
    string? ReportPath,
    bool RequiresRestore);

public sealed record ProjectIngestionReport(
    string SchemaVersion,
    string Status,
    string SourceProjectPath,
    string? ShadowProjectPath,
    bool IsShadowBuild,
    string? TargetFramework,
    bool IsWindowsTargetedWinUI,
    IReadOnlyList<ProjectIngestionFile> IncludedFiles,
    IReadOnlyList<ProjectIngestionExcludedItem> ExcludedWindowsOnlyItems,
    IReadOnlyList<ProjectIngestionCatalogStatus> CatalogStatuses,
    IReadOnlyList<ProjectIngestionUnsupportedFeature> UnsupportedFeatures,
    IReadOnlyList<ProjectIngestionXamlDiagnostic> XamlDiagnostics);

public sealed record ProjectIngestionFile(
    string Path,
    string Kind);

public sealed record ProjectIngestionExcludedItem(
    string Kind,
    string Include,
    string Status,
    string Reason);

public sealed record ProjectIngestionCatalogStatus(
    string Id,
    string Kind,
    string Api,
    string Status,
    string Source);

public sealed record ProjectIngestionUnsupportedFeature(
    string Id,
    string Kind,
    string Api,
    string Status,
    string Source,
    string Reason);

public sealed record ProjectIngestionXamlDiagnostic(
    string Code,
    string Message,
    string Severity,
    string? FilePath,
    int? Line,
    int? Column);

public sealed class ProjectIngestionService
{
    private const string ShadowTargetFramework = "net10.0";

    public async Task<ProjectIngestionBuildPlan> PrepareAsync(
        string projectPath,
        string? outputDirectory,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        var model = ProjectModel.Read(projectPath);
        if (!model.IsWindowsTargetedWinUI)
        {
            return new ProjectIngestionBuildPlan(projectPath, null, null, RequiresRestore: false);
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException("Windows-targeted WinUI projects require an output directory for compat shadow build artifacts.");
        }

        var outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);

        var shadowDirectory = Path.Combine(outputRoot, "shadow-build", Path.GetFileNameWithoutExtension(projectPath));
        Directory.CreateDirectory(shadowDirectory);

        var reportPath = Path.Combine(outputRoot, "project-ingestion.json");
        var shadowProjectPath = Path.Combine(shadowDirectory, Path.GetFileName(projectPath));
        var report = BuildDiscoveryReport(model, configuration);
        var hasBlockingDiagnostics = report.Status == ProjectIngestionStatuses.Failed;
        if (!hasBlockingDiagnostics)
        {
            report = report with { ShadowProjectPath = shadowProjectPath };
        }

        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, JsonDefaults.Options), cancellationToken);
        if (hasBlockingDiagnostics)
        {
            throw new InvalidOperationException($"Compat shadow build discovery failed. See project-ingestion.json: {reportPath}");
        }

        await File.WriteAllTextAsync(
            shadowProjectPath,
            ShadowProjectWriter.Write(model, shadowDirectory, configuration),
            cancellationToken);

        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, JsonDefaults.Options), cancellationToken);

        return new ProjectIngestionBuildPlan(shadowProjectPath, report, reportPath, RequiresRestore: true);
    }

    /// <summary>
    /// Runs project ingestion discovery only: it classifies project metadata and XAML without
    /// generating or building a compat shadow project, and never throws on blocking diagnostics.
    /// Returns <c>null</c> when the project is not a Windows-targeted WinUI project.
    /// </summary>
    public ProjectIngestionReport? Inspect(string projectPath, string configuration)
    {
        var model = ProjectModel.Read(projectPath);
        return model.IsWindowsTargetedWinUI ? BuildDiscoveryReport(model, configuration) : null;
    }

    private static ProjectIngestionReport BuildDiscoveryReport(ProjectModel model, string configuration)
    {
        var includedFiles = model.SourceFiles
            .Select(path => new ProjectIngestionFile(model.RelativeToProject(path), "compile"))
            .Concat(model.XamlFiles.Select(path => new ProjectIngestionFile(model.RelativeToProject(path), "xaml")))
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ToArray();
        var excludedItems = BuildExcludedItems(model);
        var catalogStatuses = BuildCatalogStatuses(model);
        var unsupportedFeatures = BuildUnsupportedFeatures(model);
        var xamlDiagnostics = BuildXamlDiagnostics(model, configuration);
        var hasBlockingDiagnostics =
            unsupportedFeatures.Any(feature => !CompatibilityStatuses.IsAvailableOnMac(feature.Status)) ||
            xamlDiagnostics.Any(diagnostic => diagnostic.Severity == "Error");

        return new ProjectIngestionReport(
            SchemaVersion: ArtifactSchemas.ProjectIngestion,
            Status: hasBlockingDiagnostics ? ProjectIngestionStatuses.Failed : ProjectIngestionStatuses.Passed,
            SourceProjectPath: model.ProjectPath,
            ShadowProjectPath: null,
            IsShadowBuild: true,
            TargetFramework: model.TargetFramework,
            IsWindowsTargetedWinUI: true,
            IncludedFiles: includedFiles,
            ExcludedWindowsOnlyItems: excludedItems,
            CatalogStatuses: catalogStatuses,
            UnsupportedFeatures: unsupportedFeatures,
            XamlDiagnostics: xamlDiagnostics);
    }

    private static ProjectIngestionExcludedItem[] BuildExcludedItems(ProjectModel model)
    {
        var excluded = new List<ProjectIngestionExcludedItem>();
        foreach (var package in model.PackageReferences.Where(package =>
            string.Equals(package, "Microsoft.WindowsAppSDK", StringComparison.OrdinalIgnoreCase)))
        {
            excluded.Add(new ProjectIngestionExcludedItem(
                Kind: "PackageReference",
                Include: package,
                Status: CatalogStatus("project-item:Microsoft.WindowsAppSDK"),
                Reason: "Windows App SDK build targets are replaced by the macOS compatibility facade in the shadow project."));
        }

        return excluded
            .OrderBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Include, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProjectIngestionCatalogStatus[] BuildCatalogStatuses(ProjectModel model)
    {
        var statuses = new List<ProjectIngestionCatalogStatus>
        {
            CatalogStatusEntry("project-property:TargetFramework.windows", model.TargetFramework ?? "TargetFramework"),
            CatalogStatusEntry("project-property:UseWinUI", "UseWinUI")
        };

        if (model.XamlFiles.Any(path => Path.GetFileName(path).Equals("App.xaml", StringComparison.OrdinalIgnoreCase)))
        {
            statuses.Add(CatalogStatusEntry("project-item:ApplicationDefinition", "App.xaml"));
        }

        if (model.XamlFiles.Any(path => !Path.GetFileName(path).Equals("App.xaml", StringComparison.OrdinalIgnoreCase)))
        {
            statuses.Add(CatalogStatusEntry("project-item:Page", "*.xaml"));
        }

        if (model.PackageReferences.Any(package => string.Equals(package, "Microsoft.WindowsAppSDK", StringComparison.OrdinalIgnoreCase)))
        {
            statuses.Add(CatalogStatusEntry("project-item:Microsoft.WindowsAppSDK", "Microsoft.WindowsAppSDK"));
        }

        if (model.Properties.ContainsKey("WindowsPackageType"))
        {
            statuses.Add(CatalogStatusEntry("project-property:WindowsPackageType", "WindowsPackageType"));
        }

        return statuses
            .OrderBy(status => status.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProjectIngestionUnsupportedFeature[] BuildUnsupportedFeatures(ProjectModel model)
    {
        var unsupported = new List<ProjectIngestionUnsupportedFeature>();
        if (model.Properties.TryGetValue("WindowsAppSDKSelfContained", out var selfContained) &&
            string.Equals(selfContained, "true", StringComparison.OrdinalIgnoreCase))
        {
            unsupported.Add(UnsupportedFeature(
                "project-property:WindowsAppSDKSelfContained",
                "WindowsAppSDKSelfContained",
                "Self-contained Windows App SDK deployment is a Windows packaging feature and is not part of the macOS shadow build."));
        }

        if (model.Properties.TryGetValue("WindowsPackageType", out var packageType) &&
            !string.IsNullOrWhiteSpace(packageType) &&
            !string.Equals(packageType, "None", StringComparison.OrdinalIgnoreCase))
        {
            unsupported.Add(UnsupportedFeature(
                "project-property:WindowsPackageType",
                "WindowsPackageType",
                $"WindowsPackageType '{packageType}' requires Windows packaging targets that the macOS shadow build does not execute."));
        }

        return unsupported
            .OrderBy(feature => feature.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProjectIngestionXamlDiagnostic[] BuildXamlDiagnostics(ProjectModel model, string configuration)
    {
        return XamlCompilerInvoker.CompileFiles(model.XamlFiles, configuration);
    }

    private static ProjectIngestionCatalogStatus CatalogStatusEntry(string id, string source)
    {
        var entry = CompatibilityCatalog.Current.FindById(id)
            ?? throw new InvalidOperationException($"Compatibility catalog entry '{id}' was not found.");
        return new ProjectIngestionCatalogStatus(entry.Id, entry.Kind, entry.Api, entry.Status, source);
    }

    private static ProjectIngestionUnsupportedFeature UnsupportedFeature(string id, string source, string reason)
    {
        var entry = CompatibilityCatalog.Current.FindById(id)
            ?? throw new InvalidOperationException($"Compatibility catalog entry '{id}' was not found.");
        return new ProjectIngestionUnsupportedFeature(entry.Id, entry.Kind, entry.Api, entry.Status, source, reason);
    }

    private static string CatalogStatus(string id)
    {
        return CompatibilityCatalog.Current.FindById(id)?.Status ?? CompatibilityStatuses.Unknown;
    }

    private sealed class ProjectModel
    {
        private ProjectModel(
            string projectPath,
            XDocument document,
            IReadOnlyDictionary<string, string> properties,
            IReadOnlyList<string> packageReferences,
            IReadOnlyList<string> sourceFiles,
            IReadOnlyList<string> xamlFiles)
        {
            ProjectPath = projectPath;
            ProjectDirectory = Path.GetDirectoryName(projectPath)!;
            Document = document;
            Properties = properties;
            PackageReferences = packageReferences;
            SourceFiles = sourceFiles;
            XamlFiles = xamlFiles;
        }

        public string ProjectPath { get; }

        public string ProjectDirectory { get; }

        public XDocument Document { get; }

        public IReadOnlyDictionary<string, string> Properties { get; }

        public IReadOnlyList<string> PackageReferences { get; }

        public IReadOnlyList<string> SourceFiles { get; }

        public IReadOnlyList<string> XamlFiles { get; }

        public string? AssemblyName => ReadProperty("AssemblyName") ?? Path.GetFileNameWithoutExtension(ProjectPath);

        public string? RootNamespace => ReadProperty("RootNamespace") ?? AssemblyName;

        public string? TargetFramework => ReadProperty("TargetFramework") ?? ReadProperty("TargetFrameworks")?.Split(';')[0].Trim();

        public bool IsWindowsTargetedWinUI =>
            TargetFramework?.Contains("-windows", StringComparison.OrdinalIgnoreCase) == true &&
            string.Equals(ReadProperty("UseWinUI"), "true", StringComparison.OrdinalIgnoreCase) &&
            (PackageReferences.Any(package => string.Equals(package, "Microsoft.WindowsAppSDK", StringComparison.OrdinalIgnoreCase)) || XamlFiles.Count > 0);

        public static ProjectModel Read(string projectPath)
        {
            var resolvedProject = Path.GetFullPath(projectPath);
            var projectDirectory = Path.GetDirectoryName(resolvedProject)!;
            var document = XDocument.Load(resolvedProject);
            var properties = document
                .Descendants()
                .Where(element => !element.HasElements && element.Parent?.Name.LocalName == "PropertyGroup")
                .GroupBy(element => element.Name.LocalName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value.Trim(), StringComparer.Ordinal);
            var packageReferences = document
                .Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var sourceFiles = EnumerateProjectFiles(projectDirectory, "*.cs")
                .Where(path => !IsGeneratedSource(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var xamlFiles = DiscoverXamlFiles(projectDirectory, document)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            return new ProjectModel(resolvedProject, document, properties, packageReferences, sourceFiles, xamlFiles);
        }

        public string RelativeToProject(string path)
        {
            return Path.GetRelativePath(ProjectDirectory, path).Replace('\\', '/');
        }

        private string? ReadProperty(string propertyName)
        {
            return Properties.TryGetValue(propertyName, out var value) ? value : null;
        }

        private static IEnumerable<string> DiscoverXamlFiles(string projectDirectory, XDocument document)
        {
            var explicitItems = document
                .Descendants()
                .Where(element => element.Name.LocalName is "ApplicationDefinition" or "Page" or "WinUI3MacXaml")
                .Select(element => element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)));

            return explicitItems
                .Concat(EnumerateProjectFiles(projectDirectory, "*.xaml"))
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> EnumerateProjectFiles(string projectDirectory, string pattern)
        {
            return Directory.EnumerateFiles(projectDirectory, pattern, SearchOption.AllDirectories)
                .Where(path =>
                {
                    var relative = Path.GetRelativePath(projectDirectory, path);
                    return !relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                        !relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                });
        }

        private static bool IsGeneratedSource(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static class ShadowProjectWriter
    {
        public static string Write(ProjectModel model, string shadowDirectory, string configuration)
        {
            var runnerPath = ResolveRunnerPath(configuration);
            var compatPath = ResolveRuntimeDependency("WinUI3.MacCompat.dll", configuration);
            var document = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("OutputType", "Library"),
                        new XElement("TargetFramework", ShadowTargetFramework),
                        new XElement("AssemblyName", model.AssemblyName),
                        new XElement("RootNamespace", model.RootNamespace),
                        new XElement("ImplicitUsings", model.Properties.TryGetValue("ImplicitUsings", out var implicitUsings) ? implicitUsings : "enable"),
                        new XElement("Nullable", model.Properties.TryGetValue("Nullable", out var nullable) ? nullable : "enable"),
                        new XElement("EnableDefaultCompileItems", "false")),
                    new XElement("ItemGroup",
                        new XElement("Reference",
                            new XAttribute("Include", "WinUI3.MacCompat"),
                            new XElement("HintPath", compatPath),
                            new XElement("Private", "true"))),
                    new XElement("ItemGroup",
                        model.SourceFiles.Select(path =>
                            new XElement("Compile",
                                new XAttribute("Include", path),
                                new XAttribute("Link", model.RelativeToProject(path))))),
                    new XElement("ItemGroup",
                        model.XamlFiles.Select(path =>
                            new XElement("WinUI3MacXaml",
                                new XAttribute("Include", path),
                                new XAttribute("Link", model.RelativeToProject(path))))),
                    new XElement("Target",
                        new XAttribute("Name", "WinUI3MacGenerateXaml"),
                        new XAttribute("BeforeTargets", "CoreCompile"),
                        new XAttribute("Condition", "'@(WinUI3MacXaml)' != ''"),
                        new XElement("MakeDir", new XAttribute("Directories", "$(IntermediateOutputPath)")),
                        new XElement("Exec", new XAttribute(
                            "Command",
                            $"dotnet \"{runnerPath}\" xaml compile --output \"$(IntermediateOutputPath)WinUI3MacXaml.g.cs\" @(WinUI3MacXaml->'\"%(FullPath)\"', ' ')")),
                        new XElement("ItemGroup",
                            new XElement("Compile", new XAttribute("Include", "$(IntermediateOutputPath)WinUI3MacXaml.g.cs"))))));

            return document.ToString();
        }

        private static string ResolveRunnerPath(string configuration)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var localRunner = Path.Combine(baseDirectory, "WinUI3.MacRunner.dll");
            if (File.Exists(localRunner))
            {
                return localRunner;
            }

            var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            var sourceRunner = Path.Combine(repositoryRoot, "src", "WinUI3.MacRunner", "bin", configuration, ShadowTargetFramework, "WinUI3.MacRunner.dll");
            if (File.Exists(sourceRunner))
            {
                return sourceRunner;
            }

            throw new FileNotFoundException("WinUI3.MacRunner.dll was not found. Build the runner before invoking compat shadow build.", sourceRunner);
        }

        private static string ResolveRuntimeDependency(string fileName, string configuration)
        {
            var dependencyPath = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(dependencyPath))
            {
                return dependencyPath;
            }

            var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            var sourceDependency = Path.Combine(repositoryRoot, "src", "WinUI3.MacRunner", "bin", configuration, ShadowTargetFramework, fileName);
            if (File.Exists(sourceDependency))
            {
                return sourceDependency;
            }

            throw new FileNotFoundException($"Runtime dependency '{fileName}' was not found.", sourceDependency);
        }

        private static string FindRepositoryRoot(string startDirectory)
        {
            var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
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

    private static class XamlCompilerInvoker
    {
        public static ProjectIngestionXamlDiagnostic[] CompileFiles(IEnumerable<string> xamlFiles, string configuration)
        {
            var assembly = Assembly.LoadFrom(ResolveMacXamlPath(configuration));
            var compilerType = assembly.GetType("WinUI3.MacXaml.MacXamlCompiler", throwOnError: true)!;
            var compiler = Activator.CreateInstance(compilerType)
                ?? throw new InvalidOperationException("Mac XAML compiler could not be created.");
            var compileFile = compilerType.GetMethod("CompileFile", new[] { typeof(string) })
                ?? throw new InvalidOperationException("Mac XAML compiler does not expose CompileFile(string).");
            var diagnosticsProperty = assembly.GetType("WinUI3.MacXaml.XamlCompilationResult", throwOnError: true)!
                .GetProperty("Diagnostics")
                ?? throw new InvalidOperationException("XAML compilation result does not expose Diagnostics.");

            var diagnostics = new List<ProjectIngestionXamlDiagnostic>();
            foreach (var xamlFile in xamlFiles)
            {
                var result = compileFile.Invoke(compiler, new object[] { xamlFile })
                    ?? throw new InvalidOperationException($"XAML compiler returned no result for '{xamlFile}'.");
                var resultDiagnostics = diagnosticsProperty.GetValue(result) as System.Collections.IEnumerable
                    ?? Array.Empty<object>();
                foreach (var diagnostic in resultDiagnostics)
                {
                    diagnostics.Add(ReadDiagnostic(diagnostic));
                }
            }

            return diagnostics
                .OrderBy(diagnostic => diagnostic.FilePath, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Line)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ToArray();
        }

        private static ProjectIngestionXamlDiagnostic ReadDiagnostic(object diagnostic)
        {
            var type = diagnostic.GetType();
            return new ProjectIngestionXamlDiagnostic(
                Read<string>(type, diagnostic, "Code"),
                Read<string>(type, diagnostic, "Message"),
                Read<string>(type, diagnostic, "Severity"),
                Read<string?>(type, diagnostic, "FilePath"),
                Read<int?>(type, diagnostic, "Line"),
                Read<int?>(type, diagnostic, "Column"));
        }

        private static T Read<T>(Type type, object instance, string propertyName)
        {
            var property = type.GetProperty(propertyName)
                ?? throw new InvalidOperationException($"XAML diagnostic does not expose {propertyName}.");
            return (T)property.GetValue(instance)!;
        }

        private static string ResolveMacXamlPath(string configuration)
        {
            var localXaml = Path.Combine(AppContext.BaseDirectory, "WinUI3.MacXaml.dll");
            if (File.Exists(localXaml))
            {
                return localXaml;
            }

            var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            var sourceXaml = Path.Combine(repositoryRoot, "src", "WinUI3.MacXaml", "bin", configuration, ShadowTargetFramework, "WinUI3.MacXaml.dll");
            if (File.Exists(sourceXaml))
            {
                return sourceXaml;
            }

            throw new FileNotFoundException("WinUI3.MacXaml.dll was not found. Build the XAML compiler before invoking compat shadow build.", sourceXaml);
        }

        private static string FindRepositoryRoot(string startDirectory)
        {
            var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
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
}
