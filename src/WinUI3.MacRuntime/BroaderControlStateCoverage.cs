namespace WinUI3.MacRuntime;

public sealed record BroaderControlStateCoverageDocument(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Profile,
    string Policy,
    BroaderControlStateCoverageTotals Totals,
    BroaderControlStateModel StateModel,
    BroaderVisualStateManagerExpansion VisualStateManagerExpansion,
    BroaderResourceThemeCoverage ResourceThemeCoverage,
    BroaderAccessibilityPatternExpansion AccessibilityPatternExpansion,
    IReadOnlyList<BroaderControlStateCoverageRow> Controls,
    string Status);

public sealed record BroaderControlStateCoverageTotals(
    int ControlCount,
    IReadOnlyDictionary<string, int> SupportStatusCounts,
    IReadOnlyDictionary<string, int> RendererStatusCounts,
    IReadOnlyDictionary<string, int> AutomationStatusCounts,
    int FullyStateCoveredControlCount,
    int PartialStateCoveredControlCount,
    int PlannedStateCoveredControlCount);

public sealed record BroaderControlStateModel(
    IReadOnlyList<string> States,
    string Policy,
    IReadOnlyList<string> PromotionRequirements);

public sealed record BroaderVisualStateManagerExpansion(
    string Status,
    IReadOnlyList<string> RequiredStateGroups,
    IReadOnlyList<string> RuntimeCoverage,
    IReadOnlyList<string> PlannedCoverage);

public sealed record BroaderResourceThemeCoverage(
    string Status,
    IReadOnlyList<string> RequiredTokens,
    IReadOnlyList<string> RuntimeCoverage,
    IReadOnlyList<string> PlannedCoverage);

public sealed record BroaderAccessibilityPatternExpansion(
    string Status,
    IReadOnlyList<string> RequiredPatterns,
    IReadOnlyList<string> RuntimeCoverage,
    IReadOnlyList<string> PlannedCoverage);

public sealed record BroaderControlStateCoverageRow(
    string Control,
    string Family,
    string SupportStatus,
    string AutomationStatus,
    string RendererStatus,
    IReadOnlyList<string> RequiredStates,
    IReadOnlyList<string> SupportedStates,
    IReadOnlyList<string> PartialStates,
    IReadOnlyList<string> PlannedStates,
    IReadOnlyList<string> VisualStateGroups,
    IReadOnlyList<string> ResourceThemeTokens,
    IReadOnlyList<string> AccessibilityPatterns,
    IReadOnlyList<string> ScenarioCoverage,
    IReadOnlyList<string> KnownGaps,
    string PromotionStatus);

public static class BroaderControlStateCoverageBuilder
{
    public const string DefaultArtifactPath = "docs/visual-parity/broader-control-state-coverage.json";

    private const string Profile = "phase14-broader-control-state-coverage";

    private const string Policy =
        "Broader control/state coverage is a compatibility dashboard contract, not a native-fidelity claim. Controls remain supported, partial, or planned until portable-headless render, AutomationCore, state, resource/theme, and scenario evidence all line up.";

    private static readonly string[] RequiredStates =
    {
        "default",
        "hover",
        "pressed",
        "disabled",
        "focused",
        "selected"
    };

    public static BroaderControlStateCoverageDocument Build()
    {
        var controls = ControlRows()
            .OrderBy(row => row.Family, StringComparer.Ordinal)
            .ThenBy(row => row.Control, StringComparer.Ordinal)
            .ToArray();

        return new BroaderControlStateCoverageDocument(
            ArtifactSchemas.BroaderControlStateCoverage,
            DateTimeOffset.UnixEpoch,
            Profile,
            Policy,
            Totals(controls),
            StateModel(),
            VisualStateManagerExpansion(),
            ResourceThemeCoverage(),
            AccessibilityPatternExpansion(),
            controls,
            controls.Any(row => row.SupportStatus != "supported" || row.PlannedStates.Count > 0)
                ? "tracked-with-gaps"
                : "passed");
    }

    public static BroaderControlStateCoverageDocument Write(string? outputPath = null)
    {
        var document = Build();
        var resolvedOutputPath = Path.GetFullPath(outputPath ?? DefaultArtifactPath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
        File.WriteAllText(resolvedOutputPath, System.Text.Json.JsonSerializer.Serialize(document, JsonDefaults.Options));
        return document;
    }

    private static BroaderControlStateCoverageTotals Totals(IReadOnlyList<BroaderControlStateCoverageRow> controls)
    {
        return new BroaderControlStateCoverageTotals(
            controls.Count,
            CountBy(controls, row => row.SupportStatus),
            CountBy(controls, row => row.RendererStatus),
            CountBy(controls, row => row.AutomationStatus),
            controls.Count(row => row.PlannedStates.Count == 0 && row.PartialStates.Count == 0),
            controls.Count(row => row.PartialStates.Count > 0),
            controls.Count(row => row.PlannedStates.Count > 0));
    }

    private static IReadOnlyDictionary<string, int> CountBy(
        IReadOnlyList<BroaderControlStateCoverageRow> controls,
        Func<BroaderControlStateCoverageRow, string> selector)
    {
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in controls)
        {
            var key = selector(row);
            counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        return counts;
    }

    private static BroaderControlStateModel StateModel()
    {
        return new BroaderControlStateModel(
            RequiredStates,
            "Every Phase 14 control must name default, hover, pressed, disabled, focused, and selected coverage explicitly; missing states stay planned instead of being inferred from default screenshots.",
            new[]
            {
                "portable-headless render tree state flag",
                "AutomationCore pattern or property exposure",
                "scenario fixture or checked-in evidence path",
                "resource/theme token mapping when visual state depends on theme",
                "documented known gaps for partial and planned rows"
            });
    }

    private static BroaderVisualStateManagerExpansion VisualStateManagerExpansion()
    {
        return new BroaderVisualStateManagerExpansion(
            "partial",
            new[]
            {
                "CommonStates",
                "FocusStates",
                "SelectionStates",
                "CheckStates",
                "ValidationStates",
                "InfoBarSeverityStates",
                "DialogOverlayStates",
                "ProgressStates"
            },
            new[]
            {
                "CommonStates: default, disabled, focused",
                "SelectionStates: selected for list-like controls",
                "ProgressStates: determinate progress values"
            },
            new[]
            {
                "hover and pressed visuals across all controls",
                "Flyout/Dialog overlay state transitions",
                "ProgressRing indeterminate animation phase",
                "InfoBar severity icon/resource variants"
            });
    }

    private static BroaderResourceThemeCoverage ResourceThemeCoverage()
    {
        return new BroaderResourceThemeCoverage(
            "partial",
            new[]
            {
                "ThemeResource",
                "SystemControlForegroundBaseHighBrush",
                "SystemControlBackgroundAccentBrush",
                "ControlCornerRadius",
                "FocusVisualPrimaryBrush",
                "InfoBarSeverityIconForeground",
                "ProgressBarIndicatorBrush"
            },
            new[]
            {
                "ThemeResource lookup diagnostics",
                "foreground/background brush fallback",
                "focus visual brush tracking"
            },
            new[]
            {
                "full WinUI resource dictionary parity",
                "InfoBar severity-specific tokens",
                "ComboBox popup token set",
                "Dialog/Flyout overlay tokens"
            });
    }

    private static BroaderAccessibilityPatternExpansion AccessibilityPatternExpansion()
    {
        return new BroaderAccessibilityPatternExpansion(
            "partial",
            new[]
            {
                "InvokePattern",
                "ValuePattern",
                "RangeValuePattern",
                "SelectionPattern",
                "SelectionItemPattern",
                "ExpandCollapsePattern",
                "ScrollPattern",
                "WindowPattern"
            },
            new[]
            {
                "InvokePattern",
                "ValuePattern",
                "RangeValuePattern",
                "SelectionItemPattern",
                "ExpandCollapsePattern"
            },
            new[]
            {
                "SelectionPattern container semantics",
                "WindowPattern dialog modality",
                "ScrollPattern for virtualized list ranges",
                "live-region/event semantics for InfoBar and progress"
            });
    }

    private static IReadOnlyList<BroaderControlStateCoverageRow> ControlRows()
    {
        return new[]
        {
            Row(
                "ComboBox",
                "Selection and pickers",
                "partial",
                "partial",
                "partial",
                supportedStates: new[] { "default", "disabled", "focused", "selected" },
                partialStates: new[] { "pressed" },
                visualStateGroups: new[] { "CommonStates", "FocusStates", "SelectionStates" },
                resourceThemeTokens: new[] { "ThemeResource", "SystemControlForegroundBaseHighBrush", "FocusVisualPrimaryBrush" },
                accessibilityPatterns: new[] { "ExpandCollapsePattern", "SelectionItemPattern", "ValuePattern" },
                scenarioCoverage: new[] { "planned: component-status-pickers-combobox-light" },
                knownGaps: new[] { "popup placement/rendering remains planned", "hover visual is planned" }),
            Row(
                "ListView",
                "Collections and templates",
                "partial",
                "partial",
                "partial",
                supportedStates: new[] { "default", "disabled", "focused", "selected" },
                partialStates: new[] { "hover" },
                visualStateGroups: new[] { "CommonStates", "FocusStates", "SelectionStates" },
                resourceThemeTokens: new[] { "ThemeResource", "SystemControlForegroundBaseHighBrush" },
                accessibilityPatterns: new[] { "SelectionPattern", "SelectionItemPattern", "ScrollPattern" },
                scenarioCoverage: new[] { "planned: component-collections-listview-light" },
                knownGaps: new[] { "virtualization is planned", "pressed item visual is planned" }),
            Row(
                "InfoBar",
                "Status and feedback",
                "partial",
                "partial",
                "partial",
                supportedStates: new[] { "default", "disabled", "focused" },
                partialStates: new[] { "hover", "pressed" },
                visualStateGroups: new[] { "CommonStates", "FocusStates", "InfoBarSeverityStates" },
                resourceThemeTokens: new[] { "ThemeResource", "InfoBarSeverityIconForeground" },
                accessibilityPatterns: new[] { "InvokePattern" },
                scenarioCoverage: new[] { "planned: component-status-pickers-infobar-light" },
                knownGaps: new[] { "severity-specific icon/resource parity is partial", "live-region events are planned" }),
            Row(
                "Flyout",
                "Dialogs and flyouts",
                "planned",
                "planned",
                "planned",
                supportedStates: new[] { "default" },
                partialStates: Array.Empty<string>(),
                visualStateGroups: new[] { "DialogOverlayStates" },
                resourceThemeTokens: new[] { "ThemeResource" },
                accessibilityPatterns: new[] { "WindowPattern" },
                scenarioCoverage: new[] { "planned: component-dialogs-flyouts-flyout-light" },
                knownGaps: new[] { "overlay placement, dismissal, and modality are planned" }),
            Row(
                "ContentDialog",
                "Dialogs and flyouts",
                "planned",
                "planned",
                "planned",
                supportedStates: new[] { "default", "focused" },
                partialStates: Array.Empty<string>(),
                visualStateGroups: new[] { "DialogOverlayStates", "FocusStates" },
                resourceThemeTokens: new[] { "ThemeResource", "ControlCornerRadius" },
                accessibilityPatterns: new[] { "WindowPattern", "InvokePattern" },
                scenarioCoverage: new[] { "planned: component-dialogs-flyouts-dialog-light" },
                knownGaps: new[] { "modal focus trap and button command routing are planned" }),
            Row(
                "Slider",
                "Basic input",
                "partial",
                "partial",
                "partial",
                supportedStates: new[] { "default", "disabled", "focused" },
                partialStates: new[] { "pressed" },
                visualStateGroups: new[] { "CommonStates", "FocusStates" },
                resourceThemeTokens: new[] { "ThemeResource", "SystemControlBackgroundAccentBrush", "FocusVisualPrimaryBrush" },
                accessibilityPatterns: new[] { "RangeValuePattern" },
                scenarioCoverage: new[] { "planned: component-basic-input-slider-light" },
                knownGaps: new[] { "drag interaction and tick/snapping visuals are partial", "hover visual is planned" }),
            Row(
                "ProgressRing",
                "Status and feedback",
                "planned",
                "planned",
                "partial",
                supportedStates: new[] { "default", "disabled" },
                partialStates: Array.Empty<string>(),
                visualStateGroups: new[] { "ProgressStates" },
                resourceThemeTokens: new[] { "ThemeResource" },
                accessibilityPatterns: new[] { "RangeValuePattern" },
                scenarioCoverage: new[] { "planned: component-status-pickers-progressring-light" },
                knownGaps: new[] { "indeterminate animation phase is planned", "live progress eventing is planned" }),
            Row(
                "ProgressBar",
                "Status and feedback",
                "supported",
                "partial",
                "supported",
                supportedStates: new[] { "default", "disabled", "focused", "selected", "pressed", "hover" },
                partialStates: Array.Empty<string>(),
                visualStateGroups: new[] { "CommonStates", "ProgressStates" },
                resourceThemeTokens: new[] { "ThemeResource", "ProgressBarIndicatorBrush" },
                accessibilityPatterns: new[] { "RangeValuePattern" },
                scenarioCoverage: new[] { "planned: component-status-pickers-progressbar-light" },
                knownGaps: new[] { "native visual fidelity still requires Windows-reference comparison" })
        };
    }

    private static BroaderControlStateCoverageRow Row(
        string control,
        string family,
        string supportStatus,
        string automationStatus,
        string rendererStatus,
        IReadOnlyList<string> supportedStates,
        IReadOnlyList<string> partialStates,
        IReadOnlyList<string> visualStateGroups,
        IReadOnlyList<string> resourceThemeTokens,
        IReadOnlyList<string> accessibilityPatterns,
        IReadOnlyList<string> scenarioCoverage,
        IReadOnlyList<string> knownGaps)
    {
        var plannedStates = RequiredStates
            .Except(supportedStates, StringComparer.Ordinal)
            .Except(partialStates, StringComparer.Ordinal)
            .ToArray();

        return new BroaderControlStateCoverageRow(
            control,
            family,
            supportStatus,
            automationStatus,
            rendererStatus,
            RequiredStates,
            supportedStates.OrderBy(StateOrder).ToArray(),
            partialStates.OrderBy(StateOrder).ToArray(),
            plannedStates.OrderBy(StateOrder).ToArray(),
            visualStateGroups,
            resourceThemeTokens,
            accessibilityPatterns,
            scenarioCoverage,
            knownGaps,
            PromotionStatus(supportStatus, plannedStates, partialStates));
    }

    private static int StateOrder(string state)
    {
        var index = Array.IndexOf(RequiredStates, state);
        return index >= 0 ? index : RequiredStates.Length;
    }

    private static string PromotionStatus(
        string supportStatus,
        IReadOnlyCollection<string> plannedStates,
        IReadOnlyCollection<string> partialStates)
    {
        if (supportStatus == "planned")
        {
            return "planned-control";
        }

        if (plannedStates.Count > 0 || partialStates.Count > 0)
        {
            return "not-production-ready-state-gaps";
        }

        return "coverage-dashboard-ready-reference-required";
    }
}
