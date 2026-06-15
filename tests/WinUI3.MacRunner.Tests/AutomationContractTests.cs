using System.Text.Json;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class AutomationContractTests
{
    [TestMethod]
    public async Task AutomationContractParsesEntryAutomationAndVisualSections()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-runner-automation-contract-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var scenarioPath = Path.Combine(root, "meetingchallenge-shell-home-light.json");
        await File.WriteAllTextAsync(scenarioPath, """
            {
              "name": "meetingchallenge-shell-home-light",
              "theme": "light",
              "entry": {
                "mode": "window",
                "xaml": "MainWindow.xaml",
                "route": "home",
                "session": "staff"
              },
              "automation": [
                { "type": "assertAccessibilityState", "target": "automationId=shell-nav-home", "key": "selected", "parameter": "true" },
                { "type": "selectNavigation", "target": "automationId=shell-nav-messages" },
                { "type": "waitForIdle" },
                { "type": "assertProperty", "target": "ContentFrame", "key": "CurrentRoute", "parameter": "messages" }
              ],
              "visual": {
                "capture": true,
                "renderer": "skia-v2"
              }
            }
            """);

        var scenario = await VisualScenario.LoadAsync(scenarioPath);

        Assert.AreEqual("window", scenario.Entry?.Mode);
        Assert.AreEqual("MainWindow.xaml", scenario.Entry?.Xaml);
        Assert.AreEqual("home", scenario.Entry?.Route);
        Assert.AreEqual("staff", scenario.Entry?.Session);
        Assert.HasCount(4, scenario.Automation);
        Assert.AreEqual("assertAccessibilityState", scenario.Automation[0].Type);
        Assert.AreEqual("automationId=shell-nav-home", scenario.Automation[0].Target);
        Assert.AreEqual("selected", scenario.Automation[0].Key);
        Assert.AreEqual("skia-v2", scenario.Visual?.Renderer);
        Assert.IsTrue(scenario.Visual?.Capture);
    }
}
