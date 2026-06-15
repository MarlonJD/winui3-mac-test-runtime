namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase9WindowsReferenceLaneTests
{
    [TestMethod]
    public void Phase9WindowsReferenceWorkflowDeclaresNativeWinUiLaneMetadata()
    {
        var workflowPath = Path.Combine(FindRepositoryRoot(), ".github", "workflows", "windows-native-screenshot.yml");
        Assert.IsTrue(File.Exists(workflowPath), "Windows native screenshot workflow must exist.");

        var workflow = File.ReadAllText(workflowPath);
        StringAssert.Contains(workflow, "windows-reference:");
        StringAssert.Contains(workflow, "runs-on: windows-latest");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_MODE: windows-reference");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_RUNTIME: native-winui");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_DRIVER: flaui-uia3");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_RENDERER: native-winui");
        StringAssert.Contains(workflow, "lane");
        StringAssert.Contains(workflow, "runtime");
        StringAssert.Contains(workflow, "driver");
        StringAssert.Contains(workflow, "renderer");
        StringAssert.Contains(workflow, "native-reference-targets.json");
        StringAssert.Contains(workflow, "windows-reference.png");
        StringAssert.Contains(workflow, "windows-reference.json");
        StringAssert.Contains(workflow, "name: windows-reference-screenshots");
        StringAssert.Contains(workflow, "path: artifacts/windows-reference-screenshots");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "windows-native-screenshot.yml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
