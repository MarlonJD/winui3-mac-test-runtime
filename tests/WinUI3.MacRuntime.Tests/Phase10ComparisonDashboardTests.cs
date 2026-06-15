using System.Reflection;
using System.Text.Json;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase10ComparisonDashboardTests
{
    [TestMethod]
    public void Phase10ComparisonDashboardReportsScenarioAutomationAndVisualDifferences()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-phase10-dashboard-tests", Guid.NewGuid().ToString("N"));
        var portableRoot = Path.Combine(root, "portable-headless");
        var windowsRoot = Path.Combine(root, "windows-reference");
        var outputRoot = Path.Combine(root, "dashboard");
        WritePortableScenario(portableRoot, "login-light");
        WriteWindowsReferenceScenario(windowsRoot, "login-light");

        var dashboardType = Type.GetType("WinUI3.MacRuntime.PortableHeadlessComparisonDashboard, WinUI3.MacRuntime");
        Assert.IsNotNull(dashboardType, "Phase 10 must expose a portable-headless vs windows-reference comparison dashboard.");
        var writeMethod = dashboardType.GetMethod(
            "Write",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(string), typeof(double) },
            modifiers: null);
        Assert.IsNotNull(writeMethod, "Phase 10 dashboard must write JSON and Markdown review artifacts.");

        var report = writeMethod.Invoke(null, new object[] { portableRoot, windowsRoot, outputRoot, 2.0d });

        Assert.IsNotNull(report);
        var jsonPath = Path.Combine(outputRoot, "portable-headless-comparison-dashboard.json");
        var markdownPath = Path.Combine(outputRoot, "portable-headless-comparison-dashboard.md");
        Assert.IsTrue(File.Exists(jsonPath), "Dashboard JSON must be written for automation.");
        Assert.IsTrue(File.Exists(markdownPath), "Dashboard Markdown must be written for review.");

        using var json = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var rootElement = json.RootElement;
        Assert.AreEqual("failed", rootElement.GetProperty("status").GetString());
        Assert.AreEqual("portable-headless", rootElement.GetProperty("portableLane").GetString());
        Assert.AreEqual("windows-reference", rootElement.GetProperty("windowsReferenceLane").GetString());
        Assert.AreEqual(1, rootElement.GetProperty("summary").GetProperty("scenarioCount").GetInt32());
        Assert.AreEqual(1, rootElement.GetProperty("summary").GetProperty("automationMismatchCount").GetInt32());
        Assert.AreEqual(1, rootElement.GetProperty("summary").GetProperty("visualDifferenceCount").GetInt32());
        Assert.AreEqual(1, rootElement.GetProperty("summary").GetProperty("actionableDiagnosticCount").GetInt32());

        var scenario = rootElement.GetProperty("scenarios")[0];
        Assert.AreEqual("login-light", scenario.GetProperty("scenarioName").GetString());
        Assert.AreEqual("failed", scenario.GetProperty("status").GetString());
        Assert.AreEqual("passed", scenario.GetProperty("scenarioResult").GetProperty("portableStatus").GetString());
        Assert.AreEqual("native-winui", scenario.GetProperty("scenarioResult").GetProperty("windowsReferenceSource").GetString());

        var automation = scenario.GetProperty("automation");
        Assert.AreEqual("failed", automation.GetProperty("status").GetString());
        var mismatch = automation.GetProperty("mismatches")[0];
        Assert.AreEqual("SubmitButton", mismatch.GetProperty("automationId").GetString());
        Assert.AreEqual("bounds", mismatch.GetProperty("kind").GetString());
        StringAssert.Contains(mismatch.GetProperty("message").GetString(), "exceeded tolerance");

        var visual = scenario.GetProperty("visual");
        Assert.AreEqual("failed", visual.GetProperty("status").GetString());
        Assert.AreEqual(3.25d, visual.GetProperty("changedPixelPercentage").GetDouble());

        var markdown = File.ReadAllText(markdownPath);
        StringAssert.Contains(markdown, "# Portable Headless Comparison Dashboard");
        StringAssert.Contains(markdown, "login-light");
        StringAssert.Contains(markdown, "SubmitButton");
        StringAssert.Contains(markdown, "portable-headless");
        StringAssert.Contains(markdown, "windows-reference");

        var program = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "WinUI3.MacRunner", "Program.cs"));
        StringAssert.Contains(program, "\"portable-headless-dashboard\" => RunPortableHeadlessDashboard(args[1..])");
        StringAssert.Contains(program, "portable-headless-dashboard --portable <dir> --windows-reference <dir> --output <dir>");
    }

    private static void WritePortableScenario(string root, string scenarioName)
    {
        var scenarioDirectory = Path.Combine(root, scenarioName);
        Directory.CreateDirectory(Path.Combine(scenarioDirectory, "visual"));
        File.WriteAllText(Path.Combine(scenarioDirectory, "scenario-result.json"), """
            {
              "schemaVersion": "0.1",
              "name": "login-light",
              "status": "passed",
              "steps": []
            }
            """);
        File.WriteAllText(Path.Combine(scenarioDirectory, "automation.json"), """
            {
              "schemaVersion": "0.1",
              "root": {
                "automationId": "Root",
                "name": "Login",
                "controlType": "Window",
                "bounds": { "x": 0, "y": 0, "width": 320, "height": 240 },
                "patterns": [],
                "children": [
                  {
                    "automationId": "SubmitButton",
                    "name": "Sign in",
                    "controlType": "Button",
                    "bounds": { "x": 20, "y": 80, "width": 120, "height": 32 },
                    "patterns": [ "Invoke" ],
                    "children": []
                  }
                ]
              }
            }
            """);
        File.WriteAllText(Path.Combine(scenarioDirectory, "visual", "pixel-diff.json"), """
            {
              "schemaVersion": "0.1",
              "status": "failed",
              "changedPixelPercentage": 3.25,
              "meanAbsoluteError": 1.2,
              "rootMeanSquaredError": 2.4
            }
            """);
    }

    private static void WriteWindowsReferenceScenario(string root, string scenarioName)
    {
        var scenarioDirectory = Path.Combine(root, scenarioName);
        Directory.CreateDirectory(scenarioDirectory);
        File.WriteAllText(Path.Combine(scenarioDirectory, "windows-reference.json"), """
            {
              "schemaVersion": "0.1",
              "referenceSource": "native-winui",
              "lane": "windows-reference",
              "runtime": "native-winui",
              "driver": "flaui-uia3",
              "renderer": "native-winui",
              "scenarioName": "login-light",
              "capture": { "status": "passed" }
            }
            """);
        File.WriteAllText(Path.Combine(scenarioDirectory, "native-automation.json"), """
            {
              "schemaVersion": "0.1",
              "root": {
                "automationId": "Root",
                "name": "Login",
                "controlType": "Window",
                "bounds": { "x": 0, "y": 0, "width": 320, "height": 240 },
                "patterns": [],
                "children": [
                  {
                    "automationId": "SubmitButton",
                    "name": "Sign in",
                    "controlType": "Button",
                    "bounds": { "x": 27, "y": 80, "width": 120, "height": 32 },
                    "patterns": [ "Invoke" ],
                    "children": []
                  }
                ]
              }
            }
            """);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src", "WinUI3.MacRunner")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
