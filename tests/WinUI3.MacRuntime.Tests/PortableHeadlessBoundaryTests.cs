using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class PortableHeadlessBoundaryTests
{
    [TestMethod]
    public void PortableHeadlessBoundaryNamesCoreProjects()
    {
        CollectionAssert.Contains(
            PortableHeadlessBoundary.CoreProjectRelativePaths.ToArray(),
            Path.Combine("src", "WinUI3.MacCompat"));
        CollectionAssert.Contains(
            PortableHeadlessBoundary.CoreProjectRelativePaths.ToArray(),
            Path.Combine("src", "WinUI3.MacRuntime"));
        CollectionAssert.Contains(
            PortableHeadlessBoundary.CoreProjectRelativePaths.ToArray(),
            Path.Combine("src", "WinUI3.MacXaml"));
    }

    [TestMethod]
    public void PortableHeadlessCoreDoesNotReferencePlatformUiDependencies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var relativeProjectPath in PortableHeadlessBoundary.CoreProjectRelativePaths)
        {
            var projectDirectory = Path.Combine(repositoryRoot, relativeProjectPath);
            Assert.IsTrue(Directory.Exists(projectDirectory), $"Missing portable core project directory: {relativeProjectPath}");

            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
                         .Where(IsScannedSourceFile))
            {
                var relativeFile = Path.GetRelativePath(repositoryRoot, file);
                var text = File.ReadAllText(file);

                foreach (var forbiddenDependency in PortableHeadlessBoundary.ForbiddenPlatformDependencies)
                {
                    if (text.Contains(forbiddenDependency.Token, StringComparison.Ordinal))
                    {
                        violations.Add($"{relativeFile}: contains forbidden {forbiddenDependency.Name} token `{forbiddenDependency.Token}`");
                    }
                }
            }
        }

        Assert.IsFalse(
            violations.Any(),
            "Portable headless core must not reference platform UI dependencies:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void PortableHeadlessBoundaryDocumentsAllowedRuntimeFoundation()
    {
        CollectionAssert.Contains(
            PortableHeadlessBoundary.AllowedFoundationDependencies.ToArray(),
            "System.Text.Json");
        CollectionAssert.Contains(
            PortableHeadlessBoundary.AllowedFoundationDependencies.ToArray(),
            ".NET runtime");
    }

    private static bool IsScannedSourceFile(string path)
    {
        if (string.Equals(Path.GetFileName(path), "PortableHeadlessBoundary.cs", StringComparison.Ordinal))
        {
            return false;
        }

        var extension = Path.GetExtension(path);

        return string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".props", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".targets", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinUI3.MacTestRuntime.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Could not locate repository root from test output directory.");
        return "";
    }
}
