using System.Text.Json;
using WinUI3.MacRunner.MacOS;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class MacOsAxAdapterTests
{
    [TestMethod]
    public void MacOsAxAdapterMapsAutomationCoreToNsAccessibilityScaffold()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-macos-ax-adapter-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(root, "macos-windowed-ax");
        var automation = BuildAutomationDocument();

        var scaffold = MacOsAxAdapterScaffold.Write(automation, new MacOsAxAdapterOptions(
            OutputDirectory: outputRoot,
            ScenarioName: "login-light"));

        Assert.AreEqual("macos-windowed-ax", scaffold.Mode);
        Assert.AreEqual("ax", scaffold.Driver);
        Assert.AreEqual("local-manual", scaffold.CiPolicy);
        Assert.AreEqual("not-default-pr-ci", scaffold.DefaultPrCi);
        Assert.IsTrue(File.Exists(scaffold.AxTreePath));
        Assert.IsTrue(File.Exists(scaffold.AdapterSourcePath));
        Assert.IsTrue(File.Exists(scaffold.MetadataPath));

        using var axTree = JsonDocument.Parse(File.ReadAllText(scaffold.AxTreePath));
        var nodes = axTree.RootElement.GetProperty("nodes").EnumerateArray().ToArray();
        AssertNode(nodes, "SubmitButton", "AXButton", "AXPress");
        AssertNode(nodes, "TitleText", "AXStaticText");
        AssertNode(nodes, "EmailBox", "AXTextField", "AXValue");
        AssertNode(nodes, "RememberBox", "AXCheckBox", "AXPress");
        AssertNode(nodes, "PriorityRadio", "AXRadioButton", "AXSelected");
        AssertNode(nodes, "ResultsList", "AXList");
        AssertNode(nodes, "ResultRow", "AXRow", "AXSelected");
        AssertNode(nodes, "Scroller", "AXScrollArea", "AXScroll");

        var source = File.ReadAllText(scaffold.AdapterSourcePath);
        StringAssert.Contains(source, "import AppKit");
        StringAssert.Contains(source, "RuntimeAccessibilityElement: NSAccessibilityElement");
        StringAssert.Contains(source, "NSAccessibility.Role.button");
        StringAssert.Contains(source, "NSAccessibility.Role.staticText");
        StringAssert.Contains(source, "NSAccessibility.Role.textField");
        StringAssert.Contains(source, "NSAccessibility.Role.checkBox");
        StringAssert.Contains(source, "NSAccessibility.Role.radioButton");
        StringAssert.Contains(source, "NSAccessibility.Role.list");
        StringAssert.Contains(source, "NSAccessibility.Role.row");
        StringAssert.Contains(source, "NSAccessibility.Role.scrollArea");
        StringAssert.Contains(source, "accessibilityPerformPress");
        StringAssert.Contains(source, "setAccessibilityValue");
        StringAssert.Contains(source, "NSAccessibility.post");
        Assert.IsFalse(source.Contains("AXUIElement", StringComparison.Ordinal), "Phase 12 maps to NSAccessibilityElement, not a separate AXUIElement client.");
        Assert.IsFalse(source.Contains("CAMetalLayer", StringComparison.Ordinal), "Phase 12 must not introduce raw Metal rendering.");

        using var metadata = JsonDocument.Parse(File.ReadAllText(scaffold.MetadataPath));
        Assert.AreEqual("0.1", metadata.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("macos-windowed-ax", metadata.RootElement.GetProperty("mode").GetString());
        Assert.AreEqual("ax", metadata.RootElement.GetProperty("driver").GetString());
        Assert.AreEqual("local-manual", metadata.RootElement.GetProperty("ciPolicy").GetString());
        Assert.AreEqual("not-default-pr-ci", metadata.RootElement.GetProperty("defaultPrCi").GetString());
        Assert.AreEqual("login-light", metadata.RootElement.GetProperty("scenarioName").GetString());

        var program = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "WinUI3.MacRunner", "Program.cs"));
        StringAssert.Contains(program, "\"macos-ax-adapter\" => RunMacOsAxAdapter(args[1..])");
        StringAssert.Contains(program, "macos-ax-adapter --automation <automation-core.json> --output <dir>");
    }

    private static void AssertNode(JsonElement[] nodes, string automationId, string role, params string[] actions)
    {
        var node = nodes.Single(item => item.GetProperty("automationId").GetString() == automationId);
        Assert.AreEqual(role, node.GetProperty("axRole").GetString());
        var actualActions = node.GetProperty("axActions").EnumerateArray().Select(item => item.GetString()).ToArray();
        foreach (var action in actions)
        {
            CollectionAssert.Contains(actualActions, action);
        }
    }

    private static AutomationDocument BuildAutomationDocument()
    {
        return new AutomationDocument(
            ArtifactSchemas.AutomationCore,
            DateTimeOffset.UnixEpoch,
            Node(
                "root",
                "Root",
                "Window",
                AutomationControlType.Window,
                new AutomationBounds(0, 0, 320, 240),
                children:
                [
                    Node("root/0", "SubmitButton", "Sign in", AutomationControlType.Button, new AutomationBounds(20, 80, 120, 32), Set(AutomationPattern.Invoke)),
                    Node("root/1", "TitleText", "Welcome", AutomationControlType.Text, new AutomationBounds(20, 20, 120, 24)),
                    Node("root/2", "EmailBox", "Email", AutomationControlType.Edit, new AutomationBounds(20, 120, 180, 32), Set(AutomationPattern.Value), value: "a@example.com"),
                    Node("root/3", "RememberBox", "Remember me", AutomationControlType.CheckBox, new AutomationBounds(20, 160, 180, 32), Set(AutomationPattern.Toggle)),
                    Node("root/4", "PriorityRadio", "High", AutomationControlType.RadioButton, new AutomationBounds(20, 200, 180, 32), Set(AutomationPattern.SelectionItem), selected: true),
                    Node(
                        "root/5",
                        "ResultsList",
                        "Results",
                        AutomationControlType.List,
                        new AutomationBounds(220, 20, 80, 120),
                        children:
                        [
                            Node("root/5/0", "ResultRow", "First result", AutomationControlType.ListItem, new AutomationBounds(220, 20, 80, 32), Set(AutomationPattern.SelectionItem))
                        ]),
                    Node("root/6", "Scroller", "Scroll area", AutomationControlType.Pane, new AutomationBounds(0, 0, 320, 240), Set(AutomationPattern.Scroll))
                ]));
    }

    private static AutomationNode Node(
        string runtimeId,
        string automationId,
        string name,
        AutomationControlType controlType,
        AutomationBounds bounds,
        IReadOnlySet<AutomationPattern>? patterns = null,
        IReadOnlyList<AutomationNode>? children = null,
        string? value = null,
        bool selected = false)
    {
        return new AutomationNode(
            runtimeId,
            automationId,
            name,
            controlType,
            bounds,
            IsEnabled: true,
            IsOffscreen: false,
            IsKeyboardFocusable: true,
            HasKeyboardFocus: false,
            children ?? Array.Empty<AutomationNode>(),
            patterns ?? new HashSet<AutomationPattern>(),
            Value: value,
            IsSelected: selected);
    }

    private static IReadOnlySet<AutomationPattern> Set(params AutomationPattern[] patterns)
    {
        return new HashSet<AutomationPattern>(patterns);
    }

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
