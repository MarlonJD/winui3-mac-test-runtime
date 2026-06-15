using System.Text.Json;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Automation;

public sealed record WindowsCustomRuntimeUiaProviderOptions(
    string OutputDirectory,
    string ScenarioName);

public sealed record WindowsCustomRuntimeUiaProviderScaffold(
    string SchemaVersion,
    string Lane,
    string Runtime,
    string Driver,
    string Provider,
    string Renderer,
    string CiPolicy,
    string DefaultPrCi,
    string ReferenceBoundary,
    string ScenarioName,
    string UiaTreePath,
    string ProviderSourcePath,
    string MetadataPath)
{
    public static WindowsCustomRuntimeUiaProviderScaffold Write(
        AutomationDocument automation,
        WindowsCustomRuntimeUiaProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(automation);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        var uiaTreePath = Path.Combine(outputDirectory, "windows-custom-runtime-uia-tree.json");
        var providerSourcePath = Path.Combine(outputDirectory, "WindowsCustomRuntimeUiaProvider.cs");
        var metadataPath = Path.Combine(outputDirectory, "windows-custom-runtime-uia-provider.json");

        var uiaTree = WindowsCustomRuntimeUiaTree.FromAutomation(automation);
        File.WriteAllText(uiaTreePath, JsonSerializer.Serialize(uiaTree, JsonOptions));
        File.WriteAllText(providerSourcePath, BuildProviderSource());

        var scaffold = new WindowsCustomRuntimeUiaProviderScaffold(
            SchemaVersion: ArtifactSchemas.WindowsCustomRuntimeUiaProvider,
            Lane: "windows-custom-runtime",
            Runtime: "custom-runtime",
            Driver: "flaui-uia3",
            Provider: "custom-runtime-uia-provider",
            Renderer: "custom-runtime-renderer",
            CiPolicy: "optional-windows-only",
            DefaultPrCi: "not-default-pr-ci",
            ReferenceBoundary: "not-windows-reference",
            ScenarioName: options.ScenarioName,
            UiaTreePath: uiaTreePath,
            ProviderSourcePath: providerSourcePath,
            MetadataPath: metadataPath);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(scaffold, JsonOptions));
        return scaffold;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string BuildProviderSource()
    {
        return """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Windows.Automation;
            using System.Windows.Automation.Provider;

            namespace WinUI3.CustomRuntime.WindowsAutomation;

            public sealed class WindowsCustomRuntimeUiaProvider
            {
                private readonly RuntimeUiaElement root;

                public WindowsCustomRuntimeUiaProvider(RuntimeUiaNode rootNode)
                {
                    root = new RuntimeUiaElement(rootNode, parent: null);
                }

                public IRawElementProviderSimple RootProvider => root;
            }

            public sealed record RuntimeUiaNode(
                string RuntimeId,
                string? AutomationId,
                string? Name,
                string UiaControlType,
                double X,
                double Y,
                double Width,
                double Height,
                bool IsEnabled,
                bool HasKeyboardFocus,
                string? Value,
                bool IsSelected,
                string? ToggleState,
                IReadOnlyList<string> Patterns,
                IReadOnlyList<RuntimeUiaNode> Children);

            public sealed class RuntimeUiaElement : IRawElementProviderSimple,
                IRawElementProviderFragment,
                IRawElementProviderFragmentRoot,
                IInvokeProvider,
                IValueProvider,
                IToggleProvider,
                ISelectionItemProvider,
                IScrollProvider
            {
                private readonly RuntimeUiaNode node;
                private readonly RuntimeUiaElement? parent;
                private readonly RuntimeUiaElement[] children;
                private string? value;
                private ToggleState toggleState;
                private bool isSelected;
                private double horizontalScrollPercent;
                private double verticalScrollPercent;

                public RuntimeUiaElement(RuntimeUiaNode node, RuntimeUiaElement? parent)
                {
                    this.node = node;
                    this.parent = parent;
                    value = node.Value;
                    toggleState = node.ToggleState == "On" ? ToggleState.On : node.ToggleState == "Indeterminate" ? ToggleState.Indeterminate : ToggleState.Off;
                    isSelected = node.IsSelected;
                    children = new RuntimeUiaElement[node.Children.Count];
                    for (var index = 0; index < children.Length; index++)
                    {
                        children[index] = new RuntimeUiaElement(node.Children[index], this);
                    }
                }

                public ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider;

                public IRawElementProviderSimple HostRawElementProvider => null!;

                public object? GetPatternProvider(int patternId)
                {
                    if (patternId == InvokePatternIdentifiers.Pattern.Id && node.Patterns.Contains("InvokePattern")) return this;
                    if (patternId == ValuePatternIdentifiers.Pattern.Id && node.Patterns.Contains("ValuePattern")) return this;
                    if (patternId == TogglePatternIdentifiers.Pattern.Id && node.Patterns.Contains("TogglePattern")) return this;
                    if (patternId == SelectionItemPatternIdentifiers.Pattern.Id && node.Patterns.Contains("SelectionItemPattern")) return this;
                    if (patternId == ScrollPatternIdentifiers.Pattern.Id && node.Patterns.Contains("ScrollPattern")) return this;
                    return null;
                }

                public object? GetPropertyValue(int propertyId)
                {
                    if (propertyId == AutomationElementIdentifiers.AutomationIdProperty.Id) return node.AutomationId ?? string.Empty;
                    if (propertyId == AutomationElementIdentifiers.NameProperty.Id) return node.Name ?? string.Empty;
                    if (propertyId == AutomationElementIdentifiers.IsEnabledProperty.Id) return node.IsEnabled;
                    if (propertyId == AutomationElementIdentifiers.HasKeyboardFocusProperty.Id) return node.HasKeyboardFocus;
                    if (propertyId == AutomationElementIdentifiers.BoundingRectangleProperty.Id) return new System.Windows.Rect(node.X, node.Y, node.Width, node.Height);
                    if (propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id) return ControlTypeFromName(node.UiaControlType).Id;
                    return null;
                }

                public IRawElementProviderFragmentRoot FragmentRoot => parent?.FragmentRoot ?? (IRawElementProviderFragmentRoot)this;

                public IRawElementProviderFragment? Navigate(NavigateDirection direction)
                {
                    if (direction == NavigateDirection.Parent) return parent;
                    if (direction == NavigateDirection.FirstChild) return children.Length == 0 ? null : children[0];
                    if (direction == NavigateDirection.LastChild) return children.Length == 0 ? null : children[^1];
                    if (parent is null) return null;
                    var siblings = parent.children;
                    var index = Array.IndexOf(siblings, this);
                    if (direction == NavigateDirection.NextSibling && index >= 0 && index + 1 < siblings.Length) return siblings[index + 1];
                    if (direction == NavigateDirection.PreviousSibling && index > 0) return siblings[index - 1];
                    return null;
                }

                public int[] GetRuntimeId() => new[] { AutomationInteropProvider.AppendRuntimeId, node.RuntimeId.GetHashCode(StringComparison.Ordinal) };

                public System.Windows.Rect BoundingRectangle => new(node.X, node.Y, node.Width, node.Height);

                public IRawElementProviderSimple[] GetEmbeddedFragmentRoots() => Array.Empty<IRawElementProviderSimple>();

                public void SetFocus()
                {
                }

                public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
                {
                    return Flatten(this).LastOrDefault(element => element.BoundingRectangle.Contains(x, y));
                }

                public IRawElementProviderFragment? GetFocus()
                {
                    return Flatten(this).FirstOrDefault(element => (bool?)element.GetPropertyValue(AutomationElementIdentifiers.HasKeyboardFocusProperty.Id) == true);
                }

                public void Invoke()
                {
                }

                public bool IsReadOnly => false;

                public string Value => value ?? string.Empty;

                public void SetValue(string value)
                {
                    this.value = value;
                }

                public ToggleState ToggleState => toggleState;

                public void Toggle()
                {
                    toggleState = toggleState == ToggleState.On ? ToggleState.Off : ToggleState.On;
                }

                public bool IsSelected => isSelected;

                public IRawElementProviderSimple SelectionContainer => parent ?? this;

                public void AddToSelection() => isSelected = true;

                public void RemoveFromSelection() => isSelected = false;

                public void Select() => isSelected = true;

                public bool HorizontallyScrollable => node.Patterns.Contains("ScrollPattern");

                public double HorizontalScrollPercent => horizontalScrollPercent;

                public double HorizontalViewSize => 100;

                public bool VerticallyScrollable => node.Patterns.Contains("ScrollPattern");

                public double VerticalScrollPercent => verticalScrollPercent;

                public double VerticalViewSize => 100;

                public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
                {
                }

                public void SetScrollPercent(double horizontalPercent, double verticalPercent)
                {
                    horizontalScrollPercent = horizontalPercent;
                    verticalScrollPercent = verticalPercent;
                }

                private static IEnumerable<RuntimeUiaElement> Flatten(RuntimeUiaElement element)
                {
                    yield return element;
                    foreach (var child in element.children.SelectMany(Flatten))
                    {
                        yield return child;
                    }
                }

                private static ControlType ControlTypeFromName(string name)
                {
                    return name switch
                    {
                        "Window" => ControlType.Window,
                        "Pane" => ControlType.Pane,
                        "Group" => ControlType.Group,
                        "Text" => ControlType.Text,
                        "Button" => ControlType.Button,
                        "Edit" => ControlType.Edit,
                        "CheckBox" => ControlType.CheckBox,
                        "RadioButton" => ControlType.RadioButton,
                        "List" => ControlType.List,
                        "ListItem" => ControlType.ListItem,
                        "ComboBox" => ControlType.ComboBox,
                        "Slider" => ControlType.Slider,
                        "ProgressBar" => ControlType.ProgressBar,
                        _ => ControlType.Custom
                    };
                }
            }
            """;
    }
}

public sealed record WindowsCustomRuntimeUiaTree(
    string SchemaVersion,
    string Lane,
    string Runtime,
    string Driver,
    IReadOnlyList<WindowsCustomRuntimeUiaNode> Nodes)
{
    public static WindowsCustomRuntimeUiaTree FromAutomation(AutomationDocument automation)
    {
        return new WindowsCustomRuntimeUiaTree(
            ArtifactSchemas.WindowsCustomRuntimeUiaProvider,
            "windows-custom-runtime",
            "custom-runtime",
            "flaui-uia3",
            Flatten(automation.Root).ToArray());
    }

    private static IEnumerable<WindowsCustomRuntimeUiaNode> Flatten(AutomationNode node)
    {
        yield return WindowsCustomRuntimeUiaNode.FromAutomation(node);
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }
}

public sealed record WindowsCustomRuntimeUiaNode(
    string RuntimeId,
    string? AutomationId,
    string? Name,
    string UiaControlType,
    WindowsCustomRuntimeUiaBounds BoundingRectangle,
    IReadOnlyList<string> Patterns,
    string? Value,
    bool IsEnabled,
    bool HasKeyboardFocus,
    bool IsSelected,
    string? ToggleState,
    bool HorizontallyScrollable,
    bool VerticallyScrollable)
{
    public static WindowsCustomRuntimeUiaNode FromAutomation(AutomationNode node)
    {
        return new WindowsCustomRuntimeUiaNode(
            node.RuntimeId,
            node.AutomationId,
            node.Name,
            ControlTypeFor(node),
            new WindowsCustomRuntimeUiaBounds(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height),
            PatternsFor(node).ToArray(),
            node.Value,
            node.IsEnabled,
            node.HasKeyboardFocus,
            node.IsSelected,
            node.ToggleState?.ToString(),
            node.HorizontallyScrollable,
            node.VerticallyScrollable);
    }

    private static string ControlTypeFor(AutomationNode node)
    {
        return node.ControlType switch
        {
            AutomationControlType.Window => "Window",
            AutomationControlType.Pane => "Pane",
            AutomationControlType.Group => "Group",
            AutomationControlType.Text => "Text",
            AutomationControlType.Button => "Button",
            AutomationControlType.Edit => "Edit",
            AutomationControlType.CheckBox => "CheckBox",
            AutomationControlType.RadioButton => "RadioButton",
            AutomationControlType.List => "List",
            AutomationControlType.ListItem => "ListItem",
            AutomationControlType.ComboBox => "ComboBox",
            AutomationControlType.Slider => "Slider",
            AutomationControlType.ProgressBar => "ProgressBar",
            _ => "Custom"
        };
    }

    private static IEnumerable<string> PatternsFor(AutomationNode node)
    {
        if (node.Patterns.Contains(AutomationPattern.Invoke))
        {
            yield return "InvokePattern";
        }

        if (node.Patterns.Contains(AutomationPattern.Value))
        {
            yield return "ValuePattern";
        }

        if (node.Patterns.Contains(AutomationPattern.Toggle))
        {
            yield return "TogglePattern";
        }

        if (node.Patterns.Contains(AutomationPattern.SelectionItem))
        {
            yield return "SelectionItemPattern";
        }

        if (node.Patterns.Contains(AutomationPattern.Scroll))
        {
            yield return "ScrollPattern";
        }
    }
}

public sealed record WindowsCustomRuntimeUiaBounds(double X, double Y, double Width, double Height);
