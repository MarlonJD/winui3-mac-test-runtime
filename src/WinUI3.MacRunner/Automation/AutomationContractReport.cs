namespace WinUI3.MacRunner.Automation;

/// <summary>A single FlaUI/UIA concept and whether the artifact adapter implements it.</summary>
public sealed record AutomationConcept(string Name, string UiaSurface, string Notes);

/// <summary>Paths to the emitted automation adapter report artifacts.</summary>
public sealed record AutomationAdapterReportPaths(string CompatibilityReportPath, string ParityReportPath);

/// <summary>
/// Explicit compatibility report for <see cref="FlaUIArtifactAdapter"/>. It states
/// the FlaUI/UIA3-shaped concepts the artifact adapter supports and the concepts it
/// deliberately does not, so callers never mistake it for a native macOS UIA provider.
/// </summary>
public sealed record AutomationContractReport(
    string SchemaVersion,
    bool IsNativeUiaProvider,
    string Summary,
    IReadOnlyList<AutomationConcept> SupportedConcepts,
    IReadOnlyList<AutomationConcept> UnsupportedConcepts)
{
    public const string Schema = "0.1";

    public static AutomationContractReport ForArtifactAdapter()
    {
        var supported = new AutomationConcept[]
        {
            new("FindByAutomationId", "AutomationIdProperty", "Lookup by automation id over accessibility artifact."),
            new("FindByName", "NameProperty", "Lookup by element name over accessibility artifact."),
            new("FindBySelector", "TreeWalker/Condition", "Resolve automationId=/name=/bare selectors."),
            new("ControlTypeMapping", "ControlTypeProperty", "Map WinUI type/role to a UIA control type."),
            new("IsEnabled", "IsEnabledProperty", "Read recorded enabled state."),
            new("HasKeyboardFocus", "HasKeyboardFocusProperty", "Read recorded focus state."),
            new("IsKeyboardFocusable", "IsKeyboardFocusableProperty", "Read recorded focusable state."),
            new("SelectionItemPattern.IsSelected", "SelectionItemPattern", "Read recorded selection state."),
            new("TogglePattern.ToggleState", "TogglePattern", "Read recorded checked/toggle state."),
            new("ExpandCollapsePattern.ExpandCollapseState", "ExpandCollapsePattern", "Read recorded expanded state."),
            new("ValuePattern.Value", "ValuePattern", "Read recorded value/text."),
            new("HelpText", "HelpTextProperty", "Read recorded help text."),
            new("BoundingRectangle", "BoundingRectangleProperty", "Read layout bounds when tree layout is present."),
            new("TreeNavigation", "TreeWalker children", "Walk projected children/descendants."),
            new("ActionResultLookup", "Recorded interactions", "Look up scenario action results by selector."),
        };

        var unsupported = new AutomationConcept[]
        {
            new("NativeUiaProvider", "IRawElementProviderSimple", "No live macOS UI Automation provider; this is an artifact adapter only."),
            new("UnchangedFlaUITestExecution", "FlaUI.UIA3 driver", "Unmodified FlaUI/UIA3 tests do not run against macOS runtime yet."),
            new("PatternMethodInvocation", "Invoke/Toggle/ExpandCollapse methods", "Read-only snapshot; patterns cannot be invoked to change state."),
            new("RealPointerKeyboardInput", "Mouse/Keyboard/Pointer", "No real input injection; actions come from recorded scenario runs."),
            new("UiaEventSubscriptions", "Automation events", "No StructureChanged/PropertyChanged/Focus event subscriptions."),
            new("WindowHandlesAndProcessAttachment", "HWND / Application.Attach", "No window handles or live process attachment on macOS."),
            new("LiveRequery", "Cached vs. live tree", "Snapshot only; no re-query after the run."),
            new("TextPattern", "TextPattern / TextRange", "Document text ranges are not emitted by runtime artifacts."),
            new("GridTablePattern", "GridPattern / TablePattern", "Grid/table cell navigation is not emitted by runtime artifacts."),
            new("RangeValuePattern", "RangeValuePattern", "Numeric min/max/value ranges are not surfaced by the adapter."),
            new("ScreenshotCapture", "Capture APIs", "Pixel capture is a separate visual artifact, not part of this adapter."),
        };

        return new AutomationContractReport(
            Schema,
            IsNativeUiaProvider: false,
            Summary: "FlaUI/UIA3-shaped read adapter over macOS runtime artifacts (tree.json, accessibility.json, interactions.json). Not a native macOS UIA provider.",
            SupportedConcepts: supported,
            UnsupportedConcepts: unsupported);
    }
}

/// <summary>How a single scenario action resolved on each tier.</summary>
public enum AutomationParityStatus
{
    PassedOnMac,
    FailedOnMac,
    SkippedOnMac,
    NotRunOnWindows,
    PassedOnWindows,
    FailedOnWindows
}

/// <summary>Per-action parity record across the macOS runtime and the Windows reference tier.</summary>
public sealed record AutomationActionParity(
    int Index,
    string Type,
    string? Target,
    string? Selector,
    AutomationParityStatus MacStatus,
    AutomationParityStatus WindowsReferenceStatus);

/// <summary>
/// macOS-only automation parity report (Phase 8 report shape). Each recorded action
/// is reported as passed/failed/skipped on macOS. The Windows reference tier is left
/// as <see cref="AutomationParityStatus.NotRunOnWindows"/>; populating it is the job
/// of the native Windows FlaUI/UIA3 reference probe, which is out of scope here.
/// </summary>
public sealed record AutomationParityReport(
    string SchemaVersion,
    bool WindowsReferenceRun,
    int PassedOnMac,
    int FailedOnMac,
    int SkippedOnMac,
    IReadOnlyList<AutomationActionParity> Actions)
{
    public const string Schema = "0.1";

    public static AutomationParityReport FromActions(IReadOnlyList<ArtifactActionResult> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var entries = actions
            .Select(action => new AutomationActionParity(
                action.Index,
                action.Type,
                action.Target,
                action.Selector,
                MapMacStatus(action.Status),
                AutomationParityStatus.NotRunOnWindows))
            .ToArray();

        return new AutomationParityReport(
            Schema,
            WindowsReferenceRun: false,
            PassedOnMac: entries.Count(entry => entry.MacStatus == AutomationParityStatus.PassedOnMac),
            FailedOnMac: entries.Count(entry => entry.MacStatus == AutomationParityStatus.FailedOnMac),
            SkippedOnMac: entries.Count(entry => entry.MacStatus == AutomationParityStatus.SkippedOnMac),
            Actions: entries);
    }

    private static AutomationParityStatus MapMacStatus(string status)
    {
        return status switch
        {
            "passed" => AutomationParityStatus.PassedOnMac,
            "failed" => AutomationParityStatus.FailedOnMac,
            "skipped" => AutomationParityStatus.SkippedOnMac,
            "unsupported" => AutomationParityStatus.SkippedOnMac,
            _ => AutomationParityStatus.SkippedOnMac
        };
    }
}
