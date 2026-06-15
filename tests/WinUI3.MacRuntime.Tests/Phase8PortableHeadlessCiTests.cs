namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase8PortableHeadlessCiTests
{
    [TestMethod]
    public void Phase8DefaultCiHasPortableHeadlessUbuntuArtifactLane()
    {
        var workflowPath = Path.Combine(FindRepositoryRoot(), ".github", "workflows", "ci.yml");
        Assert.IsTrue(File.Exists(workflowPath), "Default CI workflow must exist.");

        var workflow = File.ReadAllText(workflowPath);
        StringAssert.Contains(workflow, "portable-headless:");
        StringAssert.Contains(workflow, "runs-on: ubuntu-latest");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_MODE: portable-headless");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_DRIVER: internal");
        StringAssert.Contains(workflow, "WINUI3_COMPAT_RENDERER: skia-offscreen");
        StringAssert.Contains(workflow, "dotnet test --no-build --filter \"PortableHeadless|Phase2|Phase3|Phase4|Phase5|Phase6|Phase7\"");
        StringAssert.Contains(workflow, "product-evidence --profile strict-scenario-sweep");
        StringAssert.Contains(workflow, "--output artifacts/portable-headless/strict-scenario-sweep");
        StringAssert.Contains(workflow, "name: portable-headless-artifacts");
        StringAssert.Contains(workflow, "path: artifacts/portable-headless");
        StringAssert.Contains(workflow, "if-no-files-found: error");
        Assert.IsFalse(workflow.Contains("macos-latest", StringComparison.OrdinalIgnoreCase), "Default CI must not add hosted macOS to the PR path.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "ci.yml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
