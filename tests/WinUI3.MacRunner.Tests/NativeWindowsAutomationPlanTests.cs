using System.Text.Json;
using WinUI3.MacRunner.Automation;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class NativeWindowsAutomationPlanTests
{
    [TestMethod]
    public async Task NativeWindowsAutomationPlanDescribesProbeArtifactsAndBoundaries()
    {
        var scenarioPath = await WriteScenarioAsync("""
            {
              "name": "native-shell-light",
              "theme": "light",
              "viewport": { "width": 1280, "height": 800 },
              "entry": {
                "mode": "window",
                "xaml": "MainWindow.xaml",
                "route": "home",
                "session": "staff"
              },
              "automation": [
                { "type": "assertAccessibilityState", "target": "automationId=shell-nav-home", "key": "selected", "parameter": "true" },
                { "type": "waitForIdle" }
              ],
              "visual": {
                "capture": true,
                "renderer": "skia-v2"
              }
            }
            """);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);
        var output = Path.Combine(Path.GetTempPath(), "winui3-native-plan-tests", Guid.NewGuid().ToString("N"));

        var plan = NativeWindowsAutomationPlan.Create(
            scenario,
            scenarioPath,
            new NativeWindowsAutomationProbeOptions(
                OutputDirectory: output,
                WindowTitle: "Native Shell",
                AppCommand: new[] { "FixtureApp.exe", "--scenario", scenarioPath },
                AttachProcessId: null,
                CaptureToolPath: "tools/WindowsWindowCapture/WindowsWindowCapture.csproj",
                Timeout: TimeSpan.FromSeconds(20)));

        Assert.AreEqual("0.1", plan.SchemaVersion);
        Assert.AreEqual("native-shell-light", plan.ScenarioName);
        Assert.IsTrue(plan.IsNativeWindowsReference);
        Assert.AreEqual(Path.Combine(output, "native-automation.json"), plan.NativeAutomationPath);
        Assert.AreEqual(Path.Combine(output, "windows-reference.json"), plan.WindowsReferencePath);
        Assert.AreEqual(Path.Combine(output, "windows-reference.png"), plan.WindowsReferencePngPath);
        CollectionAssert.Contains(plan.SupportedActionTypes.ToArray(), "assertAccessibilityState");
        CollectionAssert.Contains(plan.SupportedActionTypes.ToArray(), "selectNavigation");
        CollectionAssert.Contains(plan.UnsupportedActionTypes.ToArray(), "navigateFrame");
        StringAssert.Contains(plan.Boundary, "native Windows UIA/FlaUI reference");
    }

    [TestMethod]
    public async Task NativeWindowsAutomationPlanMapsScenarioActionsToProbeCommands()
    {
        var scenarioPath = await WriteScenarioAsync("""
            {
              "name": "native-action-map",
              "automation": [
                { "type": "click", "target": "automationId=saveButton" },
                { "type": "selectNavigation", "target": "automationId=shell-nav-messages" },
                { "type": "typeText", "target": "automationId=searchBox", "parameter": "licence" },
                { "type": "assertProperty", "target": "ContentFrame", "key": "CurrentRoute", "parameter": "messages" },
                { "type": "waitForIdle" }
              ]
            }
            """);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);

        var plan = NativeWindowsAutomationPlan.Create(
            scenario,
            scenarioPath,
            NativeWindowsAutomationProbeOptions.ForLaunch(
                outputDirectory: Path.GetTempPath(),
                windowTitle: "Native Action Map",
                appCommand: new[] { "FixtureApp.exe" }));

        Assert.HasCount(5, plan.Actions);
        Assert.AreEqual(NativeWindowsAutomationCommandKind.Invoke, plan.Actions[0].CommandKind);
        Assert.AreEqual("saveButton", plan.Actions[0].Selector.Value);
        Assert.AreEqual(NativeWindowsAutomationSelectorKind.AutomationId, plan.Actions[0].Selector.Kind);
        Assert.AreEqual(NativeWindowsAutomationCommandKind.Select, plan.Actions[1].CommandKind);
        Assert.AreEqual(NativeWindowsAutomationCommandKind.SetValue, plan.Actions[2].CommandKind);
        Assert.AreEqual(NativeWindowsAutomationCommandKind.Unsupported, plan.Actions[3].CommandKind);
        Assert.AreEqual("assertProperty is macOS runtime artifact-only and is not a native UIA property assertion.", plan.Actions[3].UnsupportedReason);
        Assert.AreEqual(NativeWindowsAutomationCommandKind.WaitForIdle, plan.Actions[4].CommandKind);
    }

    [TestMethod]
    public void NativeWindowsAutomationArtifactsUseSeparateReferenceSchemas()
    {
        var action = new NativeWindowsAutomationActionPlan(
            Index: 0,
            Type: "navigateFrame",
            Target: "ContentFrame",
            Key: null,
            Parameter: null,
            Selector: NativeWindowsAutomationSelector.Bare("ContentFrame"),
            CommandKind: NativeWindowsAutomationCommandKind.Unsupported,
            UnsupportedReason: "navigateFrame is handled by the macOS source-level runtime.");
        var report = NativeWindowsAutomationReport.FromResults(
            scenarioName: "native-schema",
            scenarioPath: "/tmp/native-schema.json",
            outputDirectory: "/tmp/native-schema",
            results: new[]
            {
                NativeWindowsAutomationActionResult.Skipped(action, "navigateFrame is handled by the macOS source-level runtime.")
            },
            windowsReferencePath: "/tmp/native-schema/windows-reference.json",
            windowsReferencePngPath: null);
        var reference = WindowsReferenceProvenance.Skipped(
            scenarioName: "native-schema",
            scenarioPath: "/tmp/native-schema.json",
            reason: "Scenario did not request visual capture.");

        using var nativeJson = JsonDocument.Parse(JsonSerializer.Serialize(report, NativeWindowsAutomationJson.Options));
        using var referenceJson = JsonDocument.Parse(JsonSerializer.Serialize(reference, NativeWindowsAutomationJson.Options));

        Assert.AreEqual("0.1", nativeJson.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("native-windows-uia3-flaui", nativeJson.RootElement.GetProperty("referenceSource").GetString());
        Assert.AreEqual("native-schema", nativeJson.RootElement.GetProperty("scenarioName").GetString());
        Assert.AreEqual(1, nativeJson.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32());
        Assert.AreEqual("skipped", nativeJson.RootElement.GetProperty("actions")[0].GetProperty("status").GetString());
        Assert.AreEqual("/tmp/native-schema/windows-reference.json", nativeJson.RootElement.GetProperty("artifacts").GetProperty("windowsReferenceJson").GetString());

        Assert.AreEqual("0.1", referenceJson.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("native-winui", referenceJson.RootElement.GetProperty("referenceSource").GetString());
        Assert.AreEqual("skipped", referenceJson.RootElement.GetProperty("capture").GetProperty("status").GetString());
        Assert.AreEqual("WindowsWindowCapture", referenceJson.RootElement.GetProperty("capture").GetProperty("tool").GetString());
    }

    private static async Task<string> WriteScenarioAsync(string json)
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-native-plan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "scenario.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
