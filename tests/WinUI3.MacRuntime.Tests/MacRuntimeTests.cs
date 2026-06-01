using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using System.Security.Cryptography;
using WinUI3.MacCompat.Diagnostics;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
public sealed class MacRuntimeTests
{
    [TestMethod]
    public void DoctorMarksWineAsOptional()
    {
        var report = MacDoctor.Check();

        Assert.IsFalse(report.PrimaryPathRequiresWine);
        Assert.IsFalse(report.Wine.Required);
        Assert.AreEqual("optional", report.Wine.Status);
    }

    [TestMethod]
    public void TreeBuilderExportsFacadeBackedTree()
    {
        var root = new StackPanel { Name = "RootStack" };
        root.Children.Add(new TextBlock { Name = "GreetingText", Text = "Hello" });
        root.Children.Add(new Button { Name = "PrimaryButton", Content = "Continue" });
        var window = new Window { Title = "Tiny", Content = root };
        window.Activate();

        var tree = UiTreeBuilder.Build(window);

        Assert.AreEqual("Microsoft.UI.Xaml.Window", tree.Root.Type);
        Assert.AreEqual("Tiny", tree.Root.Properties["title"]);
        Assert.HasCount(1, tree.Root.Children);
        var stack = tree.Root.Children[0];
        Assert.AreEqual("RootStack", stack.Name);
        Assert.HasCount(2, stack.Children);
        Assert.AreEqual("GreetingText", stack.Children[0].Name);
        Assert.AreEqual("Hello", stack.Children[0].Properties["text"]);
        Assert.AreEqual("PrimaryButton", stack.Children[1].Name);
        Assert.AreEqual("Continue", stack.Children[1].Properties["content"]);
    }

    [TestMethod]
    public void BindingOperationsRefreshesTreeAndReportsFailures()
    {
        var textBlock = new TextBlock { Name = "TitleText" };
        BindingOperations.SetBinding(textBlock, nameof(TextBlock.Text), new Binding("Title"));

        var root = new StackPanel { DataContext = new { Title = "Bound title" } };
        root.Children.Add(textBlock);

        BindingOperations.RefreshTree(root);

        Assert.AreEqual("Bound title", textBlock.Text);
        Assert.HasCount(0, BindingOperations.CurrentFailures);

        BindingOperations.SetBinding(textBlock, nameof(TextBlock.Text), new Binding("Missing"));
        BindingOperations.RefreshTree(root);

        Assert.HasCount(1, BindingOperations.CurrentFailures);
        Assert.AreEqual("TitleText", BindingOperations.CurrentFailures[0].ElementName);
    }

    [TestMethod]
    public void InteractionScriptClicksButtonsAndRefreshesBindings()
    {
        var window = new Window();
        var button = new Button { Name = "RefreshButton" };
        var title = new TextBlock { Name = "TitleText" };
        var root = new StackPanel { DataContext = new MutableState("Before") };
        root.Children.Add(title);
        root.Children.Add(button);
        window.Content = root;
        BindingOperations.SetBinding(title, nameof(TextBlock.Text), new Binding("Title"));
        button.Click += (_, _) => root.DataContext = new MutableState("After");

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[] { new InteractionAction("click", "RefreshButton", null, null, null, null) }));

        Assert.HasCount(1, report.Steps);
        Assert.AreEqual("passed", report.Steps[0].Status);
        Assert.AreEqual("After", title.Text);
    }

    [TestMethod]
    public void AccessibilityTreeUsesAutomationNamesAndFocusState()
    {
        var button = new Button { Name = "PrimaryButton", Content = "Continue" };
        AutomationProperties.SetName(button, "Primary action");
        AutomationProperties.SetHelpText(button, "Runs the primary action");
        button.Focus(FocusState.Programmatic);

        var window = new Window { Content = button };
        var accessibility = AccessibilityTreeBuilder.Build(UiTreeBuilder.Build(window));

        var node = accessibility.Root.Children[0];
        Assert.AreEqual("button", node.Role);
        Assert.AreEqual("Primary action", node.Label);
        Assert.AreEqual("Runs the primary action", node.HelpText);
        Assert.IsTrue(node.IsFocused);
    }

    [TestMethod]
    public void UnsupportedApiRegistryReportsUnsupportedFacadeUse()
    {
        UnsupportedApiRegistry.Clear();

        _ = new MicaBackdrop();

        Assert.HasCount(1, UnsupportedApiRegistry.Current);
        Assert.AreEqual("Microsoft.UI.Xaml.Media.MicaBackdrop", UnsupportedApiRegistry.Current[0].Api);
        Assert.AreEqual("unsupported", UnsupportedApiRegistry.Current[0].Status);
    }

    [TestMethod]
    public async Task SkiaSnapshotRendererWritesPng()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-snapshot-tests", Guid.NewGuid().ToString("N"));
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new TextBlock
            {
                Name = "GreetingText",
                Text = "Hello"
            }
        });

        var snapshot = await new SkiaSnapshotRenderer().RenderAsync(tree, outputDirectory);

        Assert.AreEqual("skia-png", snapshot.Renderer);
        Assert.IsTrue(snapshot.IsNonBlank);
        Assert.IsTrue(File.Exists(snapshot.FilePath));
        var header = await File.ReadAllBytesAsync(snapshot.FilePath);
        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, header[..4]);
    }

    [TestMethod]
    public async Task VisualScenarioLoadsStrictScenarioOptions()
    {
        var scenarioPath = Path.Combine(Path.GetTempPath(), "winui3-mac-scenario-tests", Guid.NewGuid().ToString("N"), "scenario.json");
        Directory.CreateDirectory(Path.GetDirectoryName(scenarioPath)!);
        await File.WriteAllTextAsync(scenarioPath, """
            {
              "fixtureName": "sample",
              "name": "sample-light",
              "viewport": { "width": 800, "height": 600 },
              "scale": 1.5,
              "theme": "light",
              "strictVisual": true,
              "interactions": [
                { "type": "click", "target": "PrimaryButton" }
              ],
              "thresholds": {
                "changedPixelPercentage": 0.5,
                "maxChannelDelta": 16,
                "meanAbsoluteError": 2.0,
                "rootMeanSquaredError": 4.0
              }
            }
            """);

        var scenario = await VisualScenario.LoadAsync(scenarioPath);

        Assert.AreEqual("sample-light", scenario.Name);
        Assert.AreEqual(new VisualViewport(800, 600), scenario.Viewport);
        Assert.AreEqual(1.5, scenario.Scale);
        Assert.IsTrue(scenario.StrictVisual);
        Assert.HasCount(1, scenario.Interactions);
        Assert.AreEqual(0.5, scenario.Thresholds.ChangedPixelPercentage);
    }

    [TestMethod]
    public void VisualLayoutEngineExportsDeterministicLayout()
    {
        var window = new Window
        {
            Title = "Layout",
            Content = new StackPanel
            {
                Name = "RootStack",
                Spacing = 8,
                Children =
                {
                    new TextBlock { Name = "TitleText", Text = "Title" },
                    new Button { Name = "PrimaryButton", Content = "Continue" }
                }
            }
        };
        var tree = UiTreeBuilder.Build(window);
        var settings = new VisualRunSettings(null, "layout", "skia-v2", new VisualViewport(800, 600), 1, "light", true, new VisualThresholds());

        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);

        Assert.AreEqual(DateTimeOffset.UnixEpoch, arranged.GeneratedAt);
        Assert.HasCount(0, unsupported);
        var stack = arranged.Root.Children[0];
        Assert.IsNotNull(stack.Layout);
        Assert.AreEqual(48, stack.Layout!.Y);
        Assert.AreEqual(800, stack.Layout.Width);
        Assert.AreEqual(28, stack.Children[0].Layout!.Height);
        Assert.AreEqual(84, stack.Children[1].Layout!.Y);
    }

    [TestMethod]
    public void VisualLayoutEngineReportsUnsupportedVisualTypes()
    {
        var tree = new UiTreeDocument(
            "0.1",
            DateTimeOffset.UtcNow,
            new UiNode("PublicFixture.UnsupportedWidget", "Unsupported", new Dictionary<string, object?>(), Array.Empty<UiNode>()));
        var settings = new VisualRunSettings(null, "unsupported", "skia-v2", new VisualViewport(320, 240), 1, "light", true, new VisualThresholds());

        _ = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);

        Assert.HasCount(1, unsupported);
        Assert.AreEqual("PublicFixture.UnsupportedWidget", unsupported[0].Api);
        Assert.AreEqual("visual-renderer", unsupported[0].Kind);
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererWritesDeterministicRuntimePng()
    {
        var outputA = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "a");
        var outputB = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "b");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Deterministic",
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Hello" },
                    new Button { Content = "Continue" }
                }
            }
        });
        var settings = new VisualRunSettings(null, "deterministic", "skia-v2", new VisualViewport(640, 480), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "deterministic", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var first = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputA, options);
        var second = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputB, options);

        Assert.AreEqual("skia-v2-png", first.Renderer);
        Assert.AreEqual(640, first.Width);
        Assert.AreEqual(480, first.Height);
        CollectionAssert.AreEqual(await Sha256Async(first.FilePath), await Sha256Async(second.FilePath));
    }

    [TestMethod]
    public void PixelDiffReportsThresholdFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-pixel-diff-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var reference = Path.Combine(directory, "reference.png");
        var runtime = Path.Combine(directory, "runtime.png");
        var diff = Path.Combine(directory, "diff.png");
        WriteSolidPng(reference, new SKColor(0, 0, 0));
        WriteSolidPng(runtime, new SKColor(255, 255, 255));

        var result = PixelDiff.Compare(reference, runtime, diff, new VisualThresholds
        {
            ChangedPixelPercentage = 1,
            MaxChannelDelta = 8,
            MeanAbsoluteError = 1,
            RootMeanSquaredError = 1
        });

        Assert.AreEqual("failed", result.Status);
        Assert.AreEqual(100, result.ChangedPixelPercentage);
        Assert.IsTrue(File.Exists(diff));
    }

    private static async Task<byte[]> Sha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        return await SHA256.HashDataAsync(stream);
    }

    private static void WriteSolidPng(string path, SKColor color)
    {
        using var bitmap = new SKBitmap(4, 4);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private sealed record MutableState(string Title);
}
