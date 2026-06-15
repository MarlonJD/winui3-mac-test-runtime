using System.Text.Json;
using WinUI3.MacRunner.Automation;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Tests;

[TestClass]
public sealed class WindowsCustomRuntimeUiaProviderTests
{
    [TestMethod]
    public void WindowsCustomRuntimeUiaProviderScaffoldMapsAutomationCoreWithoutBecomingWindowsReference()
    {
        var root = Path.Combine(Path.GetTempPath(), "winui3-windows-custom-uia-tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(root, "windows-custom-runtime");
        var automation = BuildAutomationDocument();

        var scaffold = WindowsCustomRuntimeUiaProviderScaffold.Write(automation, new WindowsCustomRuntimeUiaProviderOptions(
            OutputDirectory: outputRoot,
            ScenarioName: "login-light"));

        Assert.AreEqual("windows-custom-runtime", scaffold.Lane);
        Assert.AreEqual("custom-runtime", scaffold.Runtime);
        Assert.AreEqual("flaui-uia3", scaffold.Driver);
        Assert.AreEqual("custom-runtime-uia-provider", scaffold.Provider);
        Assert.AreEqual("not-windows-reference", scaffold.ReferenceBoundary);
        Assert.AreEqual("not-default-pr-ci", scaffold.DefaultPrCi);
        Assert.IsTrue(File.Exists(scaffold.UiaTreePath));
        Assert.IsTrue(File.Exists(scaffold.ProviderSourcePath));
        Assert.IsTrue(File.Exists(scaffold.MetadataPath));

        using var providerTree = JsonDocument.Parse(File.ReadAllText(scaffold.UiaTreePath));
        var nodes = providerTree.RootElement.GetProperty("nodes").EnumerateArray().ToArray();
        AssertNode(nodes, "SubmitButton", "Button", "InvokePattern");
        AssertNode(nodes, "TitleText", "Text");
        AssertNode(nodes, "EmailBox", "Edit", "ValuePattern");
        AssertNode(nodes, "RememberBox", "CheckBox", "TogglePattern");
        AssertNode(nodes, "PriorityRadio", "RadioButton", "SelectionItemPattern");
        AssertNode(nodes, "ResultsList", "List");
        AssertNode(nodes, "ResultRow", "ListItem", "SelectionItemPattern");
        AssertNode(nodes, "Scroller", "Pane", "ScrollPattern");

        var source = File.ReadAllText(scaffold.ProviderSourcePath);
        StringAssert.Contains(source, "using System.Windows.Automation.Provider;");
        StringAssert.Contains(source, "RuntimeUiaElement : IRawElementProviderSimple");
        StringAssert.Contains(source, "IRawElementProviderFragment");
        StringAssert.Contains(source, "IInvokeProvider");
        StringAssert.Contains(source, "IValueProvider");
        StringAssert.Contains(source, "IToggleProvider");
        StringAssert.Contains(source, "ISelectionItemProvider");
        StringAssert.Contains(source, "IScrollProvider");
        StringAssert.Contains(source, "ProviderOptions.ServerSideProvider");
        Assert.IsFalse(source.Contains("native-winui", StringComparison.Ordinal), "Custom-runtime provider must not claim native WinUI reference status.");

        using var metadata = JsonDocument.Parse(File.ReadAllText(scaffold.MetadataPath));
        Assert.AreEqual("0.1", metadata.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("windows-custom-runtime", metadata.RootElement.GetProperty("lane").GetString());
        Assert.AreEqual("custom-runtime", metadata.RootElement.GetProperty("runtime").GetString());
        Assert.AreEqual("flaui-uia3", metadata.RootElement.GetProperty("driver").GetString());
        Assert.AreEqual("custom-runtime-uia-provider", metadata.RootElement.GetProperty("provider").GetString());
        Assert.AreEqual("not-windows-reference", metadata.RootElement.GetProperty("referenceBoundary").GetString());

        var program = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "WinUI3.MacRunner", "Program.cs"));
        StringAssert.Contains(program, "\"windows-custom-runtime-uia\" => RunWindowsCustomRuntimeUia(args[1..])");
        StringAssert.Contains(program, "windows-custom-runtime-uia --automation <automation-core.json> --output <dir>");
    }

    private static void AssertNode(JsonElement[] nodes, string automationId, string controlType, params string[] patterns)
    {
        var node = nodes.Single(item => item.GetProperty("automationId").GetString() == automationId);
        Assert.AreEqual(controlType, node.GetProperty("uiaControlType").GetString());
        var actualPatterns = node.GetProperty("patterns").EnumerateArray().Select(item => item.GetString()).ToArray();
        foreach (var pattern in patterns)
        {
            CollectionAssert.Contains(actualPatterns, pattern);
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
