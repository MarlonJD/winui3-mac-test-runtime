using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase5AutomationCoreTests
{
    [TestMethod]
    public void Phase5AutomationCoreExportsSemanticPatternsAndLayoutBounds()
    {
        var tree = new UiTreeDocument(
            ArtifactSchemas.UiTree,
            DateTimeOffset.UtcNow,
            new UiNode(
                "Microsoft.UI.Xaml.Window",
                "RootWindow",
                new Dictionary<string, object?> { ["visibility"] = "Visible" },
                new[]
                {
                    new UiNode(
                        "Microsoft.UI.Xaml.Controls.StackPanel",
                        "RootStack",
                        new Dictionary<string, object?>
                        {
                            ["spacing"] = 8d,
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Button",
                                "SubmitButton",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "submit",
                                    ["automationName"] = "Submit",
                                    ["content"] = "Submit",
                                    ["isEnabled"] = true,
                                    ["isFocusable"] = true,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBox",
                                "UserNameBox",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "user-name",
                                    ["text"] = "marlon",
                                    ["isEnabled"] = true,
                                    ["isFocusable"] = true,
                                    ["isFocused"] = true,
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
                                "Microsoft.UI.Xaml.Controls.RadioButton",
                                "AdminRole",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "role-admin",
                                    ["content"] = "Admin",
                                    ["isChecked"] = false,
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
                                    ["isSelected"] = true,
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
                                Array.Empty<UiNode>())
                        })
                }));
        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "phase5-automation-core", "skia-v2", new VisualViewport(360, 360), 1, "light", true, new VisualThresholds()),
            out var unsupported);

        var automation = AutomationCore.Build(arranged);

        var button = RequireAutomationId(automation.Root, "submit");
        var textBox = RequireAutomationId(automation.Root, "user-name");
        var checkBox = RequireAutomationId(automation.Root, "remember");
        var radio = RequireAutomationId(automation.Root, "role-admin");
        var navigationItem = RequireAutomationId(automation.Root, "shell-nav-messages");
        var scrollViewer = RequireAutomationId(automation.Root, "main-scroll");

        Assert.HasCount(0, unsupported);
        Assert.AreEqual(ArtifactSchemas.AutomationCore, automation.SchemaVersion);
        Assert.AreEqual(DateTimeOffset.UnixEpoch, automation.GeneratedAt);
        Assert.AreEqual(AutomationControlType.Button, button.ControlType);
        CollectionAssert.Contains(button.Patterns.ToArray(), AutomationPattern.Invoke);
        Assert.AreEqual("Submit", button.Name);
        Assert.IsGreaterThan(0d, button.Bounds.Width);
        Assert.IsFalse(button.IsOffscreen);
        Assert.AreEqual(AutomationControlType.Edit, textBox.ControlType);
        CollectionAssert.Contains(textBox.Patterns.ToArray(), AutomationPattern.Value);
        Assert.AreEqual("marlon", textBox.Value);
        Assert.IsTrue(textBox.HasKeyboardFocus);
        Assert.AreEqual(AutomationControlType.CheckBox, checkBox.ControlType);
        CollectionAssert.Contains(checkBox.Patterns.ToArray(), AutomationPattern.Toggle);
        Assert.AreEqual(AutomationToggleState.On, checkBox.ToggleState);
        Assert.AreEqual(AutomationControlType.RadioButton, radio.ControlType);
        CollectionAssert.Contains(radio.Patterns.ToArray(), AutomationPattern.SelectionItem);
        Assert.AreEqual(AutomationControlType.ListItem, navigationItem.ControlType);
        CollectionAssert.Contains(navigationItem.Patterns.ToArray(), AutomationPattern.SelectionItem);
        Assert.IsTrue(navigationItem.IsSelected);
        Assert.AreEqual(AutomationControlType.Pane, scrollViewer.ControlType);
        CollectionAssert.Contains(scrollViewer.Patterns.ToArray(), AutomationPattern.Scroll);
        Assert.IsTrue(scrollViewer.VerticallyScrollable);
        Assert.IsFalse(scrollViewer.HorizontallyScrollable);
    }

    private static AutomationNode RequireAutomationId(AutomationNode root, string automationId)
    {
        if (string.Equals(root.AutomationId, automationId, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = TryFindAutomationId(child, automationId);
            if (found is not null)
            {
                return found;
            }
        }

        throw new AssertFailedException($"Expected automation node '{automationId}'.");
    }

    private static AutomationNode? TryFindAutomationId(AutomationNode node, string automationId)
    {
        if (string.Equals(node.AutomationId, automationId, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = TryFindAutomationId(child, automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
