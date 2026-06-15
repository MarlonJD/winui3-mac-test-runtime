using System.Text.Json;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.MacOS;

public sealed record MacOsAxAdapterOptions(
    string OutputDirectory,
    string ScenarioName);

public sealed record MacOsAxAdapterScaffold(
    string SchemaVersion,
    string Mode,
    string Driver,
    string CiPolicy,
    string DefaultPrCi,
    string ScenarioName,
    string AxTreePath,
    string AdapterSourcePath,
    string MetadataPath)
{
    public static MacOsAxAdapterScaffold Write(AutomationDocument automation, MacOsAxAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(automation);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        var axTreePath = Path.Combine(outputDirectory, "macos-ax-tree.json");
        var adapterSourcePath = Path.Combine(outputDirectory, "MacOsAxAdapter.swift");
        var metadataPath = Path.Combine(outputDirectory, "macos-windowed-ax-adapter.json");
        var axTree = MacOsAxTree.FromAutomation(automation);

        File.WriteAllText(axTreePath, JsonSerializer.Serialize(axTree, JsonOptions));
        File.WriteAllText(adapterSourcePath, BuildSwiftAdapter());

        var scaffold = new MacOsAxAdapterScaffold(
            SchemaVersion: "0.1",
            Mode: "macos-windowed-ax",
            Driver: "ax",
            CiPolicy: "local-manual",
            DefaultPrCi: "not-default-pr-ci",
            ScenarioName: options.ScenarioName,
            AxTreePath: axTreePath,
            AdapterSourcePath: adapterSourcePath,
            MetadataPath: metadataPath);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(scaffold, JsonOptions));
        return scaffold;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string BuildSwiftAdapter()
    {
        return """
            import AppKit
            import Foundation

            struct RuntimeAxNode {
                let runtimeId: String
                let automationId: String
                let label: String
                let role: NSAccessibility.Role
                let frame: CGRect
                let actions: [String]
                let value: String?
                let children: [RuntimeAxNode]
            }

            final class RuntimeAccessibilityElement: NSAccessibilityElement {
                let node: RuntimeAxNode
                var mutableValue: String?

                init(node: RuntimeAxNode, parent: Any?) {
                    self.node = node
                    self.mutableValue = node.value
                    super.init()
                    setAccessibilityParent(parent)
                    setAccessibilityRole(node.role)
                    setAccessibilityLabel(node.label)
                    setAccessibilityFrame(node.frame)
                    setAccessibilityChildren(node.children.map { RuntimeAccessibilityElement(node: $0, parent: self) })
                    if let value = node.value {
                        setAccessibilityValue(value)
                    }
                }

                override func accessibilityActionNames() -> [NSAccessibility.Action] {
                    node.actions.compactMap { action in
                        switch action {
                        case "AXPress": return .press
                        case "AXSelected": return .pick
                        case "AXScroll": return .scrollToVisible
                        default: return nil
                        }
                    }
                }

                override func accessibilityPerformPress() -> Bool {
                    NSAccessibility.post(element: self, notification: .valueChanged)
                    return node.actions.contains("AXPress") || node.actions.contains("AXSelected")
                }

                override func setAccessibilityValue(_ accessibilityValue: Any?) {
                    mutableValue = accessibilityValue as? String
                    super.setAccessibilityValue(accessibilityValue)
                    NSAccessibility.post(element: self, notification: .valueChanged)
                }

                override func accessibilityValue() -> Any? {
                    mutableValue
                }
            }

            func roleFromString(_ role: String) -> NSAccessibility.Role {
                switch role {
                case "AXButton": return NSAccessibility.Role.button
                case "AXStaticText": return NSAccessibility.Role.staticText
                case "AXTextField": return NSAccessibility.Role.textField
                case "AXCheckBox": return NSAccessibility.Role.checkBox
                case "AXRadioButton": return NSAccessibility.Role.radioButton
                case "AXList": return NSAccessibility.Role.list
                case "AXRow": return NSAccessibility.Role.row
                case "AXScrollArea": return NSAccessibility.Role.scrollArea
                default: return NSAccessibility.Role.group
                }
            }
            """;
    }
}

public sealed record MacOsAxTree(
    string SchemaVersion,
    string Mode,
    string Driver,
    IReadOnlyList<MacOsAxNode> Nodes)
{
    public static MacOsAxTree FromAutomation(AutomationDocument automation)
    {
        return new MacOsAxTree(
            "0.1",
            "macos-windowed-ax",
            "ax",
            Flatten(automation.Root).ToArray());
    }

    private static IEnumerable<MacOsAxNode> Flatten(AutomationNode node)
    {
        yield return MacOsAxNode.FromAutomation(node);
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}

public sealed record MacOsAxNode(
    string RuntimeId,
    string? AutomationId,
    string? Name,
    string AxRole,
    MacOsAxFrame AxFrame,
    IReadOnlyList<string> AxActions,
    string? Value,
    bool IsEnabled,
    bool IsSelected)
{
    public static MacOsAxNode FromAutomation(AutomationNode node)
    {
        return new MacOsAxNode(
            node.RuntimeId,
            node.AutomationId,
            node.Name,
            RoleFor(node),
            new MacOsAxFrame(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height),
            ActionsFor(node).ToArray(),
            node.Value,
            node.IsEnabled,
            node.IsSelected);
    }

    private static string RoleFor(AutomationNode node)
    {
        if (node.Patterns.Contains(AutomationPattern.Scroll))
        {
            return "AXScrollArea";
        }

        return node.ControlType switch
        {
            AutomationControlType.Button => "AXButton",
            AutomationControlType.Text => "AXStaticText",
            AutomationControlType.Edit => "AXTextField",
            AutomationControlType.CheckBox => "AXCheckBox",
            AutomationControlType.RadioButton => "AXRadioButton",
            AutomationControlType.List => "AXList",
            AutomationControlType.ListItem => "AXRow",
            AutomationControlType.Pane when node.Patterns.Contains(AutomationPattern.Scroll) => "AXScrollArea",
            _ => "AXGroup"
        };
    }

    private static IEnumerable<string> ActionsFor(AutomationNode node)
    {
        if (node.Patterns.Contains(AutomationPattern.Invoke) ||
            node.Patterns.Contains(AutomationPattern.Toggle))
        {
            yield return "AXPress";
        }

        if (node.Patterns.Contains(AutomationPattern.Value))
        {
            yield return "AXValue";
        }

        if (node.Patterns.Contains(AutomationPattern.SelectionItem))
        {
            yield return "AXSelected";
        }

        if (node.Patterns.Contains(AutomationPattern.Scroll))
        {
            yield return "AXScroll";
        }
    }
}

public sealed record MacOsAxFrame(double X, double Y, double Width, double Height);
