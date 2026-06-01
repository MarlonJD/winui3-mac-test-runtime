using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using WinUI3.MacCompatibility;
using WinUI3.MacCompat.Diagnostics;
using WinUI3.MacRenderer.Skia;
using WinUI3.MacRuntime;

namespace WinUI3.MacRuntime.Tests;

[TestClass]
[DoNotParallelize]
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
    public void BindingOperationsTracksPropertyChangedAndTwoWayUpdates()
    {
        var state = new MutableObservableState("Before");
        var textBox = new TextBox { Name = "TitleBox" };
        BindingOperations.SetBinding(textBox, nameof(TextBox.Text), new Binding(nameof(MutableObservableState.Title), BindingMode.TwoWay));
        var root = new StackPanel { DataContext = state };
        root.Children.Add(textBox);

        BindingOperations.RefreshTree(root);
        Assert.AreEqual("Before", textBox.Text);

        state.Title = "After notify";
        Assert.AreEqual("After notify", textBox.Text);

        textBox.Text = "Updated from target";
        BindingOperations.UpdateSource(textBox, nameof(TextBox.Text));
        Assert.AreEqual("Updated from target", state.Title);
    }

    [TestMethod]
    public void BindingOperationsRefreshesObservableItemsControlSources()
    {
        var state = new CollectionState();
        state.Tasks.Add("Review queue");
        var listView = new ListView { Name = "TaskList" };
        BindingOperations.SetBinding(listView, nameof(ItemsControl.Items), new Binding(nameof(CollectionState.Tasks)));
        var root = new StackPanel { DataContext = state };
        root.Children.Add(listView);

        BindingOperations.RefreshTree(root);
        Assert.HasCount(1, listView.Items);

        state.Tasks.Add("Publish summary");
        Assert.HasCount(2, listView.Items);
    }

    [TestMethod]
    public void ButtonsExecuteCommandsAndExportCommandState()
    {
        var command = new TestCommand();
        var button = new Button { Name = "SaveButton", Content = "Save", Command = command, CommandParameter = "save" };

        button.PerformClick();
        var tree = UiTreeBuilder.Build(new Window { Content = button });

        Assert.AreEqual("save", command.LastParameter);
        Assert.IsTrue((bool)tree.Root.Children[0].Properties["commandCanExecute"]!);
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
    public void InteractionScriptTypesSelectsAndAssertsProperties()
    {
        var state = new MutableObservableState("Initial");
        var searchBox = new TextBox { Name = "SearchBox" };
        BindingOperations.SetBinding(searchBox, nameof(TextBox.Text), new Binding(nameof(MutableObservableState.Title), BindingMode.TwoWay));
        var listView = new ListView { Name = "TaskList" };
        listView.Items.Add("Review queue");
        listView.Items.Add("Archive completed task");
        var root = new StackPanel { DataContext = state };
        root.Children.Add(searchBox);
        root.Children.Add(listView);
        var window = new Window { Content = root };
        BindingOperations.RefreshTree(window);

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[]
            {
                new InteractionAction("typeText", "SearchBox", null, null, null, "Closed tasks"),
                new InteractionAction("selectItem", "TaskList", null, null, null, "Archive completed task"),
                new InteractionAction("assertProperty", "SearchBox", "Text", null, null, "Closed tasks")
            }));

        Assert.IsTrue(report.Steps.All(step => step.Status == "passed"));
        Assert.AreEqual("Closed tasks", state.Title);
        Assert.AreEqual("Archive completed task", listView.SelectedItem);
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
    public void TreeBuilderExportsLevel2ControlProperties()
    {
        var comboBox = new ComboBox { Name = "StatusComboBox", PlaceholderText = "Status", SelectedIndex = 1 };
        comboBox.Items.Add("Open");
        comboBox.Items.Add("Closed");
        comboBox.SelectedIndex = 1;

        var root = new StackPanel
        {
            Children =
            {
                new CheckBox { Name = "EnabledCheckBox", Content = "Enabled", IsChecked = true },
                new RadioButton { Name = "PriorityRadioButton", Content = "High", GroupName = "Priority", IsChecked = true },
                comboBox,
                new ProgressBar { Name = "Progress", Minimum = 0, Maximum = 100, Value = 65 },
                new InfoBar { Name = "StatusInfo", Title = "Ready", Message = "Public fixture state", Severity = InfoBarSeverity.Success },
                new CommandBar
                {
                    Name = "CommandSurface",
                    PrimaryCommands =
                    {
                        new AppBarButton { Name = "SaveCommand", Label = "Save" }
                    }
                }
            }
        };

        var tree = UiTreeBuilder.Build(new Window { Content = root });
        var nodes = Flatten(tree.Root).ToArray();

        Assert.IsTrue(nodes.Any(node => node.Type.EndsWith(".CheckBox", StringComparison.Ordinal) && Equals(node.Properties["isChecked"], true)));
        Assert.IsTrue(nodes.Any(node => node.Type.EndsWith(".RadioButton", StringComparison.Ordinal) && Equals(node.Properties["groupName"], "Priority")));
        Assert.IsTrue(nodes.Any(node => node.Type.EndsWith(".ComboBox", StringComparison.Ordinal) && Equals(node.Properties["selectedItem"], "Closed")));
        Assert.IsTrue(nodes.Any(node => node.Type.EndsWith(".ProgressBar", StringComparison.Ordinal) && Equals(node.Properties["value"], 65d)));
        Assert.IsTrue(nodes.Any(node => node.Type.EndsWith(".InfoBar", StringComparison.Ordinal) && Equals(node.Properties["severity"], "Success")));
        Assert.IsTrue(nodes.Any(node => node.Type.EndsWith(".CommandBar", StringComparison.Ordinal) && Equals(node.Properties["primaryCommandCount"], 1)));
    }

    [TestMethod]
    public void StyleOperationsAppliesSupportedSetterProperties()
    {
        var button = new Button { Name = "StyledButton", Content = "Save" };
        var style = new Style { TargetType = "Button" };
        style.Setters.Add(new Setter(nameof(Button.Foreground), "#2562D9"));

        StyleOperations.Apply(button, style);

        var tree = UiTreeBuilder.Build(new Window { Content = button });

        Assert.AreEqual("#2562D9", tree.Root.Children[0].Properties["foreground"]);
    }

    [TestMethod]
    public void UnsupportedApiRegistryReportsUnsupportedFacadeUse()
    {
        UnsupportedApiRegistry.Clear();

        _ = new MicaBackdrop();

        Assert.HasCount(1, UnsupportedApiRegistry.Current);
        Assert.AreEqual("Microsoft.UI.Xaml.Media.MicaBackdrop", UnsupportedApiRegistry.Current[0].Api);
        Assert.AreEqual(CompatibilityStatuses.Planned, UnsupportedApiRegistry.Current[0].Status);
    }

    [TestMethod]
    public void UnsupportedApiRegistryReportsUnknownPublicApiUse()
    {
        UnsupportedApiRegistry.Clear();

        UnsupportedApiRegistry.Report("Microsoft.UI.Xaml.Controls.UnknownPublicControl", "compat-api", "test");

        Assert.HasCount(1, UnsupportedApiRegistry.Current);
        Assert.AreEqual(CompatibilityStatuses.Unknown, UnsupportedApiRegistry.Current[0].Status);
    }

    [TestMethod]
    public void CompatibilityCatalogClassifiesFullRoadmapSeed()
    {
        var catalog = CompatibilityCatalog.Current;

        Assert.AreEqual("0.1", catalog.Document.SchemaVersion);
        CollectionAssert.AreEqual(
            catalog.Entries.Select(entry => entry.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            catalog.Entries.Select(entry => entry.Id).ToArray());
        Assert.AreEqual(
            CompatibilityStatuses.Planned,
            catalog.FindByApi("Microsoft.UI.Xaml.Media.MicaBackdrop")?.Status);
        Assert.AreEqual(
            CompatibilityStatuses.Planned,
            catalog.FindByApi("Microsoft.UI.Composition.Compositor")?.Status);
        Assert.AreEqual(
            CompatibilityStatuses.WindowsOnly,
            catalog.FindByApi("Windows.System.Launcher")?.Status);
        Assert.AreEqual(
            CompatibilityStatuses.NotSupported,
            catalog.FindByApi("Microsoft.UI.Xaml.Controls.WebView2")?.Status);
        Assert.IsTrue(catalog.Entries.Any(entry => entry.Kind == "fluent-resource"));
        Assert.IsTrue(catalog.Entries.Any(entry => entry.Kind == "project-property"));
        Assert.IsTrue(catalog.Entries.Any(entry => entry.Kind == "visual-state"));
    }

    [TestMethod]
    public async Task ProjectBuildServiceBuildsWindowsWinUIProjectThroughShadowBuild()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-shadow-project-tests", Guid.NewGuid().ToString("N"));
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-shadow-output-tests", Guid.NewGuid().ToString("N"));
        await WritePublicWindowsWinUIProjectAsync(projectDirectory, """
            <Window
                x:Class="PublicFixture.MainWindow"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Title="Public Fixture">
              <StackPanel>
                <TextBlock x:Name="TitleText" Text="Public workbench" />
                <Button x:Name="PrimaryButton" Content="Review" />
              </StackPanel>
            </Window>
            """);

        var projectPath = Path.Combine(projectDirectory, "PublicFixture.csproj");
        var result = await new ProjectBuildService().BuildAsync(projectPath, outputDirectory, "Debug");

        Assert.IsTrue(File.Exists(result.AssemblyPath));
        Assert.IsNotNull(result.ProjectIngestion);
        Assert.IsNotNull(result.ProjectIngestionJsonPath);
        Assert.IsTrue(File.Exists(result.ProjectIngestionJsonPath));
        Assert.AreEqual("passed", result.ProjectIngestion.Status);
        Assert.IsTrue(result.ProjectIngestion.IsShadowBuild);
        Assert.AreEqual("net10.0-windows10.0.19041.0", result.ProjectIngestion.TargetFramework);
        Assert.AreEqual(projectPath, result.ProjectPath);
        Assert.AreNotEqual(projectPath, result.ProjectIngestion.ShadowProjectPath);
        Assert.IsTrue(result.ProjectIngestion.IncludedFiles.Any(file => file.Path == "MainWindow.xaml" && file.Kind == "xaml"));
        Assert.IsTrue(result.ProjectIngestion.ExcludedWindowsOnlyItems.Any(item => item.Include == "Microsoft.WindowsAppSDK"));
        Assert.IsTrue(result.ProjectIngestion.CatalogStatuses.Any(status => status.Id == "project-property:UseWinUI" && status.Status == CompatibilityStatuses.Supported));
    }

    [TestMethod]
    public async Task ProjectBuildServiceFailsShadowBuildOnCatalogedUnsupportedProjectFeatures()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-shadow-project-tests", Guid.NewGuid().ToString("N"));
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-shadow-output-tests", Guid.NewGuid().ToString("N"));
        await WritePublicWindowsWinUIProjectAsync(projectDirectory, """
            <Window
                x:Class="PublicFixture.MainWindow"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Title="Public Fixture">
              <TextBlock Text="Public workbench" />
            </Window>
            """, windowsAppSdkSelfContained: true);

        var exception = await AssertThrowsAsync<InvalidOperationException>(() =>
            new ProjectBuildService().BuildAsync(Path.Combine(projectDirectory, "PublicFixture.csproj"), outputDirectory, "Debug"));

        StringAssert.Contains(exception.Message, "project-ingestion.json");
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "project-ingestion.json")));
        Assert.AreEqual("failed", report.RootElement.GetProperty("status").GetString());
        var unsupported = report.RootElement.GetProperty("unsupportedFeatures").EnumerateArray().ToArray();
        Assert.IsTrue(unsupported.Any(feature =>
            feature.GetProperty("id").GetString() == "project-property:WindowsAppSDKSelfContained" &&
            feature.GetProperty("status").GetString() == CompatibilityStatuses.Planned));
    }

    [TestMethod]
    public async Task ProjectBuildServiceWritesCatalogedXamlDiagnosticsBeforeShadowBuild()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-shadow-project-tests", Guid.NewGuid().ToString("N"));
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-shadow-output-tests", Guid.NewGuid().ToString("N"));
        await WritePublicWindowsWinUIProjectAsync(projectDirectory, """
            <Window
                x:Class="PublicFixture.MainWindow"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Title="Public Fixture">
              <MicaBackdrop />
            </Window>
            """);

        var exception = await AssertThrowsAsync<InvalidOperationException>(() =>
            new ProjectBuildService().BuildAsync(Path.Combine(projectDirectory, "PublicFixture.csproj"), outputDirectory, "Debug"));

        StringAssert.Contains(exception.Message, "project-ingestion.json");
        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "project-ingestion.json")));
        Assert.AreEqual("failed", report.RootElement.GetProperty("status").GetString());
        var diagnostics = report.RootElement.GetProperty("xamlDiagnostics").EnumerateArray().ToArray();
        Assert.IsTrue(diagnostics.Any(diagnostic =>
            diagnostic.GetProperty("code").GetString() == "XAML1001" &&
            diagnostic.GetProperty("message").GetString()?.Contains("cataloged as planned", StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public async Task MacApplicationHostWritesVersionedDiagnosticArtifactsAndSarifRules()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-artifact-schema-tests", Guid.NewGuid().ToString("N"));
        var result = await new MacApplicationHost().RunAsync(new MacRunOptions(
            AssemblyPath: Assembly.GetExecutingAssembly().Location,
            ProjectPath: null,
            OutputDirectory: outputDirectory));

        Assert.AreEqual(ArtifactSchemas.RunReport, result.Run.SchemaVersion);
        AssertJsonDocument(result.BindingFailuresJsonPath, ArtifactSchemas.BindingFailures, "failures", 1);
        AssertJsonDocument(result.ResourceFailuresJsonPath, ArtifactSchemas.ResourceFailures, "failures", 1);
        AssertJsonDocument(result.UnsupportedApisJsonPath, ArtifactSchemas.UnsupportedApis, "apis", 1);

        using var sarif = JsonDocument.Parse(await File.ReadAllTextAsync(result.DiagnosticsSarifPath));
        var ruleIds = sarif.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results")
            .EnumerateArray()
            .Select(resultElement => resultElement.GetProperty("ruleId").GetString())
            .ToArray();

        Assert.IsTrue(ruleIds.Contains(DiagnosticRuleIds.BindingFailure));
        Assert.IsTrue(ruleIds.Contains(DiagnosticRuleIds.ResourceFailure));
        Assert.IsTrue(ruleIds.Contains(DiagnosticRuleIds.UnsupportedApi));
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
              "requirements": [
                {
                  "component": "Button",
                  "target": "PrimaryButton",
                  "expectedStatus": "supported",
                  "minimumVisualGrade": "usable",
                  "visualGrade": "usable",
                  "requiredProperties": [ "content", "click" ],
                  "knownGaps": [ "Exact native chrome is approximated." ]
                }
              ],
              "sourceFeatures": [
                {
                  "feature": "ThemeResource",
                  "kind": "resource",
                  "target": "PrimaryButton",
                  "expectedStatus": "partial"
                }
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
        Assert.HasCount(1, scenario.Requirements);
        Assert.AreEqual("Button", scenario.Requirements[0].Component);
        Assert.HasCount(1, scenario.SourceFeatures);
        Assert.AreEqual("ThemeResource", scenario.SourceFeatures[0].Feature);
        Assert.AreEqual(0.5, scenario.Thresholds.ChangedPixelPercentage);
    }

    [TestMethod]
    public void ComponentEvidenceBuilderReportsScenarioRequirements()
    {
        var scenario = new VisualScenario
        {
            FixtureName = "component-parity-lab",
            Name = "component-basic-input-light",
            Requirements = new[]
            {
                new VisualRequirement
                {
                    Component = "Button",
                    Target = "PrimaryButton",
                    ExpectedStatus = CompatibilityStatuses.Supported,
                    MinimumVisualGrade = "usable",
                    VisualGrade = "usable",
                    KnownGaps = new[] { "Native chrome is approximated." }
                },
                new VisualRequirement
                {
                    Component = "RepeatButton",
                    Target = "DiagnosticRepeatButton",
                    ExpectedStatus = CompatibilityStatuses.Planned,
                    MinimumVisualGrade = "not-rendered",
                    VisualGrade = "not-rendered"
                }
            },
            SourceFeatures = new[]
            {
                new SourceFeatureRequirement
                {
                    Feature = "ThemeResource",
                    Target = "ThemeText",
                    ExpectedStatus = CompatibilityStatuses.Partial
                }
            }
        };
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Button { Name = "PrimaryButton", Content = "Run" },
                    new TextBlock { Name = "DiagnosticRepeatButton", Text = "RepeatButton diagnostic" },
                    new TextBlock { Name = "ThemeText", Text = "Theme row" }
                }
            }
        });
        var interactions = new InteractionReport(
            ArtifactSchemas.InteractionReport,
            new[] { new InteractionStepResult(0, "click", "passed", "PrimaryButton", null) });

        var evidence = ComponentEvidenceBuilder.Build(scenario, tree, interactions, metrics: null);

        Assert.AreEqual(ArtifactSchemas.ComponentEvidence, evidence.SchemaVersion);
        Assert.AreEqual("passed", evidence.Status);
        Assert.HasCount(2, evidence.Components);
        Assert.AreEqual("present", evidence.Components[0].Presence);
        Assert.AreEqual("passed", evidence.Components[0].InteractionStatus);
        Assert.AreEqual("not-rendered", evidence.Components[1].VisualGrade);
        Assert.HasCount(1, evidence.SourceFeatures);
        Assert.AreEqual("present", evidence.SourceFeatures[0].Presence);
    }

    [TestMethod]
    public async Task ComponentInventoryCoversComponentLabScenarioRequirements()
    {
        var repositoryRoot = FindRepositoryRoot();
        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        var scenarioDirectory = Path.Combine(repositoryRoot, "fixtures", "ComponentParityLab.WinUI", "scenarios");
        using var inventory = JsonDocument.Parse(await File.ReadAllTextAsync(inventoryPath));
        var inventoryComponents = inventory.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("component").GetString())
            .ToHashSet(StringComparer.Ordinal);
        var knownStatuses = new[]
        {
            CompatibilityStatuses.Supported,
            CompatibilityStatuses.Partial,
            CompatibilityStatuses.Planned,
            CompatibilityStatuses.WindowsOnly,
            CompatibilityStatuses.NotSupported,
            CompatibilityStatuses.Unknown
        };

        foreach (var entry in inventory.RootElement.GetProperty("entries").EnumerateArray())
        {
            CollectionAssert.Contains(knownStatuses, entry.GetProperty("catalogStatus").GetString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.GetProperty("demoPage").GetString()));
        }

        foreach (var scenarioPath in Directory.EnumerateFiles(scenarioDirectory, "*.json"))
        {
            var scenario = await VisualScenario.LoadAsync(scenarioPath);
            foreach (var requirement in scenario.Requirements)
            {
                CollectionAssert.Contains(
                    inventoryComponents.ToArray(),
                    requirement.Component,
                    $"Inventory is missing scenario requirement '{requirement.Component}' from {Path.GetFileName(scenarioPath)}.");
            }
        }
    }

    [TestMethod]
    public async Task ComponentLabScenariosCoverDownstreamSourceAuditGaps()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scenarioDirectory = Path.Combine(repositoryRoot, "fixtures", "ComponentParityLab.WinUI", "scenarios");
        var requiredGaps = new[]
        {
            "SymbolIcon",
            "XamlControlsResources",
            "ResourceDictionary.ThemeDictionaries",
            "ThemeResource",
            "StaticResource",
            "Style",
            "Setter",
            "Color",
            "SolidColorBrush",
            "CornerRadius",
            "DataTemplate",
            "ListView.ItemTemplate",
            "ItemsControl.ItemTemplate",
            "CommandBar.Content",
            "AppBarButton.Icon",
            "AutoSuggestBox.QueryIcon",
            "NavigationView.MenuItems",
            "NavigationView.PaneFooter",
            "ToolTipService.SetToolTip",
            "Window.SystemBackdrop / MicaBackdrop"
        };
        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scenarioPath in Directory.EnumerateFiles(scenarioDirectory, "*.json"))
        {
            var scenario = await VisualScenario.LoadAsync(scenarioPath);
            foreach (var sourceFeature in scenario.SourceFeatures)
            {
                covered.Add(sourceFeature.Feature);
            }
        }

        foreach (var gap in requiredGaps)
        {
            CollectionAssert.Contains(
                covered.ToArray(),
                gap,
                $"Source-audit gap '{gap}' is not covered by component lab source features.");
        }
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
            ArtifactSchemas.UiTree,
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinUI3.MacTestRuntime.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
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

    private static async Task WritePublicWindowsWinUIProjectAsync(
        string projectDirectory,
        string mainWindowXaml,
        bool windowsAppSdkSelfContained = false)
    {
        Directory.CreateDirectory(projectDirectory);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "PublicFixture.csproj"), $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
                <UseWinUI>true</UseWinUI>
                <WindowsPackageType>None</WindowsPackageType>
                <WindowsAppSDKSelfContained>{{windowsAppSdkSelfContained.ToString().ToLowerInvariant()}}</WindowsAppSDKSelfContained>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <AssemblyName>PublicFixture</AssemblyName>
                <RootNamespace>PublicFixture</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
              </ItemGroup>
              <ItemGroup>
                <ApplicationDefinition Include="App.xaml" />
                <Page Include="MainWindow.xaml" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "App.xaml"), """
            <Application
                x:Class="PublicFixture.App"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
            </Application>
            """);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "App.xaml.cs"), """
            using Microsoft.UI.Xaml;

            namespace PublicFixture;

            public sealed partial class App : Application
            {
                protected override void OnLaunched(LaunchActivatedEventArgs args)
                {
                    InitializeComponent();
                    MainWindow = new MainWindow();
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "MainWindow.xaml"), mainWindowXaml);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "MainWindow.xaml.cs"), """
            using Microsoft.UI.Xaml;

            namespace PublicFixture;

            public sealed partial class MainWindow : Window
            {
                public MainWindow()
                {
                    InitializeComponent();
                }
            }
            """);
    }

    private static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).FullName}.");
        throw new InvalidOperationException("Expected exception assertion did not stop execution.");
    }

    private static IEnumerable<UiNode> Flatten(UiNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var nested in Flatten(child))
            {
                yield return nested;
            }
        }
    }

    private static void AssertJsonDocument(string path, string schemaVersion, string itemsProperty, int minimumItemCount)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        Assert.AreEqual(schemaVersion, document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.IsGreaterThanOrEqualTo(minimumItemCount, document.RootElement.GetProperty(itemsProperty).GetArrayLength());
    }

    private sealed record MutableState(string Title);

    private sealed class MutableObservableState : INotifyPropertyChanged
    {
        private string title;

        public MutableObservableState(string title)
        {
            this.title = title;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title
        {
            get => title;
            set
            {
                title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
    }

    private sealed class CollectionState
    {
        public ObservableCollection<string> Tasks { get; } = new();
    }

    private sealed class TestCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public object? LastParameter { get; private set; }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            LastParameter = parameter;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

public sealed class ArtifactSchemaTestApp : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var missingBindingText = new TextBlock { Name = "MissingBindingText" };
        BindingOperations.SetBinding(missingBindingText, nameof(TextBlock.Text), new Binding("MissingTitle"));

        var missingResourceText = new TextBlock
        {
            Name = "MissingResourceText",
            Text = ResourceOperations.ResolveString(new ResourceDictionary(), "MissingTitle", nameof(TextBlock.Text))
        };

        MainWindow = new Window
        {
            Title = "Artifact Schema Test",
            SystemBackdrop = new MicaBackdrop(),
            Content = new StackPanel
            {
                DataContext = new { Title = "Public artifact schema fixture" },
                Children =
                {
                    missingBindingText,
                    missingResourceText
                }
            }
        };
    }
}
