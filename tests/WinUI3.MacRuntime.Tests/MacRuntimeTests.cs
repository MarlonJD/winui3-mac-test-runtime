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
using System.Text.RegularExpressions;
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
        AutomationProperties.SetAutomationId(searchBox, "settings-search-box");
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
                new InteractionAction("typeText", "automationId=settings-search-box", null, null, null, "Closed tasks"),
                new InteractionAction("selectItem", "TaskList", null, null, null, "Archive completed task"),
                new InteractionAction("assertProperty", "SearchBox", "Text", null, null, "Closed tasks")
            }));

        Assert.IsTrue(report.Steps.All(step => step.Status == "passed"));
        Assert.AreEqual("automationId", report.Steps[0].SelectorKind);
        Assert.AreEqual("TextBox", report.Steps[0].TargetType);
        Assert.AreEqual("settings-search-box", report.Steps[0].ObservedState?["automationId"]);
        Assert.AreEqual("Closed tasks", state.Title);
        Assert.AreEqual("Archive completed task", listView.SelectedItem);
    }

    [TestMethod]
    public void InteractionScriptFailureReportsSelectorAndObservedState()
    {
        var textBlock = new TextBlock { Name = "StatusText", Text = "Waiting" };
        AutomationProperties.SetAutomationId(textBlock, "status-output");
        var window = new Window { Content = textBlock };

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[]
            {
                new InteractionAction("assertProperty", "status-output", "Text", null, null, "Done")
            }));

        var step = report.Steps.Single();
        Assert.AreEqual("failed", step.Status);
        Assert.AreEqual("status-output", step.Selector);
        Assert.AreEqual("automationId", step.SelectorKind);
        Assert.AreEqual("TextBlock", step.TargetType);
        Assert.AreEqual("Done", step.Expected);
        Assert.AreEqual("Waiting", step.Actual);
        Assert.AreEqual("Waiting", step.ObservedState?["text"]);
    }

    [TestMethod]
    public void InteractionScriptOpensInvokesAndDismissesPopups()
    {
        var invoked = string.Empty;
        var menuItem = new MenuFlyoutItem { Text = "Approve" };
        menuItem.Click += (_, _) => invoked = "Approve";
        var menuFlyout = new MenuFlyout
        {
            Items =
            {
                menuItem,
                new MenuFlyoutItem { Text = "Defer" }
            }
        };
        var menuButton = new Button { Name = "MenuButton", Content = "Open", Flyout = menuFlyout };
        var dialog = new ContentDialog { Name = "DecisionDialog", Title = "Decision", PrimaryButtonText = "OK" };
        var root = new StackPanel
        {
            Children =
            {
                menuButton,
                dialog
            }
        };
        var window = new Window { Content = root };

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[]
            {
                new InteractionAction("openPopup", "MenuButton", null, null, null, null),
                new InteractionAction("invokeMenuItem", "MenuButton", null, null, null, "Approve"),
                new InteractionAction("openPopup", "DecisionDialog", null, null, null, null),
                new InteractionAction("dismissPopup", "DecisionDialog", null, null, null, null)
            }));

        Assert.IsTrue(report.Steps.All(step => step.Status == "passed"));
        Assert.AreEqual("True", report.Steps[0].Expected);
        Assert.AreEqual("True", report.Steps[0].Actual);
        Assert.AreEqual("Approve", report.Steps[1].Expected);
        Assert.AreEqual("Approve", report.Steps[1].Actual);
        Assert.AreEqual("False", report.Steps[3].Expected);
        Assert.AreEqual("False", report.Steps[3].Actual);
        Assert.IsTrue(menuFlyout.IsOpen);
        Assert.AreEqual("Approve", menuFlyout.InvokedItem);
        Assert.AreEqual("Approve", invoked);
        Assert.IsFalse(dialog.IsOpen);
        Assert.AreEqual("dismissed", dialog.Result);
    }

    [TestMethod]
    public void AccessibilityTreeUsesAutomationNamesAndFocusState()
    {
        var button = new Button { Name = "PrimaryButton", Content = "Continue" };
        AutomationProperties.SetAutomationId(button, "primary-action");
        AutomationProperties.SetName(button, "Primary action");
        AutomationProperties.SetHelpText(button, "Runs the primary action");
        button.Focus(FocusState.Programmatic);

        var window = new Window { Content = button };
        var accessibility = AccessibilityTreeBuilder.Build(UiTreeBuilder.Build(window));

        var node = accessibility.Root.Children[0];
        Assert.AreEqual("button", node.Role);
        Assert.AreEqual("primary-action", node.AutomationId);
        Assert.AreEqual("Primary action", node.Label);
        Assert.AreEqual("Runs the primary action", node.HelpText);
        Assert.IsTrue(node.IsFocused);
        Assert.IsTrue(node.IsFocusable.GetValueOrDefault());
    }

    [TestMethod]
    public void AccessibilityTreeExportsPopupExpandedState()
    {
        var button = new Button
        {
            Name = "MenuButton",
            Content = "Open",
            Flyout = new MenuFlyout { IsOpen = true }
        };

        var accessibility = AccessibilityTreeBuilder.Build(UiTreeBuilder.Build(new Window { Content = button }));

        var popup = accessibility.Root.Children[0].Children.Single(node => node.Role == "popup");
        Assert.IsTrue(popup.IsExpanded);
        Assert.IsTrue(popup.IsEnabled);
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
    public void ResourceOperationsResolvesThemeDictionaryBeforeFallback()
    {
        var resources = new ResourceDictionary
        {
            ["AccentBrush"] = "#2562D9"
        };
        resources.ThemeDictionaries["Dark"] = new ResourceDictionary
        {
            ["AccentBrush"] = "#7CA7FF"
        };

        ResourceOperations.SetTheme("dark");
        try
        {
            Assert.AreEqual("#7CA7FF", ResourceOperations.ResolveString(resources, "AccentBrush", "Foreground"));
        }
        finally
        {
            ResourceOperations.SetTheme("light");
        }
    }

    [TestMethod]
    public void SkiaV2ThemeProvidesLightDarkAndHighContrastTokens()
    {
        var light = SkiaV2Theme.For("light");
        var dark = SkiaV2Theme.For("dark");
        var highContrast = SkiaV2Theme.For("high-contrast");

        Assert.AreNotEqual(light.AppBackground, dark.AppBackground);
        Assert.AreNotEqual(light.TextPrimary, dark.TextPrimary);
        Assert.AreEqual(new SKColor(0xff, 0xff, 0xff), highContrast.TextPrimary);
        Assert.AreEqual(new SKColor(0xff, 0xff, 0xff), highContrast.Stroke);
        Assert.AreEqual(new SKColor(0x00, 0xff, 0xff), highContrast.Accent);
        Assert.AreEqual(0, highContrast.PopupShadowOffset);
        Assert.IsGreaterThan(0, light.ControlCornerRadius);
        Assert.IsGreaterThan(0, light.FocusStrokeWidth);
    }

    [TestMethod]
    public void FluentDrawingPrimitivesResolveControlStateColors()
    {
        var theme = SkiaV2Theme.For("light");

        var enabled = FluentDrawingPrimitives.ControlColors(theme, new FluentControlState());
        var disabled = FluentDrawingPrimitives.ControlColors(theme, new FluentControlState(IsEnabled: false));
        var checkedState = FluentDrawingPrimitives.ControlColors(
            theme,
            new FluentControlState(IsChecked: true),
            accentWhenChecked: true);
        var selected = FluentDrawingPrimitives.ControlColors(theme, new FluentControlState(IsSelected: true));

        Assert.AreEqual(theme.Surface, enabled.Fill);
        Assert.AreEqual(theme.DisabledSurface, disabled.Fill);
        Assert.AreEqual(theme.TextDisabled, disabled.Text);
        Assert.AreEqual(theme.Accent, checkedState.Fill);
        Assert.AreEqual(theme.Surface, checkedState.Text);
        Assert.AreEqual(theme.AccentSoft, selected.Fill);
        Assert.AreEqual(theme.Accent, selected.Text);
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
    public void CompatibilityCatalogDocsPublishMatchingCounts()
    {
        var catalog = CompatibilityCatalog.Current;
        var expectedStatusCounts = CountBy(catalog.Entries, entry => entry.Status);
        var documents = new[]
        {
            "README.md",
            "docs/compatibility/matrix.md",
            "docs/compatibility/api-catalog.md",
            "docs/release/production-evidence-view.md"
        };

        foreach (var document in documents)
        {
            var text = File.ReadAllText(RepositoryPath(document));

            Assert.IsTrue(
                ContainsCatalogTotal(text, catalog.Entries.Count),
                $"{document} must publish the catalog total from winui-api-compatibility.catalog.json.");

            foreach (var (status, count) in expectedStatusCounts)
            {
                Assert.IsTrue(
                    ContainsCatalogStatusCount(text, status, count),
                    $"{document} must publish {count} '{status}' catalog entries.");
            }
        }
    }

    [TestMethod]
    public void CompatibilityCatalogVisualReadinessInventoryAccountsForEveryEntry()
    {
        var catalog = CompatibilityCatalog.Current;
        var expectedStatusCounts = CountBy(catalog.Entries, entry => entry.Status);
        var expectedKindCounts = CountBy(catalog.Entries, entry => entry.Kind);
        var expectedBucketCounts = catalog.Entries
            .GroupBy(entry => (entry.Kind, entry.Status))
            .ToDictionary(group => $"{group.Key.Kind}|{group.Key.Status}", group => group.Count(), StringComparer.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/compatibility/visual-readiness-inventory.json")));
        var root = document.RootElement;

        Assert.AreEqual("0.1", root.GetProperty("schemaVersion").GetString());
        var snapshot = root.GetProperty("catalogSnapshot");
        Assert.AreEqual(catalog.Entries.Count, snapshot.GetProperty("total").GetInt32());
        AssertCountsEqual(expectedStatusCounts, snapshot.GetProperty("statusCounts"), "status");
        AssertCountsEqual(expectedKindCounts, snapshot.GetProperty("kindCounts"), "kind");

        var audit = root.GetProperty("allCatalogReadinessAudit");
        Assert.AreEqual(catalog.Entries.Count, audit.GetProperty("accountedEntries").GetInt32());
        Assert.AreEqual(0, audit.GetProperty("unassignedDispositionCount").GetInt32());
        Assert.AreEqual(catalog.Entries.Count, SumObjectCounts(audit.GetProperty("dispositionCounts")));

        var actualBucketCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var bucket in audit.GetProperty("auditBuckets").EnumerateArray())
        {
            var kind = RequireNonEmptyString(bucket, "kind");
            var status = RequireNonEmptyString(bucket, "status");
            var count = bucket.GetProperty("count").GetInt32();

            Assert.IsGreaterThan(0, count);
            Assert.IsTrue(expectedKindCounts.ContainsKey(kind), $"Unknown audit kind '{kind}'.");
            Assert.IsTrue(expectedStatusCounts.ContainsKey(status), $"Unknown audit status '{status}'.");
            _ = RequireNonEmptyString(bucket, "disposition");
            _ = RequireNonEmptyString(bucket, "primaryBlocker");
            _ = RequireNonEmptyString(bucket, "evidenceProfile");

            actualBucketCounts[$"{kind}|{status}"] = count;
        }

        CollectionAssert.AreEquivalent(
            expectedBucketCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray(),
            actualBucketCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray());
        Assert.AreEqual(catalog.Entries.Count, actualBucketCounts.Values.Sum());

        var blockerIds = root.GetProperty("productionBlockerMapping")
            .EnumerateArray()
            .Select(entry => RequireNonEmptyString(entry, "id"))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(
            Enumerable.Range(0, 13).Select(index => $"PB-{index:000}").ToArray(),
            blockerIds);

        var promotionGrades = root.GetProperty("promotionRules")
            .EnumerateArray()
            .Select(entry => RequireNonEmptyString(entry, "grade"))
            .ToArray();
        CollectionAssert.AreEqual(
            new[] { "not-rendered", "usable", "good", "production-ready" },
            promotionGrades);

        var phases = root.GetProperty("phaseReadinessGates")
            .EnumerateArray()
            .Select(entry => RequireNonEmptyString(entry, "phase"))
            .OrderBy(phase => phase, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(
            new[] { "Phase 2", "Phase 3", "Phase 4", "Phase 5", "Phase 6", "Phase 7", "Phase 8", "Phase 9" },
            phases);
    }

    [TestMethod]
    public void CompatibilityCatalogReadinessAuditAccountsForEveryEntry()
    {
        var catalog = CompatibilityCatalog.Current;
        var statusCounts = CountBy(catalog.Entries, entry => entry.Status);
        var audit = CatalogReadinessAudit.Build(catalog);

        Assert.AreEqual(catalog.Entries.Count, audit.AccountedEntries);
        Assert.HasCount(catalog.Entries.Count, audit.Entries);
        Assert.AreEqual(0, audit.UnassignedDispositionCount);

        var knownDispositions = new[]
        {
            CatalogReadinessAudit.DispositionSourceLevelImplementation,
            CatalogReadinessAudit.DispositionBoundedImplementation,
            CatalogReadinessAudit.DispositionDiagnosticExclusion,
            CatalogReadinessAudit.DispositionWindowsOnlyExclusion,
            CatalogReadinessAudit.DispositionNonGoalExclusion,
        };

        foreach (var entry in audit.Entries)
        {
            CollectionAssert.Contains(knownDispositions, entry.Disposition, $"{entry.Id} has an unknown disposition.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.OwnerPhase), $"{entry.Id} has no owner phase.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.PrimaryBlocker), $"{entry.Id} has no primary blocker.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.EvidenceProfile), $"{entry.Id} has no evidence profile.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.ReleaseGate), $"{entry.Id} has no release gate.");
        }

        Assert.AreEqual(catalog.Entries.Count, audit.DispositionCounts.Values.Sum());
        Assert.AreEqual(catalog.Entries.Count, audit.OwnerPhaseCounts.Values.Sum());
        Assert.AreEqual(catalog.Entries.Count, audit.BlockerCounts.Values.Sum());

        // Each disposition tracks its catalog status count one-to-one.
        Assert.AreEqual(
            statusCounts[CompatibilityStatuses.Supported],
            audit.DispositionCounts[CatalogReadinessAudit.DispositionSourceLevelImplementation]);
        Assert.AreEqual(
            statusCounts[CompatibilityStatuses.Partial],
            audit.DispositionCounts[CatalogReadinessAudit.DispositionBoundedImplementation]);
        Assert.AreEqual(
            statusCounts[CompatibilityStatuses.Planned],
            audit.DispositionCounts[CatalogReadinessAudit.DispositionDiagnosticExclusion]);
        Assert.AreEqual(
            statusCounts[CompatibilityStatuses.WindowsOnly],
            audit.DispositionCounts[CatalogReadinessAudit.DispositionWindowsOnlyExclusion]);
        Assert.AreEqual(
            statusCounts[CompatibilityStatuses.NotSupported],
            audit.DispositionCounts[CatalogReadinessAudit.DispositionNonGoalExclusion]);
    }

    [TestMethod]
    public void CompatibilityCatalogReadinessAuditMatchesInventoryBuckets()
    {
        var audit = CatalogReadinessAudit.Build(CompatibilityCatalog.Current);

        var auditBuckets = audit.Entries
            .GroupBy(entry => (entry.Kind, entry.Status))
            .ToDictionary(
                group => $"{group.Key.Kind}|{group.Key.Status}",
                group => (
                    Count: group.Count(),
                    Disposition: group.Select(entry => entry.Disposition).Distinct(StringComparer.Ordinal).Single(),
                    Blocker: group.Select(entry => entry.PrimaryBlocker).Distinct(StringComparer.Ordinal).Single()),
                StringComparer.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/compatibility/visual-readiness-inventory.json")));
        var inventoryBuckets = document.RootElement.GetProperty("allCatalogReadinessAudit").GetProperty("auditBuckets");

        var matched = 0;
        foreach (var bucket in inventoryBuckets.EnumerateArray())
        {
            var key = $"{RequireNonEmptyString(bucket, "kind")}|{RequireNonEmptyString(bucket, "status")}";
            Assert.IsTrue(auditBuckets.TryGetValue(key, out var expected), $"Inventory bucket '{key}' is missing from the per-entry audit.");
            Assert.AreEqual(expected.Count, bucket.GetProperty("count").GetInt32(), $"Bucket '{key}' count mismatch.");
            Assert.AreEqual(expected.Disposition, RequireNonEmptyString(bucket, "disposition"), $"Bucket '{key}' disposition mismatch.");
            Assert.AreEqual(expected.Blocker, RequireNonEmptyString(bucket, "primaryBlocker"), $"Bucket '{key}' blocker mismatch.");
            matched++;
        }

        Assert.AreEqual(auditBuckets.Count, matched, "Every per-entry audit bucket must be represented in the inventory.");
    }

    [TestMethod]
    public void CompatibilityCatalogReadinessAuditFileIsUpToDate()
    {
        var audit = CatalogReadinessAudit.Build(CompatibilityCatalog.Current);
        var expected = JsonSerializer.Serialize(audit, JsonDefaults.Options);
        var actual = File.ReadAllText(RepositoryPath("docs/compatibility/all-catalog-readiness-audit.json"));

        Assert.AreEqual(
            NormalizeArtifact(expected),
            NormalizeArtifact(actual),
            "docs/compatibility/all-catalog-readiness-audit.json is out of date. Regenerate with 'winui3-mac-runner catalog-audit'.");
    }

    [TestMethod]
    public void CompatibilityCatalogReadinessAuditDocPublishesMatchingTotals()
    {
        var audit = CatalogReadinessAudit.Build(CompatibilityCatalog.Current);
        var text = File.ReadAllText(RepositoryPath("docs/compatibility/all-catalog-readiness-audit.md"));

        Assert.IsTrue(
            ContainsCatalogTotal(text, audit.AccountedEntries),
            "all-catalog-readiness-audit.md must publish the 126/126 catalog total.");

        foreach (var (disposition, count) in audit.DispositionCounts)
        {
            Assert.IsTrue(
                Regex.IsMatch(text, $@"\|\s*{count}\s*\|", RegexOptions.CultureInvariant),
                $"all-catalog-readiness-audit.md must publish the {disposition} count {count}.");
        }
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
        var settings = new VisualRunSettings(null, scenario.Name, "skia-v2", new VisualViewport(800, 600), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var interactions = new InteractionReport(
            ArtifactSchemas.InteractionReport,
            new[] { new InteractionStepResult(0, "click", "passed", "PrimaryButton", null) });

        var evidence = ComponentEvidenceBuilder.Build(scenario, arranged, interactions, metrics: null);

        Assert.AreEqual(ArtifactSchemas.ComponentEvidence, evidence.SchemaVersion);
        Assert.AreEqual("passed", evidence.Status);
        Assert.HasCount(2, evidence.Components);
        Assert.AreEqual("present", evidence.Components[0].Presence);
        var layoutRegion = evidence.Components[0].LayoutRegion ?? throw new AssertFailedException("Expected component evidence to include layout region.");
        if (layoutRegion.Width <= 0 || layoutRegion.Height <= 0)
        {
            Assert.Fail("Expected component evidence layout region to have a positive size.");
        }

        Assert.AreEqual("passed", evidence.Components[0].InteractionStatus);
        Assert.IsNotNull(evidence.Components[0].ComponentThresholds);
        Assert.AreEqual("not-evaluated", evidence.Components[0].NativeQualityGrade);
        Assert.IsNull(evidence.Components[0].Inspection);
        Assert.AreEqual("not-rendered", evidence.Components[1].VisualGrade);
        Assert.AreEqual("not-evaluated", evidence.Components[1].NativeQualityGrade);
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
    public async Task BroaderControlInventoryTracksPrioritizedControlsHonestly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        using var inventory = JsonDocument.Parse(await File.ReadAllTextAsync(inventoryPath));
        var root = inventory.RootElement;

        var entries = root.GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("component").GetString()!, entry => entry, StringComparer.Ordinal);

        var broader = root.GetProperty("broaderControlInventory");
        Assert.AreEqual("Phase 7", broader.GetProperty("phase").GetString());

        var validFamilies = broader.GetProperty("validFamilies")
            .EnumerateArray()
            .Select(family => family.GetString()!)
            .ToArray();

        var expectedControls = new[]
        {
            "AutoSuggestBox", "PasswordBox", "NumberBox", "Slider", "ToggleSwitch", "DropDownButton",
            "SplitButton", "ToggleSplitButton", "MenuBar", "TeachingTip", "Expander", "TabView", "TreeView",
            "GridView", "CalendarView", "DatePicker", "TimePicker", "ColorPicker", "RatingControl", "PersonPicture",
        };

        var knownGrades = new[] { "not-rendered", "usable", "good", "production-ready" };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var priorities = new HashSet<int>();

        foreach (var control in broader.GetProperty("controls").EnumerateArray())
        {
            var name = RequireNonEmptyString(control, "control");
            Assert.IsTrue(seen.Add(name), $"Broader inventory lists '{name}' more than once.");

            CollectionAssert.Contains(validFamilies, RequireNonEmptyString(control, "targetFamily"), $"{name} targets an unknown family.");
            _ = RequireNonEmptyString(control, "disposition");
            _ = RequireNonEmptyString(control, "promotionExitCriteria");
            Assert.IsGreaterThan(0, control.GetProperty("requiredStates").GetArrayLength(), $"{name} must declare required states.");
            Assert.IsTrue(priorities.Add(control.GetProperty("priority").GetInt32()), $"{name} reuses a priority value.");

            var grade = RequireNonEmptyString(control, "currentGrade");
            CollectionAssert.Contains(knownGrades, grade, $"{name} has an unknown current grade.");

            Assert.IsTrue(entries.TryGetValue(name, out var entry), $"{name} is missing a tracking row in 'entries'.");

            // Honesty gate: a control may claim a rendered grade only when its
            // tracking row carries the matching catalog status, visual evidence,
            // and (when required) interaction coverage. Otherwise it must stay
            // not-rendered so the roadmap cannot hide a false support claim.
            if (grade == "not-rendered")
            {
                Assert.AreEqual(
                    "not-rendered",
                    entry.GetProperty("visualEvidence").GetString(),
                    $"{name} is not-rendered in the plan but claims visual evidence in 'entries'.");
            }
            else
            {
                var status = entry.GetProperty("catalogStatus").GetString();
                Assert.IsTrue(
                    status is CompatibilityStatuses.Supported or CompatibilityStatuses.Partial,
                    $"{name} claims grade '{grade}' but its catalog status is '{status}'.");
                Assert.AreNotEqual(
                    "not-rendered",
                    entry.GetProperty("visualEvidence").GetString(),
                    $"{name} claims grade '{grade}' without visual evidence.");
                if (control.GetProperty("interactionRequired").GetBoolean())
                {
                    Assert.AreNotEqual(
                        "none",
                        entry.GetProperty("interactionCoverage").GetString(),
                        $"{name} requires interaction evidence before claiming grade '{grade}'.");
                }
            }
        }

        CollectionAssert.AreEquivalent(
            expectedControls,
            seen.ToArray(),
            "The broader control inventory must track exactly the prioritized Phase 7 controls.");
    }

    [TestMethod]
    public void MaterialMotionApproximationsCoverEveryPhase8CatalogEntry()
    {
        var audit = CatalogReadinessAudit.Build(CompatibilityCatalog.Current);
        var phase8Apis = audit.Entries
            .Where(entry => entry.Area is "materials" or "composition" or "motion")
            .Select(entry => entry.Api)
            .ToHashSet(StringComparer.Ordinal);

        using var registry = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/compatibility/material-motion-approximations.json")));
        var root = registry.RootElement;

        Assert.IsFalse(root.GetProperty("osCompositionClaim").GetBoolean(), "The material/motion registry must not claim OS composition.");

        var covered = new HashSet<string>(StringComparer.Ordinal);
        var knownGrades = new[] { "not-rendered", "usable", "good", "production-ready" };

        foreach (var surface in root.GetProperty("surfaces").EnumerateArray())
        {
            var name = RequireNonEmptyString(surface, "surface");
            var disposition = RequireNonEmptyString(surface, "disposition");
            _ = RequireNonEmptyString(surface, "approximation");
            _ = RequireNonEmptyString(surface, "reducedMotion");
            _ = RequireNonEmptyString(surface, "highContrast");
            _ = RequireNonEmptyString(surface, "provenanceRequirement");
            CollectionAssert.Contains(knownGrades, RequireNonEmptyString(surface, "currentGrade"));

            // No surface may claim real Windows OS composition on macOS.
            Assert.IsFalse(surface.GetProperty("isOsComposition").GetBoolean(), $"Surface '{name}' must not claim OS composition.");

            // Motion surfaces capture deterministic end states, not timing.
            if (surface.GetProperty("kind").GetString() == "motion")
            {
                StringAssert.Contains(disposition, "end-state", $"Motion surface '{name}' must capture deterministic end states, not animation timing.");
            }

            foreach (var api in surface.GetProperty("winuiApis").EnumerateArray())
            {
                covered.Add(api.GetString()!);
            }
        }

        foreach (var api in phase8Apis)
        {
            Assert.Contains(api, covered, $"Material/motion catalog API '{api}' is not covered by the approximation registry.");
        }
    }

    [TestMethod]
    public void VisualDriftDashboardGatesComponentCropNotWholeScreen()
    {
        using var dashboard = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/visual-parity/visual-drift-dashboard.json")));
        var root = dashboard.RootElement;

        Assert.AreEqual("component-crop", root.GetProperty("gatedMetric").GetString());
        Assert.AreEqual("whole-screen", root.GetProperty("informationalMetric").GetString());
        _ = RequireNonEmptyString(root, "policy");
        _ = RequireNonEmptyString(root, "latestRunId");

        var families = root.GetProperty("families").EnumerateArray().ToArray();
        Assert.IsGreaterThan(0, families.Length);

        foreach (var family in families)
        {
            var name = RequireNonEmptyString(family, "family");
            var crop = family.GetProperty("componentCropDrift");
            var whole = family.GetProperty("wholeScreenDrift");

            Assert.IsTrue(crop.GetProperty("gated").GetBoolean(), $"{name} component-crop drift must be the gated metric.");
            Assert.IsFalse(whole.GetProperty("gated").GetBoolean(), $"{name} whole-screen drift must be informational only.");

            // Whole-screen drift must match the checked-in pixel-diff artifact exactly.
            var pixelDiffPath = RepositoryPath(RequireNonEmptyString(family, "pixelDiffPath"));
            using var pixelDiff = JsonDocument.Parse(File.ReadAllText(pixelDiffPath));
            Assert.AreEqual(
                pixelDiff.RootElement.GetProperty("changedPixelPercentage").GetDouble(),
                whole.GetProperty("changedPixelPercentage").GetDouble(),
                0.000001,
                $"{name} whole-screen drift must match its pixel-diff artifact.");
        }
    }

    [TestMethod]
    public void ComponentQualityDashboardMatchesPublicEvidence()
    {
        var expected = ComponentQualityDashboard.BuildFromPublicEvidence(RepositoryRoot());
        var actual = File.ReadAllText(RepositoryPath("docs/visual-parity/component-quality-dashboard.json"));

        Assert.AreEqual(
            NormalizeArtifact(JsonSerializer.Serialize(expected, JsonDefaults.Options)),
            NormalizeArtifact(actual),
            "docs/visual-parity/component-quality-dashboard.json is out of date. Regenerate with 'winui3-mac-runner component-quality-dashboard'.");

        Assert.AreEqual("blocked", expected.Status);
        Assert.AreEqual(expected.Totals.ComponentCount, expected.Totals.BlockingRowCount);
        Assert.AreEqual(0, expected.Totals.MissingMacRuntimeCrops);
        Assert.AreEqual(0, expected.Totals.MissingNativeReferenceCrops);
        Assert.AreEqual(0, expected.Totals.MissingNativeReferenceProvenance);
        Assert.AreEqual(0, expected.Totals.MissingComponentDiffs);
        Assert.IsGreaterThan(0, expected.Totals.MissingInspectionNotes);

        var provenanceBlockers = 0;
        foreach (var blocker in expected.Blockers)
        {
            Assert.IsTrue(
                blocker.Reasons.Any(reason => reason.Contains("nativeQualityGrade", StringComparison.Ordinal)),
                $"{blocker.ScenarioName}/{blocker.Component} must explain the missing native-quality grade.");
            Assert.IsTrue(
                blocker.Reasons.Any(reason => reason.Contains("manual screenshot inspection", StringComparison.Ordinal)),
                $"{blocker.ScenarioName}/{blocker.Component} must require manual inspection metadata.");
            if (blocker.Reasons.Any(reason => reason.Contains("reference provenance", StringComparison.Ordinal)))
            {
                provenanceBlockers++;
            }
        }

        Assert.AreEqual(expected.Totals.MissingNativeReferenceProvenance, provenanceBlockers);
    }

    [TestMethod]
    public void VisualReviewIndexMatchesPublicEvidence()
    {
        var outputDirectory = RepositoryPath("docs/visual-parity");
        var expected = VisualReviewIndexArtifacts.Build(RepositoryRoot(), outputDirectory);
        var actualJson = File.ReadAllText(RepositoryPath("docs/visual-parity/public-visual-review-index.json"));
        var actualHtml = File.ReadAllText(RepositoryPath("docs/visual-parity/public-visual-review-index.html"));

        Assert.AreEqual(
            NormalizeArtifact(JsonSerializer.Serialize(expected, JsonDefaults.Options)),
            NormalizeArtifact(actualJson),
            "docs/visual-parity/public-visual-review-index.json is out of date. Regenerate with 'winui3-mac-runner visual-review-index'.");
        Assert.AreEqual(
            NormalizeArtifact(VisualReviewIndexArtifacts.BuildHtml(expected)),
            NormalizeArtifact(actualHtml),
            "docs/visual-parity/public-visual-review-index.html is out of date. Regenerate with 'winui3-mac-runner visual-review-index'.");

        Assert.AreEqual(58, expected.Summary.ComponentCount);
        Assert.AreEqual(58, expected.Summary.CompleteTriptychCount);
        Assert.AreEqual(0, expected.Summary.MissingReviewFiles);
        Assert.AreEqual(0, expected.Summary.MissingNativeReferenceCrops);
        Assert.AreEqual(0, expected.Summary.MissingMacRuntimeCrops);
        Assert.AreEqual(0, expected.Summary.MissingDiffCrops);
        Assert.AreEqual(58, expected.Summary.MissingInspectionNotes);
        Assert.AreEqual(58, expected.Summary.BlockingRowCount);
        Assert.HasCount(58, expected.Rows);
    }

    [TestMethod]
    public void ComponentInspectionApplierAppliesReviewedFinalGrades()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-inspection-tests", Guid.NewGuid().ToString("N"));
        var componentDirectory = Path.Combine(directory, "components", "button-primarybutton");
        Directory.CreateDirectory(componentDirectory);
        WritePatternPng(Path.Combine(componentDirectory, "windows-reference.png"));
        WritePatternPng(Path.Combine(componentDirectory, "mac-runtime.png"));
        WritePatternPng(Path.Combine(componentDirectory, "pixel-diff.png"));
        var evidence = TestInspectableEvidence();
        var inspection = new ComponentInspectionDocument(
            SchemaVersion: ArtifactSchemas.ComponentInspection,
            Rows: new[]
            {
                new ComponentInspectionRow(
                    Component: "Button",
                    Target: "PrimaryButton",
                    VisualGrade: "good",
                    NativeQualityGrade: "good",
                    InspectedBy: "manual-reviewer",
                    InspectedDate: "2026-06-02",
                    NativeReferenceRunId: "26777029415",
                    ComparisonArtifactPaths: null,
                    AcceptedGaps: new[] { "Glyph antialiasing differs within accepted tolerance." },
                    ToleranceReason: "macOS font rasterization differs from Windows.",
                    Notes: "Native, macOS, and diff crops were manually inspected.")
            });

        var updated = ComponentInspectionApplier.Apply(evidence, inspection, directory);

        var component = updated.Components[0];
        Assert.AreEqual("good", component.VisualGrade);
        Assert.AreEqual("good", component.NativeQualityGrade);
        Assert.IsNotNull(component.Inspection);
        Assert.AreEqual("manual-reviewer", component.Inspection.InspectedBy);
        Assert.AreEqual("26777029415", component.Inspection.NativeReferenceRunId);
        Assert.HasCount(3, component.Inspection.ComparisonArtifactPaths);
    }

    [TestMethod]
    public void ComponentInspectionApplierRejectsNonFinalGrades()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-inspection-tests", Guid.NewGuid().ToString("N"));
        var componentDirectory = Path.Combine(directory, "components", "button-primarybutton");
        Directory.CreateDirectory(componentDirectory);
        WritePatternPng(Path.Combine(componentDirectory, "windows-reference.png"));
        WritePatternPng(Path.Combine(componentDirectory, "mac-runtime.png"));
        WritePatternPng(Path.Combine(componentDirectory, "pixel-diff.png"));
        var inspection = new ComponentInspectionDocument(
            SchemaVersion: ArtifactSchemas.ComponentInspection,
            Rows: new[]
            {
                new ComponentInspectionRow(
                    Component: "Button",
                    Target: "PrimaryButton",
                    VisualGrade: "usable",
                    NativeQualityGrade: "good",
                    InspectedBy: "manual-reviewer",
                    InspectedDate: "2026-06-02",
                    NativeReferenceRunId: "26777029415",
                    ComparisonArtifactPaths: null,
                    AcceptedGaps: Array.Empty<string>(),
                    ToleranceReason: null,
                    Notes: "Native, macOS, and diff crops were manually inspected.")
            });

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ComponentInspectionApplier.Apply(TestInspectableEvidence(), inspection, directory));
        StringAssert.Contains(exception.Message, "visualGrade must be good or production-ready");
    }

    [TestMethod]
    public void ReleaseCandidateArtifactGatesAreAccountedFor()
    {
        // Mirrors the deterministic local checks of 'winui3-mac-runner
        // release-candidate'. The current component-quality dashboard is expected
        // to block release until native reference crops and manual inspections
        // exist for every public component row. CI adds native reference capture,
        // the full strict sweep, and the package dry run.

        // Zero unknown production surfaces in the committed corpus inventory.
        using (var corpus = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/compatibility/corpus-unknown-apis.json"))))
        {
            Assert.AreEqual(0, corpus.RootElement.GetProperty("entries").GetArrayLength(), "Corpus inventory must report zero unknown public surfaces.");
        }

        using (var dashboard = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/visual-parity/component-quality-dashboard.json"))))
        {
            Assert.AreEqual("blocked", dashboard.RootElement.GetProperty("status").GetString());
            Assert.IsGreaterThan(0, dashboard.RootElement.GetProperty("totals").GetProperty("blockingRowCount").GetInt32());
        }

        // Native provenance for every checked-in visual reference.
        var referenceFiles = Directory.EnumerateFiles(
            RepositoryPath("docs/visual-parity/examples"),
            "windows-reference.json",
            SearchOption.AllDirectories).ToArray();
        Assert.IsGreaterThan(0, referenceFiles.Length);
        foreach (var referenceFile in referenceFiles)
        {
            using var reference = JsonDocument.Parse(File.ReadAllText(referenceFile));
            Assert.AreEqual(
                "native-winui",
                reference.RootElement.GetProperty("referenceSource").GetString(),
                $"{Path.GetFileName(Path.GetDirectoryName(referenceFile))} must declare native-winui provenance.");
        }

        // Release and support-policy documents are present and name the gate.
        string[] requiredDocs =
        {
            "docs/release/final-production-gate.md",
            "docs/release/support-policy.md",
            "docs/release/level-7-release-readiness.md",
            "docs/release/production-evidence-view.md",
        };
        foreach (var relative in requiredDocs)
        {
            Assert.IsTrue(File.Exists(RepositoryPath(relative)), $"Missing release document {relative}.");
        }

        var evidenceView = File.ReadAllText(RepositoryPath("docs/release/production-evidence-view.md"));
        StringAssert.Contains(evidenceView, "release-candidate", "The production evidence view must document the release candidate gate.");
    }

    [TestMethod]
    public void WindowsNativeReferenceWorkflowCoversEveryComponentParityScenario()
    {
        var workflow = File.ReadAllText(RepositoryPath(".github/workflows/windows-native-screenshot.yml"));
        var scenarioRoot = RepositoryPath("fixtures/ComponentParityLab.WinUI/scenarios");
        var componentScenarioPaths = Directory.EnumerateFiles(scenarioRoot, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsGreaterThan(0, componentScenarioPaths.Length);
        foreach (var scenarioPath in componentScenarioPaths)
        {
            StringAssert.Contains(
                workflow,
                $"Path = \"{scenarioPath}\"",
                $"windows-native-screenshot.yml must capture native WinUI references for {scenarioPath}.");
        }
    }

    [TestMethod]
    public async Task NativeReferenceImporterNormalizesCompleteComponentReferenceArtifact()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-source", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-output", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceRoot);
        var scenarioRoot = RepositoryPath("fixtures/ComponentParityLab.WinUI/scenarios");
        var scenarioPaths = Directory.EnumerateFiles(scenarioRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var scenarioPath in scenarioPaths)
        {
            var scenario = await VisualScenario.LoadAsync(scenarioPath);
            var artifactDirectory = Path.Combine(sourceRoot, scenario.Name);
            Directory.CreateDirectory(artifactDirectory);
            WriteSolidPng(Path.Combine(artifactDirectory, "windows-reference.png"), new SKColor(250, 250, 250), scenario.Viewport.Width, scenario.Viewport.Height);
            var relativeScenarioPath = Path.GetRelativePath(RepositoryRoot(), scenarioPath).Replace('\\', '/');
            var provenance = new NativeReferenceProvenance(
                ReferenceSource: "native-winui",
                FixtureProjectPath: "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
                ScenarioPath: relativeScenarioPath,
                ScenarioName: scenario.Name,
                CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
                WorkflowRunId: "26777029415",
                RunnerImage: "win25 20260525.149.1",
                WindowsAppSdkVersion: null,
                Viewport: scenario.Viewport,
                Scale: scenario.Scale,
                Theme: scenario.Theme,
                CaptureMode: "client-area",
                Dimensions: new ReferenceImageDimensions(scenario.Viewport.Width, scenario.Viewport.Height),
                CapturedAt: "2026-06-01T19:31:04.2512607+00:00");
            File.WriteAllText(
                Path.Combine(artifactDirectory, "windows-reference.json"),
                JsonSerializer.Serialize(provenance, JsonDefaults.Options));
        }

        var import = NativeReferenceImporter.Import(RepositoryRoot(), sourceRoot, outputRoot);

        Assert.AreEqual("passed", import.Status);
        Assert.HasCount(import.ExpectedComponentScenarioCount, scenarioPaths);
        Assert.AreEqual(scenarioPaths.Length, import.ImportedReferenceCount);
        Assert.HasCount(0, import.MissingComponentScenarioPaths);
        Assert.HasCount(0, import.Problems);
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "native-reference-import.json")));
        foreach (var item in import.Items)
        {
            Assert.AreEqual("imported", item.Status);
            Assert.AreEqual("native-winui", item.ReferenceSource);
            Assert.IsTrue(File.Exists(Path.Combine(outputRoot, item.ImportedReferenceImagePath)));
            Assert.IsTrue(File.Exists(Path.Combine(outputRoot, item.ImportedReferenceMetadataPath)));
        }

        var firstScenario = await VisualScenario.LoadAsync(scenarioPaths[0]);
        var resolvedReference = NativeReferenceImporter.ResolveReferenceImagePath(
            RepositoryRoot(),
            outputRoot,
            firstScenario.Name,
            scenarioPaths[0]);
        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(outputRoot, firstScenario.Name, "windows-reference.png")),
            resolvedReference);
    }

    [TestMethod]
    public async Task ProductionStateCoverageReferencesExistingScenarios()
    {
        var repositoryRoot = FindRepositoryRoot();
        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        using var inventory = JsonDocument.Parse(await File.ReadAllTextAsync(inventoryPath));

        foreach (var stateCoverage in inventory.RootElement.GetProperty("productionStateCoverage").EnumerateArray())
        {
            var relativePath = stateCoverage.GetProperty("path").GetString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(relativePath));

            var scenarioPath = Path.Combine(repositoryRoot, relativePath);
            Assert.IsTrue(File.Exists(scenarioPath), $"Missing production state scenario '{relativePath}'.");

            var scenario = await VisualScenario.LoadAsync(scenarioPath);
            Assert.AreEqual(stateCoverage.GetProperty("scenario").GetString(), scenario.Name);
        }
    }

    [TestMethod]
    public async Task PhaseFiveReadinessScenariosKeepClaimedRingZeroAndRingOneUsable()
    {
        var repositoryRoot = FindRepositoryRoot();
        var requiredScenarios = new[]
        {
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-checked-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-disabled-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-focused-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-focused-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-invalid-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-collections-selected-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-command-invoked-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-disabled-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-open-popup-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-dialogs-flyouts-open-popup-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-loading-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-error-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-success-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-dark.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-high-contrast.json",
            "fixtures/ComponentParityLab.WinUI/scenarios/component-navigation-workbench-light.json",
            "fixtures/ProductionSmoke.WinUI/scenarios/production-smoke-light.json",
            "fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json",
            "fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json",
            "fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-deferred-light.json"
        };
        var claimedStatuses = new[] { CompatibilityStatuses.Supported, CompatibilityStatuses.Partial };
        var auditedClaimedRequirements = 0;

        foreach (var relativePath in requiredScenarios)
        {
            var scenario = await VisualScenario.LoadAsync(Path.Combine(repositoryRoot, relativePath));

            Assert.IsTrue(scenario.StrictVisual, $"{relativePath} must be a strict visual scenario.");
            foreach (var requirement in scenario.Requirements)
            {
                if (claimedStatuses.Contains(requirement.ExpectedStatus))
                {
                    auditedClaimedRequirements++;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(requirement.Target), $"{relativePath} claimed requirement '{requirement.Component}' must target a crop region.");
                    Assert.IsTrue(
                        ComponentEvidenceBuilder.MeetsMinimumVisualGrade(requirement.MinimumVisualGrade, "usable"),
                        $"{relativePath} claimed requirement '{requirement.Component}' must require at least usable visuals.");
                    Assert.IsTrue(
                        ComponentEvidenceBuilder.MeetsMinimumVisualGrade(requirement.VisualGrade ?? requirement.MinimumVisualGrade, requirement.MinimumVisualGrade),
                        $"{relativePath} claimed requirement '{requirement.Component}' must not publish a grade below its minimum.");
                    continue;
                }

                if (requirement.ExpectedStatus == CompatibilityStatuses.Planned)
                {
                    Assert.AreEqual("not-rendered", requirement.VisualGrade ?? requirement.MinimumVisualGrade);
                }
            }
        }

        Assert.IsGreaterThan(0, auditedClaimedRequirements);
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
    public void CorpusInventoryClassifiesEveryDiscoveredSurfaceWithoutUnknowns()
    {
        var manifestPath = Path.Combine(FindRepositoryRoot(), "fixtures", "corpus.json");

        var result = new CorpusInventoryService().Generate(manifestPath, "Debug");

        Assert.IsTrue(
            result.Summary.Apps.All(app => app.IngestionStatus == "passed"),
            "Every corpus app must ingest through the compat shadow build without blocking diagnostics.");
        Assert.HasCount(0, result.Unknown.Entries, "Every discovered corpus surface must be classified in the catalog.");
        Assert.IsGreaterThan(0, result.Inventory.Entries.Count);

        var knownStatuses = new[]
        {
            CompatibilityStatuses.Supported,
            CompatibilityStatuses.Partial,
            CompatibilityStatuses.Planned,
            CompatibilityStatuses.WindowsOnly,
            CompatibilityStatuses.NotSupported
        };
        foreach (var entry in result.Inventory.Entries)
        {
            CollectionAssert.Contains(
                knownStatuses,
                entry.Status,
                $"Discovered surface '{entry.Kind} {entry.Construct}' has unexpected status '{entry.Status}'.");
            Assert.IsGreaterThan(0, entry.UsedBy.Count);
        }

        AssertCorpusEntry(result.Inventory, "xaml-element", "Window", CompatibilityStatuses.Supported);
        AssertCorpusEntry(result.Inventory, "xaml-element", "NavigationView", CompatibilityStatuses.Partial);
        AssertCorpusEntry(result.Inventory, "xaml-resource", "ThemeResource", CompatibilityStatuses.Partial);
        AssertCorpusEntry(result.Inventory, "xaml-markup", "Binding", CompatibilityStatuses.Partial);
        AssertCorpusEntry(result.Inventory, "project-item", "Microsoft.WindowsAppSDK", CompatibilityStatuses.WindowsOnly);
    }

    [TestMethod]
    public void CorpusInventoryMatchesTrackedBaseline()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repositoryRoot, "fixtures", "corpus.json");
        var compatibilityDirectory = Path.Combine(repositoryRoot, "docs", "compatibility");

        var result = new CorpusInventoryService().Generate(manifestPath, "Debug");

        AssertMatchesBaseline(
            Path.Combine(compatibilityDirectory, "corpus-inventory.json"),
            JsonSerializer.Serialize(result.Inventory, JsonDefaults.Options));
        AssertMatchesBaseline(
            Path.Combine(compatibilityDirectory, "corpus-unknown-apis.json"),
            JsonSerializer.Serialize(result.Unknown, JsonDefaults.Options));
    }

    [TestMethod]
    public void CorpusInventoryGenerationIsDeterministic()
    {
        var manifestPath = Path.Combine(FindRepositoryRoot(), "fixtures", "corpus.json");
        var service = new CorpusInventoryService();

        var first = JsonSerializer.Serialize(service.Generate(manifestPath, "Debug").Inventory, JsonDefaults.Options);
        var second = JsonSerializer.Serialize(service.Generate(manifestPath, "Debug").Inventory, JsonDefaults.Options);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public async Task ClaimedSupportedComponentsAreNeverNotRendered()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scenarioRoots = new[]
        {
            Path.Combine(repositoryRoot, "fixtures", "ComponentParityLab.WinUI", "scenarios"),
            Path.Combine(repositoryRoot, "fixtures", "ProductionSmoke.WinUI", "scenarios"),
            Path.Combine(repositoryRoot, "fixtures", "PublicAdminWorkbench.WinUI", "scenarios"),
            Path.Combine(repositoryRoot, "fixtures", "ResourceCatalogApp.WinUI", "scenarios")
        };
        var claimedStatuses = new[] { CompatibilityStatuses.Supported, CompatibilityStatuses.Partial };
        var audited = 0;

        foreach (var scenarioPath in scenarioRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.json"))
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var scenario = await VisualScenario.LoadAsync(scenarioPath);
            var scenarioName = Path.GetFileName(scenarioPath);
            foreach (var requirement in scenario.Requirements)
            {
                if (!claimedStatuses.Contains(requirement.ExpectedStatus))
                {
                    continue;
                }

                audited++;
                var grade = requirement.VisualGrade ?? requirement.MinimumVisualGrade;
                Assert.AreNotEqual(
                    "not-rendered",
                    grade,
                    $"{scenarioName} claims supported component '{requirement.Component}/{requirement.Target}' as not-rendered.");
                Assert.AreNotEqual(
                    "not-rendered",
                    requirement.MinimumVisualGrade,
                    $"{scenarioName} sets a not-rendered minimum for claimed component '{requirement.Component}/{requirement.Target}'.");
                Assert.IsTrue(
                    ComponentEvidenceBuilder.MeetsMinimumVisualGrade(grade, requirement.MinimumVisualGrade),
                    $"{scenarioName} claims '{requirement.Component}/{requirement.Target}' grade '{grade}' below minimum '{requirement.MinimumVisualGrade}'.");
            }
        }

        Assert.IsGreaterThan(0, audited, "Expected to audit at least one claimed supported component.");
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
        Assert.AreEqual(0, stack.Layout!.Y);
        Assert.AreEqual(800, stack.Layout.Width);
        Assert.AreEqual(24, stack.Children[0].Layout!.Height);
        Assert.AreEqual(32, stack.Children[1].Layout!.Y);
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
    public async Task SkiaV2SnapshotRendererDrawsFluentControlChrome()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "chrome");
        var comboBox = new ComboBox { Name = "StatusComboBox", PlaceholderText = "Status" };
        comboBox.Items.Add("Closed");
        comboBox.SelectedIndex = 0;

        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Control chrome",
            Content = new StackPanel
            {
                Children =
                {
                    new ToggleButton { Name = "PinnedToggleButton", Content = "Pinned", IsChecked = true },
                    new CheckBox { Name = "EnabledCheckBox", Content = "Enabled", IsChecked = true },
                    comboBox,
                    new ProgressBar { Name = "LoadingProgressBar", IsIndeterminate = true },
                    new ProgressRing { Name = "LoadingProgressRing", IsActive = true },
                    new Slider { Name = "VolumeSlider", Minimum = 0, Maximum = 100, Value = 64 },
                    new ToggleSwitch { Name = "EnabledToggleSwitch", Header = "Enabled", IsOn = true },
                    new RatingControl { Name = "QualityRatingControl", MaxRating = 5, Value = 4 },
                    new DropDownButton { Name = "ChoiceDropDownButton", Content = "Choose" },
                    new SplitButton { Name = "ChoiceSplitButton", Content = "Split" },
                    new ToggleSplitButton { Name = "PinnedToggleSplitButton", Content = "Toggle split", IsChecked = true },
                    new InfoBar { Name = "StatusInfoBar", Title = "Complete", Message = "Done", Severity = InfoBarSeverity.Success }
                }
            }
        });
        var theme = SkiaV2Theme.For("light");
        var settings = new VisualRunSettings(null, "chrome", "skia-v2", new VisualViewport(640, 760), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "chrome", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var toggle = RequireNode(arranged.Root, "PinnedToggleButton").Layout!;
        var checkBox = RequireNode(arranged.Root, "EnabledCheckBox").Layout!;
        var combo = RequireNode(arranged.Root, "StatusComboBox").Layout!;
        var progressBar = RequireNode(arranged.Root, "LoadingProgressBar").Layout!;
        var progressRing = RequireNode(arranged.Root, "LoadingProgressRing").Layout!;
        var slider = RequireNode(arranged.Root, "VolumeSlider").Layout!;
        var toggleSwitch = RequireNode(arranged.Root, "EnabledToggleSwitch").Layout!;
        var rating = RequireNode(arranged.Root, "QualityRatingControl").Layout!;
        var toggleSplit = RequireNode(arranged.Root, "PinnedToggleSplitButton").Layout!;
        var infoBar = RequireNode(arranged.Root, "StatusInfoBar").Layout!;

        Assert.IsGreaterThan(100, CountExactPixels(bitmap, new SKRect((float)toggle.X, (float)toggle.Y, (float)(toggle.X + toggle.Width), (float)(toggle.Y + toggle.Height)), theme.Accent));
        Assert.IsGreaterThan(50, CountExactPixels(bitmap, new SKRect((float)checkBox.X + 2, (float)checkBox.Y + 9, (float)checkBox.X + 22, (float)checkBox.Y + 29), theme.Accent));
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)checkBox.X + 2, (float)checkBox.Y + 9, (float)checkBox.X + 22, (float)checkBox.Y + 29), theme.Surface));
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)(combo.X + combo.Width - 28), (float)combo.Y + 14, (float)(combo.X + combo.Width - 8), (float)combo.Y + 26), theme.TextSecondary));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)progressBar.X, (float)(progressBar.Y + progressBar.Height / 2 - 3), (float)(progressBar.X + progressBar.Width), (float)(progressBar.Y + progressBar.Height / 2 + 3)), theme.Accent));
        Assert.IsGreaterThan(10, CountExactPixels(bitmap, new SKRect((float)progressRing.X, (float)progressRing.Y, (float)(progressRing.X + progressRing.Width), (float)(progressRing.Y + progressRing.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)slider.X, (float)slider.Y, (float)(slider.X + slider.Width), (float)(slider.Y + slider.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)toggleSwitch.X, (float)toggleSwitch.Y, (float)(toggleSwitch.X + toggleSwitch.Width), (float)(toggleSwitch.Y + toggleSwitch.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)rating.X, (float)rating.Y, (float)(rating.X + rating.Width), (float)(rating.Y + rating.Height)), theme.Accent));
        Assert.IsGreaterThan(40, CountExactPixels(bitmap, new SKRect((float)toggleSplit.X, (float)toggleSplit.Y, (float)(toggleSplit.X + toggleSplit.Width), (float)(toggleSplit.Y + toggleSplit.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)infoBar.X + 12, (float)infoBar.Y + 14, (float)infoBar.X + 36, (float)infoBar.Y + 38), theme.Success));
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)infoBar.X + 16, (float)infoBar.Y + 18, (float)infoBar.X + 32, (float)infoBar.Y + 34), theme.Surface));
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

    [TestMethod]
    public void ComponentCropperClampsScaledBounds()
    {
        var bounds = ComponentCropper.BoundsFor(
            new UiLayoutBox(
                X: 8.25,
                Y: 6.5,
                Width: 80,
                Height: 40,
                DesiredWidth: 80,
                DesiredHeight: 40,
                Margin: new UiThickness(0, 0, 0, 0),
                Padding: new UiThickness(0, 0, 0, 0),
                HorizontalAlignment: "Stretch",
                VerticalAlignment: "Stretch",
                Visibility: "Visible"),
            imageWidth: 120,
            imageHeight: 80,
            scale: 1.5);

        Assert.IsNotNull(bounds);
        Assert.IsGreaterThanOrEqualTo(0, bounds.X);
        Assert.IsGreaterThanOrEqualTo(0, bounds.Y);
        Assert.IsGreaterThan(0, bounds.Width);
        Assert.IsGreaterThan(0, bounds.Height);
        Assert.IsLessThanOrEqualTo(120, bounds.X + bounds.Width);
        Assert.IsLessThanOrEqualTo(80, bounds.Y + bounds.Height);
    }

    [TestMethod]
    public void ComponentCropperDetectsBlankCrops()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var blank = Path.Combine(directory, "blank.png");
        var patterned = Path.Combine(directory, "patterned.png");

        WriteSolidPng(blank, new SKColor(255, 255, 255));
        WritePatternPng(patterned);

        Assert.IsTrue(ComponentCropper.IsBlankCrop(blank));
        Assert.IsFalse(ComponentCropper.IsBlankCrop(patterned));
    }

    [TestMethod]
    public void ComponentCropperFailsClaimedComponentWithBlankCrop()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimeImage = Path.Combine(directory, "mac-runtime.png");
        WriteSolidPng(runtimeImage, new SKColor(255, 255, 255), width: 240, height: 160);
        var scenario = new VisualScenario
        {
            FixtureName = "crop-test",
            Name = "crop-test-light",
            Requirements = new[]
            {
                new VisualRequirement
                {
                    Component = "Button",
                    Target = "PrimaryButton",
                    ExpectedStatus = CompatibilityStatuses.Supported,
                    MinimumVisualGrade = "usable",
                    VisualGrade = "usable",
                    ComponentThresholds = new VisualThresholds
                    {
                        ChangedPixelPercentage = 5,
                        MeanAbsoluteError = 2,
                        RootMeanSquaredError = 4
                    }
                }
            }
        };
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new Button { Name = "PrimaryButton", Content = "Continue" }
        });
        var settings = new VisualRunSettings(scenario, scenario.Name, "skia-v2", new VisualViewport(240, 160), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var evidence = ComponentEvidenceBuilder.Build(scenario, arranged, interactions: null, metrics: null);

        var withCrops = ComponentCropper.WriteCrops(evidence, runtimeImage, referenceImagePath: null, directory, scale: 1, settings.Thresholds);

        Assert.AreEqual("failed", withCrops.Status);
        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.AreEqual("failed", crop.Status);
        Assert.IsTrue(crop.RuntimeBlank);
        Assert.IsTrue(File.Exists(crop.MacRuntimePath));
        Assert.AreEqual(5, crop.Thresholds.ChangedPixelPercentage);
    }

    [TestMethod]
    public void ComponentCropperAttachesNativeReferenceProvenance()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimeImage = Path.Combine(directory, "mac-runtime.png");
        var referenceImage = Path.Combine(directory, "windows-reference.png");
        WritePatternPng(runtimeImage);
        WritePatternPng(referenceImage);
        var scenario = new VisualScenario
        {
            FixtureName = "crop-test",
            Name = "crop-test-light",
            Requirements = new[]
            {
                new VisualRequirement
                {
                    Component = "Button",
                    Target = "PrimaryButton",
                    ExpectedStatus = CompatibilityStatuses.Supported,
                    MinimumVisualGrade = "usable",
                    VisualGrade = "usable"
                }
            }
        };
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new Button { Name = "PrimaryButton", Content = "Continue" }
        });
        var settings = new VisualRunSettings(scenario, scenario.Name, "skia-v2", new VisualViewport(240, 160), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var evidence = ComponentEvidenceBuilder.Build(scenario, arranged, interactions: null, metrics: null);
        var provenance = TestNativeReferenceProvenance();

        var withCrops = ComponentCropper.WriteCrops(
            evidence,
            runtimeImage,
            referenceImage,
            directory,
            scale: 1,
            settings.Thresholds,
            provenance);

        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.IsNotNull(crop.NativeReferenceProvenance);
        Assert.AreEqual("native-winui", crop.NativeReferenceProvenance.ReferenceSource);
        Assert.AreEqual("26777029415", crop.NativeReferenceProvenance.WorkflowRunId);
        Assert.AreEqual("fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json", crop.NativeReferenceProvenance.ScenarioPath);
    }

    [TestMethod]
    public void ComponentCropperCanWriteRelativeArtifactPaths()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimeImage = Path.Combine(directory, "mac-runtime.png");
        var referenceImage = Path.Combine(directory, "windows-reference.png");
        WritePatternPng(runtimeImage);
        WritePatternPng(referenceImage);
        var thresholds = new VisualThresholds();
        var evidence = new ComponentEvidenceDocument(
            SchemaVersion: ArtifactSchemas.ComponentEvidence,
            FixtureName: "crop-test",
            ScenarioName: "crop-test-light",
            Components: new[]
            {
                new ComponentEvidenceEntry(
                    Component: "Button",
                    Kind: "control",
                    Target: "PrimaryButton",
                    LayoutRegion: new UiLayoutBox(
                        X: 0,
                        Y: 0,
                        Width: 8,
                        Height: 8,
                        DesiredWidth: 8,
                        DesiredHeight: 8,
                        Margin: new UiThickness(0, 0, 0, 0),
                        Padding: new UiThickness(0, 0, 0, 0),
                        HorizontalAlignment: "stretch",
                        VerticalAlignment: "stretch",
                        Visibility: "visible"),
                    CatalogStatus: "supported",
                    Presence: "present",
                    InteractionStatus: "passed",
                    VisualGrade: "usable",
                    ComponentThresholds: null,
                    ChangedPixelPercentage: null,
                    MeanAbsoluteError: null,
                    RootMeanSquaredError: null,
                    Crop: null,
                    NativeQualityGrade: "not-evaluated",
                    Inspection: null,
                    KnownGaps: Array.Empty<string>())
            },
            SourceFeatures: Array.Empty<SourceFeatureEvidenceEntry>(),
            Status: "passed");

        var withCrops = ComponentCropper.WriteCrops(
            evidence,
            runtimeImage,
            referenceImage,
            directory,
            scale: 1,
            thresholds,
            nativeReferenceProvenance: null,
            useRelativePaths: true);

        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.AreEqual("components/button-primarybutton/windows-reference.png", crop.NativeReferencePath);
        Assert.AreEqual("components/button-primarybutton/mac-runtime.png", crop.MacRuntimePath);
        Assert.AreEqual("components/button-primarybutton/pixel-diff.png", crop.PixelDiffPath);
        var macRuntimePath = crop.MacRuntimePath ?? throw new AssertFailedException("Expected macOS runtime crop path.");
        Assert.IsTrue(File.Exists(Path.Combine(directory, macRuntimePath)));
    }

    [TestMethod]
    public void VisualReviewArtifactsWritesSideBySideCropPage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-review-tests", Guid.NewGuid().ToString("N"));
        var cropsDirectory = Path.Combine(directory, "components", "button-primaryactionbutton");
        Directory.CreateDirectory(cropsDirectory);
        var nativeCrop = Path.Combine(cropsDirectory, "windows-reference.png");
        var runtimeCrop = Path.Combine(cropsDirectory, "mac-runtime.png");
        var diffCrop = Path.Combine(cropsDirectory, "pixel-diff.png");
        WriteSolidPng(nativeCrop, new SKColor(250, 250, 250), width: 16, height: 12);
        WritePatternPng(runtimeCrop);
        WriteSolidPng(diffCrop, new SKColor(255, 0, 0), width: 16, height: 12);

        var thresholds = new VisualThresholds
        {
            ChangedPixelPercentage = 5,
            MeanAbsoluteError = 2,
            RootMeanSquaredError = 4
        };
        var evidence = new ComponentEvidenceDocument(
            SchemaVersion: ArtifactSchemas.ComponentEvidence,
            FixtureName: "review-test",
            ScenarioName: "review-test-light",
            Components: new[]
            {
                new ComponentEvidenceEntry(
                    Component: "Button",
                    Kind: "control",
                    Target: "PrimaryActionButton",
                    LayoutRegion: new UiLayoutBox(
                        X: 0,
                        Y: 0,
                        Width: 16,
                        Height: 12,
                        DesiredWidth: 16,
                        DesiredHeight: 12,
                        Margin: new UiThickness(0, 0, 0, 0),
                        Padding: new UiThickness(0, 0, 0, 0),
                        HorizontalAlignment: "stretch",
                        VerticalAlignment: "stretch",
                        Visibility: "visible"),
                    CatalogStatus: "supported",
                    Presence: "present",
                    InteractionStatus: "passed",
                    VisualGrade: "good",
                    ComponentThresholds: thresholds,
                    ChangedPixelPercentage: 1.25,
                    MeanAbsoluteError: 0.5,
                    RootMeanSquaredError: 0.75,
                    Crop: new ComponentCropEvidence(
                        Status: "passed",
                        Bounds: new ComponentCropBounds(0, 0, 16, 12),
                        NativeReferencePath: nativeCrop,
                        MacRuntimePath: runtimeCrop,
                        PixelDiffPath: diffCrop,
                        RuntimeBlank: false,
                        Thresholds: thresholds,
                        ChangedPixelPercentage: 1.25,
                        MeanAbsoluteError: 0.5,
                        RootMeanSquaredError: 0.75,
                        Message: "Component crop passed.")
                    {
                        NativeReferenceProvenance = TestNativeReferenceProvenance()
                    },
                    NativeQualityGrade: "good",
                    Inspection: null,
                    KnownGaps: new[] { "Manual inspection is pending for the generated crop triptych." })
            },
            SourceFeatures: Array.Empty<SourceFeatureEvidenceEntry>(),
            Status: "passed");
        var evidencePath = Path.Combine(directory, "component-evidence.json");
        File.WriteAllText(evidencePath, JsonSerializer.Serialize(evidence, JsonDefaults.Options));

        var review = VisualReviewArtifacts.Write(evidencePath, directory);

        Assert.AreEqual(1, review.Summary.ComponentCount);
        Assert.AreEqual(1, review.Summary.CompleteTriptychCount);
        Assert.AreEqual(1, review.Summary.MissingInspectionNotes);
        Assert.IsTrue(File.Exists(Path.Combine(directory, "visual-review.html")));
        Assert.IsTrue(File.Exists(Path.Combine(directory, "visual-review.json")));

        var html = File.ReadAllText(Path.Combine(directory, "visual-review.html"));
        StringAssert.Contains(html, "Native WinUI reference");
        StringAssert.Contains(html, "macOS runtime");
        StringAssert.Contains(html, "Pixel diff");
        StringAssert.Contains(html, "ready-for-manual-inspection");
        StringAssert.Contains(html, "native-winui");
        StringAssert.Contains(html, "26777029415");

        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "visual-review.json")));
        Assert.AreEqual(ArtifactSchemas.VisualReview, json.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual(".", json.RootElement.GetProperty("outputDirectory").GetString());
        Assert.AreEqual("visual-review.html", json.RootElement.GetProperty("htmlPath").GetString());
        Assert.AreEqual(1, json.RootElement.GetProperty("rows").GetArrayLength());
        var row = json.RootElement.GetProperty("rows")[0];
        Assert.AreEqual("native-winui", row.GetProperty("referenceSource").GetString());
        Assert.AreEqual("26777029415", row.GetProperty("nativeReferenceRunId").GetString());
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

    private static void AssertCorpusEntry(
        CorpusInventoryDocument inventory,
        string kind,
        string construct,
        string expectedStatus)
    {
        var entry = inventory.Entries.SingleOrDefault(item => item.Kind == kind && item.Construct == construct)
            ?? throw new AssertFailedException($"Corpus inventory is missing '{kind} {construct}'.");
        Assert.AreEqual(expectedStatus, entry.Status);
    }

    private static void AssertMatchesBaseline(string baselinePath, string generatedJson)
    {
        Assert.IsTrue(File.Exists(baselinePath), $"Missing tracked corpus baseline '{baselinePath}'.");
        var committed = File.ReadAllText(baselinePath).Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
        var generated = generatedJson.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
        Assert.AreEqual(
            committed,
            generated,
            $"Corpus baseline '{Path.GetFileName(baselinePath)}' drifted; run `ingest --write-baseline` after review.");
    }

    private static void WriteSolidPng(string path, SKColor color, int width = 4, int height = 4)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static void WritePatternPng(string path)
    {
        using var bitmap = new SKBitmap(8, 8);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(255, 255, 255));
        using var paint = new SKPaint { Color = new SKColor(37, 98, 217) };
        canvas.DrawRect(new SKRect(2, 2, 6, 6), paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static ComponentEvidenceDocument TestInspectableEvidence()
    {
        var thresholds = new VisualThresholds
        {
            ChangedPixelPercentage = 5,
            MeanAbsoluteError = 2,
            RootMeanSquaredError = 4
        };
        return new ComponentEvidenceDocument(
            SchemaVersion: ArtifactSchemas.ComponentEvidence,
            FixtureName: "inspection-test",
            ScenarioName: "inspection-test-light",
            Components: new[]
            {
                new ComponentEvidenceEntry(
                    Component: "Button",
                    Kind: "control",
                    Target: "PrimaryButton",
                    LayoutRegion: new UiLayoutBox(
                        X: 0,
                        Y: 0,
                        Width: 8,
                        Height: 8,
                        DesiredWidth: 8,
                        DesiredHeight: 8,
                        Margin: new UiThickness(0, 0, 0, 0),
                        Padding: new UiThickness(0, 0, 0, 0),
                        HorizontalAlignment: "stretch",
                        VerticalAlignment: "stretch",
                        Visibility: "visible"),
                    CatalogStatus: "supported",
                    Presence: "present",
                    InteractionStatus: "passed",
                    VisualGrade: "usable",
                    ComponentThresholds: thresholds,
                    ChangedPixelPercentage: 1.25,
                    MeanAbsoluteError: 0.5,
                    RootMeanSquaredError: 0.75,
                    Crop: new ComponentCropEvidence(
                        Status: "passed",
                        Bounds: new ComponentCropBounds(0, 0, 8, 8),
                        NativeReferencePath: "components/button-primarybutton/windows-reference.png",
                        MacRuntimePath: "components/button-primarybutton/mac-runtime.png",
                        PixelDiffPath: "components/button-primarybutton/pixel-diff.png",
                        RuntimeBlank: false,
                        Thresholds: thresholds,
                        ChangedPixelPercentage: 1.25,
                        MeanAbsoluteError: 0.5,
                        RootMeanSquaredError: 0.75,
                        Message: "Component crop passed.")
                    {
                        NativeReferenceProvenance = TestNativeReferenceProvenance()
                    },
                    NativeQualityGrade: "not-evaluated",
                    Inspection: null,
                    KnownGaps: Array.Empty<string>())
            },
            SourceFeatures: Array.Empty<SourceFeatureEvidenceEntry>(),
            Status: "passed");
    }

    private static NativeReferenceProvenance TestNativeReferenceProvenance()
    {
        return new NativeReferenceProvenance(
            ReferenceSource: "native-winui",
            FixtureProjectPath: "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            ScenarioPath: "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            ScenarioName: "component-basic-input-light",
            CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
            WorkflowRunId: "26777029415",
            RunnerImage: "win25 20260525.149.1",
            WindowsAppSdkVersion: null,
            Viewport: new VisualViewport(1028, 720),
            Scale: 1,
            Theme: "light",
            CaptureMode: "client-area",
            Dimensions: new ReferenceImageDimensions(1028, 720),
            CapturedAt: "2026-06-01T19:31:04.2512607+00:00");
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

    private static Dictionary<string, int> CountBy(
        IEnumerable<CompatibilityCatalogEntry> entries,
        Func<CompatibilityCatalogEntry, string> selector)
    {
        return entries
            .GroupBy(selector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    private static void AssertCountsEqual(
        IReadOnlyDictionary<string, int> expected,
        JsonElement actual,
        string label)
    {
        var actualCounts = actual.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetInt32(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            expected.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray(),
            actualCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray(),
            $"Visual readiness inventory {label} counts must match the compatibility catalog.");
    }

    private static int SumObjectCounts(JsonElement element)
    {
        return element.EnumerateObject().Sum(property => property.Value.GetInt32());
    }

    private static string RequireNonEmptyString(JsonElement element, string propertyName)
    {
        var value = element.GetProperty(propertyName).GetString();

        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Expected '{propertyName}' to be set.");
        return value;
    }

    private static bool ContainsCatalogTotal(string text, int total)
    {
        return Regex.IsMatch(text, $@"\b{total}\s+entries\b", RegexOptions.CultureInvariant) ||
            Regex.IsMatch(text, $@"Total catalog entries:\s+\*\*{total}\*\*", RegexOptions.CultureInvariant) ||
            Regex.IsMatch(text, $@"\*\*{total}/{total}\*\*", RegexOptions.CultureInvariant);
    }

    private static bool ContainsCatalogStatusCount(string text, string status, int count)
    {
        var escapedStatus = Regex.Escape(status);

        return Regex.IsMatch(text, $@"\|\s*`{escapedStatus}`\s*\|\s*{count}\s*\|", RegexOptions.CultureInvariant) ||
            Regex.IsMatch(text, $@"\b{count}\s+`{escapedStatus}`", RegexOptions.CultureInvariant);
    }

    private static string NormalizeArtifact(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot(), relativePath);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinUI3.MacTestRuntime.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static UiNode RequireNode(UiNode root, string name)
    {
        if (string.Equals(root.Name, name, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = FindNode(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        throw new AssertFailedException($"Expected arranged tree to contain '{name}'.");
    }

    private static UiNode? FindNode(UiNode root, string name)
    {
        if (string.Equals(root.Name, name, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = FindNode(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static int CountExactPixels(SKBitmap bitmap, SKRect rect, SKColor color)
    {
        var left = Math.Clamp((int)Math.Floor(rect.Left), 0, bitmap.Width);
        var top = Math.Clamp((int)Math.Floor(rect.Top), 0, bitmap.Height);
        var right = Math.Clamp((int)Math.Ceiling(rect.Right), left, bitmap.Width);
        var bottom = Math.Clamp((int)Math.Ceiling(rect.Bottom), top, bitmap.Height);
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (bitmap.GetPixel(x, y) == color)
                {
                    count++;
                }
            }
        }

        return count;
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
