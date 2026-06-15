using System.Xml.Linq;

namespace WinUI3.MacRunner.ProjectIngestion;

public static class WinUIProjectInspector
{
    public static WinUIProjectModel Inspect(string projectPath)
    {
        var resolvedProject = Path.GetFullPath(projectPath);
        if (!File.Exists(resolvedProject))
        {
            throw new FileNotFoundException("Project file was not found.", resolvedProject);
        }

        var rootDirectory = Path.GetDirectoryName(resolvedProject)
            ?? throw new InvalidOperationException($"Project path has no directory: {resolvedProject}");
        var document = XDocument.Load(resolvedProject);
        var properties = ReadProperties(document);
        var explicitXamlItems = ReadXamlItems(document, rootDirectory).ToArray();
        var allXamlFiles = explicitXamlItems
            .Concat(EnumerateFiles(rootDirectory, "*.xaml").Select(path => ProjectFile(rootDirectory, path, "ImplicitXaml")))
            .GroupBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => PreferExplicitItem(group))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();

        var applicationXaml = allXamlFiles.FirstOrDefault(IsApplicationXaml);
        var resourceDictionaries = allXamlFiles
            .Where(file => !IsApplicationXaml(file) && IsResourceDictionary(file.FullPath))
            .Select(file => file with { ItemType = "ResourceDictionary" })
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
        var resourcePaths = resourceDictionaries
            .Select(file => file.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pageXaml = allXamlFiles
            .Where(file => !IsApplicationXaml(file) && !resourcePaths.Contains(file.FullPath))
            .Select(file => file with { ItemType = "Page" })
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();

        return new WinUIProjectModel(
            ProjectPath: resolvedProject,
            RootDirectory: rootDirectory,
            TargetFramework: ReadTargetFramework(properties),
            UseWinUI: properties.TryGetValue("UseWinUI", out var useWinUI) && string.Equals(useWinUI, "true", StringComparison.OrdinalIgnoreCase),
            WindowsPackageType: properties.GetValueOrDefault("WindowsPackageType"),
            PackageReferences: ReadPackageReferences(document),
            ProjectReferences: ReadProjectReferences(document, rootDirectory),
            ApplicationXaml: applicationXaml is null ? null : applicationXaml with { ItemType = "ApplicationDefinition" },
            PageXamlFiles: pageXaml,
            ResourceDictionaryXamlFiles: resourceDictionaries,
            ContentAssets: ReadContentAssets(document, rootDirectory),
            SourceFiles: EnumerateFiles(rootDirectory, "*.cs")
                .Where(path => !IsGeneratedSource(path))
                .Select(path => ProjectFile(rootDirectory, path, "Compile"))
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .ToArray());
    }

    private static Dictionary<string, string> ReadProperties(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => !element.HasElements && element.Parent?.Name.LocalName == "PropertyGroup")
            .GroupBy(element => element.Name.LocalName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value.Trim(), StringComparer.Ordinal);
    }

    private static string? ReadTargetFramework(IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("TargetFramework", out var targetFramework))
        {
            return targetFramework;
        }

        return properties.TryGetValue("TargetFrameworks", out var targetFrameworks)
            ? targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
            : null;
    }

    private static IReadOnlyList<WinUIPackageReference> ReadPackageReferences(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => new WinUIPackageReference(
                element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value ?? string.Empty,
                element.Attribute("Version")?.Value))
            .Where(package => !string.IsNullOrWhiteSpace(package.Include))
            .GroupBy(package => package.Include, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(package => package.Include, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<WinUIProjectReference> ReadProjectReferences(XDocument document, string rootDirectory)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(include => new WinUIProjectReference(include!, ResolveProjectInclude(rootDirectory, include!)))
            .OrderBy(reference => reference.Include, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<WinUIProjectFile> ReadContentAssets(XDocument document, string rootDirectory)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "Content")
            .Select(element => element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => ProjectFile(rootDirectory, ResolveProjectInclude(rootDirectory, value!), "Content"))
            .Where(file => File.Exists(file.FullPath))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<WinUIProjectFile> ReadXamlItems(XDocument document, string rootDirectory)
    {
        foreach (var element in document.Descendants().Where(element =>
            element.Name.LocalName is "ApplicationDefinition" or "Page" or "WinUI3MacXaml"))
        {
            var include = element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            var fullPath = ResolveProjectInclude(rootDirectory, include);
            if (File.Exists(fullPath))
            {
                yield return ProjectFile(rootDirectory, fullPath, element.Name.LocalName);
            }
        }
    }

    private static WinUIProjectFile PreferExplicitItem(IEnumerable<WinUIProjectFile> files)
    {
        return files
            .OrderBy(file => file.ItemType == "ImplicitXaml" ? 1 : 0)
            .First();
    }

    private static WinUIProjectFile ProjectFile(string rootDirectory, string fullPath, string itemType)
    {
        var resolved = Path.GetFullPath(fullPath);
        return new WinUIProjectFile(
            Path.GetRelativePath(rootDirectory, resolved).Replace('\\', '/'),
            resolved,
            itemType);
    }

    private static string ResolveProjectInclude(string rootDirectory, string include)
    {
        return Path.GetFullPath(Path.Combine(rootDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));
    }

    private static bool IsApplicationXaml(WinUIProjectFile file)
    {
        return string.Equals(file.ItemType, "ApplicationDefinition", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(file.RelativePath, "App.xaml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResourceDictionary(string fullPath)
    {
        try
        {
            return XDocument.Load(fullPath).Root?.Name.LocalName == "ResourceDictionary";
        }
        catch (Exception) when (File.Exists(fullPath))
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateFiles(string rootDirectory, string pattern)
    {
        return Directory.EnumerateFiles(rootDirectory, pattern, SearchOption.AllDirectories)
            .Where(path =>
            {
                var relative = Path.GetRelativePath(rootDirectory, path);
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
