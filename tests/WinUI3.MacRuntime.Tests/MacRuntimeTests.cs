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
    public void DownstreamProductionXamlGapSummaryTracksSanitizedBaseline()
    {
        var summary = DownstreamXamlGapSummary.Load(RepositoryPath("docs/compatibility/downstream-production-xaml-gap-summary.json"));

        Assert.AreEqual("2026-06-06", summary.BaselineDate);
        Assert.AreEqual(121, summary.TotalDiagnostics);
        Assert.AreEqual(35, summary.SurfaceFamilies.Count);
        Assert.IsTrue(summary.SurfaceFamilies.All(family => !family.Surface.Contains("/Users/", StringComparison.Ordinal)));
        Assert.IsTrue(summary.SurfaceFamilies.Any(family =>
            family.Surface == "Grid.RowDefinitions" &&
            family.Count == 16 &&
            family.CurrentTreatment == "layout gap"));

        var diagnostics = new[]
        {
            new DownstreamXamlDiagnosticRecord(
                "XAML1002",
                "Unsupported XAML property 'Grid.RowDefinitions' is cataloged as planned.",
                "Error",
                "/Users/private/apps/windows/src/MeetingChallenge.Windows/HomePage.xaml",
                12,
                8),
            new DownstreamXamlDiagnosticRecord(
                "XAML1005",
                "Unsupported attached property 'TextBlock.Grid.Row' is not present in the WinUI compatibility catalog.",
                "Error",
                "/Users/private/apps/windows/src/MeetingChallenge.Windows/AdminPage.xaml",
                24,
                10)
        };

        var generated = DownstreamXamlGapSummary.FromDiagnostics(
            diagnostics,
            DownstreamXamlGapSummary.DefaultFileCategoryClassifier);

        Assert.AreEqual(2, generated.TotalDiagnostics);
        Assert.IsTrue(generated.FileCategories.Any(category => category.Category == "home-read-surface" && category.Count == 1));
        Assert.IsTrue(generated.FileCategories.Any(category => category.Category == "admin-workbench" && category.Count == 1));
        Assert.IsTrue(generated.SurfaceFamilies.All(family => !family.Surface.Contains("/Users/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void DownstreamWindowsConsumptionDocsPublishNativeComparisonGate()
    {
        var docs = File.ReadAllText(RepositoryPath("docs/consumption/downstream-windows-apps.md"));

        StringAssert.Contains(docs, "winui3-mac-runner-downstream-windows-probe-sweep");
        StringAssert.Contains(docs, "--require-native-comparison");
        StringAssert.Contains(docs, "native comparison is required");
        StringAssert.Contains(docs, "artifacts/native-reference-import");
    }

    [TestMethod]
    public void TreeBuilderExportsProductionLayoutSizingAndScrollProperties()
    {
        var grid = new Grid
        {
            Name = "ReadSurfaceGrid",
            RowDefinitions = "Auto,*",
            RowSpacing = 12,
            Padding = "16",
            MinHeight = 240,
            MaxWidth = 720
        };
        var title = new TextBlock { Name = "TitleText", Text = "Production-like row" };
        Grid.SetRow(title, 1);
        Grid.SetColumnSpan(title, 2);
        grid.Children.Add(title);

        var scrollViewer = new ScrollViewer
        {
            Name = "ReadSurfaceScroll",
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new Border
            {
                Name = "CardBorder",
                Padding = "20,12",
                BorderThickness = "1",
                BorderBrush = "CardStrokeBrush",
                MaxWidth = 640,
                Child = grid
            }
        };

        var tree = UiTreeBuilder.Build(new Window { Content = scrollViewer });
        var scrollNode = tree.Root.Children.Single(node => node.Name == "ReadSurfaceScroll");
        var borderNode = scrollNode.Children.Single(node => node.Name == "CardBorder");
        var gridNode = borderNode.Children.Single(node => node.Name == "ReadSurfaceGrid");
        var titleNode = gridNode.Children.Single(node => node.Name == "TitleText");

        Assert.AreEqual("Disabled", scrollNode.Properties["horizontalScrollBarVisibility"]);
        Assert.AreEqual("20,12", borderNode.Properties["padding"]);
        Assert.AreEqual("1", borderNode.Properties["borderThickness"]);
        Assert.AreEqual("CardStrokeBrush", borderNode.Properties["borderBrush"]);
        Assert.AreEqual(640d, borderNode.Properties["maxWidth"]);
        Assert.AreEqual("Auto,*", gridNode.Properties["rowDefinitions"]);
        Assert.AreEqual(12d, gridNode.Properties["rowSpacing"]);
        Assert.AreEqual("16", gridNode.Properties["padding"]);
        Assert.AreEqual(240d, gridNode.Properties["minHeight"]);
        Assert.AreEqual(720d, gridNode.Properties["maxWidth"]);
        Assert.AreEqual(1, titleNode.Properties["gridRow"]);
        Assert.AreEqual(2, titleNode.Properties["gridColumnSpan"]);
    }

    [TestMethod]
    public void TreeAndAccessibilityProtectPasswordTextAndExportMultilineMetadata()
    {
        var passwordBox = new PasswordBox
        {
            Name = "SecretBox",
            Password = "not-a-real-secret",
            PlaceholderText = "Password",
            Header = "Account password"
        };
        var notesBox = new TextBox
        {
            Name = "NotesBox",
            Text = "Line one",
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            MinHeight = 96
        };
        var window = new Window
        {
            Content = new StackPanel
            {
                Children =
                {
                    passwordBox,
                    notesBox
                }
            }
        };

        var tree = UiTreeBuilder.Build(window);
        var passwordNode = tree.Root.Children[0].Children.Single(node => node.Name == "SecretBox");
        var notesNode = tree.Root.Children[0].Children.Single(node => node.Name == "NotesBox");
        var accessibility = AccessibilityTreeBuilder.Build(tree);
        var passwordAccessibility = accessibility.Root.Children[0].Children.Single(node => node.Name == "SecretBox");

        Assert.IsFalse(passwordNode.Properties.ContainsKey("password"));
        Assert.AreEqual(17, passwordNode.Properties["passwordLength"]);
        Assert.AreEqual(true, passwordNode.Properties["isPassword"]);
        Assert.AreEqual("Password", passwordNode.Properties["placeholderText"]);
        Assert.AreEqual("Account password", passwordNode.Properties["header"]);
        Assert.AreEqual("Wrap", notesNode.Properties["textWrapping"]);
        Assert.AreEqual(true, notesNode.Properties["acceptsReturn"]);
        Assert.AreEqual(96d, notesNode.Properties["minHeight"]);
        Assert.AreEqual("passwordbox", passwordAccessibility.Role);
        Assert.AreEqual("Account password", passwordAccessibility.Label);
        Assert.AreEqual("********", passwordAccessibility.Value);
    }

    [TestMethod]
    public void DownstreamWindowsProbePublishesProductionLikePageScenarios()
    {
        var probeRoot = Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe"));
        var project = File.ReadAllText(Path.Combine(probeRoot, "MeetingChallenge.WinUI.MacRuntimeProbe.csproj"));
        var windowCode = File.ReadAllText(Path.Combine(probeRoot, "MainWindow.xaml.cs"));
        var scenarioDirectory = Path.Combine(probeRoot, "scenarios");

        foreach (var page in new[]
        {
            "MessagesProbePage",
            "AdminWorkbenchProbePage",
            "StatusStatesProbePage",
            "SettingsProfileProbePage"
        })
        {
            StringAssert.Contains(project, $"Pages\\{page}.xaml");
            Assert.IsTrue(File.Exists(Path.Combine(probeRoot, "Pages", page + ".xaml")));
            Assert.IsTrue(File.Exists(Path.Combine(probeRoot, "Pages", page + ".xaml.cs")));
        }

        foreach (var tag in new[] { "\"messages\"", "\"admin-workbench\"", "\"status-states\"", "\"settings-profile\"" })
        {
            StringAssert.Contains(windowCode, tag);
        }

        foreach (var scenario in new[]
        {
            "login-light.json",
            "messages-multiline-light.json",
            "admin-dashboard-light.json",
            "admin-workbench-light.json",
            "command-search-light.json",
            "status-states-light.json",
            "settings-profile-light.json"
        })
        {
            Assert.IsTrue(File.Exists(Path.Combine(scenarioDirectory, scenario)), scenario);
        }
    }

    [TestMethod]
    public void DownstreamStatusProbeCoversLoadingErrorSuccessWarningAndClosedStates()
    {
        var statusPage = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "Pages",
            "StatusStatesProbePage.xaml")));
        var scenario = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "scenarios",
            "status-states-light.json")));

        foreach (var required in new[]
        {
            "LoadingInfoBar",
            "ErrorInfoBar",
            "SuccessInfoBar",
            "WarningInfoBar",
            "ClosedInfoBar",
            "LoadingProgressRing",
            "SyncProgressBar"
        })
        {
            StringAssert.Contains(statusPage, required);
            StringAssert.Contains(scenario, required);
        }

        StringAssert.Contains(statusPage, "Severity=\"Success\"");
        StringAssert.Contains(statusPage, "Severity=\"Warning\"");
        StringAssert.Contains(statusPage, "IsOpen=\"False\"");
        StringAssert.Contains(statusPage, "ProgressBar");
    }

    [TestMethod]
    public void DownstreamHomeProbeCoversReadSurfaceListDetailAndProgress()
    {
        var homePage = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "Pages",
            "HomeProbePage.xaml")));
        var scenario = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "scenarios",
            "shell-staff-light.json")));

        foreach (var required in new[]
        {
            "HomeReadListView",
            "HomeDetailBorder",
            "HomePrimaryActionButton",
            "HomeReadProgressBar",
            "HomeStatusInfoBar",
            "HomeSummaryTextBlock"
        })
        {
            StringAssert.Contains(homePage, required);
            StringAssert.Contains(scenario, required);
        }

        StringAssert.Contains(homePage, "ColumnDefinitions=\"320,16,*\"");
        StringAssert.Contains(homePage, "ListView");
        StringAssert.Contains(homePage, "ProgressBar");
        StringAssert.Contains(homePage, "BorderBrush");
    }

    [TestMethod]
    public void DownstreamMessagesProbeCoversConversationListDetailComposerAndSend()
    {
        var messagesPage = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "Pages",
            "MessagesProbePage.xaml")));
        var scenario = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "scenarios",
            "messages-multiline-light.json")));

        foreach (var required in new[]
        {
            "ConversationListView",
            "MessageDetailBorder",
            "MessageThreadTitleTextBlock",
            "MessageThreadStatusInfoBar",
            "MessageComposerTextBox",
            "SendMessageButton"
        })
        {
            StringAssert.Contains(messagesPage, required);
            StringAssert.Contains(scenario, required);
        }

        StringAssert.Contains(messagesPage, "ColumnDefinitions=\"300,16,*\"");
        StringAssert.Contains(messagesPage, "AcceptsReturn=\"True\"");
        StringAssert.Contains(messagesPage, "ListView");
        StringAssert.Contains(messagesPage, "BorderBrush");
    }

    [TestMethod]
    public void DownstreamLoginProbeCoversFormValidationLoadingAndPasswordSafety()
    {
        var loginPage = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "Pages",
            "LoginProbePage.xaml")));
        var scenario = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "scenarios",
            "login-light.json")));

        foreach (var required in new[]
        {
            "LoginStatusInfoBar",
            "UsernameBox",
            "PasswordBox",
            "RememberDeviceCheckBox",
            "LoginProgressBar",
            "SignInButton"
        })
        {
            StringAssert.Contains(loginPage, required);
            StringAssert.Contains(scenario, required);
        }

        StringAssert.Contains(loginPage, "Password=\"not-a-real-secret\"");
        StringAssert.Contains(scenario, "PasswordBox");
        Assert.IsFalse(scenario.Contains("not-a-real-secret", StringComparison.Ordinal));
        StringAssert.Contains(loginPage, "ProgressBar");
        StringAssert.Contains(loginPage, "CheckBox");
    }

    [TestMethod]
    public void DownstreamAdminProbeCoversDashboardListDetailAndActions()
    {
        var adminPage = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "Pages",
            "AdminProbePage.xaml")));
        var scenario = File.ReadAllText(Path.GetFullPath(Path.Combine(
            RepositoryPath("."),
            "..",
            "..",
            "apps",
            "windows",
            "tests",
            "MeetingChallenge.WinUI.MacRuntimeProbe",
            "scenarios",
            "admin-dashboard-light.json")));

        foreach (var required in new[]
        {
            "AdminQueueListView",
            "AdminDetailBorder",
            "AdminKpiProgressBar",
            "AdminApproveButton",
            "AdminEscalateButton",
            "AdminStatusInfoBar"
        })
        {
            StringAssert.Contains(adminPage, required);
            StringAssert.Contains(scenario, required);
        }

        StringAssert.Contains(adminPage, "ColumnDefinitions=\"320,16,*\"");
        StringAssert.Contains(adminPage, "ProgressBar");
        StringAssert.Contains(adminPage, "BorderBrush");
        StringAssert.Contains(adminPage, "Button");
    }

    [TestMethod]
    public void DownstreamWindowsProbePublishesScenarioSweepScript()
    {
        var scriptPath = RepositoryPath("tools/winui3-mac-runner-downstream-windows-probe-sweep");
        Assert.IsTrue(File.Exists(scriptPath), scriptPath);
        var script = File.ReadAllText(scriptPath);

        foreach (var scenario in new[]
        {
            "login-light.json",
            "shell-staff-light.json",
            "messages-multiline-light.json",
            "admin-dashboard-light.json",
            "admin-workbench-light.json",
            "command-search-light.json",
            "status-states-light.json",
            "settings-profile-light.json"
        })
        {
            StringAssert.Contains(script, scenario);
        }

        StringAssert.Contains(script, "strict-visual");
        StringAssert.Contains(script, "summary.json");
        StringAssert.Contains(script, "summary.md");
        StringAssert.Contains(script, "review.html");
        StringAssert.Contains(script, "<img");
        StringAssert.Contains(script, "nativeComparison");
        StringAssert.Contains(script, "No Windows reference image was provided.");
        StringAssert.Contains(script, "Native comparison exceeded conservative pixel thresholds.");
        StringAssert.Contains(script, "--reference)");
        StringAssert.Contains(script, "scenario_reference=\"$reference\"");
        StringAssert.Contains(script, "--require-native-comparison)");
        StringAssert.Contains(script, "require_native_comparison");
        StringAssert.Contains(script, "\\\"required\\\": $require_native_comparison");
        StringAssert.Contains(script, "native comparison is required");
        StringAssert.Contains(script, "referenceReadiness");
        StringAssert.Contains(script, "missingReferenceCount");
        StringAssert.Contains(script, "missingReferences");
        StringAssert.Contains(script, "Reference readiness");
        StringAssert.Contains(script, "runtimeImageIntegrity");
        StringAssert.Contains(script, "distinctColorCount");
        StringAssert.Contains(script, "nonBackgroundPixelPercentage");
        StringAssert.Contains(script, "Image integrity");
        StringAssert.Contains(script, "imageIntegrityWarning");
        StringAssert.Contains(script, "lowContentWarning");
        StringAssert.Contains(script, "imageSizeWarning");
        StringAssert.Contains(script, "expectedViewport");
        StringAssert.Contains(script, "actualImageSize");
        StringAssert.Contains(script, "scenarioCoverage");
        StringAssert.Contains(script, "requiredScenarioGroups");
        StringAssert.Contains(script, "read-surface");
        StringAssert.Contains(script, "command-search");
        StringAssert.Contains(script, "coverageComplete");
        StringAssert.Contains(script, "missingScenarioGroups");
        StringAssert.Contains(script, "expected_scenario_groups");
        StringAssert.Contains(script, "coverage is incomplete");
        StringAssert.Contains(script, "interactionSummary");
        StringAssert.Contains(script, "assertPropertyCount");
        StringAssert.Contains(script, "interactionMissing");
        StringAssert.Contains(script, "route anchor");
        StringAssert.Contains(script, "artifactSummary");
        StringAssert.Contains(script, "missingArtifactCount");
        StringAssert.Contains(script, "artifactMissing");
        StringAssert.Contains(script, "artifact completeness");
        StringAssert.Contains(script, "fontSummary");
        StringAssert.Contains(script, "fontProvenance");
        StringAssert.Contains(script, "textResolvedSource");
        StringAssert.Contains(script, "symbolResolvedSource");
        StringAssert.Contains(script, "font provenance");
        StringAssert.Contains(script, "--windows-screenshot-dir");
        StringAssert.Contains(script, "windowsScreenshotSummary");
        StringAssert.Contains(script, "windowsScreenshotMatched");
        StringAssert.Contains(script, "Windows runner screenshot");
        StringAssert.Contains(script, "windows-reference.png");
        StringAssert.Contains(script, "scenario_reference=\"$windows_screenshot_dir/$scenario_name/windows-reference.png\"");
        StringAssert.Contains(script, "scenario_reference_viewport=\"$(reference_viewport \"$scenario_reference\")\"");
        StringAssert.Contains(script, "--viewport \"$scenario_reference_viewport\"");
        StringAssert.Contains(script, "--reference \"$scenario_reference\"");
        StringAssert.Contains(script, "\"windowsReference\":\"$scenario_output/visual/windows-reference.png\"");
        StringAssert.Contains(script, "\"macRuntime\":\"$scenario_output/visual/mac-runtime.png\"");
        StringAssert.Contains(script, "\"pixelDiffImage\":\"$scenario_output/visual/pixel-diff.png\"");
        StringAssert.Contains(script, "\"pixelDiff\":\"$scenario_output/visual/pixel-diff.json\"");
        StringAssert.Contains(script, "windows-reference.png</a>");
        StringAssert.Contains(script, "pixel-diff.png</a>");
    }

    [TestMethod]
    public void DownstreamNativeVisualParityAuditTracksEightScenarioBaseline()
    {
        var audit = DownstreamNativeVisualParityAudit.Load(
            RepositoryPath("docs/visual-parity/downstream-native-visual-parity-audit.json"));

        Assert.AreEqual("2026-06-08", audit.AuditDate);
        Assert.AreEqual("2026-06-06", audit.ReferenceCaptureDate);
        Assert.AreEqual("png", audit.EvidenceFormat);
        Assert.AreEqual(960, audit.Viewport.Width);
        Assert.AreEqual(640, audit.Viewport.Height);

        var expectedOrder = new[]
        {
            "login-light",
            "status-states-light",
            "messages-multiline-light",
            "shell-staff-light",
            "admin-dashboard-light",
            "admin-workbench-light",
            "command-search-light",
            "settings-profile-light"
        };
        CollectionAssert.AreEqual(
            expectedOrder,
            audit.Scenarios.OrderBy(scenario => scenario.Priority).Select(scenario => scenario.Scenario).ToArray());

        foreach (var scenario in audit.Scenarios)
        {
            Assert.AreEqual(960, scenario.Width, scenario.Scenario);
            Assert.AreEqual(640, scenario.Height, scenario.Scenario);
            Assert.AreEqual("failed", scenario.Baseline.ThresholdStatus, scenario.Scenario);
            Assert.AreEqual("L0", scenario.Baseline.LadderLevel, scenario.Scenario);
            Assert.AreEqual("passed", scenario.Baseline.ArtifactStatus, scenario.Scenario);
            Assert.AreEqual("passed", scenario.Baseline.FontProvenanceStatus, scenario.Scenario);
            Assert.AreEqual("passed", scenario.Baseline.ImageIntegrityStatus, scenario.Scenario);
        }

        var login = audit.Scenarios.Single(scenario => scenario.Scenario == "login-light");
        Assert.AreEqual(1, login.Priority);
        AssertMetricClose(97.911133d, login.Baseline.ChangedPixelPercentage);
        AssertMetricClose(7.169520d, login.Baseline.MeanAbsoluteError);
        AssertMetricClose(21.312369d, login.Baseline.RootMeanSquaredError);

        var dashboard = audit.Scenarios.Single(scenario => scenario.Scenario == "admin-dashboard-light");
        AssertMetricClose(8.055549d, dashboard.Baseline.MeanAbsoluteError);
        AssertMetricClose(27.656882d, dashboard.Baseline.RootMeanSquaredError);

        // The whole audit must stay sanitized and environment-agnostic: no private home paths and
        // no machine-specific absolute evidence paths leak into the checked-in manifest.
        var serialized = JsonSerializer.Serialize(audit, JsonDefaults.Options);
        Assert.IsFalse(serialized.Contains("/Users/", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("/private/tmp", StringComparison.Ordinal));

        CollectionAssert.AreEquivalent(
            new[] { "L0", "L1", "L2", "L3", "L4", "L5" },
            audit.ThresholdLadder.Select(level => level.Ladder).ToArray());

        Assert.IsTrue(audit.SharedGaps.Any(gap => gap.Category == "Layout"));
        Assert.IsTrue(audit.SharedGaps.Any(gap => gap.Category == "Typography"));
        Assert.IsTrue(audit.SharedGaps.Any(gap => gap.Category == "Control chrome"));

        // The ladder classifier is the reusable engine the Phase 7 ratchet depends on.
        Assert.AreEqual(
            "L0",
            DownstreamNativeVisualParityAudit.ClassifyLadder(97.911133d, 7.169520d, 21.312369d));
        Assert.AreEqual("L1", DownstreamNativeVisualParityAudit.ClassifyLadder(88d, 11d, 35d));
        Assert.AreEqual("L2", DownstreamNativeVisualParityAudit.ClassifyLadder(68d, 9d, 31d));
        Assert.AreEqual("L4", DownstreamNativeVisualParityAudit.ClassifyLadder(40d, 7d, 27d));
        // Broad app-route L5 thresholds (<=35 / <=6.5 / <=24).
        Assert.AreEqual("L5", DownstreamNativeVisualParityAudit.ClassifyLadder(30d, 6d, 23d));
        // The same metrics judged against the tighter focused L5 thresholds (<=24 / <=5.5 / <=20)
        // only reach L4, because 30% changed exceeds the focused 24% bar.
        Assert.AreEqual(
            "L4",
            DownstreamNativeVisualParityAudit.ClassifyLadder(30d, 6d, 23d, focused: true));
        // A genuinely tight focused result still reaches L5 under focused thresholds.
        Assert.AreEqual(
            "L5",
            DownstreamNativeVisualParityAudit.ClassifyLadder(22d, 5d, 19d, focused: true));

        // The same engine regenerates scenario rows from freshly parsed pixel-diff metrics.
        var rollup = DownstreamNativeVisualParityAudit.RollupFromProbeMetrics(new[]
        {
            new DownstreamNativeParityProbeMetric(
                "login-light", 1, "Login route and credential form", 960, 640,
                40d, 7d, 27d, "failed", "Changed pixels exceeds 45%.",
                "passed", "passed", "passed", "Signed-out login", new[] { "centering" })
        });
        Assert.HasCount(1, rollup);
        Assert.AreEqual("L4", rollup[0].Baseline.LadderLevel);
        Assert.AreEqual(960, rollup[0].Width);
    }

    [TestMethod]
    public void DownstreamProbeSweepPublishesNativeComparisonMetricRollup()
    {
        var script = File.ReadAllText(RepositoryPath("tools/winui3-mac-runner-downstream-windows-probe-sweep"));

        // Per-scenario pixel metrics must be parsed out of pixel-diff.json...
        StringAssert.Contains(script, "changedPixelPercentage");
        StringAssert.Contains(script, "meanAbsoluteError");
        StringAssert.Contains(script, "rootMeanSquaredError");
        StringAssert.Contains(script, "maxChannelDelta");
        StringAssert.Contains(script, "ladderLevel");
        // ...and rolled up into the summary nativeComparison block for review without private PNGs.
        StringAssert.Contains(script, "metricRollup");
        StringAssert.Contains(script, "worstChangedPixelPercentage");
        StringAssert.Contains(script, "Native comparison metrics");
    }

    [TestMethod]
    public void DownstreamProbeSweepKeepsEvidencePngOnly()
    {
        var script = File.ReadAllText(RepositoryPath("tools/winui3-mac-runner-downstream-windows-probe-sweep"));

        StringAssert.Contains(script, "evidenceFormat");
        StringAssert.Contains(script, "png-only");
        StringAssert.Contains(script, "Evidence must remain PNG");
        // The PNG-only guard must inspect the actual comparison inputs and reject JPG references.
        StringAssert.Contains(script, ".jpg");
        StringAssert.Contains(script, ".jpeg");
        StringAssert.Contains(script, "evidence_format_warning");
        StringAssert.Contains(script, "evidenceFormatWarnings");
    }

    [TestMethod]
    public void DownstreamProbeSweepReportsRouteContentAndSelectionWarnings()
    {
        var script = File.ReadAllText(RepositoryPath("tools/winui3-mac-runner-downstream-windows-probe-sweep"));

        StringAssert.Contains(script, "routeContentWarning");
        StringAssert.Contains(script, "selectionStateWarning");
        StringAssert.Contains(script, "routeSelectionWarnings");
        StringAssert.Contains(script, "expected_route_anchor");
        StringAssert.Contains(script, "selectedNavigationItem");
        StringAssert.Contains(script, "Route/selection");
    }

    private static void AssertMetricClose(double expected, double actual)
    {
        Assert.IsTrue(
            Math.Abs(expected - actual) < 0.0001d,
            $"Expected {expected} but got {actual}.");
    }

    [TestMethod]
    public void VisualLayoutEngineTreatsAutoSuggestBoxAsSupportedVisualSurface()
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
                        "Microsoft.UI.Xaml.Controls.AutoSuggestBox",
                        "SearchBox",
                        new Dictionary<string, object?>
                        {
                            ["text"] = "applications",
                            ["visibility"] = "Visible",
                            ["isEnabled"] = true
                        },
                        Array.Empty<UiNode>())
                }));

        _ = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "autosuggestbox", "skia-v2", new VisualViewport(320, 120), 1, "light", true, new VisualThresholds()),
            out var unsupported);

        Assert.HasCount(0, unsupported);
    }

    [TestMethod]
    public void ElementQueryTraversesCommandBarContent()
    {
        var searchBox = new AutoSuggestBox { Name = "WorkbenchSearchBox", Text = "applications" };
        var window = new Window
        {
            Content = new CommandBar
            {
                Name = "WorkbenchCommandBar",
                Content = searchBox
            }
        };

        var result = ElementQuery.FindBySelector(window, "WorkbenchSearchBox");

        Assert.AreSame(searchBox, result.Element);
        Assert.AreEqual("name", result.SelectorKind);
    }

    [TestMethod]
    public void VisualLayoutEngineArrangesGridRowsAndPadding()
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
                        "MessageGrid",
                        new Dictionary<string, object?>
                        {
                            ["rowDefinitions"] = "Auto,12,*",
                            ["rowDefinitionHeights"] = new[] { "Auto", "12", "*" },
                            ["padding"] = "32",
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBlock",
                                "Title",
                                new Dictionary<string, object?> { ["text"] = "Messages", ["visibility"] = "Visible" },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBox",
                                "Composer",
                                new Dictionary<string, object?> { ["gridRow"] = 2, ["text"] = "Line one", ["visibility"] = "Visible" },
                                Array.Empty<UiNode>())
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "grid-rows", "skia-v2", new VisualViewport(400, 300), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var grid = arranged.Root.Children.Single();
        var title = grid.Children.Single(child => child.Name == "Title");
        var composer = grid.Children.Single(child => child.Name == "Composer");

        Assert.HasCount(0, unsupported);
        Assert.AreEqual(32d, title.Layout.X);
        Assert.AreEqual(32d, title.Layout.Y);
        Assert.IsGreaterThanOrEqualTo(68d, composer.Layout.Y);
    }

    [TestMethod]
    public void VisualLayoutEngineAppliesContainerMaxWidthAndMinHeight()
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
                            ["maxWidth"] = 520d,
                            ["minHeight"] = 420d,
                            ["padding"] = "32",
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.TextBox",
                                "UsernameBox",
                                new Dictionary<string, object?> { ["text"] = "staffer", ["visibility"] = "Visible" },
                                Array.Empty<UiNode>())
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "container-constraints", "skia-v2", new VisualViewport(1044, 720), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var grid = arranged.Root.Children.Single();
        var username = grid.Children.Single(child => child.Name == "UsernameBox");

        Assert.HasCount(0, unsupported);
        Assert.AreEqual(520d, grid.Layout.Width);
        Assert.AreEqual(720d, grid.Layout.Height);
        Assert.AreEqual(456d, username.Layout.Width);
    }

    [TestMethod]
    public void VisualLayoutEngineCentersConstrainedLoginPanel()
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
                        "LoginPanel",
                        new Dictionary<string, object?>
                        {
                            ["maxWidth"] = 520d,
                            ["horizontalAlignment"] = "Center",
                            ["padding"] = "32",
                            ["visibility"] = "Visible"
                        },
                        Array.Empty<UiNode>())
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "centered-login", "skia-v2", new VisualViewport(1044, 720), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var loginPanel = arranged.Root.Children.Single();

        Assert.HasCount(0, unsupported);
        Assert.AreEqual(520d, loginPanel.Layout.Width);
        Assert.AreEqual(262d, loginPanel.Layout.X);
    }

    [TestMethod]
    public void VisualLayoutEngineUsesNaturalButtonWidthInsteadOfStretchWhenAlignmentIsLeft()
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
                        new Dictionary<string, object?> { ["visibility"] = "Visible" },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Button",
                                "SignInButton",
                                new Dictionary<string, object?>
                                {
                                    ["content"] = "Sign in",
                                    ["horizontalAlignment"] = "Left",
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>())
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "natural-button", "skia-v2", new VisualViewport(360, 120), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var button = arranged.Root.Children.Single().Children.Single();

        Assert.HasCount(0, unsupported);
        Assert.IsLessThan(140d, button.Layout.Width);
        Assert.AreEqual(0d, button.Layout.X);
    }

    [TestMethod]
    public void VisualLayoutEngineLetsAutoGridRowsFitPasswordBoxHeader()
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
                            ["rowDefinitions"] = "Auto",
                            ["rowDefinitionHeights"] = new[] { "Auto" },
                            ["padding"] = "32",
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.PasswordBox",
                                "PasswordBox",
                                new Dictionary<string, object?>
                                {
                                    ["header"] = "Password",
                                    ["passwordLength"] = 17,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>())
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "password-auto-row", "skia-v2", new VisualViewport(520, 220), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var password = arranged.Root.Children.Single().Children.Single(child => child.Name == "PasswordBox");

        Assert.HasCount(0, unsupported);
        Assert.IsGreaterThanOrEqualTo(56d, password.Layout.Height);
    }

    [TestMethod]
    public void VisualLayoutEngineSizesBorderToTallChildContent()
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
                        "RootGrid",
                        new Dictionary<string, object?>
                        {
                            ["rowDefinitions"] = "Auto",
                            ["rowDefinitionHeights"] = new[] { "Auto" },
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Border",
                                "ProfilePanel",
                                new Dictionary<string, object?>
                                {
                                    ["padding"] = "20",
                                    ["visibility"] = "Visible"
                                },
                                new[]
                                {
                                    new UiNode(
                                        "Microsoft.UI.Xaml.Controls.StackPanel",
                                        "FieldStack",
                                        new Dictionary<string, object?> { ["spacing"] = 10d, ["visibility"] = "Visible" },
                                        new[]
                                        {
                                            new UiNode("Microsoft.UI.Xaml.Controls.TextBox", "FieldA", new Dictionary<string, object?> { ["text"] = "A", ["visibility"] = "Visible" }, Array.Empty<UiNode>()),
                                            new UiNode("Microsoft.UI.Xaml.Controls.TextBox", "FieldB", new Dictionary<string, object?> { ["text"] = "B", ["visibility"] = "Visible" }, Array.Empty<UiNode>()),
                                            new UiNode("Microsoft.UI.Xaml.Controls.TextBox", "FieldC", new Dictionary<string, object?> { ["text"] = "C", ["visibility"] = "Visible" }, Array.Empty<UiNode>()),
                                            new UiNode("Microsoft.UI.Xaml.Controls.Button", "SaveButton", new Dictionary<string, object?> { ["content"] = "Save", ["visibility"] = "Visible" }, Array.Empty<UiNode>())
                                        })
                                })
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "border-tall-child", "skia-v2", new VisualViewport(420, 320), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var border = RequireNode(arranged.Root, "ProfilePanel").Layout!;
        var stack = RequireNode(arranged.Root, "FieldStack").Layout!;

        Assert.HasCount(0, unsupported);
        Assert.IsGreaterThanOrEqualTo(198d, border.Height);
        Assert.IsGreaterThanOrEqualTo(border.Y + border.Height, stack.Y + stack.Height + 20d);
    }

    [TestMethod]
    public void VisualLayoutEngineDoesNotReserveAutoRowHeightForClosedInfoBar()
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
                        "RootGrid",
                        new Dictionary<string, object?>
                        {
                            ["rowDefinitions"] = "Auto,Auto",
                            ["rowDefinitionHeights"] = new[] { "Auto", "Auto" },
                            ["rowSpacing"] = 12d,
                            ["visibility"] = "Visible"
                        },
                        new[]
                        {
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.InfoBar",
                                "ClosedInfoBar",
                                new Dictionary<string, object?>
                                {
                                    ["gridRow"] = 0d,
                                    ["isOpen"] = false,
                                    ["visibility"] = "Visible"
                                },
                                Array.Empty<UiNode>()),
                            new UiNode(
                                "Microsoft.UI.Xaml.Controls.Border",
                                "NextPanel",
                                new Dictionary<string, object?>
                                {
                                    ["gridRow"] = 1d,
                                    ["padding"] = "12",
                                    ["visibility"] = "Visible"
                                },
                                new[]
                                {
                                    new UiNode("Microsoft.UI.Xaml.Controls.TextBlock", "PanelText", new Dictionary<string, object?> { ["text"] = "Visible panel", ["visibility"] = "Visible" }, Array.Empty<UiNode>())
                                })
                        })
                }));

        var arranged = VisualLayoutEngine.Arrange(
            tree,
            new VisualRunSettings(null, "closed-infobar-auto-row", "skia-v2", new VisualViewport(420, 180), 1, "light", true, new VisualThresholds()),
            out var unsupported);
        var infoBar = RequireNode(arranged.Root, "ClosedInfoBar").Layout!;
        var panel = RequireNode(arranged.Root, "NextPanel").Layout!;

        Assert.HasCount(0, unsupported);
        Assert.AreEqual(0d, infoBar.Height);
        Assert.IsTrue(panel.Y <= 16d);
    }

    [TestMethod]
    public async Task SkiaV2SnapshotPublishesRuntimeImageIntegrity()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-snapshot-integrity", Guid.NewGuid().ToString("N"));
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new Button { Name = "PrimaryButton", Content = "Run" }
        });
        var settings = new VisualRunSettings(null, "image-integrity", "skia-v2", new VisualViewport(240, 120), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(
            arranged,
            outputDirectory,
            new SnapshotRenderOptions(settings.Renderer, settings.ScenarioName, settings.Viewport, settings.Scale, settings.Theme, settings.StrictVisual, "mac-runtime.png"));

        Assert.IsTrue(snapshot.IsNonBlank);
        Assert.IsNotNull(snapshot.RuntimeImageIntegrity);
        Assert.IsTrue(snapshot.RuntimeImageIntegrity.IsNonBlank);
        Assert.IsTrue(snapshot.RuntimeImageIntegrity.DistinctColorCount > 1);
        Assert.IsTrue(snapshot.RuntimeImageIntegrity.NonBackgroundPixelPercentage > 0d);
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
    public void InteractionScriptRecordsBeforeAndAfterStateForMutations()
    {
        var searchBox = new TextBox { Name = "SearchBox", Text = "Initial" };
        var listView = new ListView { Name = "TaskList" };
        listView.Items.Add("Review queue");
        listView.Items.Add("Archive completed task");
        listView.SelectedIndex = 0;
        var root = new StackPanel
        {
            Children =
            {
                searchBox,
                listView
            }
        };
        var window = new Window { Content = root };

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[]
            {
                new InteractionAction("typeText", "SearchBox", null, null, null, "Closed tasks"),
                new InteractionAction("selectItem", "TaskList", null, null, null, "Archive completed task")
            }));

        Assert.IsTrue(report.Steps.All(step => step.Status == "passed"));
        Assert.AreEqual("Initial", report.Steps[0].BeforeState?["text"]);
        Assert.AreEqual("Closed tasks", report.Steps[0].AfterState?["text"]);
        Assert.AreEqual("Closed tasks", report.Steps[0].ObservedState?["text"]);
        Assert.AreEqual("0", report.Steps[1].BeforeState?["selectedIndex"]);
        Assert.AreEqual("Review queue", report.Steps[1].BeforeState?["selectedItem"]);
        Assert.AreEqual("1", report.Steps[1].AfterState?["selectedIndex"]);
        Assert.AreEqual("Archive completed task", report.Steps[1].AfterState?["selectedItem"]);
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
    public void InteractionScriptWaitsForIdleAndAssertsAccessibilityState()
    {
        var button = new Button { Name = "PrimaryButton", Content = "Continue" };
        AutomationProperties.SetAutomationId(button, "primary-action");
        AutomationProperties.SetName(button, "Primary action");
        var window = new Window { Content = button };

        var report = new InteractionScriptRunner(new TypeResolver(Array.Empty<Type>()))
            .Run(window, new InteractionScript(new[]
            {
                new InteractionAction("waitForIdle", null, null, null, null, null),
                new InteractionAction("assertAccessibilityState", "automationId=primary-action", "role", null, null, "button"),
                new InteractionAction("assertAccessibilityState", "automationId=primary-action", "label", null, null, "Primary action")
            }));

        Assert.IsTrue(report.Steps.All(step => step.Status == "passed"));
        Assert.AreEqual("waitForIdle", report.Steps[0].Type);
        Assert.AreEqual("automationId=primary-action", report.Steps[1].Selector);
        Assert.AreEqual("automationId", report.Steps[1].SelectorKind);
        Assert.AreEqual("button", report.Steps[1].Actual);
        Assert.AreEqual("Primary action", report.Steps[2].ObservedState?["label"]);
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
    public void FluentDrawingPrimitivesDrawSelectedRadioWithAccentRing()
    {
        using var bitmap = new SKBitmap(40, 40, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = false };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawRadioButton(
            canvas,
            paint,
            20,
            20,
            theme,
            new FluentControlState(IsChecked: true));

        Assert.IsGreaterThan(
            0,
            CountExactPixels(bitmap, new SKRect(9, 9, 31, 13), theme.Accent) +
                CountExactPixels(bitmap, new SKRect(9, 27, 31, 31), theme.Accent) +
                CountExactPixels(bitmap, new SKRect(9, 13, 13, 27), theme.Accent) +
                CountExactPixels(bitmap, new SKRect(27, 13, 31, 27), theme.Accent),
            "A selected RadioButton should use accent chrome for the outer ring as well as the center dot.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesDrawSelectedRadioWithNativeFilledRingAndCenterKnockout()
    {
        using var bitmap = new SKBitmap(40, 40, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = false };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawRadioButton(
            canvas,
            paint,
            20,
            20,
            theme,
            new FluentControlState(IsChecked: true));

        Assert.AreEqual(theme.Surface, bitmap.GetPixel(20, 20), "The native selected RadioButton keeps a white center knockout.");
        Assert.AreEqual(theme.Accent, bitmap.GetPixel(27, 20), "The native selected RadioButton fills the selected ring between the edge and center knockout.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesDrawSelectedRadioWithNativeKnockoutEdge()
    {
        using var bitmap = new SKBitmap(40, 40, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = true };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawRadioButton(
            canvas,
            paint,
            20,
            20,
            theme,
            new FluentControlState(IsChecked: true));

        Assert.IsTrue(
            IsRadioKnockoutEdgeLike(bitmap.GetPixel(25, 20)),
            $"Selected RadioButton center knockout should reach the native horizontal inner edge; actual pixel was {bitmap.GetPixel(25, 20)}.");
        Assert.IsTrue(
            IsRadioKnockoutEdgeLike(bitmap.GetPixel(20, 25)),
            $"Selected RadioButton center knockout should reach the native vertical inner edge; actual pixel was {bitmap.GetPixel(20, 25)}.");
        Assert.IsTrue(
            IsAccentLike(bitmap.GetPixel(27, 20)),
            $"Selected RadioButton should keep an accent ring outside the native center knockout; actual pixel was {bitmap.GetPixel(27, 20)}.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesDrawCheckedCheckBoxWithCompactNativeTick()
    {
        using var bitmap = new SKBitmap(28, 28, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = false };
        var theme = SkiaV2Theme.For("light");
        var box = new SKRect(2, 2, 22, 22);
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawCheckBox(
            canvas,
            paint,
            box,
            theme,
            new FluentControlState(IsChecked: true));

        var tickBounds = BoundsOfPixelsMatching(bitmap, box, pixel => pixel == theme.Surface);

        Assert.IsFalse(tickBounds.IsEmpty, "Expected a checked CheckBox tick glyph.");
        Assert.IsLessThanOrEqualTo(12, tickBounds.Width, $"Checked CheckBox tick should stay compact; actual bounds were {tickBounds}.");
        Assert.IsLessThanOrEqualTo(9, tickBounds.Height, $"Checked CheckBox tick should stay compact; actual bounds were {tickBounds}.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesSpreadsRatingStarsAcrossNativeSizedBounds()
    {
        using var bitmap = new SKBitmap(120, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = false };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawRatingStars(
            canvas,
            paint,
            new SKRect(0, 0, 120, 32),
            theme,
            maxRating: 5,
            value: 5,
            isEnabled: true);

        Assert.IsGreaterThan(
            0,
            CountExactPixels(bitmap, new SKRect(108, 0, 120, 32), theme.Accent),
            "A five-star RatingControl should use the available 120 px native crop width instead of clustering stars left.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesDrawRatingStarsWithNativeFilledStarScale()
    {
        using var bitmap = new SKBitmap(120, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = true };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawRatingStars(
            canvas,
            paint,
            new SKRect(0, 0, 120, 32),
            theme,
            maxRating: 5,
            value: 4,
            isEnabled: true);

        var filledBounds = BoundsOfPixelsMatching(bitmap, new SKRect(0, 0, 96, 32), IsAccentLike);

        Assert.IsFalse(filledBounds.IsEmpty, "Expected filled RatingControl stars.");
        Assert.IsLessThanOrEqualTo(89, filledBounds.Width, $"Filled RatingControl stars should match native compact width; actual bounds were {filledBounds}.");
        Assert.IsLessThanOrEqualTo(15, filledBounds.Height, $"Filled RatingControl stars should match native compact height; actual bounds were {filledBounds}.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesDrawRatingStarsWithVisibleNativeEmptyStroke()
    {
        using var bitmap = new SKBitmap(120, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = true };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawRatingStars(
            canvas,
            paint,
            new SKRect(0, 0, 120, 32),
            theme,
            maxRating: 5,
            value: 4,
            isEnabled: true);

        Assert.IsGreaterThan(
            20,
            CountDarkPixels(bitmap, new SKRect(92, 6, 116, 26), maximumChannelValue: 210),
            "The unfilled RatingControl star should use a visible native-like stroke instead of a near-white outline.");
    }

    [TestMethod]
    public void FluentDrawingPrimitivesDrawChevronDownUsesCompactNativeStroke()
    {
        using var bitmap = new SKBitmap(24, 16, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { IsAntialias = false };
        var theme = SkiaV2Theme.For("light");
        canvas.Clear(theme.AppBackground);

        FluentDrawingPrimitives.DrawChevronDown(canvas, paint, 6, 5, theme.TextSecondary);

        var bounds = BoundsOfPixelsOtherThan(bitmap, theme.AppBackground);
        Assert.IsLessThanOrEqualTo(9, bounds.Width, $"Chevron should fit the native compact affordance width; actual bounds were {bounds}.");
        Assert.IsLessThanOrEqualTo(5, bounds.Height, $"Chevron should fit the native compact affordance height; actual bounds were {bounds}.");
        Assert.IsLessThanOrEqualTo(
            18,
            CountExactPixels(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height), theme.TextSecondary),
            "The dropdown chevron should use a light 1 px stroke rather than the previous heavy stroke.");
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
    public void CompatibilityLevelsMapEveryCatalogDisposition()
    {
        var audit = CatalogReadinessAudit.Build(CompatibilityCatalog.Current);
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/compatibility/compatibility-levels.json")));
        var root = document.RootElement;

        Assert.AreEqual("0.1", root.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("source-level-public-subset", RequireNonEmptyString(root, "productContract"));
        Assert.AreEqual("L2", RequireNonEmptyString(root, "currentCompatibilityLevel"));

        var levels = root.GetProperty("levels")
            .EnumerateArray()
            .Select(level => RequireNonEmptyString(level, "id"))
            .ToArray();
        CollectionAssert.AreEqual(new[] { "L0", "L1", "L2", "L3", "L4", "L5", "L6" }, levels);

        var mappings = root.GetProperty("catalogDispositionMappings")
            .EnumerateArray()
            .ToDictionary(
                mapping => RequireNonEmptyString(mapping, "disposition"),
                mapping => mapping,
                StringComparer.Ordinal);

        foreach (var disposition in audit.DispositionCounts.Keys)
        {
            Assert.IsTrue(mappings.ContainsKey(disposition), $"Missing compatibility-level mapping for disposition '{disposition}'.");
        }

        var levelSet = levels.ToHashSet(StringComparer.Ordinal);
        foreach (var entry in audit.Entries)
        {
            var mapping = mappings[entry.Disposition];
            var level = RequireNonEmptyString(mapping, "minimumCompatibilityLevel");
            Assert.IsTrue(levelSet.Contains(level), $"{entry.Id} maps to unknown compatibility level '{level}'.");
            Assert.AreEqual(entry.ReleaseGate, RequireNonEmptyString(mapping, "releaseGate"), $"{entry.Id} release gate mapping drifted.");
        }

        Assert.AreEqual("L2", RequireNonEmptyString(mappings[CatalogReadinessAudit.DispositionSourceLevelImplementation], "minimumCompatibilityLevel"));
        Assert.AreEqual("L2", RequireNonEmptyString(mappings[CatalogReadinessAudit.DispositionBoundedImplementation], "minimumCompatibilityLevel"));
        Assert.AreEqual("L0", RequireNonEmptyString(mappings[CatalogReadinessAudit.DispositionDiagnosticExclusion], "minimumCompatibilityLevel"));
        Assert.AreEqual("L0", RequireNonEmptyString(mappings[CatalogReadinessAudit.DispositionWindowsOnlyExclusion], "minimumCompatibilityLevel"));
        Assert.AreEqual("L0", RequireNonEmptyString(mappings[CatalogReadinessAudit.DispositionNonGoalExclusion], "minimumCompatibilityLevel"));
    }

    [TestMethod]
    public void CompatibilityLevelDocsKeepNativeQualitySeparateFromSourceLevelProduction()
    {
        var text = File.ReadAllText(RepositoryPath("docs/compatibility/compatibility-levels.md"));

        StringAssert.Contains(text, "## Current Level");
        StringAssert.Contains(text, "L2");
        StringAssert.Contains(text, "source-level");
        StringAssert.Contains(text, "native-quality");
        StringAssert.Contains(text, "does not mean native-quality visual fidelity");
    }

    [TestMethod]
    public void ProductEvidencePublicProductProfileUsesStableBatchGateSteps()
    {
        var plan = ProductEvidencePlan.Create("public-product", "artifacts/product-evidence/public-product");

        Assert.AreEqual("public-product", plan.Profile);
        Assert.AreEqual("artifacts/product-evidence/public-product", plan.OutputRoot);
        CollectionAssert.AreEqual(
            new[]
            {
                "catalog-audit",
                "component-quality-dashboard",
                "state-coverage-matrix",
                "native-quality-family-tranches",
                "native-reference-readiness",
                "visual-drift-dashboard-freshness",
                "visual-review-index",
                "strict-scenario-sweep",
                "public-admin-workbench",
                "production-e2e-workbench",
                "release-candidate-dry-run"
            },
            plan.Steps.Select(step => step.Name).ToArray());

        var dashboardStep = plan.Steps.Single(step => step.Name == "component-quality-dashboard");
        Assert.AreEqual("source-level-release", dashboardStep.BlockingScope);
        Assert.AreEqual("PATH=\"$PWD/tools:$PATH\" winui3-mac-runner component-quality-dashboard --check", dashboardStep.Command);
        CollectionAssert.Contains(dashboardStep.ArtifactPaths.ToArray(), "docs/visual-parity/component-quality-dashboard.json");

        var sweepStep = plan.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("external-evidence-required", sweepStep.ExecutionMode);
        Assert.AreEqual("product-polish", sweepStep.BlockingScope);
        StringAssert.Contains(sweepStep.Command, "product-evidence --profile strict-scenario-sweep");

        var publicAdminStep = plan.Steps.Single(step => step.Name == "public-admin-workbench");
        StringAssert.Contains(publicAdminStep.Command, "product-evidence --profile strict-scenario-sweep");
        CollectionAssert.Contains(
            publicAdminStep.ArtifactPaths.ToArray(),
            "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json");

        var productionE2eStep = plan.Steps.Single(step => step.Name == "production-e2e-workbench");
        StringAssert.Contains(productionE2eStep.Command, "product-evidence --profile strict-scenario-sweep");
        CollectionAssert.Contains(
            productionE2eStep.ArtifactPaths.ToArray(),
            "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json");

        var stateMatrixStep = plan.Steps.Single(step => step.Name == "state-coverage-matrix");
        Assert.AreEqual("local-check", stateMatrixStep.ExecutionMode);
        Assert.AreEqual("state-interaction-accessibility", stateMatrixStep.BlockingScope);
        CollectionAssert.Contains(stateMatrixStep.ArtifactPaths.ToArray(), "docs/visual-parity/state-coverage-matrix.json");

        var familyTrancheStep = plan.Steps.Single(step => step.Name == "native-quality-family-tranches");
        Assert.AreEqual("local-check", familyTrancheStep.ExecutionMode);
        Assert.AreEqual("native-quality-promotion", familyTrancheStep.BlockingScope);
        CollectionAssert.Contains(familyTrancheStep.ArtifactPaths.ToArray(), "docs/visual-parity/native-quality-family-tranches.json");
    }

    [TestMethod]
    public void ProductEvidenceStrictScenarioSweepProfileDiscoversEveryPublicScenario()
    {
        var plan = ProductEvidencePlan.Create(
            "strict-scenario-sweep",
            "artifacts/product-evidence/strict-scenario-sweep",
            RepositoryRoot());
        var scenarioPaths = Directory
            .EnumerateFiles(RepositoryPath("fixtures"), "*.json", SearchOption.AllDirectories)
            .Where(path => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains("scenarios", StringComparer.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.AreEqual("strict-scenario-sweep", plan.Profile);
        Assert.AreEqual(scenarioPaths.Length, plan.Steps.Count);
        Assert.AreEqual(36, plan.Steps.Count);
        CollectionAssert.AreEqual(scenarioPaths, plan.Steps.Select(step => step.ScenarioPath).ToArray());

        var basicInput = plan.Steps.Single(step => step.Name == "component-basic-input-light");
        Assert.AreEqual("local-check", basicInput.ExecutionMode);
        Assert.AreEqual("strict-scenario-sweep", basicInput.BlockingScope);
        Assert.AreEqual("fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj", basicInput.ProjectPath);
        Assert.AreEqual("fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json", basicInput.ScenarioPath);
        Assert.AreEqual("artifacts/product-evidence/strict-scenario-sweep/component-basic-input-light", basicInput.OutputDirectory);
        CollectionAssert.Contains(
            basicInput.ArtifactPaths.ToArray(),
            "artifacts/product-evidence/strict-scenario-sweep/component-basic-input-light/visual/visual-run.json");
    }

    [TestMethod]
    public async Task ProductEvidenceStrictScenarioSweepProfileWritesScenarioStepReport()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-strict-sweep-tests", Guid.NewGuid().ToString("N"));

        var report = await ProductEvidenceRunner.RunAsync(
            RepositoryRoot(),
            "strict-scenario-sweep",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("scenario passed in test harness.", step.ArtifactPaths)));

        Assert.AreEqual("passed", report.Status);
        Assert.AreEqual(36, report.Summary.TotalSteps);
        Assert.AreEqual(36, report.Summary.PassedSteps);
        Assert.AreEqual(0, report.Summary.ExternalEvidenceSteps);
        Assert.IsTrue(report.Steps.All(step => step.ExecutionMode == "local-check"));
        Assert.IsTrue(report.Steps.All(step => !string.IsNullOrWhiteSpace(step.ProjectPath)));
        Assert.IsTrue(report.Steps.All(step => !string.IsNullOrWhiteSpace(step.ScenarioPath)));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "product-evidence.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "product-evidence.md")));
    }

    [TestMethod]
    public void ProductEvidenceRejectsUnknownProfile()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ProductEvidencePlan.Create("unknown-profile", "artifacts/product-evidence/unknown"));
    }

    [TestMethod]
    public async Task ProductEvidenceReportSummarizesFailedStepsAndWritesStableArtifacts()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(step.Name == "component-quality-dashboard"
                ? ProductEvidenceStepOutcome.Failed("component-quality-dashboard.json is out of date.", step.ArtifactPaths)
                : ProductEvidenceStepOutcome.Passed("step is current.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        Assert.AreEqual(1, report.Summary.FailedSteps);
        Assert.AreEqual(7, report.Summary.PassedSteps);
        Assert.AreEqual(3, report.Summary.ExternalEvidenceSteps);
        Assert.AreEqual("component-quality-dashboard", report.Steps.Single(step => step.Status == "failed").Name);
        Assert.AreEqual("component-quality-dashboard.json is out of date.", report.Steps.Single(step => step.Status == "failed").FailureReason);
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "product-evidence.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "product-evidence.md")));
    }

    [TestMethod]
    public async Task ProductEvidenceAttachesCompletedExternalArtifacts()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        CreateStrictScenarioFixture(repositoryRoot, "component-b");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-b/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("passed", report.Status);
        Assert.AreEqual(11, report.Summary.PassedSteps);
        Assert.AreEqual(0, report.Summary.FailedSteps);
        Assert.AreEqual(0, report.Summary.ExternalEvidenceSteps);
        Assert.AreEqual("passed", report.Steps.Single(step => step.Name == "strict-scenario-sweep").Status);
        Assert.AreEqual("passed", report.Steps.Single(step => step.Name == "public-admin-workbench").Status);
        Assert.AreEqual("passed", report.Steps.Single(step => step.Name == "production-e2e-workbench").Status);
    }

    [TestMethod]
    public void StateCoverageMatrixBuildsVisibleDefaultOnlyRowsFromInventory()
    {
        var matrix = StateCoverageMatrixBuilder.Build(RepositoryRoot());

        Assert.AreEqual(ArtifactSchemas.StateCoverageMatrix, matrix.SchemaVersion);
        Assert.IsGreaterThan(0, matrix.Totals.ComponentCount);
        Assert.IsGreaterThan(0, matrix.Totals.RequirementCount);
        Assert.IsGreaterThan(0, matrix.Totals.DefaultOnlyComponentCount);

        var button = matrix.Components.Single(component => component.Component == "Button");
        Assert.AreEqual("Basic input", button.OwnerFamily);
        Assert.AreEqual("default-only", button.CoverageStatus);
        CollectionAssert.Contains(button.RequiredStates.ToArray(), "focused");
        CollectionAssert.Contains(button.RequiredStates.ToArray(), "disabled");
        CollectionAssert.Contains(button.CoveredStates.ToArray(), "default");
        CollectionAssert.Contains(button.MissingStates.ToArray(), "focused");

        var focusedButton = matrix.Requirements.Single(requirement =>
            requirement.Component == "Button" &&
            requirement.State == "focused" &&
            requirement.ScenarioName == "component-basic-input-focused-light");
        Assert.IsTrue(focusedButton.ScenarioExists);
        Assert.AreEqual("missing-state-evidence", focusedButton.EvidenceStatus);
        Assert.AreEqual("missing-state-evidence", focusedButton.CoverageStatus);
    }

    [TestMethod]
    public void StateCoverageMatrixPublishesStrictSweepReleaseEvidencePaths()
    {
        var matrix = StateCoverageMatrixBuilder.Build(RepositoryRoot());

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(matrix, JsonDefaults.Options));
        var checkedToggle = document.RootElement
            .GetProperty("requirements")
            .EnumerateArray()
            .Single(requirement =>
                requirement.GetProperty("component").GetString() == "ToggleButton" &&
                requirement.GetProperty("state").GetString() == "checked" &&
                requirement.GetProperty("scenarioName").GetString() == "component-basic-input-checked-light");

        Assert.IsTrue(checkedToggle.TryGetProperty("releaseEvidenceProfile", out var profile));
        Assert.AreEqual("strict-scenario-sweep", profile.GetString());
        Assert.AreEqual(
            "artifacts/product-evidence/strict-scenario-sweep/component-basic-input-checked-light/visual/component-evidence.json",
            checkedToggle.GetProperty("releaseComponentEvidencePath").GetString());
        Assert.AreEqual(
            "artifacts/product-evidence/strict-scenario-sweep/component-basic-input-checked-light/accessibility.json",
            checkedToggle.GetProperty("releaseAccessibilityEvidencePath").GetString());
        Assert.AreEqual(
            "artifacts/product-evidence/strict-scenario-sweep/component-basic-input-checked-light/visual/visual-run.json",
            checkedToggle.GetProperty("releaseVisualRunPath").GetString());
        Assert.AreEqual(
            "required-via-public-product",
            checkedToggle.GetProperty("releaseEvidenceStatus").GetString());
    }

    [TestMethod]
    public void StateCoverageMatrixIncludesThemeStateRequirements()
    {
        var matrix = StateCoverageMatrixBuilder.Build(RepositoryRoot());

        var darkTheme = matrix.Requirements.Single(requirement =>
            requirement.Component == "ThemeResource" &&
            requirement.State == "dark" &&
            requirement.ScenarioName == "component-layout-media-dark");
        var highContrastTheme = matrix.Requirements.Single(requirement =>
            requirement.Component == "ThemeResource" &&
            requirement.State == "high-contrast" &&
            requirement.ScenarioName == "component-layout-media-high-contrast");

        Assert.AreEqual("Theming and resources", darkTheme.OwnerFamily);
        Assert.AreEqual("artifacts/product-evidence/strict-scenario-sweep/component-layout-media-dark/visual/component-evidence.json", darkTheme.ReleaseComponentEvidencePath);
        Assert.AreEqual("Theming and resources", highContrastTheme.OwnerFamily);
        Assert.AreEqual("artifacts/product-evidence/strict-scenario-sweep/component-layout-media-high-contrast/visual/component-evidence.json", highContrastTheme.ReleaseComponentEvidencePath);
    }

    [TestMethod]
    public void StateCoverageMatrixMatchesTrackedArtifact()
    {
        var expected = StateCoverageMatrixBuilder.Build(RepositoryRoot());
        var actual = File.ReadAllText(RepositoryPath("docs/visual-parity/state-coverage-matrix.json"));

        Assert.AreEqual(
            NormalizeArtifact(JsonSerializer.Serialize(expected, JsonDefaults.Options)),
            NormalizeArtifact(actual),
            "docs/visual-parity/state-coverage-matrix.json is out of date. Regenerate with 'winui3-mac-runner state-coverage-matrix'.");

        Assert.IsTrue(
            expected.Components.Any(component => component.CoverageStatus == "default-only"),
            "The state matrix must label components whose checked-in evidence is still default-only.");
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksFailedAttachedExternalArtifacts()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        CreateStrictScenarioFixture(repositoryRoot, "component-b");
        await WriteStrictScenarioSweepReport(repositoryRoot, "component-b");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-b/visual/visual-run.json", "failed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        Assert.AreEqual(1, report.Summary.FailedSteps);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-b");
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksIncompleteStrictScenarioSweepArtifacts()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        CreateStrictScenarioFixture(repositoryRoot, "component-b");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-b");
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksStrictSweepMissingStateCoverageComponentEvidence()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        WriteProductionStateCoverageInventory(repositoryRoot, "component-a", "Button", minimumVisualGrade: "usable");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteStrictScenarioAccessibility(repositoryRoot, "component-a");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-a/Button");
    }

    [TestMethod]
    public async Task ProductEvidenceAcceptsStrictSweepStateCoverageComponentEvidence()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        WriteProductionStateCoverageInventory(repositoryRoot, "component-a", "Button", minimumVisualGrade: "usable");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteStrictScenarioAccessibility(
            repositoryRoot,
            "component-a",
            "PrimaryTarget",
            role: "button",
            isFocused: true);
        WriteStrictScenarioComponentEvidence(repositoryRoot, "component-a", "Button", visualGrade: "usable", interactionStatus: "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("passed", report.Status);
        Assert.AreEqual("passed", report.Steps.Single(step => step.Name == "strict-scenario-sweep").Status);
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksStrictSweepWhenStateMatrixReleasePathContractIsStale()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        WriteProductionStateCoverageInventory(repositoryRoot, "component-a", "Button", minimumVisualGrade: "usable");
        WriteStateCoverageMatrixReleaseContract(
            repositoryRoot,
            "component-a",
            "Button",
            "focused",
            releaseComponentEvidencePath: "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/stale-component-evidence.json");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteStrictScenarioAccessibility(
            repositoryRoot,
            "component-a",
            "PrimaryTarget",
            role: "button",
            isFocused: true);
        WriteStrictScenarioComponentEvidence(repositoryRoot, "component-a", "Button", visualGrade: "usable", interactionStatus: "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-a/Button=release-component-evidence-path");
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksStrictSweepMissingAccessibilityTargetEvidence()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        WriteProductionStateCoverageInventory(repositoryRoot, "component-a", "Button", minimumVisualGrade: "usable");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteStrictScenarioAccessibility(repositoryRoot, "component-a");
        WriteStrictScenarioComponentEvidence(repositoryRoot, "component-a", "Button", visualGrade: "usable", interactionStatus: "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-a/Button=missing-accessibility-node:PrimaryTarget");
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksStrictSweepMismatchedCheckedAccessibilityState()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        WriteProductionStateCoverageInventory(repositoryRoot, "component-a", "CheckBox", minimumVisualGrade: "usable", state: "checked");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteStrictScenarioAccessibility(
            repositoryRoot,
            "component-a",
            "PrimaryTarget",
            role: "checkbox",
            isChecked: false);
        WriteStrictScenarioComponentEvidence(repositoryRoot, "component-a", "CheckBox", visualGrade: "usable", interactionStatus: "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-a/CheckBox=accessibility-state:checked");
    }

    [TestMethod]
    public async Task ProductEvidenceBlocksStrictSweepWithoutDisabledAccessibilityState()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-product-evidence-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "public-product");
        CreateStrictScenarioFixture(repositoryRoot, "component-a");
        WriteProductionStateCoverageInventory(repositoryRoot, "component-a", "Button", minimumVisualGrade: "usable", state: "disabled");
        await WriteStrictScenarioSweepReport(repositoryRoot);
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/component-a/visual/visual-run.json", "passed");
        WriteStrictScenarioAccessibility(
            repositoryRoot,
            "component-a",
            "PrimaryTarget",
            role: "button",
            isEnabled: true);
        WriteStrictScenarioComponentEvidence(repositoryRoot, "component-a", "Button", visualGrade: "usable", interactionStatus: "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/public-admin-workbench-light/visual/visual-run.json", "passed");
        WriteVisualRunStatus(repositoryRoot, "artifacts/product-evidence/strict-scenario-sweep/production-e2e-workbench-light/visual/visual-run.json", "passed");

        var report = await ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "public-product",
            outputRoot,
            step => Task.FromResult(ProductEvidenceStepOutcome.Passed("local step passed.", step.ArtifactPaths)));

        Assert.AreEqual("blocked", report.Status);
        var sweep = report.Steps.Single(step => step.Name == "strict-scenario-sweep");
        Assert.AreEqual("failed", sweep.Status);
        StringAssert.Contains(sweep.FailureReason, "component-a=accessibility-state:disabled");
    }

    [TestMethod]
    public void NativeQualityFamilyTranchesBuildsMilestoneCFamilyQueues()
    {
        var tranches = NativeQualityFamilyTrancheBuilder.Build(RepositoryRoot());

        Assert.AreEqual(ArtifactSchemas.NativeQualityFamilyTranches, tranches.SchemaVersion);
        CollectionAssert.AreEqual(
            new[]
            {
                "selection-controls",
                "button-link",
                "dropdown-menu",
                "text-forms",
                "navigation-list",
                "status-progress"
            },
            tranches.Families.Select(family => family.FamilyId).ToArray());
        Assert.IsGreaterThan(0, tranches.Totals.RowCount);
        Assert.IsGreaterThan(0, tranches.Totals.NotEvaluatedRowCount);
        Assert.IsGreaterThan(0, tranches.Totals.DefaultOnlyComponentCount);
        Assert.AreEqual("tracked-with-native-quality-gaps", tranches.Status);

        var selection = tranches.Families.Single(family => family.FamilyId == "selection-controls");
        CollectionAssert.Contains(selection.Components.ToArray(), "CheckBox");
        CollectionAssert.Contains(selection.Components.ToArray(), "RadioButton");
        Assert.AreEqual("native-quality-blocked", selection.Status);
        Assert.IsTrue(selection.NextActions.Any(action => action.Contains("state", StringComparison.OrdinalIgnoreCase)));

        var buttonLink = tranches.Families.Single(family => family.FamilyId == "button-link");
        CollectionAssert.Contains(buttonLink.Components.ToArray(), "Button");
        CollectionAssert.Contains(buttonLink.Components.ToArray(), "HyperlinkButton");

        var dropdown = tranches.Families.Single(family => family.FamilyId == "dropdown-menu");
        CollectionAssert.Contains(dropdown.Components.ToArray(), "ComboBox");

        var textForms = tranches.Families.Single(family => family.FamilyId == "text-forms");
        CollectionAssert.Contains(textForms.Components.ToArray(), "TextBox");

        var navigationList = tranches.Families.Single(family => family.FamilyId == "navigation-list");
        CollectionAssert.Contains(navigationList.Components.ToArray(), "ListView");
        CollectionAssert.Contains(navigationList.Components.ToArray(), "NavigationView");

        var statusProgress = tranches.Families.Single(family => family.FamilyId == "status-progress");
        CollectionAssert.Contains(statusProgress.Components.ToArray(), "InfoBar");
        CollectionAssert.Contains(statusProgress.Components.ToArray(), "ProgressBar");
    }

    [TestMethod]
    public void NativeQualityFamilyTranchesExposeCropThresholdBlockersForNotEvaluatedRows()
    {
        var tranches = NativeQualityFamilyTrancheBuilder.Build(RepositoryRoot());

        var radio = tranches.Rows.Single(row =>
            row.FamilyId == "selection-controls" &&
            row.Component == "RadioButton" &&
            row.Target == "HighPriorityRadioButton");
        var toggle = tranches.Rows.Single(row =>
            row.FamilyId == "selection-controls" &&
            row.Component == "ToggleButton" &&
            row.Target == "PinnedToggleButton");

        StringAssert.Contains(radio.RemainingBlocker, "crop status is 'failed'");
        StringAssert.Contains(radio.RemainingBlocker, "changedPixelPercentage 19.375 exceeds threshold 18");
        StringAssert.Contains(toggle.RemainingBlocker, "crop status is 'failed'");
        StringAssert.Contains(toggle.RemainingBlocker, "changedPixelPercentage 22.434701 exceeds threshold 18");
    }

    [TestMethod]
    public void NativeQualityFamilyTranchesExposeFamilyStateRequirementQueues()
    {
        var tranches = NativeQualityFamilyTrancheBuilder.Build(RepositoryRoot());
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(tranches, JsonDefaults.Options));

        var selection = document.RootElement.GetProperty("families")
            .EnumerateArray()
            .Single(family => family.GetProperty("familyId").GetString() == "selection-controls");

        Assert.IsTrue(
            selection.TryGetProperty("stateRequirementCount", out var stateRequirementCount),
            "Family tranche rows must publish the number of state requirements that block family-level promotion.");
        Assert.AreEqual(4, stateRequirementCount.GetInt32());

        Assert.IsTrue(
            selection.TryGetProperty("missingStateRequirementCount", out var missingStateRequirementCount),
            "Family tranche rows must publish unresolved state requirement counts.");
        Assert.AreEqual(4, missingStateRequirementCount.GetInt32());

        CollectionAssert.AreEqual(
            new[] { "checked", "disabled" },
            selection.GetProperty("stateRequirementStates")
                .EnumerateArray()
                .Select(state => state.GetString())
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "component-basic-input-checked-light", "component-basic-input-disabled-light" },
            selection.GetProperty("stateRequirementScenarios")
                .EnumerateArray()
                .Select(scenario => scenario.GetString())
                .ToArray());
    }

    [TestMethod]
    public void NativeQualityFamilyTranchesMatchesTrackedArtifact()
    {
        var expected = NativeQualityFamilyTrancheBuilder.Build(RepositoryRoot());
        var actual = File.ReadAllText(RepositoryPath("docs/visual-parity/native-quality-family-tranches.json"));

        Assert.AreEqual(
            NormalizeArtifact(JsonSerializer.Serialize(expected, JsonDefaults.Options)),
            NormalizeArtifact(actual),
            "docs/visual-parity/native-quality-family-tranches.json is out of date. Regenerate with 'winui3-mac-runner native-quality-family-tranches'.");
    }

    [TestMethod]
    public void EvidenceFreshnessFlagsStaleVisualDriftDashboardMetrics()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-freshness-tests", Guid.NewGuid().ToString("N"));
        var visualDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity");
        var evidenceDirectory = Path.Combine(visualDirectory, "examples", "scenario-light");
        Directory.CreateDirectory(evidenceDirectory);
        File.WriteAllText(Path.Combine(evidenceDirectory, "pixel-diff.json"), """
            {
              "schemaVersion": "0.1",
              "changedPixelPercentage": 42.5,
              "meanAbsoluteError": 1.0,
              "rootMeanSquaredError": 2.0,
              "status": "failed"
            }
            """);
        File.WriteAllText(Path.Combine(visualDirectory, "visual-drift-dashboard.json"), """
            {
              "schemaVersion": "0.1",
              "gatedMetric": "component-crop",
              "informationalMetric": "whole-screen",
              "families": [
                {
                  "family": "Scenario",
                  "scenario": "scenario-light",
                  "pixelDiffPath": "docs/visual-parity/examples/scenario-light/pixel-diff.json",
                  "componentCropDrift": { "gated": true },
                  "wholeScreenDrift": {
                    "gated": false,
                    "changedPixelPercentage": 1.25
                  }
                }
              ]
            }
            """);

        var result = EvidenceFreshness.CheckVisualDriftDashboard(repositoryRoot);

        Assert.AreEqual("failed", result.Status);
        Assert.IsTrue(result.Problems.Any(problem => problem.Contains("Scenario", StringComparison.Ordinal)));
        Assert.IsTrue(result.Problems.Any(problem => problem.Contains("pixel-diff.json", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void EvidenceFreshnessFlagsStaleComponentQualityDashboard()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-freshness-tests", Guid.NewGuid().ToString("N"));
        var visualDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity");
        var evidenceDirectory = Path.Combine(visualDirectory, "examples", "scenario-light");
        Directory.CreateDirectory(evidenceDirectory);
        File.WriteAllText(Path.Combine(evidenceDirectory, "component-evidence.json"), TestComponentEvidenceJson("PrimaryButton"));
        File.WriteAllText(
            Path.Combine(visualDirectory, "component-quality-dashboard.json"),
            JsonSerializer.Serialize(ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot), JsonDefaults.Options));
        File.WriteAllText(Path.Combine(evidenceDirectory, "component-evidence.json"), TestComponentEvidenceJson("SecondaryButton"));

        var result = EvidenceFreshness.CheckComponentQualityDashboard(repositoryRoot);

        Assert.AreEqual("failed", result.Status);
        Assert.IsTrue(result.Problems.Any(problem => problem.Contains("component-quality-dashboard.json", StringComparison.Ordinal)));
        Assert.IsTrue(result.Problems.Any(problem => problem.Contains("component-evidence.json", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void VisualCompareReportClassifiesBatchComponentRows()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-visual-compare-tests", Guid.NewGuid().ToString("N"));
        var beforeRoot = Path.Combine(root, "before");
        var afterRoot = Path.Combine(root, "after");
        WriteCompareComponentEvidence(
            beforeRoot,
            "scenario-light",
            ("Button", "PrimaryButton", "failed", 12.5, 3.0, 5.0),
            ("Slider", "VolumeSlider", "passed", 2.0, 1.0, 1.5),
            ("CheckBox", "EnabledCheckBox", "failed", 8.0, 2.0, 4.0));
        WriteCompareComponentEvidence(
            afterRoot,
            "scenario-light",
            ("Button", "PrimaryButton", "passed", 4.0, 1.0, 2.0),
            ("Slider", "VolumeSlider", "passed", 5.0, 2.0, 3.0),
            ("CheckBox", "EnabledCheckBox", "failed", 3.0, 1.0, 2.0));

        var report = VisualComparisonReport.Create(beforeRoot, afterRoot);

        Assert.AreEqual(3, report.Summary.TotalRows);
        Assert.AreEqual(1, report.Summary.NewlyPassingRows);
        Assert.AreEqual(1, report.Summary.RegressedRows);
        Assert.AreEqual(1, report.Summary.ImprovedRows);
        Assert.AreEqual("newly-passing", report.Rows.Single(row => row.Target == "PrimaryButton").Status);
        Assert.AreEqual("regressed", report.Rows.Single(row => row.Target == "VolumeSlider").Status);
        Assert.AreEqual("improved", report.Rows.Single(row => row.Target == "EnabledCheckBox").Status);
    }

    [TestMethod]
    public void VisualCompareReportWritesJsonAndMarkdown()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-mac-visual-compare-tests", Guid.NewGuid().ToString("N"));
        var beforeRoot = Path.Combine(root, "before");
        var afterRoot = Path.Combine(root, "after");
        var outputRoot = Path.Combine(root, "compare");
        WriteCompareComponentEvidence(beforeRoot, "scenario-light", ("Button", "PrimaryButton", "passed", 3.0, 1.0, 1.5));
        WriteCompareComponentEvidence(afterRoot, "scenario-light", ("Button", "PrimaryButton", "passed", 2.0, 0.5, 1.0));

        var report = VisualComparisonReport.Write(beforeRoot, afterRoot, outputRoot);

        Assert.AreEqual("passed", report.Status);
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "visual-compare.json")));
        Assert.IsTrue(File.Exists(Path.Combine(outputRoot, "visual-compare.md")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(outputRoot, "visual-compare.md")), "PrimaryButton");
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
    public void ComponentEvidenceBuilderCarriesRendererFontDiagnostics()
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
                    VisualGrade = "usable"
                }
            }
        };
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new Button { Name = "PrimaryButton", Content = "Run" }
        });
        var settings = new VisualRunSettings(null, scenario.Name, "skia-v2", new VisualViewport(240, 160), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var diagnostics = new SnapshotFontDiagnostics(
            Text: new FontRoleDiagnostic(
                Role: FontResolver.TextRole,
                RequestedFamilies: new[] { "Segoe UI" },
                MatchedFamily: "Segoe UI",
                ResolvedFamily: "Segoe UI",
                RequestedFamilyAvailable: true,
                FallbackMode: FontResolver.RequestedFamilyMode),
            Symbol: new FontRoleDiagnostic(
                Role: FontResolver.SymbolRole,
                RequestedFamilies: new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" },
                MatchedFamily: "Segoe Fluent Icons",
                ResolvedFamily: "Segoe Fluent Icons",
                RequestedFamilyAvailable: true,
                FallbackMode: FontResolver.RequestedFamilyMode));

        var evidence = ComponentEvidenceBuilder.Build(scenario, arranged, interactions: null, metrics: null, fontDiagnostics: diagnostics);

        Assert.IsNotNull(evidence.FontDiagnostics);
        Assert.AreEqual("Segoe UI", evidence.FontDiagnostics.Text.ResolvedFamily);
        Assert.AreEqual("Segoe Fluent Icons", evidence.FontDiagnostics.Symbol.ResolvedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, evidence.FontDiagnostics.Symbol.FallbackMode);
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

        Assert.AreEqual(ArtifactSchemas.ComponentQualityDashboard, expected.SchemaVersion);
        Assert.AreEqual("passed", expected.Status);
        Assert.AreEqual(0, expected.Totals.BlockingRowCount);
        Assert.AreEqual(0, expected.Totals.MissingMacRuntimeCrops);
        Assert.AreEqual(0, expected.Totals.MissingNativeReferenceCrops);
        Assert.AreEqual(0, expected.Totals.MissingNativeReferenceProvenance);
        Assert.AreEqual(0, expected.Totals.MissingComponentDiffs);
        Assert.AreEqual(0, expected.Totals.MissingInspectionNotes);
        Assert.AreEqual(0, expected.Totals.InvalidNativeReferenceRows);
        Assert.AreEqual(0, expected.Totals.UntrustedNativeReferenceRows);
        Assert.AreEqual(0, expected.Totals.ReferenceIntegrityBlockingRowCount);
        Assert.AreEqual(expected.Totals.ComponentCount, expected.Totals.NativeReferenceReadinessCounts.Values.Sum());
        Assert.IsTrue(expected.Rows.All(row => !string.IsNullOrWhiteSpace(row.NativeReferenceReadiness)));
        Assert.IsTrue(expected.Rows.All(row => !string.IsNullOrWhiteSpace(row.NativeReferenceBoundsSource)));
        Assert.IsTrue(expected.Rows.All(row => !string.IsNullOrWhiteSpace(row.NativeReferenceIntegrityBlockerReason)));
        Assert.HasCount(0, expected.Blockers);
        Assert.AreEqual(51, expected.Rows.Count(row => row.TargetGrade == "usable-or-better"));
        Assert.AreEqual(7, expected.Rows.Count(row => row.TargetGrade == "excluded-from-source-level-claim"));
        Assert.IsTrue(expected.Rows
            .Where(row => row.TargetGrade == "usable-or-better")
            .All(row => row.VisualGrade is "usable" or "good" or "production-ready"));
        Assert.IsTrue(expected.Rows
            .Where(row => row.TargetGrade == "excluded-from-source-level-claim")
            .All(row => row.VisualGrade == "not-rendered"));
    }

    [TestMethod]
    public void ComponentQualityDashboardBlocksCropWithoutNativeReferenceBounds()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-dashboard-tests", Guid.NewGuid().ToString("N"));
        var evidenceDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples", "scenario-light");
        Directory.CreateDirectory(evidenceDirectory);
        File.WriteAllText(Path.Combine(evidenceDirectory, "component-evidence.json"), """
            {
              "schemaVersion": "0.5",
              "fixtureName": "test-fixture",
              "scenarioName": "scenario-light",
              "components": [
                {
                  "component": "Button",
                  "kind": "control",
                  "target": "PrimaryButton",
                  "catalogStatus": "supported",
                  "presence": "present",
                  "interactionStatus": "passed",
                  "visualGrade": "good",
                  "changedPixelPercentage": 0.1,
                  "meanAbsoluteError": 0.1,
                  "rootMeanSquaredError": 0.1,
                  "crop": {
                    "status": "passed",
                    "bounds": { "x": 0, "y": 0, "width": 10, "height": 10 },
                    "nativeReferencePath": "components/button-primarybutton/windows-reference.png",
                    "macRuntimePath": "components/button-primarybutton/mac-runtime.png",
                    "pixelDiffPath": "components/button-primarybutton/pixel-diff.png",
                    "runtimeBlank": false,
                    "thresholds": {
                      "changedPixelPercentage": 5,
                      "maxChannelDelta": 255,
                      "meanAbsoluteError": 2,
                      "rootMeanSquaredError": 4
                    },
                    "changedPixelPercentage": 0.1,
                    "meanAbsoluteError": 0.1,
                    "rootMeanSquaredError": 0.1,
                    "nativeReferenceProvenance": {
                      "referenceSource": "native-winui",
                      "fixtureProjectPath": "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
                      "scenarioPath": "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
                      "scenarioName": "component-basic-input-light",
                      "commitSha": "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
                      "workflowRunId": "26777029415",
                      "runnerImage": "win25",
                      "viewport": { "width": 1028, "height": 720 },
                      "scale": 1,
                      "theme": "light",
                      "captureMode": "client-area",
                      "dimensions": { "width": 1028, "height": 720 },
                      "capturedAt": "2026-06-01T19:31:04.2512607+00:00"
                    }
                  },
                  "nativeQualityGrade": "good",
                  "inspection": {
                    "inspectedBy": "manual-reviewer",
                    "inspectedDate": "2026-06-02",
                    "nativeReferenceRunId": "26777029415",
                    "comparisonArtifactPaths": [
                      "components/button-primarybutton/windows-reference.png",
                      "components/button-primarybutton/mac-runtime.png",
                      "components/button-primarybutton/pixel-diff.png"
                    ],
                    "acceptedGaps": [],
                    "notes": "Inspected."
                  },
                  "knownGaps": []
                }
              ],
              "sourceFeatures": [],
              "status": "passed"
            }
            """);

        var dashboard = ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot);

        Assert.AreEqual(1, dashboard.Totals.ComponentCount);
        Assert.AreEqual(0, dashboard.Totals.MissingNativeReferenceCrops);
        Assert.AreEqual(1, dashboard.Totals.ReferenceIntegrityBlockingRowCount);
        Assert.AreEqual(1, dashboard.Totals.InvalidNativeReferenceRows);
        Assert.AreEqual(1, dashboard.Totals.UntrustedNativeReferenceRows);
        Assert.AreEqual("needs-native-crop-bounds", dashboard.Rows[0].NativeReferenceStatus);
        Assert.AreEqual("needs-native-crop-bounds", dashboard.Rows[0].NativeReferenceReadiness);
        Assert.IsTrue(dashboard.Rows[0].NativeReferenceCropExists);
        Assert.IsFalse(dashboard.Rows[0].NativeReferenceCropValid);
        Assert.IsFalse(dashboard.Rows[0].NativeReferenceHasWindowsNativeElementBounds);
        StringAssert.Contains(dashboard.Rows[0].NativeReferenceIntegrityBlockerReason, "Native reference crop integrity");
        Assert.IsTrue(dashboard.Blockers[0].Reasons.Any(reason => reason.Contains("Native reference status", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void NativeReferenceReadinessUsesTrustedCropOverStaleSemanticBlocker()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-readiness-tests", Guid.NewGuid().ToString("N"));
        var evidenceDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples", "scenario-light");
        Directory.CreateDirectory(evidenceDirectory);
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "docs", "visual-parity"));
        var evidence = new ComponentEvidenceDocument(
            SchemaVersion: ArtifactSchemas.ComponentEvidence,
            FixtureName: "test-fixture",
            ScenarioName: "scenario-light",
            Components: new[]
            {
                new ComponentEvidenceEntry(
                    Component: "Button",
                    Kind: "control",
                    Target: "PrimaryButton",
                    LayoutRegion: null,
                    CatalogStatus: "supported",
                    Presence: "present",
                    InteractionStatus: "passed",
                    VisualGrade: "good",
                    ComponentThresholds: null,
                    ChangedPixelPercentage: 0.1,
                    MeanAbsoluteError: 0.1,
                    RootMeanSquaredError: 0.1,
                    Crop: new ComponentCropEvidence(
                        Status: "passed",
                        Bounds: new ComponentCropBounds(0, 0, 10, 10),
                        NativeReferenceBounds: new ComponentCropBounds(0, 0, 10, 10),
                        NativeReferencePath: "components/button-primarybutton/windows-reference.png",
                        MacRuntimePath: "components/button-primarybutton/mac-runtime.png",
                        PixelDiffPath: "components/button-primarybutton/pixel-diff.png",
                        RuntimeBlank: false,
                        Thresholds: new VisualThresholds(),
                        ChangedPixelPercentage: 0.1,
                        MeanAbsoluteError: 0.1,
                        RootMeanSquaredError: 0.1,
                        Message: "passed")
                    {
                        NativeReferenceProvenance = new NativeReferenceProvenance(
                            ReferenceSource: "native-winui",
                            FixtureProjectPath: "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
                            ScenarioPath: "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
                            ScenarioName: "scenario-light",
                            CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
                            WorkflowRunId: "26777029415",
                            RunnerImage: "win25",
                            WindowsAppSdkVersion: null,
                            Viewport: new VisualViewport(100, 100),
                            Scale: 1,
                            Theme: "light",
                            CaptureMode: "client-area",
                            Dimensions: new ReferenceImageDimensions(100, 100),
                            CapturedAt: "2026-06-01T19:31:04.2512607+00:00"),
                        NativeReferenceTarget = new NativeReferenceTarget(
                            Component: "Button",
                            Target: "PrimaryButton",
                            IdentitySource: "automation-id",
                            AutomationId: "PrimaryButton",
                            Name: null,
                            ElementType: "Button",
                            Bounds: new NativeReferenceBounds(0, 0, 10, 10)),
                        NativeReferenceReadinessStatus = "ready",
                        NativeReferenceReadinessReason = "Native crop uses Windows native element bounds from native-reference-targets.json.",
                        NativeReferenceRequiredAction = "Keep the native target export with the Windows reference artifact.",
                        NativeReferenceBoundsSource = "windows-native-element-bounds",
                        NativeReferenceBoundsValidForPromotion = true,
                        NativeReferenceIntegrityBlockerReason = "none",
                        NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                            "ready",
                            "Native crop uses Windows native element bounds from native-reference-targets.json.",
                            "Keep the native target export with the Windows reference artifact.",
                            ReadyForPromotion: true,
                            "none")
                    },
                    NativeQualityGrade: "good",
                    Inspection: null,
                    KnownGaps: Array.Empty<string>())
            },
            SourceFeatures: Array.Empty<SourceFeatureEvidenceEntry>(),
            Status: "passed");
        File.WriteAllText(
            Path.Combine(evidenceDirectory, "component-evidence.json"),
            JsonSerializer.Serialize(evidence, JsonDefaults.Options));
        File.WriteAllText(
            Path.Combine(repositoryRoot, "docs", "visual-parity", "native-reference-readiness.json"),
            """
            {
              "schemaVersion": "0.1",
              "generatedAt": "1970-01-01T00:00:00+00:00",
              "policy": "test",
              "totals": {
                "rowCount": 1,
                "readyRowCount": 0,
                "blockingRowCount": 1,
                "statusCounts": { "diagnostic-reference": 1 }
              },
              "rows": [
                {
                  "scenarioName": "scenario-light",
                  "evidencePath": "docs/visual-parity/examples/scenario-light/component-evidence.json",
                  "component": "Button",
                  "target": "PrimaryButton",
                  "nativeReferenceStatus": "diagnostic-reference",
                  "reason": "Old semantic blocker.",
                  "requiredAction": "Regenerate evidence."
                }
              ]
            }
            """);

        var readiness = NativeReferenceReadinessBuilder.BuildFromPublicEvidence(repositoryRoot);

        Assert.AreEqual(1, readiness.Totals.ReadyRowCount);
        Assert.AreEqual(0, readiness.Totals.BlockingRowCount);
        Assert.AreEqual("ready", readiness.Rows.Single().NativeReferenceStatus);
        Assert.AreEqual(
            "Native crop uses Windows native element bounds from native-reference-targets.json.",
            readiness.Rows.Single().Reason);
    }

    [TestMethod]
    public void NativeReferenceReadinessTreatsRendererCropMismatchAsReadyWhenNativeBoundsAreTrusted()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-readiness-tests", Guid.NewGuid().ToString("N"));
        var evidenceDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples", "scenario-light");
        Directory.CreateDirectory(evidenceDirectory);
        var evidence = new ComponentEvidenceDocument(
            SchemaVersion: ArtifactSchemas.ComponentEvidence,
            FixtureName: "test-fixture",
            ScenarioName: "scenario-light",
            Components: new[]
            {
                new ComponentEvidenceEntry(
                    Component: "Button",
                    Kind: "control",
                    Target: "PrimaryButton",
                    LayoutRegion: null,
                    CatalogStatus: "supported",
                    Presence: "present",
                    InteractionStatus: "passed",
                    VisualGrade: "usable",
                    ComponentThresholds: null,
                    ChangedPixelPercentage: null,
                    MeanAbsoluteError: null,
                    RootMeanSquaredError: null,
                    Crop: new ComponentCropEvidence(
                        Status: "failed",
                        Bounds: new ComponentCropBounds(0, 0, 12, 10),
                        NativeReferenceBounds: new ComponentCropBounds(0, 0, 10, 10),
                        NativeReferencePath: "components/button-primarybutton/windows-reference.png",
                        MacRuntimePath: "components/button-primarybutton/mac-runtime.png",
                        PixelDiffPath: null,
                        RuntimeBlank: false,
                        Thresholds: new VisualThresholds(),
                        ChangedPixelPercentage: null,
                        MeanAbsoluteError: null,
                        RootMeanSquaredError: null,
                        Message: "Native and macOS crop dimensions differ.")
                    {
                        NativeReferenceProvenance = new NativeReferenceProvenance(
                            ReferenceSource: "native-winui",
                            FixtureProjectPath: "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
                            ScenarioPath: "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
                            ScenarioName: "scenario-light",
                            CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
                            WorkflowRunId: "26777029415",
                            RunnerImage: "win25",
                            WindowsAppSdkVersion: null,
                            Viewport: new VisualViewport(100, 100),
                            Scale: 1,
                            Theme: "light",
                            CaptureMode: "client-area",
                            Dimensions: new ReferenceImageDimensions(100, 100),
                            CapturedAt: "2026-06-01T19:31:04.2512607+00:00")
                        {
                            NativeReferenceTargetsPath = "native-reference-targets.json"
                        },
                        NativeReferenceTarget = new NativeReferenceTarget(
                            Component: "Button",
                            Target: "PrimaryButton",
                            IdentitySource: "automation-id",
                            AutomationId: "PrimaryButton",
                            Name: null,
                            ElementType: "Button",
                            Bounds: new NativeReferenceBounds(0, 0, 10, 10)),
                        NativeReferenceReadinessStatus = "native-crop-size-mismatch",
                        NativeReferenceReadinessReason = "Native and macOS crop dimensions differ.",
                        NativeReferenceRequiredAction = "Fix renderer layout before parity comparison.",
                        NativeReferenceBoundsSource = "windows-native-element-bounds",
                        NativeReferenceBoundsValidForPromotion = false,
                        NativeReferenceIntegrityBlockerReason = "Native and macOS crop dimensions differ.",
                        NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                            "native-crop-size-mismatch",
                            "Native and macOS crop dimensions differ.",
                            "Fix renderer layout before parity comparison.",
                            ReadyForPromotion: false,
                            "Native and macOS crop dimensions differ.")
                    },
                    NativeQualityGrade: "not-evaluated",
                    Inspection: null,
                    KnownGaps: Array.Empty<string>())
            },
            SourceFeatures: Array.Empty<SourceFeatureEvidenceEntry>(),
            Status: "failed");
        File.WriteAllText(
            Path.Combine(evidenceDirectory, "component-evidence.json"),
            JsonSerializer.Serialize(evidence, JsonDefaults.Options));

        var readiness = NativeReferenceReadinessBuilder.BuildFromPublicEvidence(repositoryRoot);

        Assert.AreEqual(1, readiness.Totals.ReadyRowCount);
        Assert.AreEqual(0, readiness.Totals.BlockingRowCount);
        Assert.AreEqual("ready", readiness.Rows.Single().NativeReferenceStatus);
    }

    [TestMethod]
    public void ComponentQualityDashboardIgnoresNestedVisualEvidenceCopies()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-dashboard-tests", Guid.NewGuid().ToString("N"));
        var evidenceDirectory = Path.Combine(repositoryRoot, "docs", "visual-parity", "examples", "scenario-light");
        var nestedVisualDirectory = Path.Combine(evidenceDirectory, "visual");
        Directory.CreateDirectory(nestedVisualDirectory);
        const string evidenceJson = """
            {
              "schemaVersion": "0.5",
              "fixtureName": "test-fixture",
              "scenarioName": "scenario-light",
              "components": [
                {
                  "component": "Button",
                  "kind": "control",
                  "target": "PrimaryButton",
                  "catalogStatus": "supported",
                  "presence": "present",
                  "interactionStatus": "passed",
                  "visualGrade": "usable",
                  "changedPixelPercentage": 0.1,
                  "meanAbsoluteError": 0.1,
                  "rootMeanSquaredError": 0.1,
                  "crop": {
                    "status": "failed",
                    "bounds": { "x": 0, "y": 0, "width": 10, "height": 10 },
                    "nativeReferencePath": "components/button-primarybutton/windows-reference.png",
                    "macRuntimePath": "components/button-primarybutton/mac-runtime.png",
                    "pixelDiffPath": "components/button-primarybutton/pixel-diff.png",
                    "runtimeBlank": false,
                    "thresholds": {
                      "changedPixelPercentage": 5,
                      "maxChannelDelta": 255,
                      "meanAbsoluteError": 2,
                      "rootMeanSquaredError": 4
                    },
                    "changedPixelPercentage": 0.1,
                    "meanAbsoluteError": 0.1,
                    "rootMeanSquaredError": 0.1
                  },
                  "nativeQualityGrade": "not-evaluated",
                  "inspection": null,
                  "knownGaps": []
                }
              ],
              "sourceFeatures": [],
              "status": "failed"
            }
            """;
        File.WriteAllText(Path.Combine(evidenceDirectory, "component-evidence.json"), evidenceJson);
        File.WriteAllText(Path.Combine(nestedVisualDirectory, "component-evidence.json"), evidenceJson);

        var dashboard = ComponentQualityDashboard.BuildFromPublicEvidence(repositoryRoot);

        Assert.AreEqual(1, dashboard.Totals.ScenarioCount);
        Assert.AreEqual(1, dashboard.Totals.ComponentCount);
        Assert.HasCount(1, dashboard.Rows);
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
        Assert.AreEqual(0, expected.Summary.MissingInspectionNotes);
        Assert.AreEqual(0, expected.Summary.InvalidNativeReferenceRows);
        Assert.AreEqual(0, expected.Summary.UntrustedNativeReferenceRows);
        Assert.AreEqual(0, expected.Summary.ReferenceIntegrityBlockingRowCount);
        Assert.AreEqual(0, expected.Summary.BlockingRowCount);
        Assert.HasCount(58, expected.Rows);
        Assert.IsTrue(expected.Rows.All(row => !string.IsNullOrWhiteSpace(row.NativeReferenceReadiness)));
        Assert.IsTrue(expected.Rows.All(row => !string.IsNullOrWhiteSpace(row.NativeReferenceBoundsSource)));
        Assert.IsTrue(expected.Rows.All(row => !string.IsNullOrWhiteSpace(row.NativeReferenceIntegrityBlockerReason)));
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
    public void ComponentInspectionApplierAppliesReviewedHarnessGrades()
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
                    NativeQualityGrade: "not-evaluated",
                    InspectedBy: "manual-reviewer",
                    InspectedDate: "2026-06-02",
                    NativeReferenceRunId: "26777029415",
                    ComparisonArtifactPaths: null,
                    AcceptedGaps: Array.Empty<string>(),
                    ToleranceReason: null,
                    Notes: "Native, macOS, and diff crops were manually inspected.")
            });

        var updated = ComponentInspectionApplier.Apply(TestInspectableEvidence(), inspection, directory);

        Assert.AreEqual("usable", updated.Components[0].VisualGrade);
        Assert.AreEqual("not-evaluated", updated.Components[0].NativeQualityGrade);
        Assert.IsNotNull(updated.Components[0].Inspection);
    }

    [TestMethod]
    public void ComponentInspectionApplierRejectsFinalGradesWithoutReadyNativeReference()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-inspection-tests", Guid.NewGuid().ToString("N"));
        var componentDirectory = Path.Combine(directory, "components", "button-primarybutton");
        Directory.CreateDirectory(componentDirectory);
        WritePatternPng(Path.Combine(componentDirectory, "windows-reference.png"));
        WritePatternPng(Path.Combine(componentDirectory, "mac-runtime.png"));
        WritePatternPng(Path.Combine(componentDirectory, "pixel-diff.png"));
        var evidence = TestInspectableEvidence();
        var crop = evidence.Components[0].Crop ?? throw new AssertFailedException("Expected crop evidence.");
        evidence = evidence with
        {
            Components = new[]
            {
                evidence.Components[0] with
                {
                    Crop = crop with
                    {
                        NativeReferenceReadinessStatus = "needs-native-crop-bounds",
                        NativeReferenceReadinessReason = "Native crop uses macOS/runtime layout bounds.",
                        NativeReferenceRequiredAction = "Recapture native target bounds.",
                        NativeReferenceBoundsSource = "windows-native-element-bounds",
                        NativeReferenceBoundsValidForPromotion = false
                    }
                }
            }
        };
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
                    AcceptedGaps: Array.Empty<string>(),
                    ToleranceReason: null,
                    Notes: "Native, macOS, and diff crops were manually inspected.")
            });

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ComponentInspectionApplier.Apply(evidence, inspection, directory));
        StringAssert.Contains(exception.Message, "nativeReferenceReadiness must be ready or verified");
    }

    [TestMethod]
    public void ComponentInspectionTemplatePrefillsEvidenceWithoutPromotingClaims()
    {
        var template = ComponentInspectionTemplate.Build(TestInspectableEvidence());

        Assert.HasCount(1, template.Rows);
        var row = template.Rows[0];
        Assert.AreEqual("Button", row.Component);
        Assert.AreEqual("PrimaryButton", row.Target);
        Assert.AreEqual("TODO-not-rendered-usable-good-or-production-ready", row.VisualGrade);
        Assert.AreEqual("TODO-not-evaluated-good-or-production-ready", row.NativeQualityGrade);
        Assert.AreEqual("26777029415", row.NativeReferenceRunId);
        Assert.IsNotNull(row.ComparisonArtifactPaths);
        Assert.HasCount(3, row.ComparisonArtifactPaths);
        StringAssert.Contains(row.Notes, "TODO");

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ComponentInspectionApplier.Apply(TestInspectableEvidence(), template, Path.GetTempPath()));
        StringAssert.Contains(exception.Message, "visualGrade must be one of");
    }

    [TestMethod]
    public void ReleaseCandidateArtifactGatesAreAccountedFor()
    {
        // Mirrors the deterministic local checks of 'winui3-mac-runner
        // release-candidate'. The component-quality dashboard is expected to pass
        // the bounded source-level harness gate without promoting native-quality
        // renderer claims. CI adds native reference capture, the full strict
        // sweep, and the package dry run.

        // Zero unknown production surfaces in the committed corpus inventory.
        using (var corpus = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/compatibility/corpus-unknown-apis.json"))))
        {
            Assert.AreEqual(0, corpus.RootElement.GetProperty("entries").GetArrayLength(), "Corpus inventory must report zero unknown public surfaces.");
        }

        using (var dashboard = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/visual-parity/component-quality-dashboard.json"))))
        {
            Assert.AreEqual("passed", dashboard.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(0, dashboard.RootElement.GetProperty("totals").GetProperty("blockingRowCount").GetInt32());
        }

        using (var tranches = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/visual-parity/native-quality-family-tranches.json"))))
        {
            Assert.AreEqual("tracked-with-native-quality-gaps", tranches.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(6, tranches.RootElement.GetProperty("totals").GetProperty("blockedFamilyCount").GetInt32());
        }

        var releaseCandidateSource = File.ReadAllText(RepositoryPath("src/WinUI3.MacRunner/ReleaseCandidate.cs"));
        StringAssert.Contains(
            releaseCandidateSource,
            "CheckNativeQualityFamilyTranches",
            "release-candidate must include the native-quality family tranche freshness gate.");
        StringAssert.Contains(
            releaseCandidateSource,
            "native-quality-family-tranches",
            "release-candidate output must name the native-quality family tranche gate.");

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

        using (var readiness = JsonDocument.Parse(File.ReadAllText(RepositoryPath("docs/visual-parity/native-reference-readiness.json"))))
        {
            var readinessRows = readiness.RootElement.GetProperty("rows").EnumerateArray().ToArray();
            Assert.HasCount(58, readinessRows);
            Assert.AreEqual(
                0,
                readiness.RootElement.GetProperty("totals").GetProperty("blockingRowCount").GetInt32(),
                "Native reference source readiness must be unblocked after verified Windows-bound rows are imported.");
            Assert.IsTrue(
                readinessRows.Where(row => row.GetProperty("nativeReferenceStatus").GetString() == "ready")
                    .All(row => string.Equals(row.GetProperty("reason").GetString(), "Native crop uses Windows native element bounds from native-reference-targets.json.", StringComparison.Ordinal)),
                "Ready public rows must be backed by imported Windows native element bounds.");
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
    public void LocalReleaseReadyCommandRunsTheCleanCheckoutGateSequence()
    {
        var scriptPath = RepositoryPath("tools/winui3-mac-release-ready-local");
        Assert.IsTrue(File.Exists(scriptPath), "Milestone E requires a single local command for the clean-checkout release-ready gate.");

        var script = File.ReadAllText(scriptPath);
        StringAssert.Contains(script, "dotnet build -v minimal /m:1 /nr:false");
        StringAssert.Contains(script, "dotnet tests/WinUI3.MacRuntime.Tests/bin/Debug/net10.0/WinUI3.MacRuntime.Tests.dll");
        StringAssert.Contains(script, "dotnet tests/WinUI3.MacXaml.Tests/bin/Debug/net10.0/WinUI3.MacXaml.Tests.dll");
        StringAssert.Contains(script, "product-evidence --profile strict-scenario-sweep");
        StringAssert.Contains(script, "product-evidence --profile public-product");
        StringAssert.Contains(script, "dotnet pack src/WinUI3.MacCompat/WinUI3.MacCompat.csproj");
        StringAssert.Contains(script, "dotnet pack src/WinUI3.MacRuntime/WinUI3.MacRuntime.csproj");
        StringAssert.Contains(script, "dotnet pack src/WinUI3.MacXaml/WinUI3.MacXaml.csproj");
        StringAssert.Contains(script, "dotnet pack src/WinUI3.MacRenderer.Skia/WinUI3.MacRenderer.Skia.csproj");
        StringAssert.Contains(script, "dotnet pack src/WinUI3.MacRunner/WinUI3.MacRunner.csproj");
        StringAssert.Contains(script, "dotnet pack src/WinUI3.MacTest.Sdk/WinUI3.MacTest.Sdk.csproj");
        StringAssert.Contains(script, "release-check --package-dir artifacts/packages");
        StringAssert.Contains(script, "release-candidate --package-dir artifacts/packages");

        var readme = File.ReadAllText(RepositoryPath("README.md"));
        StringAssert.Contains(readme, "winui3-mac-release-ready-local");
        StringAssert.Contains(readme, "clean-checkout local release-candidate command");
    }

    [TestMethod]
    public void WindowsNativeReferenceWorkflowCoversEveryComponentParityScenario()
    {
        var workflow = File.ReadAllText(RepositoryPath(".github/workflows/windows-native-screenshot.yml"));
        StringAssert.Contains(
            workflow,
            "WINUI3_MAC_NATIVE_REFERENCE_TARGETS_OUTPUT",
            "windows-native-screenshot.yml must request native target bound export.");
        StringAssert.Contains(
            workflow,
            "Native reference target export was not created",
            "component native reference captures must fail when native-reference-targets.json is missing.");
        StringAssert.Contains(
            workflow,
            "missing required public row target",
            "windows-native-screenshot.yml must validate every expected public row target.");
        StringAssert.Contains(
            workflow,
            "fixtures/PublicAdminWorkbench.WinUI/scenarios/",
            "PublicAdminWorkbench must use the same native target-bound strategy as component scenarios.");
        var componentScenarioPaths = PublicNativeReferenceScenarioPaths()
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
    public void WindowsDownstreamProbeScreenshotWorkflowCapturesClientAreaForEveryProbeScenario()
    {
        var workflowPath = RepositoryPath(".github/workflows/windows-downstream-probe-screenshot.yml");
        Assert.IsTrue(File.Exists(workflowPath), "Downstream EMSI probe screenshots need a dedicated Windows runner workflow.");

        var workflow = File.ReadAllText(workflowPath);
        StringAssert.Contains(workflow, "MeetingChallenge.WinUI.MacRuntimeProbe.csproj");
        StringAssert.Contains(workflow, "emsi_ref");
        StringAssert.Contains(workflow, "runtime_ref");
        StringAssert.Contains(workflow, "repository: MarlonJD/winui3-mac-test-runtime");
        StringAssert.Contains(workflow, "WindowsWindowCapture.csproj");
        StringAssert.Contains(workflow, "--client-area");
        StringAssert.Contains(workflow, "--reject-black-border");
        StringAssert.Contains(workflow, "--require-title-match");
        StringAssert.Contains(workflow, "--title \"Meeting Challenge Windows macOS Runtime Probe\"");
        StringAssert.Contains(workflow, "windows-downstream-probe-screenshots");
        StringAssert.Contains(workflow, "native-winui");
        StringAssert.Contains(workflow, "WINUI3_MAC_NATIVE_LAUNCH_LOG");
        StringAssert.Contains(workflow, "Environment.GetCommandLineArgs()");
        StringAssert.Contains(workflow, "ExpectedRoute=");
        StringAssert.Contains(workflow, "FinalRoute=");
        StringAssert.Contains(
            workflow,
            "did not report expected route",
            "Downstream probe screenshots must fail when scenario replay stays on the wrong route.");

        foreach (var scenario in new[]
        {
            "login-light",
            "shell-staff-light",
            "messages-multiline-light",
            "admin-dashboard-light",
            "admin-workbench-light",
            "command-search-light",
            "status-states-light",
            "settings-profile-light"
        })
        {
            StringAssert.Contains(workflow, $"Name = \"{scenario}\"");
            StringAssert.Contains(workflow, $"Path = \"apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/{scenario}.json\"");
        }
    }

    [TestMethod]
    public async Task NativeReferenceImporterNormalizesCompleteComponentReferenceArtifact()
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-source", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-output", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceRoot);
        var scenarioPaths = PublicEvidenceScenarioPaths()
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var scenarioPath in scenarioPaths)
        {
            var scenario = await VisualScenario.LoadAsync(scenarioPath);
            var artifactDirectory = Path.Combine(sourceRoot, scenario.Name);
            Directory.CreateDirectory(artifactDirectory);
            WriteSolidPng(Path.Combine(artifactDirectory, "windows-reference.png"), new SKColor(250, 250, 250), scenario.Viewport.Width, scenario.Viewport.Height);
            var relativeScenarioPath = Path.GetRelativePath(RepositoryRoot(), scenarioPath).Replace('\\', '/');
            var fixtureProjectPath = relativeScenarioPath.StartsWith("fixtures/PublicAdminWorkbench.WinUI/", StringComparison.Ordinal)
                ? "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj"
                : "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj";
            var provenance = new NativeReferenceProvenance(
                ReferenceSource: "native-winui",
                FixtureProjectPath: fixtureProjectPath,
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
            var targets = new NativeReferenceTargetDocument(
                SchemaVersion: "0.1",
                ReferenceSource: "native-winui-element-bounds",
                CoordinateSpace: "client-area",
                ScenarioName: scenario.Name,
                ScenarioPath: relativeScenarioPath,
                FixtureProjectPath: fixtureProjectPath,
                CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
                WorkflowRunId: "26777029415",
                Theme: scenario.Theme,
                Viewport: scenario.Viewport,
                Scale: scenario.Scale,
                Dimensions: new ReferenceImageDimensions(scenario.Viewport.Width, scenario.Viewport.Height),
                RootBounds: new NativeReferenceBounds(0, 0, scenario.Viewport.Width, scenario.Viewport.Height),
                CapturedAt: DateTimeOffset.UnixEpoch,
                Targets: scenario.Requirements
                    .Where(requirement => !string.IsNullOrWhiteSpace(requirement.Target))
                    .Select(requirement => new NativeReferenceTarget(
                        Component: requirement.Component,
                        Target: requirement.Target!,
                        IdentitySource: "x:Name",
                        AutomationId: requirement.Target,
                        Name: requirement.Target,
                        ElementType: "Microsoft.UI.Xaml.FrameworkElement",
                        Bounds: new NativeReferenceBounds(0, 0, 12, 10))
                    {
                        ActualSize = new ReferenceImageDimensions(12, 10),
                        BoundsSource = "x:Name",
                        CapturedAt = DateTimeOffset.UnixEpoch
                    })
                    .ToArray());
            File.WriteAllText(
                Path.Combine(artifactDirectory, "native-reference-targets.json"),
                JsonSerializer.Serialize(targets, JsonDefaults.Options));
        }

        var import = NativeReferenceImporter.Import(RepositoryRoot(), sourceRoot, outputRoot);

        Assert.AreEqual("passed", import.Status, string.Join(Environment.NewLine, import.Problems));
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
            Assert.IsNotNull(item.ImportedReferenceTargetsPath);
            Assert.IsTrue(File.Exists(Path.Combine(outputRoot, item.ImportedReferenceTargetsPath)));
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
    public async Task NativeReferenceImporterFailsWhenComponentScenarioTargetIsMissing()
    {
        await AssertNativeReferenceImporterFailsWhenRequiredTargetMissingAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            "PrimaryActionButton");
    }

    [TestMethod]
    public async Task NativeReferenceImporterFailsWhenPublicAdminWorkbenchTargetIsMissing()
    {
        await AssertNativeReferenceImporterFailsWhenRequiredTargetMissingAsync(
            "fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json",
            "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj",
            "RootNavigation");
    }

    [TestMethod]
    public async Task NativeReferenceImporterFailsWhenRequiredTargetIsOutsideScreenshotBounds()
    {
        await AssertNativeReferenceImporterFailsWithTargetMutationAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            "PrimaryActionButton",
            targets => targets
                .Select(target => string.Equals(target.Target, "PrimaryActionButton", StringComparison.Ordinal)
                    ? target with { Bounds = new NativeReferenceBounds(9999, 0, 12, 10) }
                    : target)
                .ToArray(),
            "has bounds outside the captured client area");
    }

    [TestMethod]
    public async Task NativeReferenceImporterFailsWhenRequiredTargetIdentityIsAmbiguous()
    {
        await AssertNativeReferenceImporterFailsWithTargetMutationAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            "PrimaryActionButton",
            targets =>
            {
                var duplicate = targets.Single(target => string.Equals(target.Target, "PrimaryActionButton", StringComparison.Ordinal));
                return targets.Append(duplicate with { AutomationId = "DuplicatePrimaryActionButton" }).ToArray();
            },
            "maps ambiguously");
    }

    [TestMethod]
    public async Task NativeReferenceImporterFailsWhenRequiredTargetComponentIdentityDoesNotMatchRow()
    {
        await AssertNativeReferenceImporterFailsWithTargetMutationAsync(
            "fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json",
            "fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj",
            "RootNavigation",
            targets => targets
                .Select(target => string.Equals(target.Target, "RootNavigation", StringComparison.Ordinal)
                    ? target with { Component = "Frame" }
                    : target)
                .ToArray(),
            "does not match expected public row component 'NavigationView'");
    }

    [TestMethod]
    public async Task NativeReferenceImporterFailsWhenRequiredTargetBoundsMetadataIsMissing()
    {
        await AssertNativeReferenceImporterFailsWithTargetMutationAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            "PrimaryActionButton",
            targets => targets
                .Select(target => string.Equals(target.Target, "PrimaryActionButton", StringComparison.Ordinal)
                    ? target with { BoundsSource = null }
                    : target)
                .ToArray(),
            "is missing boundsSource metadata");
    }

    [TestMethod]
    public async Task NativeReferenceImporterFailsWhenRequiredTargetIsTextOnlyDiagnostic()
    {
        await AssertNativeReferenceImporterFailsWithTargetMutationAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            "PrimaryActionButton",
            targets => targets
                .Select(target => string.Equals(target.Target, "PrimaryActionButton", StringComparison.Ordinal)
                    ? target with { ElementType = "Microsoft.UI.Xaml.Controls.TextBlock" }
                    : target)
                .ToArray(),
            "not a trustworthy native element");
    }

    [TestMethod]
    public async Task NativeReferenceImporterAcceptsCommandMenuDiagnosticTargets()
    {
        await AssertNativeReferenceImporterPassesWithTargetMutationAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            targets => targets
                .Select(target => target.Target is "DiagnosticCommandBarFlyout" or "DiagnosticMenuFlyout" or "DiagnosticContextMenuPattern"
                    ? target with { ElementType = "Microsoft.UI.Xaml.Controls.Button" }
                    : target.Target is "SaveCommandIcon"
                        ? target with { ElementType = "Microsoft.UI.Xaml.Controls.FontIcon" }
                        : target)
                .ToArray(),
            "DiagnosticCommandBarFlyout",
            "DiagnosticMenuFlyout",
            "DiagnosticContextMenuPattern",
            "SaveCommandIcon");
    }

    [TestMethod]
    public async Task NativeReferenceImporterAcceptsLayoutMediaDiagnosticTargets()
    {
        await AssertNativeReferenceImporterPassesWithTargetMutationAsync(
            "fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json",
            "fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj",
            targets => targets
                .Select(target => target.Target switch
                {
                    "DiagnosticAnnotatedScrollbar" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "DiagnosticColor" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "DiagnosticInkControls" => target with { ElementType = "Microsoft.UI.Xaml.Controls.ContentPresenter" },
                    "DiagnosticMediaPlayerElement" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "DiagnosticShapes" => target with { ElementType = "Microsoft.UI.Xaml.Shapes.Ellipse" },
                    "DiagnosticSystemBackdrop" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "DiagnosticTitleBarCustomization" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "DiagnosticWebView2" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "DiagnosticXamlControlsResources" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "LayoutBorder" => target with { ElementType = "Microsoft.UI.Xaml.Controls.Border" },
                    "LayoutMediaTitle" => target with { ElementType = "Microsoft.UI.Xaml.Controls.TextBlock" },
                    "StaticResourceText" => target with { ElementType = "Microsoft.UI.Xaml.Controls.TextBlock" },
                    "ThemeResourceText" => target with { ElementType = "Microsoft.UI.Xaml.Controls.TextBlock" },
                    _ => target
                })
                .ToArray(),
            "DiagnosticAnnotatedScrollbar",
            "DiagnosticColor",
            "DiagnosticInkControls",
            "DiagnosticMediaPlayerElement",
            "DiagnosticShapes",
            "DiagnosticSystemBackdrop",
            "DiagnosticTitleBarCustomization",
            "DiagnosticWebView2",
            "DiagnosticXamlControlsResources",
            "LayoutBorder",
            "LayoutMediaTitle",
            "StaticResourceText",
            "ThemeResourceText");
    }

    [TestMethod]
    public void PublicNativeReferenceScreenshotsAreNotBlank()
    {
        string[] scenarioNames =
        {
            "component-layout-media-light",
            "public-admin-workbench-light"
        };

        foreach (var scenarioName in scenarioNames)
        {
            var path = RepositoryPath(Path.Combine("docs", "visual-parity", "examples", scenarioName, "windows-reference.png"));
            Assert.IsTrue(File.Exists(path), $"{scenarioName} must have a checked-in Windows reference image.");
            using var bitmap = SKBitmap.Decode(path);
            Assert.IsNotNull(bitmap, $"{scenarioName} must have a decodable Windows reference image.");

            var nonWhiteRatio = NonWhitePixelRatio(bitmap);
            Assert.IsGreaterThan(
                0.03,
                nonWhiteRatio,
                $"{scenarioName} must not be blank or near-blank; non-white ratio was {nonWhiteRatio:P2}.");
        }
    }

    [TestMethod]
    public void PublicAdminWorkbenchRequiresDenseSettingsEditorComponents()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json")));
        var components = document.RootElement
            .GetProperty("requirements")
            .EnumerateArray()
            .Select(requirement => requirement.GetProperty("component").GetString())
            .Where(component => !string.IsNullOrWhiteSpace(component))
            .ToHashSet(StringComparer.Ordinal);

        string[] requiredComponents =
        {
            "NavigationView",
            "CommandBar",
            "InfoBar",
            "ListView",
            "TextBox",
            "ComboBox",
            "CheckBox",
            "RadioButton",
            "ToggleSwitch",
            "Slider",
            "ProgressBar",
            "Button",
            "AppBarButton"
        };

        foreach (var component in requiredComponents)
        {
            Assert.IsTrue(
                components.Contains(component),
                $"public-admin-workbench-light must include a Settings / Policy Editor requirement for {component}.");
        }
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
    public async Task PublicCommandsMenusScenarioKeepsPopupStateOutOfStaticNativeReference()
    {
        var scenarioPath = Path.Combine(
            FindRepositoryRoot(),
            "fixtures",
            "ComponentParityLab.WinUI",
            "scenarios",
            "component-commands-menus-light.json");
        var scenario = await VisualScenario.LoadAsync(scenarioPath);
        var popupInteractionTypes = new[] { "openPopup", "invokeMenuItem" };

        foreach (var action in scenario.Interactions)
        {
            CollectionAssert.DoesNotContain(
                popupInteractionTypes,
                action.Type,
                "The public commands/menus base scenario is static native-reference evidence; open popup state belongs in explicit open-popup diagnostics.");
        }

        foreach (var requirement in scenario.Requirements.Where(requirement =>
            requirement.Component is "CommandBarFlyout" or "MenuFlyout"))
        {
            CollectionAssert.DoesNotContain(
                requirement.RequiredProperties.ToArray(),
                "open-popup",
                $"'{requirement.Component}' must not require open-popup evidence in the static public commands/menus scenario.");
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
    public void VisualLayoutEngineExportsSymbolIconLayout()
    {
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new StackPanel
            {
                Children =
                {
                    new SymbolIcon { Name = "LinkIcon", Symbol = Symbol.Link }
                }
            }
        });
        var settings = new VisualRunSettings(null, "symbol", "skia-v2", new VisualViewport(120, 80), 1, "light", true, new VisualThresholds());

        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var icon = RequireNode(arranged.Root, "LinkIcon");

        Assert.HasCount(0, unsupported);
        Assert.AreEqual("Link", icon.Properties["symbol"]);
        Assert.AreEqual(32, icon.Layout!.Width);
        Assert.AreEqual(32, icon.Layout.Height);
    }

    [TestMethod]
    public void VisualLayoutEngineUsesNativeSizedBasicInputControlBounds()
    {
        var statusComboBox = new ComboBox { Name = "StatusComboBox", PlaceholderText = "Status" };
        statusComboBox.Items.Add("Closed");
        statusComboBox.SelectedIndex = 0;
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new Button { Name = "PrimaryActionButton", Content = "Run primary action" },
                    new ToggleButton { Name = "PinnedToggleButton", Content = "Pinned", IsChecked = true },
                    new CheckBox { Name = "EnabledCheckBox", Content = "Enabled", IsChecked = true },
                    new RadioButton { Name = "HighPriorityRadioButton", Content = "High priority", IsChecked = true },
                    statusComboBox,
                    new Microsoft.UI.Xaml.Controls.Primitives.RepeatButton { Name = "DiagnosticRepeatButton", Content = "Repeat action" },
                    new HyperlinkButton { Name = "DiagnosticHyperlinkButton", Content = "Open public link" },
                    new DropDownButton { Name = "DiagnosticDropDownButton", Content = "Choose action" },
                    new SplitButton { Name = "DiagnosticSplitButton", Content = "Split action" },
                    new ToggleSplitButton { Name = "DiagnosticToggleSplitButton", Content = "Toggle split", IsChecked = true },
                    new Slider { Name = "DiagnosticSlider", Minimum = 0, Maximum = 100, Value = 64 },
                    new ToggleSwitch { Name = "DiagnosticToggleSwitch", Header = "Enabled", IsOn = true },
                    new RatingControl { Name = "DiagnosticRatingControl", MaxRating = 5, Value = 4 }
                }
            }
        });
        var settings = new VisualRunSettings(null, "component-basic-input-light", "skia-v2", new VisualViewport(1028, 720), 1, "light", true, new VisualThresholds());

        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);

        Assert.HasCount(0, unsupported);
        AssertLayoutSize(arranged.Root, "PrimaryActionButton", width: 142, height: 32);
        AssertLayoutSize(arranged.Root, "PinnedToggleButton", width: 67, height: 32);
        AssertLayoutSize(arranged.Root, "EnabledCheckBox", width: 120, height: 32);
        AssertLayoutSize(arranged.Root, "HighPriorityRadioButton", width: 120, height: 32);
        AssertLayoutSize(arranged.Root, "StatusComboBox", width: 92, height: 32);
        AssertLayoutSize(arranged.Root, "DiagnosticRepeatButton", width: 120, height: 32);
        AssertLayoutSize(arranged.Root, "DiagnosticHyperlinkButton", width: 125, height: 32);
        AssertLayoutSize(arranged.Root, "DiagnosticDropDownButton", width: 132, height: 32);
        AssertLayoutSize(arranged.Root, "DiagnosticSplitButton", width: 127, height: 30);
        AssertLayoutSize(arranged.Root, "DiagnosticToggleSplitButton", width: 129, height: 30);
        AssertLayoutSize(arranged.Root, "DiagnosticSlider", width: 180, height: 32);
        AssertLayoutSize(arranged.Root, "DiagnosticToggleSwitch", width: 120, height: 63);
        AssertLayoutSize(arranged.Root, "DiagnosticRatingControl", width: 120, height: 32);
    }

    [TestMethod]
    public void ComponentEvidenceBuilderUsesDiagnosticDescendantLayoutForLabeledHosts()
    {
        var scenario = new VisualScenario
        {
            FixtureName = "component-parity-lab",
            Name = "component-basic-input-light",
            Requirements = new[]
            {
                new VisualRequirement
                {
                    Component = "RepeatButton",
                    Target = "DiagnosticRepeatButton",
                    ExpectedStatus = CompatibilityStatuses.Partial,
                    MinimumVisualGrade = "usable",
                    VisualGrade = "usable"
                }
            }
        };
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new ContentControl
            {
                Name = "DiagnosticRepeatButton",
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "RepeatButton", Width = 180 },
                        new Microsoft.UI.Xaml.Controls.Primitives.RepeatButton { Content = "Repeat action", MinWidth = 120 }
                    }
                }
            }
        });
        var settings = new VisualRunSettings(scenario, scenario.Name, "skia-v2", new VisualViewport(420, 120), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var hostLayout = RequireNode(arranged.Root, "DiagnosticRepeatButton").Layout!;

        var evidence = ComponentEvidenceBuilder.Build(scenario, arranged, interactions: null, metrics: null);

        var layout = evidence.Components.Single().LayoutRegion ?? throw new AssertFailedException("Expected crop target layout.");
        Assert.AreEqual(120, layout.Width);
        Assert.AreEqual(32, layout.Height);
        Assert.IsGreaterThan(hostLayout.X, layout.X, "Diagnostic crop target should isolate the descendant control after the row label.");
        Assert.IsLessThan(hostLayout.Width, layout.Width, "Diagnostic crop target must not use the whole labeled host row.");
    }

    [TestMethod]
    public void ComponentEvidenceBuilderKeepsStaticFlyoutHostLayout()
    {
        var scenario = new VisualScenario
        {
            FixtureName = "component-parity-lab",
            Name = "component-commands-menus-light",
            Requirements = new[]
            {
                new VisualRequirement
                {
                    Component = "CommandBarFlyout",
                    Target = "DiagnosticCommandBarFlyout",
                    ExpectedStatus = CompatibilityStatuses.Partial,
                    MinimumVisualGrade = "usable",
                    VisualGrade = "usable"
                }
            }
        };
        var flyout = new CommandBarFlyout();
        flyout.PrimaryCommands.Add(new AppBarButton { Label = "Approve" });
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new ContentControl
            {
                Name = "DiagnosticCommandBarFlyout",
                Content = new Button
                {
                    Content = "Command flyout",
                    MinWidth = 165,
                    Flyout = flyout
                }
            }
        });
        var settings = new VisualRunSettings(scenario, scenario.Name, "skia-v2", new VisualViewport(420, 160), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var hostLayout = RequireNode(arranged.Root, "DiagnosticCommandBarFlyout").Layout!;

        var evidence = ComponentEvidenceBuilder.Build(scenario, arranged, interactions: null, metrics: null);

        var layout = evidence.Components.Single().LayoutRegion ?? throw new AssertFailedException("Expected static flyout host layout.");
        Assert.AreEqual(hostLayout.X, layout.X);
        Assert.AreEqual(hostLayout.Y, layout.Y);
        Assert.AreEqual(hostLayout.Width, layout.Width);
        Assert.AreEqual(hostLayout.Height, layout.Height);
    }

    [TestMethod]
    public void SkiaV2FontResolverPrefersSegoeTextAndFluentSymbolFonts()
    {
        var installed = new HashSet<string>(new[]
        {
            "Helvetica Neue",
            "Segoe UI",
            "Segoe MDL2 Assets",
            "Segoe Fluent Icons"
        }, StringComparer.OrdinalIgnoreCase);
        var plan = FontResolver.Plan(installed.Contains);

        Assert.AreEqual("Segoe UI", plan.Text.MatchedFamily);
        Assert.AreEqual("Segoe Fluent Icons", plan.Symbol.MatchedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Text.FallbackMode);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void SkiaV2FontResolverFallsBackFromFluentSymbolsToMdl2()
    {
        var installed = new HashSet<string>(new[]
        {
            "Segoe UI",
            "Segoe MDL2 Assets"
        }, StringComparer.OrdinalIgnoreCase);
        var plan = FontResolver.Plan(installed.Contains);

        Assert.AreEqual("Segoe UI", plan.Text.MatchedFamily);
        Assert.AreEqual("Segoe MDL2 Assets", plan.Symbol.MatchedFamily);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Text.FallbackMode);
        Assert.AreEqual(FontResolver.RequestedFamilyMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void SkiaV2FontResolverReportsTextAndIconFallbackSeparately()
    {
        var installed = new HashSet<string>(new[]
        {
            "Helvetica Neue"
        }, StringComparer.OrdinalIgnoreCase);
        var plan = FontResolver.Plan(installed.Contains);

        Assert.IsNull(plan.Text.MatchedFamily);
        Assert.IsNull(plan.Symbol.MatchedFamily);
        Assert.AreEqual(FontResolver.PlatformFallbackMode, plan.Text.FallbackMode);
        Assert.AreEqual(FontResolver.TextFontFallbackMode, plan.Symbol.FallbackMode);
    }

    [TestMethod]
    public void VisualLayoutEnginePlacesCommandBarContentBeforePrimaryCommands()
    {
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new CommandBar
            {
                Name = "ContentCommandBar",
                Width = 360,
                Content = new TextBlock { Name = "InlineCommandContent", Text = "Inline command content" },
                PrimaryCommands =
                {
                    new AppBarButton { Name = "AcceptCommand", Label = "Accept" }
                }
            }
        });
        var settings = new VisualRunSettings(null, "commandbar-content", "skia-v2", new VisualViewport(480, 120), 1, "light", true, new VisualThresholds());

        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var content = RequireNode(arranged.Root, "InlineCommandContent");
        var command = RequireNode(arranged.Root, "AcceptCommand");

        Assert.HasCount(0, unsupported);
        Assert.IsLessThan(command.Layout!.X, content.Layout!.X, "CommandBar.Content must be laid out before primary commands.");
        Assert.IsFalse(content.Properties.ContainsKey("commandBarCompact"), "CommandBar.Content must not be tagged as compact command chrome.");
        Assert.IsTrue((bool)command.Properties["commandBarCompact"]!);
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsAdaptiveContainerDiagnostics()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "adaptive");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Adaptive containers",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new Expander { Name = "DetailsExpander", Header = "More details", Content = "Expanded content", IsExpanded = true, Width = 260, Height = 92 },
                    new AnnotatedScrollBar { Name = "MarkerScrollBar", MarkerCount = 3, Width = 180, Height = 92 },
                    new SemanticZoom { Name = "SummaryZoom", ZoomedInView = new TextBlock { Text = "Detailed item" }, ZoomedOutView = new TextBlock { Text = "Group" }, Width = 260, Height = 96 },
                    new SplitView { Name = "OpenSplitView", Pane = new TextBlock { Text = "Pane" }, Content = new TextBlock { Text = "Content" }, IsPaneOpen = true, Width = 260, Height = 96 },
                    new TwoPaneView { Name = "PairTwoPaneView", Pane1 = new TextBlock { Text = "Pane 1" }, Pane2 = new TextBlock { Text = "Pane 2" }, Width = 300, Height = 72 }
                }
            }
        });
        var theme = SkiaV2Theme.For("light");
        var settings = new VisualRunSettings(null, "adaptive", "skia-v2", new VisualViewport(520, 420), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "adaptive", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        Assert.IsTrue((bool)RequireNode(arranged.Root, "DetailsExpander").Properties["isExpanded"]!);
        Assert.IsTrue((bool)RequireNode(arranged.Root, "OpenSplitView").Properties["isPaneOpen"]!);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var scrollBar = RequireNode(arranged.Root, "MarkerScrollBar").Layout!;
        var splitView = RequireNode(arranged.Root, "OpenSplitView").Layout!;
        var twoPaneNode = RequireNode(arranged.Root, "PairTwoPaneView");
        var twoPaneView = twoPaneNode.Layout!;

        Assert.IsGreaterThan(10, CountExactPixels(bitmap, new SKRect((float)scrollBar.X, (float)scrollBar.Y, (float)(scrollBar.X + scrollBar.Width), (float)(scrollBar.Y + scrollBar.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)splitView.X, (float)splitView.Y, (float)(splitView.X + splitView.Width), (float)(splitView.Y + splitView.Height)), theme.PaneBackground));
        Assert.IsGreaterThan(100, twoPaneNode.Children[1].Layout!.X - twoPaneNode.Children[0].Layout!.X);
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)twoPaneView.X, (float)twoPaneView.Y, (float)(twoPaneView.X + twoPaneView.Width), (float)(twoPaneView.Y + twoPaneView.Height)), theme.Surface));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsAcceptedReturnTextBoxLines()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "multiline-textbox");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Messages",
            Content = new TextBox
            {
                Name = "MessageComposer",
                Text = "Line one\nLine two",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Width = 320,
                MinHeight = 96
            }
        });
        var settings = new VisualRunSettings(null, "multiline-textbox", "skia-v2", new VisualViewport(420, 180), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "multiline-textbox", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var composer = RequireNode(arranged.Root, "MessageComposer").Layout!;
        var secondLineBand = new SKRect(
            (float)(composer.X + 8),
            (float)(composer.Y + 28),
            (float)(composer.X + 160),
            (float)(composer.Y + 52));
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, secondLineBand, 80));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsPasswordBoxWithoutLeakingPassword()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "passwordbox");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Login",
            Content = new PasswordBox
            {
                Name = "PasswordBox",
                Header = "Password",
                Password = "not-a-real-secret",
                PlaceholderText = "Password",
                Width = 320
            }
        });
        var settings = new VisualRunSettings(null, "passwordbox", "skia-v2", new VisualViewport(420, 180), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "passwordbox", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        var passwordNode = RequireNode(arranged.Root, "PasswordBox");
        Assert.IsFalse(passwordNode.Properties.ContainsKey("password"));
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var password = passwordNode.Layout!;
        var chromeBand = new SKRect(
            (float)(password.X + 2),
            (float)(password.Y + 2),
            (float)(password.X + password.Width - 2),
            (float)(password.Y + password.Height - 2));
        var headerBand = new SKRect(
            (float)(password.X + 8),
            (float)(password.Y + 2),
            (float)(password.X + 120),
            (float)(password.Y + 22));
        var glyphBand = new SKRect(
            (float)(password.X + 8),
            (float)(password.Y + 28),
            (float)(password.X + 160),
            (float)(password.Y + 54));
        Assert.IsGreaterThan(100, CountBrightPixels(bitmap, chromeBand, 230));
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, headerBand, 80));
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, glyphBand, 80));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsFocusedTextBoxUnderlineAndClearButton()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "focused-textbox-clear");
        var focusedTextBox = new TextBox
        {
            Name = "FocusedTextBox",
            Text = "search query",
            Width = 240,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        focusedTextBox.Focus(FocusState.Programmatic);
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = focusedTextBox
        });
        var settings = new VisualRunSettings(null, "focused-textbox-clear", "skia-v2", new VisualViewport(320, 80), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "focused-textbox-clear", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var textBox = RequireNode(arranged.Root, "FocusedTextBox").Layout!;
        var underlineBand = new SKRect(
            (float)textBox.X,
            (float)(textBox.Y + textBox.Height - 4),
            (float)(textBox.X + textBox.Width),
            (float)(textBox.Y + textBox.Height));
        var clearBand = new SKRect(
            (float)(textBox.X + textBox.Width - 28),
            (float)(textBox.Y + 8),
            (float)(textBox.X + textBox.Width - 8),
            (float)(textBox.Y + 24));

        Assert.IsGreaterThan(24, CountExactPixels(bitmap, underlineBand, SkiaV2Theme.For("light").Accent));
        Assert.IsGreaterThan(4, CountDarkPixels(bitmap, clearBand, 170));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsNavigationViewItemIcons()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "navigation-icons");
        var item = new NavigationViewItem
        {
            Name = "HomeNavigationItem",
            Content = "Home",
            Icon = new FontIcon { Glyph = "\uE80F" }
        };
        var profileItem = new NavigationViewItem
        {
            Name = "ProfileNavigationItem",
            Content = "Profile",
            Icon = new FontIcon { Glyph = "\uE77B" }
        };
        var navigation = new NavigationView
        {
            Name = "RootNavigation",
            Content = new TextBlock { Text = "Home" }
        };
        navigation.MenuItems.Add(item);
        navigation.MenuItems.Add(profileItem);
        navigation.Select(item);
        var tree = UiTreeBuilder.Build(new Window { Content = navigation });
        var settings = new VisualRunSettings(null, "navigation-icons", "skia-v2", new VisualViewport(520, 240), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "navigation-icons", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        var arrangedItem = RequireNode(arranged.Root, "HomeNavigationItem");
        var arrangedProfileItem = RequireNode(arranged.Root, "ProfileNavigationItem");
        Assert.IsTrue(arrangedItem.Children.Any(child => child.Type.EndsWith(".FontIcon", StringComparison.Ordinal)));
        Assert.IsTrue(arrangedProfileItem.Children.Any(child => child.Type.EndsWith(".FontIcon", StringComparison.Ordinal)));
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var row = arrangedItem.Layout!;
        var iconBand = new SKRect(
            (float)(row.X + 8),
            (float)(row.Y + 8),
            (float)(row.X + 34),
            (float)(row.Y + 32));
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, iconBand, 230));
        var profileRow = arrangedProfileItem.Layout!;
        var profileIconBand = new SKRect(
            (float)(profileRow.X + 8),
            (float)(profileRow.Y + 8),
            (float)(profileRow.X + 34),
            (float)(profileRow.Y + 32));
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, profileIconBand, 230));
    }

    [TestMethod]
    public void UiTreeExportsNonZeroDefaultFontIconSize()
    {
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new FontIcon
            {
                Name = "DefaultFontIcon",
                Glyph = "\uE80F"
            }
        });

        var icon = RequireNode(tree.Root, "DefaultFontIcon");

        Assert.AreEqual(20d, icon.Properties["fontSize"]);
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsAutoSuggestBoxSearchPrimitive()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "autosuggestbox-search");
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new AutoSuggestBox
            {
                Name = "SearchBox",
                Text = "applications",
                Width = 260
            }
        });
        var settings = new VisualRunSettings(null, "autosuggestbox-search", "skia-v2", new VisualViewport(360, 120), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "autosuggestbox-search", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var search = RequireNode(arranged.Root, "SearchBox").Layout!;
        var handleBand = new SKRect(
            (float)(search.X + 21),
            (float)(search.Y + 19),
            (float)(search.X + 31),
            (float)(search.Y + 29));
        Assert.IsGreaterThan(4, CountDarkPixels(bitmap, handleBand, 230));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererHonorsSemiBoldTextBlockWeight()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "textblock-weight");
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Name = "NormalTitle", Text = "Applications queue" },
                    new TextBlock { Name = "SemiBoldTitle", Text = "Applications queue", FontWeight = "SemiBold" }
                }
            }
        });
        var settings = new VisualRunSettings(null, "textblock-weight", "skia-v2", new VisualViewport(360, 140), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "textblock-weight", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var normal = RequireNode(arranged.Root, "NormalTitle").Layout!;
        var semiBold = RequireNode(arranged.Root, "SemiBoldTitle").Layout!;
        var normalPixels = CountDarkPixels(bitmap, new SKRect((float)normal.X, (float)normal.Y, (float)(normal.X + normal.Width), (float)(normal.Y + normal.Height)), 120);
        var semiBoldPixels = CountDarkPixels(bitmap, new SKRect((float)semiBold.X, (float)semiBold.Y, (float)(semiBold.X + semiBold.Width), (float)(semiBold.Y + semiBold.Height)), 120);
        Assert.IsGreaterThan(normalPixels + 8, semiBoldPixels);
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsListViewItemChildContent()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "listview-child-content");
        var list = new ListView
        {
            Name = "QueueList",
            Width = 360,
            Height = 140,
            SelectedIndex = 0
        };
        list.Items.Add(new StackPanel
        {
            Name = "ApplicationsQueueItem",
            Children =
            {
                new TextBlock { Text = "Applications queue" },
                new TextBlock { Text = "6 pending reviews" }
            }
        });
        list.Items.Add(new StackPanel
        {
            Name = "ReportsQueueItem",
            Children =
            {
                new TextBlock { Text = "Reports queue" },
                new TextBlock { Text = "2 exports waiting" }
            }
        });
        var tree = UiTreeBuilder.Build(new Window { Content = list });
        var settings = new VisualRunSettings(null, "listview-child-content", "skia-v2", new VisualViewport(440, 220), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "listview-child-content", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var item = RequireNode(arranged.Root, "ApplicationsQueueItem").Layout!;
        var secondItem = RequireNode(arranged.Root, "ReportsQueueItem").Layout!;
        Assert.IsGreaterThanOrEqualTo(item.Y + item.Height + 4, secondItem.Y);
        var childTextBand = new SKRect(
            (float)(item.X + 8),
            (float)(item.Y + 24),
            (float)(item.X + 180),
            (float)(item.Y + 48));
        Assert.IsGreaterThan(8, CountDarkPixels(bitmap, childTextBand, 80));
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
        var fontDiagnostics = first.FontDiagnostics ?? throw new AssertFailedException("Expected skia-v2 font diagnostics.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(fontDiagnostics.Text.ResolvedFamily));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fontDiagnostics.Symbol.ResolvedFamily));
        CollectionAssert.Contains(fontDiagnostics.Text.RequestedFamilies.ToArray(), "Segoe UI");
        CollectionAssert.Contains(fontDiagnostics.Symbol.RequestedFamilies.ToArray(), "Segoe Fluent Icons");
        CollectionAssert.AreEqual(await Sha256Async(first.FilePath), await Sha256Async(second.FilePath));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererUsesSoftInsetSeparatorForCheckedToggleSplitButton()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "toggle-split");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Toggle split chrome",
            Content = new StackPanel
            {
                Children =
                {
                    new ToggleSplitButton
                    {
                        Name = "PinnedToggleSplitButton",
                        Content = "Go",
                        IsChecked = true,
                        Width = 129,
                        Height = 30,
                        HorizontalAlignment = HorizontalAlignment.Left
                    }
                }
            }
        });
        var theme = SkiaV2Theme.For("light");
        var settings = new VisualRunSettings(null, "toggle-split", "skia-v2", new VisualViewport(180, 80), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "toggle-split", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var toggleSplit = RequireNode(arranged.Root, "PinnedToggleSplitButton").Layout!;
        var separatorZone = new SKRect(
            (float)(toggleSplit.X + toggleSplit.Width - 38),
            (float)toggleSplit.Y + 4,
            (float)(toggleSplit.X + toggleSplit.Width - 28),
            (float)(toggleSplit.Y + toggleSplit.Height - 4));

        Assert.AreEqual(
            0,
            CountBrightPixels(bitmap, separatorZone, minimumChannelValue: 120),
            "Checked ToggleSplitButton should use a softened separator instead of a high-contrast light divider.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererAlignsDropdownChevronsToNativeVerticalCenter()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "dropdown-chevron");
        var comboBox = new ComboBox
        {
            Name = "StatusComboBox",
            Width = 92,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        comboBox.Items.Add("Closed");
        comboBox.SelectedIndex = 0;
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Dropdown chevrons",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    comboBox,
                    new SplitButton
                    {
                        Name = "DiagnosticSplitButton",
                        Content = "Split action",
                        Width = 127,
                        Height = 30,
                        HorizontalAlignment = HorizontalAlignment.Left
                    }
                }
            }
        });
        var settings = new VisualRunSettings(null, "dropdown-chevron", "skia-v2", new VisualViewport(180, 100), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "dropdown-chevron", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var combo = RequireNode(arranged.Root, "StatusComboBox").Layout!;
        var split = RequireNode(arranged.Root, "DiagnosticSplitButton").Layout!;
        var comboChevron = BoundsOfPixelsMatching(
            bitmap,
            new SKRect((float)(combo.X + combo.Width - 28), (float)combo.Y + 8, (float)(combo.X + combo.Width - 8), (float)combo.Y + 22),
            pixel => pixel.Red <= 180 && pixel.Green <= 180 && pixel.Blue <= 180);
        var splitChevron = BoundsOfPixelsMatching(
            bitmap,
            new SKRect((float)(split.X + split.Width - 28), (float)split.Y + 8, (float)(split.X + split.Width - 8), (float)split.Y + 22),
            pixel => pixel.Red <= 180 && pixel.Green <= 180 && pixel.Blue <= 180);

        Assert.IsFalse(comboChevron.IsEmpty, "Expected ComboBox dropdown chevron.");
        Assert.IsFalse(splitChevron.IsEmpty, "Expected SplitButton dropdown chevron.");
        Assert.IsLessThanOrEqualTo(comboChevron.Top, (int)combo.Y + 13, $"ComboBox chevron should align to native vertical center; actual bounds were {comboChevron}.");
        Assert.IsLessThanOrEqualTo(splitChevron.Top, (int)split.Y + 12, $"SplitButton chevron should align to native vertical center; actual bounds were {splitChevron}.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererCentersButtonFamilyTextInsideNativeSizedBounds()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "button-text");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Button text",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new Button
                    {
                        Name = "PrimaryActionButton",
                        Content = "Run primary action",
                        Width = 142,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Left
                    },
                    new Microsoft.UI.Xaml.Controls.Primitives.RepeatButton
                    {
                        Name = "DiagnosticRepeatButton",
                        Content = "Repeat action",
                        Width = 120,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Left
                    },
                    new ToggleButton
                    {
                        Name = "PinnedToggleButton",
                        Content = "Pinned",
                        IsChecked = true,
                        Width = 67,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Left
                    }
                }
            }
        });
        var settings = new VisualRunSettings(null, "button-text", "skia-v2", new VisualViewport(240, 160), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "button-text", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var repeat = RequireNode(arranged.Root, "DiagnosticRepeatButton").Layout!;
        var toggle = RequireNode(arranged.Root, "PinnedToggleButton").Layout!;
        var repeatBounds = BoundsOfPixelsMatching(
            bitmap,
            LayoutRectToSkRect(repeat),
            pixel => pixel.Red <= 145 && pixel.Green <= 145 && pixel.Blue <= 145);
        var toggleTextBounds = BoundsOfPixelsMatching(
            bitmap,
            Inset(LayoutRectToSkRect(toggle), 4),
            pixel => pixel.Red >= 150 && pixel.Green >= 180 && pixel.Blue >= 200);

        Assert.IsFalse(repeatBounds.IsEmpty, "Expected rendered RepeatButton text pixels.");
        Assert.IsFalse(toggleTextBounds.IsEmpty, "Expected rendered ToggleButton text pixels.");
        AssertTextCenterWithin(repeatBounds, repeat, tolerance: 1.5f);
        AssertTextCenterWithin(toggleTextBounds, toggle, tolerance: 1.5f);
        Assert.IsTrue(
            repeatBounds.Bottom <= repeat.Y + 23,
            $"RepeatButton text baseline should not sit below the native crop text box; actual bounds were {repeatBounds}.");
        Assert.IsTrue(
            toggleTextBounds.Top <= toggle.Y + 10,
            $"Checked ToggleButton text should share the native vertical centering; actual bounds were {toggleTextBounds}.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDoesNotUnderlineDefaultHyperlinkButton()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "hyperlink-button");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "HyperlinkButton",
            Content = new HyperlinkButton
            {
                Name = "DiagnosticHyperlinkButton",
                Content = "Open public link",
                Width = 125,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Left
            }
        });
        var settings = new VisualRunSettings(null, "hyperlink-button", "skia-v2", new VisualViewport(180, 64), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "hyperlink-button", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var hyperlink = RequireNode(arranged.Root, "DiagnosticHyperlinkButton").Layout!;
        var underlineBand = new SKRect(
            (float)hyperlink.X,
            (float)hyperlink.Y + 24,
            (float)(hyperlink.X + hyperlink.Width),
            (float)hyperlink.Y + 27);
        var underlineBounds = BoundsOfPixelsMatching(bitmap, underlineBand, IsBlueTextLike);

        Assert.IsTrue(
            underlineBounds.IsEmpty || underlineBounds.Width < 40,
            "Default HyperlinkButton should match native text-only chrome and avoid drawing a persistent underline.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererCentersDefaultHyperlinkButtonText()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "hyperlink-button-origin");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "HyperlinkButton origin",
            Content = new HyperlinkButton
            {
                Name = "DiagnosticHyperlinkButton",
                Content = "Open public link",
                Width = 125,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Left
            }
        });
        var settings = new VisualRunSettings(null, "hyperlink-button-origin", "skia-v2", new VisualViewport(180, 64), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "hyperlink-button-origin", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var hyperlink = RequireNode(arranged.Root, "DiagnosticHyperlinkButton").Layout!;
        var textBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(hyperlink), IsBlueTextLike);

        Assert.IsFalse(textBounds.IsEmpty, "Expected rendered HyperlinkButton text pixels.");
        AssertTextCenterWithin(textBounds, hyperlink, tolerance: 1.5f);
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererUsesNativeCheckBoxAndRadioLeadingChrome()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "check-radio-leading");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Check and radio leading chrome",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new CheckBox
                    {
                        Name = "EnabledCheckBox",
                        Content = "Enabled",
                        IsChecked = true,
                        Width = 120,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Left
                    },
                    new RadioButton
                    {
                        Name = "HighPriorityRadioButton",
                        Content = "High priority",
                        IsChecked = true,
                        Width = 120,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Left
                    }
                }
            }
        });
        var settings = new VisualRunSettings(null, "check-radio-leading", "skia-v2", new VisualViewport(180, 100), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "check-radio-leading", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var checkBox = RequireNode(arranged.Root, "EnabledCheckBox").Layout!;
        var radio = RequireNode(arranged.Root, "HighPriorityRadioButton").Layout!;
        var checkAccentBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(checkBox), IsAccentLike);
        var radioAccentBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(radio), IsAccentLike);
        var checkTextBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(checkBox), IsDarkTextLike);
        var radioTextBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(radio), IsDarkTextLike);

        Assert.IsFalse(checkAccentBounds.IsEmpty, "Expected checked CheckBox accent chrome.");
        Assert.IsFalse(radioAccentBounds.IsEmpty, "Expected selected RadioButton accent chrome.");
        Assert.IsFalse(checkTextBounds.IsEmpty, "Expected CheckBox label text.");
        Assert.IsFalse(radioTextBounds.IsEmpty, "Expected RadioButton label text.");
        Assert.IsLessThanOrEqualTo((int)checkBox.X + 1, checkAccentBounds.Left, $"CheckBox checked chrome should start at the native leading edge; actual bounds were {checkAccentBounds}.");
        Assert.IsLessThanOrEqualTo((int)radio.X + 1, radioAccentBounds.Left, $"RadioButton selected chrome should start at the native leading edge; actual bounds were {radioAccentBounds}.");
        Assert.IsLessThanOrEqualTo((int)checkBox.X + 30, checkTextBounds.Left, $"CheckBox label should use native compact leading spacing; actual bounds were {checkTextBounds}.");
        Assert.IsLessThanOrEqualTo((int)radio.X + 30, radioTextBounds.Left, $"RadioButton label should use native compact leading spacing; actual bounds were {radioTextBounds}.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererAlignsCheckedCheckBoxLabelToNativeCrop()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "checkbox-label-crop");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "CheckBox label crop",
            Content = new CheckBox
            {
                Name = "EnabledCheckBox",
                Content = "Enabled",
                IsChecked = true,
                Width = 120,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Left
            }
        });
        var settings = new VisualRunSettings(null, "checkbox-label-crop", "skia-v2", new VisualViewport(160, 64), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "checkbox-label-crop", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var checkBox = RequireNode(arranged.Root, "EnabledCheckBox").Layout!;
        var textBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(checkBox), IsDarkTextLike);

        Assert.IsFalse(textBounds.IsEmpty, "Expected CheckBox label text.");
        Assert.IsGreaterThanOrEqualTo((int)checkBox.X + 30, textBounds.Left, $"Checked CheckBox label should begin at the native text inset; actual bounds were {textBounds}.");
        Assert.IsLessThanOrEqualTo(textBounds.Top, (int)checkBox.Y + 10, $"Checked CheckBox label should align with the native label top; actual bounds were {textBounds}.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererAlignsSelectedRadioButtonLabelToNativeCrop()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "radio-label-crop");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "RadioButton label crop",
            Content = new RadioButton
            {
                Name = "HighPriorityRadioButton",
                Content = "High priority",
                IsChecked = true,
                Width = 120,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Left
            }
        });
        var settings = new VisualRunSettings(null, "radio-label-crop", "skia-v2", new VisualViewport(160, 64), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "radio-label-crop", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var radio = RequireNode(arranged.Root, "HighPriorityRadioButton").Layout!;
        var textBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(radio), IsDarkTextLike);

        Assert.IsFalse(textBounds.IsEmpty, "Expected RadioButton label text.");
        Assert.IsGreaterThanOrEqualTo((int)radio.X + 30, textBounds.Left, $"Selected RadioButton label should begin at the native text inset; actual bounds were {textBounds}.");
        Assert.IsLessThanOrEqualTo(textBounds.Top, (int)radio.Y + 11, $"Selected RadioButton label should align with the native label top; actual bounds were {textBounds}.");
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererUsesNativeCheckedToggleButtonChrome()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "checked-toggle-button");
        var theme = SkiaV2Theme.For("light");
        var tree = UiTreeBuilder.Build(new Window
        {
            Title = "Checked ToggleButton",
            Content = new StackPanel
            {
                Children =
                {
                    new ToggleButton
                    {
                        Name = "PinnedToggleButton",
                        Content = "Pinned",
                        IsChecked = true,
                        Width = 67,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Left
                    }
                }
            }
        });
        var settings = new VisualRunSettings(null, "checked-toggle-button", "skia-v2", new VisualViewport(120, 80), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out _);
        var options = new SnapshotRenderOptions("skia-v2", "checked-toggle-button", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var toggle = RequireNode(arranged.Root, "PinnedToggleButton").Layout!;
        var fillSample = bitmap.GetPixel((int)toggle.X + 8, (int)toggle.Y + 8);
        var bottomEdgeSample = bitmap.GetPixel((int)(toggle.X + toggle.Width / 2), (int)(toggle.Y + toggle.Height) - 1);

        Assert.AreEqual(theme.Accent, fillSample, "Checked ToggleButton fill should match the native checked button blue.");
        Assert.AreEqual(new SKColor(0x00, 0x3e, 0x73), bottomEdgeSample, "Checked ToggleButton should retain the darker native bottom edge.");
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
                    new SymbolIcon { Name = "LinkSymbolIcon", Symbol = Symbol.Link },
                    new DropDownButton { Name = "ChoiceDropDownButton", Content = "Choose" },
                    new SplitButton { Name = "ChoiceSplitButton", Content = "Split" },
                    new ToggleSplitButton { Name = "PinnedToggleSplitButton", Content = "Toggle split", IsChecked = true },
                    new MenuBar
                    {
                        Name = "PrimaryMenuBar",
                        Items =
                        {
                            new MenuBarItem
                            {
                                Title = "File",
                                Items =
                                {
                                    new MenuFlyoutItem { Text = "Open" }
                                }
                            }
                        }
                    },
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
        var symbol = RequireNode(arranged.Root, "LinkSymbolIcon").Layout!;
        var toggleSplit = RequireNode(arranged.Root, "PinnedToggleSplitButton").Layout!;
        var menuBar = RequireNode(arranged.Root, "PrimaryMenuBar").Layout!;
        var infoBar = RequireNode(arranged.Root, "StatusInfoBar").Layout!;

        Assert.IsGreaterThan(100, CountExactPixels(bitmap, new SKRect((float)toggle.X, (float)toggle.Y, (float)(toggle.X + toggle.Width), (float)(toggle.Y + toggle.Height)), theme.Accent));
        Assert.IsGreaterThan(50, CountExactPixels(bitmap, new SKRect((float)checkBox.X + 2, (float)checkBox.Y + 9, (float)checkBox.X + 22, (float)checkBox.Y + 29), theme.Accent));
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)checkBox.X + 2, (float)checkBox.Y + 9, (float)checkBox.X + 22, (float)checkBox.Y + 29), theme.Surface));
        Assert.IsGreaterThan(0, CountDarkPixels(bitmap, new SKRect((float)(combo.X + combo.Width - 28), (float)combo.Y + 14, (float)(combo.X + combo.Width - 8), (float)combo.Y + 26), maximumChannelValue: 180));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)progressBar.X, (float)(progressBar.Y + progressBar.Height / 2 - 3), (float)(progressBar.X + progressBar.Width), (float)(progressBar.Y + progressBar.Height / 2 + 3)), theme.Accent));
        Assert.IsGreaterThan(10, CountExactPixels(bitmap, new SKRect((float)progressRing.X, (float)progressRing.Y, (float)(progressRing.X + progressRing.Width), (float)(progressRing.Y + progressRing.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)slider.X, (float)slider.Y, (float)(slider.X + slider.Width), (float)(slider.Y + slider.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)toggleSwitch.X, (float)toggleSwitch.Y, (float)(toggleSwitch.X + toggleSwitch.Width), (float)(toggleSwitch.Y + toggleSwitch.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)rating.X, (float)rating.Y, (float)(rating.X + rating.Width), (float)(rating.Y + rating.Height)), theme.Accent));
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)symbol.X, (float)symbol.Y, (float)(symbol.X + symbol.Width), (float)(symbol.Y + symbol.Height)), theme.Accent));
        Assert.IsGreaterThan(40, CountExactPixels(bitmap, new SKRect((float)toggleSplit.X, (float)toggleSplit.Y, (float)(toggleSplit.X + toggleSplit.Width), (float)(toggleSplit.Y + toggleSplit.Height)), theme.Accent));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)menuBar.X, (float)menuBar.Y, (float)(menuBar.X + menuBar.Width), (float)(menuBar.Y + menuBar.Height)), theme.SubtleSurface));
        Assert.IsGreaterThan(20, CountExactPixels(bitmap, new SKRect((float)infoBar.X + 12, (float)infoBar.Y + 14, (float)infoBar.X + 36, (float)infoBar.Y + 38), theme.Success));
        Assert.IsGreaterThan(0, CountExactPixels(bitmap, new SKRect((float)infoBar.X + 16, (float)infoBar.Y + 18, (float)infoBar.X + 32, (float)infoBar.Y + 34), theme.Surface));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererDrawsSeverityFilledInfoBarsWithCloseButton()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "filled-infobar");
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new InfoBar
            {
                Name = "SuccessInfoBar",
                Title = "Saved",
                Message = "Profile changes are synced.",
                Severity = InfoBarSeverity.Success,
                IsClosable = true,
                Width = 360,
                Height = 64
            }
        });
        var theme = SkiaV2Theme.For("light");
        var settings = new VisualRunSettings(null, "filled-infobar", "skia-v2", new VisualViewport(420, 120), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "filled-infobar", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var infoBar = RequireNode(arranged.Root, "SuccessInfoBar").Layout!;
        var fillSample = bitmap.GetPixel((int)infoBar.X + 300, (int)infoBar.Y + 50);
        var closeBand = new SKRect(
            (float)(infoBar.X + infoBar.Width - 34),
            (float)(infoBar.Y + 14),
            (float)(infoBar.X + infoBar.Width - 12),
            (float)(infoBar.Y + 36));

        Assert.AreNotEqual(theme.Surface, fillSample, "Success InfoBar should use a filled severity surface, not the plain card surface.");
        Assert.IsTrue(fillSample.Green > fillSample.Red && fillSample.Green > fillSample.Blue, $"Expected a success-tinted fill, got {fillSample}.");
        Assert.IsGreaterThan(4, CountDarkPixels(bitmap, closeBand, 150));
    }

    [TestMethod]
    public async Task SkiaV2SnapshotRendererAlignsProgressBarAndProgressRingToNativeProbe()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "winui3-mac-skia-v2-tests", Guid.NewGuid().ToString("N"), "native-progress");
        var tree = UiTreeBuilder.Build(new Window
        {
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new ProgressBar
                    {
                        Name = "SyncProgressBar",
                        Width = 180,
                        Height = 16,
                        Value = 65,
                        HorizontalAlignment = HorizontalAlignment.Left
                    },
                    new ProgressRing
                    {
                        Name = "LoadingProgressRing",
                        Width = 24,
                        Height = 24,
                        IsActive = true,
                        HorizontalAlignment = HorizontalAlignment.Left
                    }
                }
            }
        });
        var theme = SkiaV2Theme.For("light");
        var settings = new VisualRunSettings(null, "native-progress", "skia-v2", new VisualViewport(240, 90), 1, "light", true, new VisualThresholds());
        var arranged = VisualLayoutEngine.Arrange(tree, settings, out var unsupported);
        var options = new SnapshotRenderOptions("skia-v2", "native-progress", settings.Viewport, settings.Scale, settings.Theme, true, "mac-runtime.png");

        var snapshot = await new SkiaV2SnapshotRenderer().RenderAsync(arranged, outputDirectory, options);

        Assert.HasCount(0, unsupported);
        using var bitmap = SKBitmap.Decode(snapshot.FilePath);
        Assert.IsNotNull(bitmap);
        var progressBar = RequireNode(arranged.Root, "SyncProgressBar").Layout!;
        var progressRing = RequireNode(arranged.Root, "LoadingProgressRing").Layout!;
        var barAccentBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(progressBar), pixel => pixel == theme.Accent);
        var ringAccentBounds = BoundsOfPixelsMatching(bitmap, LayoutRectToSkRect(progressRing), IsAccentLike);

        Assert.IsFalse(barAccentBounds.IsEmpty, "Expected ProgressBar accent fill.");
        Assert.IsLessThanOrEqualTo(3, barAccentBounds.Height, $"Native ProgressBar track should stay thin; actual bounds were {barAccentBounds}.");
        Assert.IsFalse(ringAccentBounds.IsEmpty, "Expected ProgressRing accent arc.");
        Assert.IsLessThanOrEqualTo(24, ringAccentBounds.Width, $"ProgressRing accent arc should fit the native 24px probe bounds; actual bounds were {ringAccentBounds}.");
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
            useRelativePaths: true,
            nativeReferenceTargets: new NativeReferenceTargetDocument(
                SchemaVersion: "0.1",
                ReferenceSource: "native-winui-element-bounds",
                CoordinateSpace: "client-area",
                ScenarioName: "crop-test-light",
                ScenarioPath: null,
                FixtureProjectPath: null,
                CommitSha: null,
                WorkflowRunId: null,
                Theme: "light",
                Viewport: new VisualViewport(240, 160),
                Scale: 1,
                Dimensions: new ReferenceImageDimensions(240, 160),
                RootBounds: new NativeReferenceBounds(0, 0, 240, 160),
                CapturedAt: DateTimeOffset.UnixEpoch,
                Targets: new[]
                {
                    new NativeReferenceTarget(
                        Component: "Button",
                        Target: "PrimaryButton",
                        IdentitySource: "x:Name",
                        AutomationId: "PrimaryButton",
                        Name: "PrimaryButton",
                        ElementType: "Microsoft.UI.Xaml.Controls.Button",
                        Bounds: new NativeReferenceBounds(0, 0, 8, 8))
                }));

        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.AreEqual("components/button-primarybutton/windows-reference.png", crop.NativeReferencePath);
        Assert.AreEqual("components/button-primarybutton/mac-runtime.png", crop.MacRuntimePath);
        Assert.AreEqual("components/button-primarybutton/pixel-diff.png", crop.PixelDiffPath);
        var macRuntimePath = crop.MacRuntimePath ?? throw new AssertFailedException("Expected macOS runtime crop path.");
        Assert.IsTrue(File.Exists(Path.Combine(directory, macRuntimePath)));
    }

    [TestMethod]
    public void ComponentCropperUsesWindowsNativeBoundsForNativeReferenceCrop()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimeImage = Path.Combine(directory, "mac-runtime.png");
        var referenceImage = Path.Combine(directory, "windows-reference.png");
        WritePatternPng(runtimeImage, width: 80, height: 80);
        WritePatternPng(referenceImage, width: 80, height: 80);
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
                        X: 2,
                        Y: 3,
                        Width: 10,
                        Height: 11,
                        DesiredWidth: 10,
                        DesiredHeight: 11,
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
        var nativeTargets = new NativeReferenceTargetDocument(
            SchemaVersion: "0.1",
            ReferenceSource: "native-winui-element-bounds",
            CoordinateSpace: "client-area",
            ScenarioName: "crop-test-light",
            ScenarioPath: null,
            FixtureProjectPath: null,
            CommitSha: null,
            WorkflowRunId: null,
            Theme: "light",
            Viewport: new VisualViewport(80, 80),
            Scale: 1,
            Dimensions: new ReferenceImageDimensions(80, 80),
            RootBounds: new NativeReferenceBounds(0, 0, 80, 80),
            CapturedAt: DateTimeOffset.UnixEpoch,
            Targets: new[]
            {
                new NativeReferenceTarget(
                    Component: "Button",
                    Target: "PrimaryButton",
                    IdentitySource: "x:Name",
                    AutomationId: "PrimaryButton",
                    Name: "PrimaryButton",
                    ElementType: "Microsoft.UI.Xaml.Controls.Button",
                    Bounds: new NativeReferenceBounds(20, 21, 10, 11))
            });

        var withCrops = ComponentCropper.WriteCrops(
            evidence,
            runtimeImage,
            referenceImage,
            directory,
            scale: 1,
            thresholds,
            nativeReferenceProvenance: TestNativeReferenceProvenance(),
            useRelativePaths: true,
            nativeReferenceTargets: nativeTargets);

        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.AreEqual(new ComponentCropBounds(2, 3, 10, 11), crop.Bounds);
        Assert.AreEqual(new ComponentCropBounds(20, 21, 10, 11), crop.NativeReferenceBounds);
        Assert.AreEqual("windows-native-element-bounds", crop.NativeReferenceBoundsSource);
        Assert.IsTrue(crop.NativeReferenceBoundsValidForPromotion);
        Assert.AreEqual(new ReferenceImageDimensions(10, 11), crop.NativeReferenceCropSize);
        Assert.AreEqual(new ReferenceImageDimensions(10, 11), crop.MacRuntimeCropSize);
        Assert.AreEqual(new ComponentCropBoundsDelta(18, 18, 0, 0), crop.NativeReferenceBoundsDelta);
    }

    [TestMethod]
    public void ComponentCropperBlocksNativeAndMacCropSizeMismatch()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimeImage = Path.Combine(directory, "mac-runtime.png");
        var referenceImage = Path.Combine(directory, "windows-reference.png");
        WritePatternPng(runtimeImage, width: 80, height: 80);
        WritePatternPng(referenceImage, width: 80, height: 80);
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
                        X: 2,
                        Y: 3,
                        Width: 10,
                        Height: 11,
                        DesiredWidth: 10,
                        DesiredHeight: 11,
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
        var nativeTargets = new NativeReferenceTargetDocument(
            SchemaVersion: "0.1",
            ReferenceSource: "native-winui-element-bounds",
            CoordinateSpace: "client-area",
            ScenarioName: "crop-test-light",
            ScenarioPath: null,
            FixtureProjectPath: null,
            CommitSha: null,
            WorkflowRunId: null,
            Theme: "light",
            Viewport: new VisualViewport(80, 80),
            Scale: 1,
            Dimensions: new ReferenceImageDimensions(80, 80),
            RootBounds: new NativeReferenceBounds(0, 0, 80, 80),
            CapturedAt: DateTimeOffset.UnixEpoch,
            Targets: new[]
            {
                new NativeReferenceTarget(
                    Component: "Button",
                    Target: "PrimaryButton",
                    IdentitySource: "x:Name",
                    AutomationId: "PrimaryButton",
                    Name: "PrimaryButton",
                    ElementType: "Microsoft.UI.Xaml.Controls.Button",
                    Bounds: new NativeReferenceBounds(20, 21, 17, 19))
            });

        var withCrops = ComponentCropper.WriteCrops(
            evidence,
            runtimeImage,
            referenceImage,
            directory,
            scale: 1,
            thresholds,
            nativeReferenceProvenance: TestNativeReferenceProvenance(),
            useRelativePaths: true,
            nativeReferenceTargets: nativeTargets);

        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.AreEqual("failed", crop.Status);
        Assert.AreEqual("native-crop-size-mismatch", crop.NativeReferenceReadiness.Status);
        Assert.AreEqual("native-crop-size-mismatch", crop.NativeReferenceReadinessStatus);
        Assert.IsFalse(crop.NativeReferenceBoundsValidForPromotion);
        Assert.AreEqual(new ComponentCropBounds(20, 21, 17, 19), crop.NativeReferenceBounds);
        Assert.AreEqual(new ReferenceImageDimensions(17, 19), crop.NativeReferenceCropSize);
        Assert.AreEqual(new ReferenceImageDimensions(10, 11), crop.MacRuntimeCropSize);
        Assert.AreEqual(new ComponentCropBoundsDelta(18, 18, 7, 8), crop.NativeReferenceBoundsDelta);
        StringAssert.Contains(crop.NativeReferenceIntegrityBlockerReason, "Phase -1 does not normalize crop sizes");
        Assert.AreEqual("components/button-primarybutton/pixel-diff.png", crop.PixelDiffPath);
        Assert.AreEqual(100, crop.ChangedPixelPercentage);
        Assert.AreEqual(255, crop.MeanAbsoluteError);
        Assert.AreEqual(255, crop.RootMeanSquaredError);
        Assert.IsTrue(File.Exists(Path.Combine(directory, crop.PixelDiffPath)));
    }

    [TestMethod]
    public void ComponentCropperBlocksNativeBoundsOutsideReferenceImage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "winui3-mac-crop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimeImage = Path.Combine(directory, "mac-runtime.png");
        var referenceImage = Path.Combine(directory, "windows-reference.png");
        WritePatternPng(runtimeImage, width: 80, height: 80);
        WritePatternPng(referenceImage, width: 80, height: 80);
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
                        X: 2,
                        Y: 3,
                        Width: 10,
                        Height: 11,
                        DesiredWidth: 10,
                        DesiredHeight: 11,
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
        var nativeTargets = new NativeReferenceTargetDocument(
            SchemaVersion: "0.1",
            ReferenceSource: "native-winui-element-bounds",
            CoordinateSpace: "client-area",
            ScenarioName: "crop-test-light",
            ScenarioPath: null,
            FixtureProjectPath: null,
            CommitSha: null,
            WorkflowRunId: null,
            Theme: "light",
            Viewport: new VisualViewport(80, 80),
            Scale: 1,
            Dimensions: new ReferenceImageDimensions(80, 80),
            RootBounds: new NativeReferenceBounds(0, 0, 80, 80),
            CapturedAt: DateTimeOffset.UnixEpoch,
            Targets: new[]
            {
                new NativeReferenceTarget(
                    Component: "Button",
                    Target: "PrimaryButton",
                    IdentitySource: "x:Name",
                    AutomationId: "PrimaryButton",
                    Name: "PrimaryButton",
                    ElementType: "Microsoft.UI.Xaml.Controls.Button",
                    Bounds: new NativeReferenceBounds(75, 21, 10, 11))
            });

        var withCrops = ComponentCropper.WriteCrops(
            evidence,
            runtimeImage,
            referenceImage,
            directory,
            scale: 1,
            thresholds,
            nativeReferenceProvenance: TestNativeReferenceProvenance(),
            useRelativePaths: true,
            nativeReferenceTargets: nativeTargets);

        var crop = withCrops.Components[0].Crop ?? throw new AssertFailedException("Expected component crop evidence.");
        Assert.AreEqual("failed", crop.Status);
        Assert.AreEqual("invalid-native-crop-bounds", crop.NativeReferenceReadiness.Status);
        Assert.AreEqual("invalid-native-crop-bounds", crop.NativeReferenceReadinessStatus);
        Assert.IsFalse(crop.NativeReferenceBoundsValidForPromotion);
        Assert.IsNull(crop.NativeReferenceBounds);
        Assert.IsNull(crop.NativeReferencePath);
        Assert.IsNotNull(crop.NativeReferenceTarget);
        StringAssert.Contains(crop.NativeReferenceIntegrityBlockerReason, "outside the reference image");
        Assert.IsNull(crop.PixelDiffPath);
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
                        NativeReferenceBounds: new ComponentCropBounds(0, 0, 16, 12),
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
                        NativeReferenceProvenance = TestNativeReferenceProvenance(),
                        NativeReferenceReadinessStatus = "ready",
                        NativeReferenceReadinessReason = "Native crop uses Windows native element bounds from native-reference-targets.json.",
                        NativeReferenceRequiredAction = "Keep the native target export with the Windows reference artifact.",
                        NativeReferenceBoundsSource = "windows-native-element-bounds",
                        NativeReferenceBoundsValidForPromotion = true,
                        NativeReferenceIntegrityBlockerReason = "none",
                        NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                            "ready",
                            "Native crop uses Windows native element bounds from native-reference-targets.json.",
                            "Keep the native target export with the Windows reference artifact.",
                            ReadyForPromotion: true,
                            "none"),
                        NativeReferenceCropSize = new ReferenceImageDimensions(16, 12),
                        MacRuntimeCropSize = new ReferenceImageDimensions(16, 12),
                        NativeReferenceBoundsDelta = new ComponentCropBoundsDelta(0, 0, 0, 0)
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
        StringAssert.Contains(html, "native reference");
        StringAssert.Contains(html, "bounds source");

        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "visual-review.json")));
        Assert.AreEqual(ArtifactSchemas.VisualReview, json.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual(".", json.RootElement.GetProperty("outputDirectory").GetString());
        Assert.AreEqual("visual-review.html", json.RootElement.GetProperty("htmlPath").GetString());
        Assert.AreEqual(1, json.RootElement.GetProperty("rows").GetArrayLength());
        var row = json.RootElement.GetProperty("rows")[0];
        Assert.AreEqual("native-winui", row.GetProperty("referenceSource").GetString());
        Assert.AreEqual("26777029415", row.GetProperty("nativeReferenceRunId").GetString());
        Assert.AreEqual("ready", row.GetProperty("nativeReferenceReadiness").GetString());
        Assert.AreEqual("windows-native-element-bounds", row.GetProperty("nativeReferenceBoundsSource").GetString());
        Assert.AreEqual("none", row.GetProperty("nativeReferenceIntegrityBlockerReason").GetString());
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

    private static IEnumerable<string> PublicNativeReferenceScenarioPaths()
    {
        foreach (var relativeDirectory in new[]
        {
            Path.Combine("fixtures", "ComponentParityLab.WinUI", "scenarios"),
            Path.Combine("fixtures", "PublicAdminWorkbench.WinUI", "scenarios")
        })
        {
            var directory = RepositoryPath(relativeDirectory);
            foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> PublicEvidenceScenarioPaths()
    {
        foreach (var evidencePath in PublicEvidenceDiscovery.FindCanonicalEvidenceFiles(RepositoryRoot()))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(evidencePath));
            var scenarioName = document.RootElement.GetProperty("scenarioName").GetString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(scenarioName));

            foreach (var scenarioPath in PublicNativeReferenceScenarioPaths())
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(scenarioPath), scenarioName, StringComparison.Ordinal))
                {
                    yield return scenarioPath;
                    break;
                }
            }
        }
    }

    private static async Task AssertNativeReferenceImporterFailsWhenRequiredTargetMissingAsync(
        string scenarioRelativePath,
        string fixtureProjectPath,
        string missingTarget)
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-source", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-output", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceRoot);

        var scenarioPath = RepositoryPath(scenarioRelativePath);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);
        Assert.IsTrue(
            scenario.Requirements.Any(requirement => requirement.Target == missingTarget),
            $"{scenarioRelativePath} must contain test target {missingTarget}.");

        var artifactDirectory = Path.Combine(sourceRoot, scenario.Name);
        Directory.CreateDirectory(artifactDirectory);
        WriteSolidPng(
            Path.Combine(artifactDirectory, "windows-reference.png"),
            new SKColor(250, 250, 250),
            scenario.Viewport.Width,
            scenario.Viewport.Height);
        var provenance = new NativeReferenceProvenance(
            ReferenceSource: "native-winui",
            FixtureProjectPath: fixtureProjectPath,
            ScenarioPath: scenarioRelativePath,
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
        var targets = new NativeReferenceTargetDocument(
            SchemaVersion: "0.1",
            ReferenceSource: "native-winui-element-bounds",
            CoordinateSpace: "client-area",
            ScenarioName: scenario.Name,
            ScenarioPath: scenarioRelativePath,
            FixtureProjectPath: fixtureProjectPath,
            CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
            WorkflowRunId: "26777029415",
            Theme: scenario.Theme,
            Viewport: scenario.Viewport,
            Scale: scenario.Scale,
            Dimensions: new ReferenceImageDimensions(scenario.Viewport.Width, scenario.Viewport.Height),
            RootBounds: new NativeReferenceBounds(0, 0, scenario.Viewport.Width, scenario.Viewport.Height),
            CapturedAt: DateTimeOffset.UnixEpoch,
            Targets: scenario.Requirements
                .Where(requirement => !string.IsNullOrWhiteSpace(requirement.Target))
                .Where(requirement => !string.Equals(requirement.Target, missingTarget, StringComparison.Ordinal))
                .Select(requirement => new NativeReferenceTarget(
                    Component: requirement.Component,
                    Target: requirement.Target!,
                    IdentitySource: "x:Name",
                    AutomationId: requirement.Target,
                    Name: requirement.Target,
                    ElementType: "Microsoft.UI.Xaml.FrameworkElement",
                    Bounds: new NativeReferenceBounds(0, 0, 12, 10))
                {
                    ActualSize = new ReferenceImageDimensions(12, 10),
                    BoundsSource = "x:Name",
                    CapturedAt = DateTimeOffset.UnixEpoch
                })
                .ToArray());
        File.WriteAllText(
            Path.Combine(artifactDirectory, "native-reference-targets.json"),
            JsonSerializer.Serialize(targets, JsonDefaults.Options));

        var import = NativeReferenceImporter.Import(RepositoryRoot(), sourceRoot, outputRoot);

        Assert.AreEqual("failed", import.Status);
        Assert.IsTrue(
            import.Problems.Any(problem => problem.Contains($"missing required public row target '{missingTarget}'", StringComparison.Ordinal)),
            $"Importer must report the missing public row target {missingTarget}.");
    }

    private static async Task AssertNativeReferenceImporterFailsWithTargetMutationAsync(
        string scenarioRelativePath,
        string fixtureProjectPath,
        string targetUnderTest,
        Func<IReadOnlyList<NativeReferenceTarget>, IReadOnlyList<NativeReferenceTarget>> mutateTargets,
        string expectedProblem)
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-source", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-output", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceRoot);

        var scenarioPath = RepositoryPath(scenarioRelativePath);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);
        Assert.IsTrue(
            scenario.Requirements.Any(requirement => requirement.Target == targetUnderTest),
            $"{scenarioRelativePath} must contain test target {targetUnderTest}.");

        var artifactDirectory = Path.Combine(sourceRoot, scenario.Name);
        Directory.CreateDirectory(artifactDirectory);
        WriteSolidPng(
            Path.Combine(artifactDirectory, "windows-reference.png"),
            new SKColor(250, 250, 250),
            scenario.Viewport.Width,
            scenario.Viewport.Height);
        var provenance = new NativeReferenceProvenance(
            ReferenceSource: "native-winui",
            FixtureProjectPath: fixtureProjectPath,
            ScenarioPath: scenarioRelativePath,
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
        var validTargets = scenario.Requirements
            .Where(requirement => !string.IsNullOrWhiteSpace(requirement.Target))
            .Select(requirement => new NativeReferenceTarget(
                Component: requirement.Component,
                Target: requirement.Target!,
                IdentitySource: "x:Name",
                AutomationId: requirement.Target,
                Name: requirement.Target,
                ElementType: "Microsoft.UI.Xaml.FrameworkElement",
                Bounds: new NativeReferenceBounds(0, 0, 12, 10))
            {
                ActualSize = new ReferenceImageDimensions(12, 10),
                BoundsSource = "x:Name",
                CapturedAt = DateTimeOffset.UnixEpoch
            })
            .ToArray();
        var targets = new NativeReferenceTargetDocument(
            SchemaVersion: "0.1",
            ReferenceSource: "native-winui-element-bounds",
            CoordinateSpace: "client-area",
            ScenarioName: scenario.Name,
            ScenarioPath: scenarioRelativePath,
            FixtureProjectPath: fixtureProjectPath,
            CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
            WorkflowRunId: "26777029415",
            Theme: scenario.Theme,
            Viewport: scenario.Viewport,
            Scale: scenario.Scale,
            Dimensions: new ReferenceImageDimensions(scenario.Viewport.Width, scenario.Viewport.Height),
            RootBounds: new NativeReferenceBounds(0, 0, scenario.Viewport.Width, scenario.Viewport.Height),
            CapturedAt: DateTimeOffset.UnixEpoch,
            Targets: mutateTargets(validTargets));
        File.WriteAllText(
            Path.Combine(artifactDirectory, "native-reference-targets.json"),
            JsonSerializer.Serialize(targets, JsonDefaults.Options));

        var import = NativeReferenceImporter.Import(RepositoryRoot(), sourceRoot, outputRoot);

        Assert.AreEqual("failed", import.Status);
        Assert.IsTrue(
            import.Problems.Any(problem => problem.Contains(expectedProblem, StringComparison.Ordinal)),
            $"Importer must report '{expectedProblem}'. Problems:{Environment.NewLine}{string.Join(Environment.NewLine, import.Problems)}");
    }

    private static async Task AssertNativeReferenceImporterPassesWithTargetMutationAsync(
        string scenarioRelativePath,
        string fixtureProjectPath,
        Func<IReadOnlyList<NativeReferenceTarget>, IReadOnlyList<NativeReferenceTarget>> mutateTargets,
        params string[] targetNames)
    {
        var sourceRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-source", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(Path.GetTempPath(), "winui3-mac-native-reference-import-output", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceRoot);

        var scenarioPath = RepositoryPath(scenarioRelativePath);
        var scenario = await VisualScenario.LoadAsync(scenarioPath);
        var artifactDirectory = Path.Combine(sourceRoot, scenario.Name);
        Directory.CreateDirectory(artifactDirectory);
        WriteSolidPng(
            Path.Combine(artifactDirectory, "windows-reference.png"),
            new SKColor(250, 250, 250),
            scenario.Viewport.Width,
            scenario.Viewport.Height);
        var provenance = new NativeReferenceProvenance(
            ReferenceSource: "native-winui",
            FixtureProjectPath: fixtureProjectPath,
            ScenarioPath: scenarioRelativePath,
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
        var validTargets = scenario.Requirements
            .Where(requirement => !string.IsNullOrWhiteSpace(requirement.Target))
            .Select(requirement => new NativeReferenceTarget(
                Component: requirement.Component,
                Target: requirement.Target!,
                IdentitySource: "x:Name",
                AutomationId: requirement.Target,
                Name: requirement.Target,
                ElementType: "Microsoft.UI.Xaml.FrameworkElement",
                Bounds: new NativeReferenceBounds(0, 0, 12, 10))
            {
                ActualSize = new ReferenceImageDimensions(12, 10),
                BoundsSource = "x:Name",
                CapturedAt = DateTimeOffset.UnixEpoch
            })
            .ToArray();
        var targets = new NativeReferenceTargetDocument(
            SchemaVersion: "0.1",
            ReferenceSource: "native-winui-element-bounds",
            CoordinateSpace: "client-area",
            ScenarioName: scenario.Name,
            ScenarioPath: scenarioRelativePath,
            FixtureProjectPath: fixtureProjectPath,
            CommitSha: "95e8d7d49f4efd610ec621db470a3d10ee6e8957",
            WorkflowRunId: "26777029415",
            Theme: scenario.Theme,
            Viewport: scenario.Viewport,
            Scale: scenario.Scale,
            Dimensions: new ReferenceImageDimensions(scenario.Viewport.Width, scenario.Viewport.Height),
            RootBounds: new NativeReferenceBounds(0, 0, scenario.Viewport.Width, scenario.Viewport.Height),
            CapturedAt: DateTimeOffset.UnixEpoch,
            Targets: mutateTargets(validTargets));
        File.WriteAllText(
            Path.Combine(artifactDirectory, "native-reference-targets.json"),
            JsonSerializer.Serialize(targets, JsonDefaults.Options));

        var import = NativeReferenceImporter.Import(RepositoryRoot(), sourceRoot, outputRoot);

        foreach (var targetName in targetNames)
        {
            Assert.IsFalse(
                import.Problems.Any(problem =>
                    problem.Contains(targetName, StringComparison.Ordinal) &&
                    problem.Contains("not a trustworthy native element", StringComparison.Ordinal)),
                string.Join(Environment.NewLine, import.Problems));
        }
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

    private static void WritePatternPng(string path, int width = 8, int height = 8)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(255, 255, 255));
        using var paint = new SKPaint { Color = new SKColor(37, 98, 217) };
        canvas.DrawRect(new SKRect(2, 2, Math.Max(3, width - 2), Math.Max(3, height - 2)), paint);
        paint.Color = new SKColor(220, 20, 60);
        canvas.DrawLine(0, 0, width, height, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static double NonWhitePixelRatio(SKBitmap bitmap)
    {
        var sampled = 0;
        var nonWhite = 0;
        var stepX = Math.Max(1, bitmap.Width / 320);
        var stepY = Math.Max(1, bitmap.Height / 240);

        for (var y = 0; y < bitmap.Height; y += stepY)
        {
            for (var x = 0; x < bitmap.Width; x += stepX)
            {
                var color = bitmap.GetPixel(x, y);
                sampled++;
                if (color.Red <= 246 || color.Green <= 246 || color.Blue <= 246)
                {
                    nonWhite++;
                }
            }
        }

        return sampled == 0 ? 0 : (double)nonWhite / sampled;
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
                        NativeReferenceBounds: new ComponentCropBounds(0, 0, 8, 8),
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
                        NativeReferenceProvenance = TestNativeReferenceProvenance(),
                        NativeReferenceReadinessStatus = "ready",
                        NativeReferenceReadinessReason = "Native crop uses Windows native element bounds from native-reference-targets.json.",
                        NativeReferenceRequiredAction = "Keep the native target export with the Windows reference artifact.",
                        NativeReferenceBoundsSource = "windows-native-element-bounds",
                        NativeReferenceBoundsValidForPromotion = true,
                        NativeReferenceIntegrityBlockerReason = "none",
                        NativeReferenceReadiness = new NativeReferenceReadinessEvidence(
                            "ready",
                            "Native crop uses Windows native element bounds from native-reference-targets.json.",
                            "Keep the native target export with the Windows reference artifact.",
                            ReadyForPromotion: true,
                            "none"),
                        NativeReferenceCropSize = new ReferenceImageDimensions(8, 8),
                        MacRuntimeCropSize = new ReferenceImageDimensions(8, 8),
                        NativeReferenceBoundsDelta = new ComponentCropBoundsDelta(0, 0, 0, 0)
                    },
                    NativeQualityGrade: "not-evaluated",
                    Inspection: null,
                    KnownGaps: Array.Empty<string>())
            },
            SourceFeatures: Array.Empty<SourceFeatureEvidenceEntry>(),
            Status: "passed");
    }

    private static string TestComponentEvidenceJson(string target)
    {
        return $$"""
            {
              "schemaVersion": "0.5",
              "fixtureName": "test-fixture",
              "scenarioName": "scenario-light",
              "components": [
                {
                  "component": "Button",
                  "kind": "control",
                  "target": "{{target}}",
                  "catalogStatus": "supported",
                  "presence": "present",
                  "interactionStatus": "passed",
                  "visualGrade": "usable",
                  "changedPixelPercentage": 0.1,
                  "meanAbsoluteError": 0.1,
                  "rootMeanSquaredError": 0.1,
                  "crop": {
                    "status": "failed",
                    "bounds": { "x": 0, "y": 0, "width": 10, "height": 10 },
                    "nativeReferencePath": "components/button/windows-reference.png",
                    "macRuntimePath": "components/button/mac-runtime.png",
                    "pixelDiffPath": "components/button/pixel-diff.png",
                    "runtimeBlank": false,
                    "thresholds": {
                      "changedPixelPercentage": 5,
                      "maxChannelDelta": 255,
                      "meanAbsoluteError": 2,
                      "rootMeanSquaredError": 4
                    },
                    "changedPixelPercentage": 0.1,
                    "meanAbsoluteError": 0.1,
                    "rootMeanSquaredError": 0.1
                  },
                  "nativeQualityGrade": "not-evaluated",
                  "inspection": null,
                  "knownGaps": []
                }
              ],
              "sourceFeatures": [],
              "status": "failed"
            }
            """;
    }

    private static void WriteVisualRunStatus(string repositoryRoot, string relativePath, string status)
    {
        var path = Path.Combine(repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $$"""
            {
              "schemaVersion": "0.1",
              "fixtureName": "test-fixture",
              "scenarioName": "{{Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)!))}}",
              "status": "{{status}}"
            }
            """);
    }

    private static void CreateStrictScenarioFixture(string repositoryRoot, string scenarioName)
    {
        var scenarioDirectory = Path.Combine(repositoryRoot, "fixtures", "ComponentParityLab.WinUI", "scenarios");
        Directory.CreateDirectory(scenarioDirectory);
        File.WriteAllText(Path.Combine(scenarioDirectory, scenarioName + ".json"), $$"""
            {
              "name": "{{scenarioName}}"
            }
            """);
    }

    private static Task<ProductEvidenceDocument> WriteStrictScenarioSweepReport(
        string repositoryRoot,
        params string[] failedScenarioNames)
    {
        var outputRoot = Path.Combine(repositoryRoot, "artifacts", "product-evidence", "strict-scenario-sweep");
        var failed = failedScenarioNames.ToHashSet(StringComparer.Ordinal);
        return ProductEvidenceRunner.RunAsync(
            repositoryRoot,
            "strict-scenario-sweep",
            outputRoot,
            step => Task.FromResult(failed.Contains(step.Name)
                ? ProductEvidenceStepOutcome.Failed($"{step.Name} failed in test harness.", step.ArtifactPaths)
                : ProductEvidenceStepOutcome.Passed($"{step.Name} passed in test harness.", step.ArtifactPaths)));
    }

    private static void WriteProductionStateCoverageInventory(
        string repositoryRoot,
        string scenarioName,
        string component,
        string minimumVisualGrade,
        string state = "focused")
    {
        var inventoryPath = Path.Combine(repositoryRoot, "docs", "compatibility", "winui-component-inventory.json");
        Directory.CreateDirectory(Path.GetDirectoryName(inventoryPath)!);
        File.WriteAllText(inventoryPath, $$"""
            {
              "schemaVersion": "0.1",
              "productionStateCoverage": [
                {
                  "productionPriority": "Ring 0",
                  "state": "{{state}}",
                  "scenario": "{{scenarioName}}",
                  "path": "fixtures/ComponentParityLab.WinUI/scenarios/{{scenarioName}}.json",
                  "components": [
                    "{{component}}"
                  ],
                  "interactionRequirement": "{{state}}",
                  "accessibilityRequirement": "role and name",
                  "minimumVisualGrade": "{{minimumVisualGrade}}",
                  "knownGaps": []
                }
              ],
              "entries": [],
              "broaderControlInventory": {
                "controls": []
              }
            }
            """);
    }

    private static void WriteStateCoverageMatrixReleaseContract(
        string repositoryRoot,
        string scenarioName,
        string component,
        string state,
        string releaseComponentEvidencePath)
    {
        var matrixPath = Path.Combine(repositoryRoot, "docs", "visual-parity", "state-coverage-matrix.json");
        Directory.CreateDirectory(Path.GetDirectoryName(matrixPath)!);
        File.WriteAllText(matrixPath, $$"""
            {
              "schemaVersion": "0.1",
              "generatedAt": "1970-01-01T00:00:00+00:00",
              "policy": "test policy",
              "totals": {
                "componentCount": 1,
                "requirementCount": 1,
                "evidenceBackedRequirementCount": 0,
                "missingEvidenceRequirementCount": 1,
                "defaultOnlyComponentCount": 1,
                "stateBackedComponentCount": 0,
                "missingDefaultEvidenceComponentCount": 0,
                "coverageStatusCounts": {
                  "default-only": 1
                }
              },
              "components": [
                {
                  "component": "{{component}}",
                  "ownerFamily": "Basic input",
                  "requiredStates": ["default", "{{state}}"],
                  "coveredStates": ["default"],
                  "missingStates": ["{{state}}"],
                  "hasDefaultEvidence": true,
                  "hasInteractionEvidence": false,
                  "hasAccessibilityEvidence": false,
                  "coverageStatus": "default-only",
                  "productionReadiness": "not-production-ready-default-only"
                }
              ],
              "requirements": [
                {
                  "component": "{{component}}",
                  "ownerFamily": "Basic input",
                  "productionPriority": "Ring 0",
                  "state": "{{state}}",
                  "scenarioName": "{{scenarioName}}",
                  "scenarioPath": "fixtures/ComponentParityLab.WinUI/scenarios/{{scenarioName}}.json",
                  "scenarioExists": true,
                  "evidencePath": null,
                  "evidenceStatus": "missing-state-evidence",
                  "visualGrade": null,
                  "interactionRequirement": "{{state}}",
                  "interactionEvidenceStatus": "missing-state-evidence",
                  "accessibilityRequirement": "role and name",
                  "accessibilityEvidenceStatus": "missing-state-evidence",
                  "minimumVisualGrade": "usable",
                  "coverageStatus": "missing-state-evidence",
                  "releaseEvidenceProfile": "strict-scenario-sweep",
                  "releaseEvidenceStatus": "required-via-public-product",
                  "releaseComponentEvidencePath": "{{releaseComponentEvidencePath}}",
                  "releaseAccessibilityEvidencePath": "artifacts/product-evidence/strict-scenario-sweep/{{scenarioName}}/accessibility.json",
                  "releaseVisualRunPath": "artifacts/product-evidence/strict-scenario-sweep/{{scenarioName}}/visual/visual-run.json",
                  "knownGaps": []
                }
              ],
              "status": "tracked-with-gaps"
            }
            """);
    }

    private static void WriteStrictScenarioComponentEvidence(
        string repositoryRoot,
        string scenarioName,
        string component,
        string visualGrade,
        string interactionStatus)
    {
        var evidencePath = Path.Combine(
            repositoryRoot,
            "artifacts",
            "product-evidence",
            "strict-scenario-sweep",
            scenarioName,
            "visual",
            "component-evidence.json");
        Directory.CreateDirectory(Path.GetDirectoryName(evidencePath)!);
        File.WriteAllText(evidencePath, $$"""
            {
              "schemaVersion": "0.5",
              "fixtureName": "component-parity-lab",
              "scenarioName": "{{scenarioName}}",
              "components": [
                {
                  "component": "{{component}}",
                  "kind": "control",
                  "target": "PrimaryTarget",
                  "layoutRegion": null,
                  "catalogStatus": "supported",
                  "presence": "present",
                  "interactionStatus": "{{interactionStatus}}",
                  "visualGrade": "{{visualGrade}}",
                  "componentThresholds": null,
                  "changedPixelPercentage": null,
                  "meanAbsoluteError": null,
                  "rootMeanSquaredError": null,
                  "crop": null,
                  "nativeQualityGrade": "not-evaluated",
                  "inspection": null,
                  "knownGaps": []
                }
              ],
              "sourceFeatures": [],
              "status": "passed"
            }
            """);
    }

    private static void WriteStrictScenarioAccessibility(
        string repositoryRoot,
        string scenarioName,
        string? targetName = null,
        string role = "button",
        bool? isFocused = null,
        bool? isEnabled = null,
        bool? isChecked = null,
        bool? isSelected = null)
    {
        var accessibilityPath = Path.Combine(
            repositoryRoot,
            "artifacts",
            "product-evidence",
            "strict-scenario-sweep",
            scenarioName,
            "accessibility.json");
        Directory.CreateDirectory(Path.GetDirectoryName(accessibilityPath)!);
        var children = targetName is null
            ? "[]"
            : $$"""
              [
                {
                  "role": "{{role}}",
                  "name": "{{targetName}}",
                  "label": "{{targetName}}",
                  "isFocused": {{JsonBool(isFocused ?? false)}},
                  "isFocusable": true{{JsonOptionalBool("isEnabled", isEnabled)}}{{JsonOptionalBool("isChecked", isChecked)}}{{JsonOptionalBool("isSelected", isSelected)}},
                  "children": []
                }
              ]
              """;
        File.WriteAllText(accessibilityPath, $$"""
            {
              "schemaVersion": "0.3",
              "generatedAt": "1970-01-01T00:00:00+00:00",
              "root": {
                "name": "test-root",
                "role": "window",
                "isFocused": false,
                "children": {{children}}
              }
            }
            """);
    }

    private static string JsonOptionalBool(string propertyName, bool? value)
    {
        return value.HasValue ? $",\n                  \"{propertyName}\": {JsonBool(value.Value)}" : string.Empty;
    }

    private static string JsonBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static void WriteCompareComponentEvidence(
        string root,
        string scenarioName,
        params (string Component, string Target, string CropStatus, double ChangedPixelPercentage, double MeanAbsoluteError, double RootMeanSquaredError)[] rows)
    {
        var visualDirectory = Path.Combine(root, scenarioName, "visual");
        Directory.CreateDirectory(visualDirectory);
        var components = rows
            .Select(row => new
            {
                component = row.Component,
                kind = "control",
                target = row.Target,
                catalogStatus = "supported",
                presence = "present",
                interactionStatus = "passed",
                visualGrade = "usable",
                changedPixelPercentage = row.ChangedPixelPercentage,
                meanAbsoluteError = row.MeanAbsoluteError,
                rootMeanSquaredError = row.RootMeanSquaredError,
                crop = new
                {
                    status = row.CropStatus,
                    bounds = new { x = 0, y = 0, width = 10, height = 10 },
                    nativeReferenceCropSize = new { width = 10, height = 10 },
                    macRuntimeCropSize = new { width = 10, height = 10 },
                    changedPixelPercentage = row.ChangedPixelPercentage,
                    meanAbsoluteError = row.MeanAbsoluteError,
                    rootMeanSquaredError = row.RootMeanSquaredError
                },
                nativeQualityGrade = "not-evaluated",
                knownGaps = Array.Empty<string>()
            })
            .ToArray();
        var document = new
        {
            schemaVersion = "0.5",
            fixtureName = "test-fixture",
            scenarioName,
            components,
            sourceFeatures = Array.Empty<object>(),
            status = rows.Any(row => row.CropStatus == "failed") ? "failed" : "passed"
        };
        File.WriteAllText(
            Path.Combine(visualDirectory, "component-evidence.json"),
            JsonSerializer.Serialize(document, JsonDefaults.Options));
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

    private static void AssertLayoutSize(UiNode root, string name, double width, double height)
    {
        var layout = RequireNode(root, name).Layout ?? throw new AssertFailedException($"Expected '{name}' to have layout.");
        Assert.AreEqual(width, layout.Width, $"{name} width");
        Assert.AreEqual(height, layout.Height, $"{name} height");
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

    private static int CountBrightPixels(SKBitmap bitmap, SKRect rect, byte minimumChannelValue)
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
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red >= minimumChannelValue &&
                    pixel.Green >= minimumChannelValue &&
                    pixel.Blue >= minimumChannelValue)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountDarkPixels(SKBitmap bitmap, SKRect rect, byte maximumChannelValue)
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
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red <= maximumChannelValue &&
                    pixel.Green <= maximumChannelValue &&
                    pixel.Blue <= maximumChannelValue)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static SKRectI BoundsOfPixelsMatching(SKBitmap bitmap, SKRect rect, Func<SKColor, bool> predicate)
    {
        var left = Math.Clamp((int)Math.Floor(rect.Left), 0, bitmap.Width);
        var top = Math.Clamp((int)Math.Floor(rect.Top), 0, bitmap.Height);
        var right = Math.Clamp((int)Math.Ceiling(rect.Right), left, bitmap.Width);
        var bottom = Math.Clamp((int)Math.Ceiling(rect.Bottom), top, bitmap.Height);
        var resultLeft = bitmap.Width;
        var resultTop = bitmap.Height;
        var resultRight = -1;
        var resultBottom = -1;

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (!predicate(bitmap.GetPixel(x, y)))
                {
                    continue;
                }

                resultLeft = Math.Min(resultLeft, x);
                resultTop = Math.Min(resultTop, y);
                resultRight = Math.Max(resultRight, x);
                resultBottom = Math.Max(resultBottom, y);
            }
        }

        if (resultRight < resultLeft || resultBottom < resultTop)
        {
            return SKRectI.Empty;
        }

        return new SKRectI(resultLeft, resultTop, resultRight + 1, resultBottom + 1);
    }

    private static SKRect LayoutRectToSkRect(UiLayoutBox rect)
    {
        return new SKRect((float)rect.X, (float)rect.Y, (float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
    }

    private static SKRect Inset(SKRect rect, float amount)
    {
        return new SKRect(rect.Left + amount, rect.Top + amount, rect.Right - amount, rect.Bottom - amount);
    }

    private static bool IsAccentLike(SKColor pixel)
    {
        return pixel.Red <= 40 &&
            pixel.Green >= 80 &&
            pixel.Green <= 150 &&
            pixel.Blue >= 150;
    }

    private static bool IsBlueTextLike(SKColor pixel)
    {
        return pixel.Red <= 160 &&
            pixel.Green >= 50 &&
            pixel.Blue >= 120;
    }

    private static bool IsDarkTextLike(SKColor pixel)
    {
        return pixel.Red <= 90 &&
            pixel.Green <= 90 &&
            pixel.Blue <= 90;
    }

    private static bool IsRadioKnockoutEdgeLike(SKColor pixel)
    {
        return pixel.Red >= 100 &&
            pixel.Green >= 160 &&
            pixel.Blue >= 210;
    }

    private static void AssertTextCenterWithin(SKRectI textBounds, UiLayoutBox controlBounds, float tolerance)
    {
        var textCenter = textBounds.Left + textBounds.Width / 2f;
        var controlCenter = (float)(controlBounds.X + controlBounds.Width / 2);
        Assert.IsTrue(
            Math.Abs(textCenter - controlCenter) <= tolerance,
            $"Expected text center {textCenter} to be within {tolerance} px of control center {controlCenter}; text bounds were {textBounds}.");
    }

    private static SKRectI BoundsOfPixelsOtherThan(SKBitmap bitmap, SKColor color)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == color)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left || bottom < top)
        {
            return SKRectI.Empty;
        }

        return new SKRectI(left, top, right + 1, bottom + 1);
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
