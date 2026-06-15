using System.Text.Json;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class Phase7SkiaOffscreenRendererTests
{
    [TestMethod]
    public async Task Phase7SkiaOffscreenRendererWritesPortablePngAndMetadata()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-phase7-" + Guid.NewGuid().ToString("N"));
        var settings = new VisualRunSettings(null, "login-headless", "skia-v2", new VisualViewport(360, 240), 1.5, "dark", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(BuildLoginTree(), settings, out _);

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(
            arranged,
            outputDirectory,
            new SnapshotRenderOptions(settings.Renderer, settings.ScenarioName, settings.Viewport, settings.Scale, settings.Theme, settings.StrictVisual, "mac-runtime.png"));

        var metadataPath = Path.Combine(outputDirectory, "mac-runtime.metadata.json");
        Assert.IsTrue(File.Exists(snapshot.FilePath), "Phase 7 must produce a PNG artifact.");
        CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, File.ReadAllBytes(snapshot.FilePath).Take(8).ToArray());
        Assert.IsTrue(File.Exists(metadataPath), "Phase 7 must produce sidecar screenshot metadata.");

        using var metadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
        var root = metadata.RootElement;
        Assert.AreEqual("portable-headless", root.GetProperty("mode").GetString());
        Assert.AreEqual("portable-headless", root.GetProperty("lane").GetString());
        Assert.AreEqual("internal-automation-core", root.GetProperty("driver").GetString());
        Assert.AreEqual("skia-offscreen", root.GetProperty("renderer").GetString());
        Assert.AreEqual("skia-v2", root.GetProperty("rendererVersion").GetString());
        Assert.AreEqual("login-headless", root.GetProperty("scenario").GetString());
        Assert.AreEqual("dark", root.GetProperty("theme").GetString());
        Assert.AreEqual(1.5d, root.GetProperty("scaleFactor").GetDouble());
        Assert.AreEqual(snapshot.Width, root.GetProperty("width").GetInt32());
        Assert.AreEqual(snapshot.Height, root.GetProperty("height").GetInt32());
        Assert.AreEqual("winui-portable-text-layout", root.GetProperty("textMeasurementMode").GetString());
        Assert.IsTrue(root.GetProperty("isNonBlank").GetBoolean());
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("platform").GetString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(root.GetProperty("fontProfile").GetString()));
        Assert.AreEqual(Path.GetFileName(snapshot.FilePath), root.GetProperty("png").GetString());
    }

    private static UiTreeDocument BuildLoginTree()
    {
        return new UiTreeDocument(
            ArtifactSchemas.UiTree,
            DateTimeOffset.UtcNow,
            new UiNode(
                "Microsoft.UI.Xaml.Window",
                "LoginWindow",
                new Dictionary<string, object?>
                {
                    ["title"] = "Portable Login",
                    ["visibility"] = "Visible"
                },
                new[]
                {
                    new UiNode(
                        "Microsoft.UI.Xaml.Controls.StackPanel",
                        "LoginStack",
                        new Dictionary<string, object?>
                        {
                            ["spacing"] = 10d,
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBlock",
                                "LoginTitle",
                                new Dictionary<string, object?>
                                {
                                    ["text"] = "Sign in",
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBox",
                                "UserNameBox",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "user-name",
                                    ["text"] = "demo@example.com",
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Button",
                                "SubmitButton",
                                new Dictionary<string, object?>
                                {
                                    ["automationId"] = "submit",
                                    ["content"] = "Submit",
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>())
                        })
                }));
    }
}
