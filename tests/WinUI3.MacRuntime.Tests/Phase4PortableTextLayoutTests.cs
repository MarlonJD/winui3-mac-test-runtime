using Microsoft.UI.Xaml;
using SkiaSharp;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase4PortableTextLayoutTests
{
    [TestMethod]
    public void Phase4NoWrapKeepsTextOnOneMeasuredLine()
    {
        var layout = WinUITextLayout.Measure(
            "A long login subtitle that should not wrap in NoWrap mode",
            TextWrapping.NoWrap,
            availableWidth: 72,
            fontSize: 10,
            lineHeight: 18);

        Assert.AreEqual(1, layout.LineCount);
        Assert.AreEqual(18d, layout.DesiredHeight);
        Assert.IsGreaterThan(72d, layout.DesiredWidth);
        Assert.AreEqual("A long login subtitle that should not wrap in NoWrap mode", layout.Lines[0].Text);
    }

    [TestMethod]
    public void Phase4WrapBreaksTextByAvailableWidth()
    {
        var layout = WinUITextLayout.Measure(
            "abcdefghijklmnop",
            TextWrapping.Wrap,
            availableWidth: 30,
            fontSize: 10,
            lineHeight: 18);

        Assert.IsGreaterThan(1, layout.LineCount);
        Assert.IsTrue(layout.Lines.All(line => line.Width <= 30d));
        Assert.AreEqual(layout.LineCount * 18d, layout.DesiredHeight);
    }

    [TestMethod]
    public void Phase4WrapWholeWordsPrefersWhitespaceAndFallsBackForLongTokens()
    {
        var wordWrapped = WinUITextLayout.Measure(
            "alpha beta gamma",
            TextWrapping.WrapWholeWords,
            availableWidth: 60,
            fontSize: 10,
            lineHeight: 18);
        var longToken = WinUITextLayout.Measure(
            "supercalifragilistic",
            TextWrapping.WrapWholeWords,
            availableWidth: 30,
            fontSize: 10,
            lineHeight: 18);

        CollectionAssert.AreEqual(new[] { "alpha", "beta", "gamma" }, wordWrapped.Lines.Select(line => line.Text).ToArray());
        Assert.IsGreaterThan(1, longToken.LineCount);
        Assert.IsTrue(longToken.Lines.All(line => line.Width <= 30d));
    }

    [TestMethod]
    public void Phase4TextBlockWrapHeightComesFromPortableLineCount()
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
                        "Microsoft.UI.Xaml.Controls.StackPanel",
                        "LoginStack",
                        new Dictionary<string, object?>
                        {
                            ["width"] = 120d,
                            ["spacing"] = 8d,
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBlock",
                                "LoginSubtitle",
                                new Dictionary<string, object?>
                                {
                                    ["text"] = "Use your organization account to continue into the secure admin portal.",
                                    ["textWrapping"] = TextWrapping.Wrap.ToString(),
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Button",
                                "SubmitButton",
                                new Dictionary<string, object?>
                                {
                                    ["content"] = "Continue",
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>())
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "phase4-text-wrap", "skia-v2", new VisualViewport(320, 240), 1, "light", true, new VisualThresholds()),
            out var unsupported);

        var subtitle = RequireNode(arranged.Root, "LoginSubtitle").Layout ?? throw new AssertFailedException("Expected subtitle layout.");
        var button = RequireNode(arranged.Root, "SubmitButton").Layout ?? throw new AssertFailedException("Expected button layout.");

        Assert.HasCount(0, unsupported);
        Assert.IsGreaterThan(24d, subtitle.Height);
        Assert.IsGreaterThan(subtitle.Y + subtitle.Height, button.Y);
    }

    [TestMethod]
    public async Task Phase4SkiaRendererDrawsWrappedTextBlockLineByLine()
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
                        "Microsoft.UI.Xaml.Controls.StackPanel",
                        "RootStack",
                        new Dictionary<string, object?>
                        {
                            ["width"] = 120d,
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBlock",
                                "WrappedText",
                                new Dictionary<string, object?>
                                {
                                    ["text"] = "Portable wrapped text should render on more than one visible line.",
                                    ["textWrapping"] = TextWrapping.Wrap.ToString(),
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>())
                        })
                }));
        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "phase4-render-lines", "skia-v2", new VisualViewport(180, 140), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var textLayout = RequireNode(arranged.Root, "WrappedText").Layout ?? throw new AssertFailedException("Expected text layout.");
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-phase4-text", Guid.NewGuid().ToString("N"));

        var result = await new SkiaV2SnapshotRenderer().RenderAsync(
            arranged,
            outputDirectory,
            new SnapshotRenderOptions("skia-v2", "phase4-render-lines", new VisualViewport(180, 140), 1, "light", true, "phase4.png"));

        using var bitmap = SKBitmap.Decode(result.FilePath);
        var lowerWrappedLineBand = new SKRect(
            (float)textLayout.X,
            (float)textLayout.Y + 34,
            (float)(textLayout.X + textLayout.Width),
            (float)(textLayout.Y + textLayout.Height - 2));

        Assert.HasCount(0, unsupported);
        Assert.IsGreaterThan(24d, textLayout.Height);
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, lowerWrappedLineBand));
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

    private static int CountDarkPixels(SKBitmap bitmap, SKRect rect)
    {
        var left = Math.Clamp((int)Math.Floor(rect.Left), 0, bitmap.Width - 1);
        var top = Math.Clamp((int)Math.Floor(rect.Top), 0, bitmap.Height - 1);
        var right = Math.Clamp((int)Math.Ceiling(rect.Right), left + 1, bitmap.Width);
        var bottom = Math.Clamp((int)Math.Ceiling(rect.Bottom), top + 1, bitmap.Height);
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.Alpha > 0 && color.Red < 120 && color.Green < 120 && color.Blue < 120)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
