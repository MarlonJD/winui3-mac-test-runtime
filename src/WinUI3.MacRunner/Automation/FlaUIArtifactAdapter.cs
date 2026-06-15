using System.Text.Json;
using System.Text.Json.Serialization;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Automation;

/// <summary>
/// Runtime-owned, FlaUI/UIA3-<em>shaped</em> read adapter over the macOS runtime
/// artifacts (<c>tree.json</c>, <c>accessibility.json</c>, <c>interactions.json</c>).
/// </summary>
/// <remarks>
/// This is an <strong>artifact adapter</strong>, not a native macOS UI Automation
/// provider. It projects already-emitted runtime evidence into a Windows-automation
/// shaped surface so integration tests can query UIA-like state without a live
/// provider. It cannot drive real input, invoke patterns, or subscribe to events,
/// and it does not let unmodified FlaUI/UIA3 tests run against macOS. See
/// <see cref="BuildCompatibilityReport"/> for the explicit supported/unsupported list.
/// </remarks>
public sealed class FlaUIArtifactAdapter
{
    public const string TreeFileName = "tree.json";
    public const string AccessibilityFileName = "accessibility.json";
    public const string InteractionsFileName = "interactions.json";

    private readonly IReadOnlyList<ArtifactAutomationElement> elements;
    private readonly IReadOnlyList<ArtifactActionResult> actions;

    private FlaUIArtifactAdapter(
        ArtifactAutomationElement root,
        IReadOnlyList<ArtifactActionResult> actions)
    {
        Root = root;
        elements = Flatten(root).ToArray();
        this.actions = actions;
    }

    /// <summary>The window/root element of the projected automation tree.</summary>
    public ArtifactAutomationElement Root { get; }

    /// <summary>All projected elements, depth-first, including the root.</summary>
    public IReadOnlyList<ArtifactAutomationElement> Elements => elements;

    /// <summary>Recorded scenario action results from <c>interactions.json</c>.</summary>
    public IReadOnlyList<ArtifactActionResult> Actions => actions;

    /// <summary>
    /// Loads the adapter from a standard runtime artifact directory. The
    /// accessibility artifact is required; tree (layout/bounds) and interactions
    /// (action results) artifacts are optional.
    /// </summary>
    public static FlaUIArtifactAdapter LoadFromDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        return LoadFromFiles(
            Path.Combine(directory, TreeFileName),
            Path.Combine(directory, AccessibilityFileName),
            Path.Combine(directory, InteractionsFileName));
    }

    /// <summary>
    /// Loads the adapter from explicit artifact paths. <paramref name="accessibilityPath"/>
    /// must exist; <paramref name="treePath"/> and <paramref name="interactionsPath"/>
    /// are used when present and silently skipped otherwise.
    /// </summary>
    public static FlaUIArtifactAdapter LoadFromFiles(
        string? treePath,
        string accessibilityPath,
        string? interactionsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessibilityPath);
        if (!File.Exists(accessibilityPath))
        {
            throw new FileNotFoundException(
                $"Required accessibility artifact '{accessibilityPath}' was not found.",
                accessibilityPath);
        }

        var accessibility = Deserialize<AccessibilityDocument>(accessibilityPath)
            ?? throw new InvalidDataException($"Accessibility artifact '{accessibilityPath}' was empty.");

        UiTreeDocument? tree = null;
        if (!string.IsNullOrWhiteSpace(treePath) && File.Exists(treePath))
        {
            tree = Deserialize<UiTreeDocument>(treePath);
        }

        IReadOnlyList<ArtifactActionResult> actions = Array.Empty<ArtifactActionResult>();
        if (!string.IsNullOrWhiteSpace(interactionsPath) && File.Exists(interactionsPath))
        {
            var report = Deserialize<InteractionReport>(interactionsPath);
            if (report is not null)
            {
                actions = report.Steps.Select(step => new ArtifactActionResult(step)).ToArray();
            }
        }

        var root = ProjectElement(accessibility.Root, tree?.Root);
        return new FlaUIArtifactAdapter(root, actions);
    }

    /// <summary>UIA <c>AutomationIdProperty</c> lookup.</summary>
    public ArtifactAutomationElement? FindByAutomationId(string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return elements.FirstOrDefault(element =>
            string.Equals(element.AutomationId, automationId, StringComparison.Ordinal));
    }

    /// <summary>UIA <c>NameProperty</c> lookup.</summary>
    public ArtifactAutomationElement? FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return elements.FirstOrDefault(element =>
            string.Equals(element.Name, name, StringComparison.Ordinal));
    }

    /// <summary>
    /// Resolves an element from a runtime selector: <c>automationId=…</c>,
    /// <c>name=…</c>, or a bare name.
    /// </summary>
    public ArtifactAutomationElement? FindBySelector(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        if (selector.StartsWith("automationId=", StringComparison.Ordinal))
        {
            return FindByAutomationId(selector["automationId=".Length..]);
        }

        if (selector.StartsWith("name=", StringComparison.Ordinal))
        {
            return FindByName(selector["name=".Length..]);
        }

        return FindByName(selector);
    }

    /// <summary>
    /// Returns the recorded action result whose selector (or, failing that, target)
    /// matches <paramref name="selector"/>, or <see langword="null"/> when no
    /// recorded action used that selector.
    /// </summary>
    public ArtifactActionResult? FindActionBySelector(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return actions.FirstOrDefault(action =>
                   string.Equals(action.Selector, selector, StringComparison.Ordinal))
               ?? actions.FirstOrDefault(action =>
                   string.Equals(action.Target, selector, StringComparison.Ordinal));
    }

    /// <summary>
    /// Builds the explicit compatibility report: which FlaUI/UIA concepts this
    /// artifact adapter supports and which remain unsupported.
    /// </summary>
    public AutomationContractReport BuildCompatibilityReport() => AutomationContractReport.ForArtifactAdapter();

    /// <summary>
    /// Builds the macOS-only automation parity report from the recorded actions.
    /// Windows reference status is always <c>NotRunOnWindows</c> here; the native
    /// Windows reference tier is out of scope for the artifact adapter.
    /// </summary>
    public AutomationParityReport BuildParityReport() => AutomationParityReport.FromActions(actions);

    /// <summary>
    /// Emits the explicit compatibility report and the macOS-only automation parity
    /// report as JSON artifacts under <paramref name="outputDirectory"/>.
    /// </summary>
    public AutomationAdapterReportPaths WriteReports(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var compatibilityPath = Path.Combine(outputDirectory, CompatibilityReportFileName);
        var parityPath = Path.Combine(outputDirectory, ParityReportFileName);
        File.WriteAllText(
            compatibilityPath,
            JsonSerializer.Serialize(BuildCompatibilityReport(), ReportJsonOptions));
        File.WriteAllText(
            parityPath,
            JsonSerializer.Serialize(BuildParityReport(), ReportJsonOptions));

        return new AutomationAdapterReportPaths(compatibilityPath, parityPath);
    }

    public const string CompatibilityReportFileName = "automation-adapter-report.json";
    public const string ParityReportFileName = "automation-parity.json";

    // Mirrors JsonDefaults (camelCase + indented + null-ignore) but renders the
    // parity enums as readable strings without mutating the shared runtime options.
    private static readonly JsonSerializerOptions ReportJsonOptions = BuildReportJsonOptions();

    private static JsonSerializerOptions BuildReportJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonDefaults.Options);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static IEnumerable<ArtifactAutomationElement> Flatten(ArtifactAutomationElement element)
    {
        yield return element;
        foreach (var child in element.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static ArtifactAutomationElement ProjectElement(AccessibilityNode accessibility, UiNode? tree)
    {
        // The accessibility tree is built 1:1 from the UI tree, so we walk both in
        // lockstep to attach framework type + layout. If a subtree diverges in
        // shape we stop attaching tree data there rather than mis-correlating.
        var canZip = tree is not null && tree.Children.Count == accessibility.Children.Count;
        var children = new List<ArtifactAutomationElement>(accessibility.Children.Count);
        for (var index = 0; index < accessibility.Children.Count; index++)
        {
            var childTree = canZip ? tree!.Children[index] : null;
            children.Add(ProjectElement(accessibility.Children[index], childTree));
        }

        return new ArtifactAutomationElement(
            automationId: accessibility.AutomationId,
            name: accessibility.Name,
            role: accessibility.Role,
            frameworkType: tree?.Type,
            controlType: ControlTypeMap.Map(tree?.Type, accessibility.Role),
            isEnabled: accessibility.IsEnabled,
            hasKeyboardFocus: accessibility.IsFocused,
            isKeyboardFocusable: accessibility.IsFocusable,
            isSelected: accessibility.IsSelected,
            isChecked: accessibility.IsChecked,
            isExpanded: accessibility.IsExpanded,
            value: accessibility.Value,
            label: accessibility.Label,
            helpText: accessibility.HelpText,
            boundingRectangle: ToBounds(tree?.Layout),
            children: children);
    }

    private static ArtifactBoundingRectangle? ToBounds(UiLayoutBox? layout)
    {
        return layout is null
            ? null
            : new ArtifactBoundingRectangle(layout.X, layout.Y, layout.Width, layout.Height);
    }

    private static T? Deserialize<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonDefaults.Options);
    }
}
