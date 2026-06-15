using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase3PortableLayoutTests
{
    [TestMethod]
    public void Phase3ContentPresenterArrangesContentAsPortableSingleSlot()
    {
        var tree = new UiTreeDocument(
            ArtifactSchemas.UiTree,
            DateTimeOffset.UtcNow,
            new UiNode(
                "Microsoft.UI.Xaml.Window",
                null,
                new Dictionary<string, object?>(),
                new[]
                {
                    new UiNode(
                        "Microsoft.UI.Xaml.Controls.Grid",
                        "LoginGrid",
                        new Dictionary<string, object?>
                        {
                            ["rowDefinitionHeights"] = new[] { "48", "*" },
                            ["padding"] = "24",
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBlock",
                                "TitleText",
                                new Dictionary<string, object?>
                                {
                                    ["text"] = "Sign in",
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.ContentPresenter",
                                "LoginPresenter",
                                new Dictionary<string, object?>
                                {
                                    ["gridRow"] = 1,
                                    ["visibility"] = "Visible"
                                },
                                new[]
                                {
                                    new UiNode(
                                        "Microsoft.UI.Xaml.Controls.StackPanel",
                                        "LoginFields",
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
                                                    ["placeholderText"] = "Username",
                                                    ["visibility"] = "Visible"
                                                },
                                                Array.Empty<UiNode>()),
                                            new UiNode(
                                                "Microsoft.UI.Xaml.Controls.Button",
                                                "SubmitButton",
                                                new Dictionary<string, object?>
                                                {
                                                    ["content"] = "Sign in",
                                                    ["visibility"] = "Visible"
                                                },
                                                Array.Empty<UiNode>())
                                        })
                                })
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "phase3-content-presenter", "skia-v2", new VisualViewport(480, 320), 1, "light", true, new VisualThresholds()),
            out var unsupported);

        var presenter = RequireNode(arranged.Root, "LoginPresenter");
        var fields = RequireNode(arranged.Root, "LoginFields");

        Assert.HasCount(0, unsupported);
        var presenterLayout = presenter.Layout ?? throw new AssertFailedException("Expected presenter layout.");
        var fieldsLayout = fields.Layout ?? throw new AssertFailedException("Expected content layout.");
        Assert.AreEqual(presenterLayout.X, fieldsLayout.X);
        Assert.AreEqual(presenterLayout.Y, fieldsLayout.Y);
        Assert.AreEqual(presenterLayout.Width, fieldsLayout.Width);
    }

    private static UiNode RequireNode(UiNode root, string name)
    {
        if (string.Equals(root.Name, name, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = TryFindNode(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        throw new AssertFailedException($"Expected to find node '{name}'.");
    }

    private static UiNode? TryFindNode(UiNode node, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = TryFindNode(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
