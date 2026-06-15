using System.Text.Json;
using WinUI3.MacRunner.MacOS;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class MacOsWindowedHostTests
{
    [TestMethod]
    public void MacOsWindowedHostScaffoldWritesManualLocalAppKitHost()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-macos-windowed-host-tests", Guid.NewGuid().ToString("N"));
        var artifactRoot = Path.Combine(root, "portable-headless", "login-light");
        var outputRoot = Path.Combine(root, "macos-windowed");
        Directory.CreateDirectory(Path.Combine(artifactRoot, "screenshots"));
        File.WriteAllText(Path.Combine(artifactRoot, "tree.json"), """
            {
              "schemaVersion": "0.1",
              "root": {
                "name": "Root",
                "type": "Microsoft.UI.Xaml.Window",
                "properties": {
                  "bounds": { "x": 0, "y": 0, "width": 320, "height": 240 }
                },
                "children": [
                  {
                    "name": "SubmitButton",
                    "type": "Microsoft.UI.Xaml.Controls.Button",
                    "properties": {
                      "automationId": "SubmitButton",
                      "bounds": { "x": 20, "y": 80, "width": 120, "height": 32 }
                    },
                    "children": []
                  }
                ]
              }
            }
            """);
        File.WriteAllBytes(Path.Combine(artifactRoot, "screenshots", "mac-runtime.png"), new byte[] { 137, 80, 78, 71 });

        var scaffold = MacOsWindowedHostScaffold.Write(new MacOsWindowedHostOptions(
            ArtifactDirectory: artifactRoot,
            OutputDirectory: outputRoot,
            ScenarioName: "login-light",
            WindowTitle: "WinUI3 macOS Windowed - login-light"));

        Assert.AreEqual("macos-windowed", scaffold.Mode);
        Assert.AreEqual("internal", scaffold.Driver);
        Assert.AreEqual("local-manual", scaffold.CiPolicy);
        Assert.AreEqual(Path.Combine(outputRoot, "MacOsWindowedHost.swift"), scaffold.HostSourcePath);
        Assert.AreEqual(Path.Combine(outputRoot, "launch-macos-windowed.sh"), scaffold.LaunchScriptPath);
        Assert.AreEqual(Path.Combine(outputRoot, "macos-windowed-host.json"), scaffold.MetadataPath);
        Assert.IsTrue(File.Exists(scaffold.HostSourcePath));
        Assert.IsTrue(File.Exists(scaffold.LaunchScriptPath));
        Assert.IsTrue(File.Exists(scaffold.MetadataPath));

        var hostSource = File.ReadAllText(scaffold.HostSourcePath);
        StringAssert.Contains(hostSource, "import AppKit");
        StringAssert.Contains(hostSource, "NSApplication.shared");
        StringAssert.Contains(hostSource, "NSWindow");
        StringAssert.Contains(hostSource, "class RuntimePreviewView: NSView");
        StringAssert.Contains(hostSource, "mouseDown");
        StringAssert.Contains(hostSource, "keyDown");
        StringAssert.Contains(hostSource, "scrollWheel");
        StringAssert.Contains(hostSource, "convertToRuntimePoint");
        StringAssert.Contains(hostSource, "hitTestRuntimeNode");
        Assert.IsFalse(hostSource.Contains("NSAccessibility", StringComparison.Ordinal), "Phase 11 must not implement the Phase 12 AX adapter.");
        Assert.IsFalse(hostSource.Contains("AXUIElement", StringComparison.Ordinal), "Phase 11 must not implement AX.");
        Assert.IsFalse(hostSource.Contains("CAMetalLayer", StringComparison.Ordinal), "Raw Metal must not become the primary windowed renderer.");

        var launchScript = File.ReadAllText(scaffold.LaunchScriptPath);
        StringAssert.Contains(launchScript, "swift MacOsWindowedHost.swift");
        StringAssert.Contains(launchScript, "macos-windowed-events.jsonl");

        using var metadata = JsonDocument.Parse(File.ReadAllText(scaffold.MetadataPath));
        Assert.AreEqual("0.1", metadata.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("macos-windowed", metadata.RootElement.GetProperty("mode").GetString());
        Assert.AreEqual("internal", metadata.RootElement.GetProperty("driver").GetString());
        Assert.AreEqual("local-manual", metadata.RootElement.GetProperty("ciPolicy").GetString());
        Assert.AreEqual("not-default-pr-ci", metadata.RootElement.GetProperty("defaultPrCi").GetString());
        Assert.AreEqual("login-light", metadata.RootElement.GetProperty("scenarioName").GetString());
        Assert.AreEqual("screenshots/mac-runtime.png", metadata.RootElement.GetProperty("runtimeImagePath").GetString());
        Assert.AreEqual("tree.json", metadata.RootElement.GetProperty("treePath").GetString());
        Assert.AreEqual("macos-windowed-events.jsonl", metadata.RootElement.GetProperty("eventLogPath").GetString());
    }
}
