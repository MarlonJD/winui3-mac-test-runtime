using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace WinUI3.MacRunner.ProjectIngestion;

public static class GeneratedHostWriter
{
    private const string HostTargetFramework = "net10.0";

    public static async Task<GeneratedHostResult> WriteAsync(
        WinUIProjectModel project,
        GeneratedHostOptions options,
        CancellationToken cancellationToken = default)
    {
        var root = HostRoot(project, options);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        Directory.CreateDirectory(root);
        var linkedXaml = EnumerateLinkedXaml(project).ToArray();
        var projectPath = Path.Combine(root, "GeneratedWinUIAppHost.csproj");

        await File.WriteAllTextAsync(Path.Combine(root, "App.xaml"), WriteAppXaml(), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "App.xaml.cs"), WriteAppCode(), cancellationToken);
        await File.WriteAllTextAsync(projectPath, WriteProject(project, linkedXaml), cancellationToken);

        return new GeneratedHostResult(
            RootDirectory: root,
            ProjectPath: projectPath,
            LinkedXamlFiles: linkedXaml,
            LinkedContentAssets: project.ContentAssets);
    }

    private static string HostRoot(WinUIProjectModel project, GeneratedHostOptions options)
    {
        var root = options.RootDirectory ?? "/private/tmp/winui3-mac-test-runtime/generated-hosts";
        var projectName = Path.GetFileNameWithoutExtension(project.ProjectPath);
        var safeName = new string(projectName.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
        return Path.Combine(root, $"{safeName}-{StableHash(project.ProjectPath)}");
    }

    private static string StableHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(value)));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static IEnumerable<WinUIProjectFile> EnumerateLinkedXaml(WinUIProjectModel project)
    {
        if (project.ApplicationXaml is not null)
        {
            yield return project.ApplicationXaml;
        }

        foreach (var file in project.PageXamlFiles)
        {
            yield return file;
        }

        foreach (var file in project.ResourceDictionaryXamlFiles)
        {
            yield return file;
        }
    }

    private static string WriteProject(WinUIProjectModel project, IReadOnlyList<WinUIProjectFile> linkedXaml)
    {
        var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
        var document = new XDocument(
            new XElement("Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("OutputType", "Exe"),
                    new XElement("TargetFramework", HostTargetFramework),
                    new XElement("AssemblyName", "GeneratedWinUIAppHost"),
                    new XElement("RootNamespace", "WinUI3.MacRunner.GeneratedHost"),
                    new XElement("ImplicitUsings", "enable"),
                    new XElement("Nullable", "enable"),
                    new XElement("EnableDefaultCompileItems", "false")),
                new XElement("ItemGroup",
                    ProjectReference(repositoryRoot, "WinUI3.MacCompat"),
                    ProjectReference(repositoryRoot, "WinUI3.MacRuntime"),
                    ProjectReference(repositoryRoot, "WinUI3.MacXaml")),
                new XElement("ItemGroup",
                    new XElement("Compile", new XAttribute("Include", "App.xaml.cs"))),
                new XElement("ItemGroup",
                    linkedXaml.Select(file =>
                        new XElement("WinUI3MacXaml",
                            new XAttribute("Include", file.FullPath),
                            new XAttribute("Link", file.RelativePath)))),
                new XElement("ItemGroup",
                    project.ContentAssets.Select(file =>
                        new XElement("Content",
                            new XAttribute("Include", file.FullPath),
                            new XAttribute("Link", file.RelativePath))))));

        return document.ToString();
    }

    private static XElement ProjectReference(string repositoryRoot, string projectName)
    {
        return new XElement(
            "ProjectReference",
            new XAttribute("Include", Path.Combine(repositoryRoot, "src", projectName, $"{projectName}.csproj")));
    }

    private static string WriteAppXaml()
    {
        return """
            <Application
                x:Class="WinUI3.MacRunner.GeneratedHost.App"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />
            """;
    }

    private static string WriteAppCode()
    {
        return """
            namespace WinUI3.MacRunner.GeneratedHost;

            public sealed class App
            {
            }

            public static class Program
            {
                public static int Main()
                {
                    return 0;
                }
            }
            """;
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
