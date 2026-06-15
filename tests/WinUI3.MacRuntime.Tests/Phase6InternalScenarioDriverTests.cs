using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase6InternalScenarioDriverTests
{
    [TestMethod]
    public void Phase6InternalScenarioDriverRunsActionsAndAssertionsThroughAutomationCore()
    {
        const string scenarioJson = """
            {
              "name": "login-basic",
              "steps": [
                { "action": "setValue", "automationId": "user-name", "value": "demo@example.com" },
                { "assert": "valueEquals", "automationId": "user-name", "value": "demo@example.com" },
                { "action": "toggle", "automationId": "remember" },
                { "action": "select", "automationId": "shell-nav-messages" },
                { "assert": "selected", "automationId": "shell-nav-messages" },
                { "action": "focus", "automationId": "user-name" },
                { "action": "scroll", "automationId": "main-scroll", "vertical": 120 },
                { "action": "invoke", "automationId": "submit" },
                { "assert": "exists", "automationId": "submit" },
                { "assert": "visible", "automationId": "submit" },
                { "assert": "textContains", "automationId": "submit", "value": "Submit" },
                { "action": "waitForIdle" },
                { "screenshot": "after-login.png" }
              ]
            }
            """;
        var automation = AutomationCore.Build(BuildArrangedTree());

        var scenario = PortableScenario.Parse(scenarioJson);
        var result = new InternalScenarioDriver().Run(automation, scenario);

        Assert.AreEqual(ArtifactSchemas.ScenarioResult, result.SchemaVersion);
        Assert.AreEqual("login-basic", result.Name);
        Assert.AreEqual("passed", result.Status, FailedStepSummary(result));
        Assert.HasCount(13, result.Steps);
        Assert.IsTrue(result.Steps.All(step => step.Status == "passed"), FailedStepSummary(result));
        Assert.AreEqual("demo@example.com", result.FinalAutomation.Root.FindByAutomationId("user-name")?.Value);
        Assert.AreEqual(AutomationToggleState.Off, result.FinalAutomation.Root.FindByAutomationId("remember")?.ToggleState);
        Assert.IsTrue(result.FinalAutomation.Root.FindByAutomationId("shell-nav-messages")?.IsSelected);
        Assert.IsTrue(result.FinalAutomation.Root.FindByAutomationId("user-name")?.HasKeyboardFocus);
        Assert.AreEqual(120d, result.FinalAutomation.Root.FindByAutomationId("main-scroll")?.VerticalScrollOffset);
        Assert.AreEqual("after-login.png", result.Steps.Last().Screenshot);
    }

    private static UiTreeDocument BuildArrangedTree()
    {
        var tree = new UiTreeDocument(
            ArtifactSchemas.UiTree,
            DateTimeOffset.UtcNow,
            new UiNode(
                "Microsoft.UI.Xaml.Window",
                null,
                new Dictionary<string, object?> { ["visibility"] = "Visible" },
                new[]
                {
                    new UiNode(
                        "Microsoft.UI.Xaml.Controls.StackPanel",
                        "LoginStack",
                        new Dictionary<string, object?>
                        {
                            ["spacing"] = 8d,
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBox",
                                "UserNameBox",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "user-name",
                                    ["text"] = "",
                                    ["isEnabled"] = true,
                                    ["isFocusable"] = true,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.CheckBox",
                                "RememberBox",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "remember",
                                    ["content"] = "Remember me",
                                    ["isChecked"] = true,
                                    ["isEnabled"] = true,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.NavigationViewItem",
                                "MessagesNavItem",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "shell-nav-messages",
                                    ["content"] = "Messages",
                                    ["isSelected"] = false,
                                    ["isEnabled"] = true,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.ScrollViewer",
                                "MainScroll",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "main-scroll",
                                    ["verticalScrollBarVisibility"] = "Auto",
                                    ["horizontalScrollBarVisibility"] = "Disabled",
                                    ["isEnabled"] = true,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Button",
                                "SubmitButton",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "submit",
                                    ["automationName"] = "Submit login",
                                    ["content"] = "Submit",
                                    ["isEnabled"] = true,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>())
                        })
                }));

        return VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "phase6-scenario", "skia-v2", new VisualViewport(360, 480), 1, "light", true, new VisualThresholds()),
            out _);
    }

    private static string FailedStepSummary(ScenarioRunResult result)
    {
        return string.Join(
            "; ",
            result.Steps
                .Where(step => step.Status != "passed")
                .Select(step => $"{step.Index}:{step.Type}:{step.AutomationId}:{step.Message}:expected={step.Expected}:actual={step.Actual}"));
    }
}
