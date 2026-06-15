using System.Text.Json;
using System.Text.Json.Serialization;
using WinUI3.MacRuntime;

namespace WinUI3.MacRunner.Automation;

public static class NativeWindowsAutomationJson
{
    public static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions(JsonDefaults.Options);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

public sealed record NativeWindowsAutomationProbeOptions(
    string OutputDirectory,
    string WindowTitle,
    IReadOnlyList<string> AppCommand,
    int? AttachProcessId,
    string? CaptureToolPath,
    TimeSpan Timeout)
{
    public static NativeWindowsAutomationProbeOptions ForLaunch(
        string outputDirectory,
        string windowTitle,
        IReadOnlyList<string> appCommand)
    {
        return new NativeWindowsAutomationProbeOptions(
            outputDirectory,
            windowTitle,
            appCommand,
            AttachProcessId: null,
            CaptureToolPath: null,
            Timeout: TimeSpan.FromSeconds(30));
    }
}

public sealed record NativeWindowsAutomationPlan(
    string SchemaVersion,
    string ScenarioName,
    string ScenarioPath,
    string OutputDirectory,
    string NativeAutomationPath,
    string WindowsReferencePath,
    string? WindowsReferencePngPath,
    bool IsNativeWindowsReference,
    string Boundary,
    IReadOnlyList<string> SupportedActionTypes,
    IReadOnlyList<string> UnsupportedActionTypes,
    IReadOnlyList<NativeWindowsAutomationActionPlan> Actions,
    NativeWindowsAutomationCapturePlan? Capture)
{
    public const string Schema = "0.1";
    public const string ReferenceSource = "native-windows-uia3-flaui";

    public static NativeWindowsAutomationPlan Create(
        VisualScenario scenario,
        string scenarioPath,
        NativeWindowsAutomationProbeOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioPath);
        ArgumentNullException.ThrowIfNull(options);

        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        var nativeAutomationPath = Path.Combine(outputDirectory, "native-automation.json");
        var windowsReferencePath = Path.Combine(outputDirectory, "windows-reference.json");
        var captureRequested = scenario.Visual?.Capture == true;
        var windowsReferencePngPath = captureRequested
            ? Path.Combine(outputDirectory, "windows-reference.png")
            : null;

        return new NativeWindowsAutomationPlan(
            Schema,
            scenario.Name,
            Path.GetFullPath(scenarioPath),
            outputDirectory,
            nativeAutomationPath,
            windowsReferencePath,
            windowsReferencePngPath,
            IsNativeWindowsReference: true,
            Boundary: "Optional native Windows UIA/FlaUI reference over a real Windows app; separate from macOS runtime artifacts and visual parity.",
            SupportedActionTypes: NativeWindowsAutomationActionMapper.SupportedActionTypes,
            UnsupportedActionTypes: NativeWindowsAutomationActionMapper.UnsupportedActionTypes,
            Actions: scenario.Automation.Select((action, index) => NativeWindowsAutomationActionMapper.Map(action, index)).ToArray(),
            Capture: captureRequested
                ? NativeWindowsAutomationCapturePlan.Create(scenario, options, scenarioPath, windowsReferencePngPath!, windowsReferencePath)
                : null);
    }
}

public sealed record NativeWindowsAutomationCapturePlan(
    string Tool,
    string? ToolPath,
    string OutputPath,
    string MetadataOutputPath,
    IReadOnlyList<string> Arguments)
{
    public static NativeWindowsAutomationCapturePlan Create(
        VisualScenario scenario,
        NativeWindowsAutomationProbeOptions options,
        string scenarioPath,
        string outputPath,
        string metadataOutputPath)
    {
        var arguments = new List<string>
        {
            "--title",
            options.WindowTitle,
            "--output",
            outputPath,
            "--metadata-output",
            metadataOutputPath,
            "--client-area",
            "--require-title-match",
            "--reference-source",
            "native-winui",
            "--scenario",
            Path.GetFullPath(scenarioPath),
            "--scenario-name",
            scenario.Name,
            "--viewport",
            scenario.Viewport.ToString(),
            "--scale",
            scenario.Scale.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--theme",
            VisualTheme.Normalize(scenario.Theme),
            "--timeout-seconds",
            Math.Ceiling(options.Timeout.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--"
        };
        arguments.AddRange(options.AppCommand);

        return new NativeWindowsAutomationCapturePlan(
            "WindowsWindowCapture",
            options.CaptureToolPath,
            outputPath,
            metadataOutputPath,
            arguments);
    }
}

public enum NativeWindowsAutomationSelectorKind
{
    AutomationId,
    Name,
    Bare
}

public sealed record NativeWindowsAutomationSelector(NativeWindowsAutomationSelectorKind Kind, string Value)
{
    public static NativeWindowsAutomationSelector Parse(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return Bare(string.Empty);
        }

        if (target.StartsWith("automationId=", StringComparison.Ordinal))
        {
            return new NativeWindowsAutomationSelector(
                NativeWindowsAutomationSelectorKind.AutomationId,
                target["automationId=".Length..]);
        }

        if (target.StartsWith("name=", StringComparison.Ordinal))
        {
            return new NativeWindowsAutomationSelector(
                NativeWindowsAutomationSelectorKind.Name,
                target["name=".Length..]);
        }

        return Bare(target);
    }

    public static NativeWindowsAutomationSelector Bare(string value) =>
        new(NativeWindowsAutomationSelectorKind.Bare, value);
}

public enum NativeWindowsAutomationCommandKind
{
    Invoke,
    Focus,
    SetValue,
    Select,
    KeyboardAccelerator,
    WaitForIdle,
    AssertUiaState,
    Unsupported
}

public sealed record NativeWindowsAutomationActionPlan(
    int Index,
    string Type,
    string? Target,
    string? Key,
    string? Parameter,
    NativeWindowsAutomationSelector Selector,
    NativeWindowsAutomationCommandKind CommandKind,
    string? UnsupportedReason);

public static class NativeWindowsAutomationActionMapper
{
    public static readonly IReadOnlyList<string> SupportedActionTypes = new[]
    {
        "click",
        "focus",
        "typeText",
        "selectItem",
        "selectNavigation",
        "invokeAccelerator",
        "waitForIdle",
        "assertAccessibilityState"
    };

    public static readonly IReadOnlyList<string> UnsupportedActionTypes = new[]
    {
        "navigateFrame",
        "assertProperty",
        "openPopup",
        "dismissPopup",
        "invokeMenuItem"
    };

    public static NativeWindowsAutomationActionPlan Map(InteractionAction action, int index)
    {
        ArgumentNullException.ThrowIfNull(action);

        var selector = NativeWindowsAutomationSelector.Parse(action.Target);
        return action.Type switch
        {
            "click" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.Invoke),
            "focus" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.Focus),
            "typeText" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.SetValue),
            "selectItem" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.Select),
            "selectNavigation" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.Select),
            "invokeAccelerator" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.KeyboardAccelerator),
            "waitForIdle" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.WaitForIdle),
            "assertAccessibilityState" => Supported(action, index, selector, NativeWindowsAutomationCommandKind.AssertUiaState),
            "assertProperty" => Unsupported(action, index, selector, "assertProperty is macOS runtime artifact-only and is not a native UIA property assertion."),
            "navigateFrame" => Unsupported(action, index, selector, "navigateFrame is handled by the macOS source-level runtime and has no native UIA action equivalent."),
            "openPopup" => Unsupported(action, index, selector, "openPopup is not mapped until native pointer/menu popup semantics are validated."),
            "dismissPopup" => Unsupported(action, index, selector, "dismissPopup is not mapped until native pointer/menu popup semantics are validated."),
            "invokeMenuItem" => Unsupported(action, index, selector, "invokeMenuItem is not mapped until native menu traversal is validated."),
            _ => Unsupported(action, index, selector, $"Unsupported scenario action type '{action.Type}'.")
        };
    }

    private static NativeWindowsAutomationActionPlan Supported(
        InteractionAction action,
        int index,
        NativeWindowsAutomationSelector selector,
        NativeWindowsAutomationCommandKind commandKind)
    {
        return new NativeWindowsAutomationActionPlan(
            index,
            action.Type,
            action.Target,
            action.Key,
            action.Parameter,
            selector,
            commandKind,
            UnsupportedReason: null);
    }

    private static NativeWindowsAutomationActionPlan Unsupported(
        InteractionAction action,
        int index,
        NativeWindowsAutomationSelector selector,
        string reason)
    {
        return new NativeWindowsAutomationActionPlan(
            index,
            action.Type,
            action.Target,
            action.Key,
            action.Parameter,
            selector,
            NativeWindowsAutomationCommandKind.Unsupported,
            reason);
    }
}

public sealed record NativeWindowsAutomationSummary(int Passed, int Failed, int Skipped);

public sealed record NativeWindowsAutomationArtifacts(string NativeAutomationJson, string WindowsReferenceJson, string? WindowsReferencePng);

public sealed record NativeWindowsAutomationReport(
    string SchemaVersion,
    string ReferenceSource,
    bool IsNativeWindowsReference,
    string Boundary,
    string ScenarioName,
    string ScenarioPath,
    string OutputDirectory,
    NativeWindowsAutomationSummary Summary,
    IReadOnlyList<string> SupportedActionTypes,
    IReadOnlyList<string> UnsupportedActionTypes,
    IReadOnlyList<NativeWindowsAutomationActionResult> Actions,
    NativeWindowsAutomationArtifacts Artifacts)
{
    public const string Schema = "0.1";

    public static NativeWindowsAutomationReport FromResults(
        string scenarioName,
        string scenarioPath,
        string outputDirectory,
        IReadOnlyList<NativeWindowsAutomationActionResult> results,
        string windowsReferencePath,
        string? windowsReferencePngPath)
    {
        return new NativeWindowsAutomationReport(
            Schema,
            NativeWindowsAutomationPlan.ReferenceSource,
            IsNativeWindowsReference: true,
            Boundary: "Native Windows reference evidence only; separate from macOS runtime artifacts and screenshot diff evidence.",
            scenarioName,
            scenarioPath,
            outputDirectory,
            new NativeWindowsAutomationSummary(
                Passed: results.Count(result => result.Status == "passed"),
                Failed: results.Count(result => result.Status == "failed"),
                Skipped: results.Count(result => result.Status == "skipped")),
            NativeWindowsAutomationActionMapper.SupportedActionTypes,
            NativeWindowsAutomationActionMapper.UnsupportedActionTypes,
            results,
            new NativeWindowsAutomationArtifacts(
                NativeAutomationJson: Path.Combine(outputDirectory, "native-automation.json"),
                WindowsReferenceJson: windowsReferencePath,
                WindowsReferencePng: windowsReferencePngPath));
    }
}

public sealed record NativeWindowsAutomationActionResult(
    int Index,
    string Type,
    string? Target,
    string Status,
    string? Selector,
    string? SelectorKind,
    string? Message,
    string? Expected,
    string? Actual,
    IReadOnlyDictionary<string, string?>? Diagnostics)
{
    public static NativeWindowsAutomationActionResult Passed(
        NativeWindowsAutomationActionPlan action,
        string? message = null,
        string? expected = null,
        string? actual = null)
    {
        return From(action, "passed", message, expected, actual, diagnostics: null);
    }

    public static NativeWindowsAutomationActionResult Failed(
        NativeWindowsAutomationActionPlan action,
        string message,
        string? expected = null,
        string? actual = null,
        IReadOnlyDictionary<string, string?>? diagnostics = null)
    {
        return From(action, "failed", message, expected, actual, diagnostics);
    }

    public static NativeWindowsAutomationActionResult Skipped(NativeWindowsAutomationActionPlan action, string message)
    {
        return From(action, "skipped", message, expected: null, actual: null, diagnostics: new Dictionary<string, string?>
        {
            ["unsupportedReason"] = message
        });
    }

    private static NativeWindowsAutomationActionResult From(
        NativeWindowsAutomationActionPlan action,
        string status,
        string? message,
        string? expected,
        string? actual,
        IReadOnlyDictionary<string, string?>? diagnostics)
    {
        return new NativeWindowsAutomationActionResult(
            action.Index,
            action.Type,
            action.Target,
            status,
            action.Target,
            action.Selector.Kind.ToString(),
            message,
            expected,
            actual,
            diagnostics);
    }
}

public sealed record WindowsReferenceCaptureState(
    bool Requested,
    string Status,
    string Tool,
    string? ScreenshotPath,
    string? MetadataPath,
    string? Reason);

public sealed record WindowsReferenceProvenance(
    string SchemaVersion,
    string ReferenceSource,
    string ScenarioName,
    string ScenarioPath,
    WindowsReferenceCaptureState Capture,
    DateTimeOffset CapturedAt)
{
    public const string Schema = "0.1";

    public static WindowsReferenceProvenance Skipped(string scenarioName, string scenarioPath, string reason)
    {
        return new WindowsReferenceProvenance(
            Schema,
            "native-winui",
            scenarioName,
            scenarioPath,
            new WindowsReferenceCaptureState(
                Requested: false,
                Status: "skipped",
                Tool: "WindowsWindowCapture",
                ScreenshotPath: null,
                MetadataPath: null,
                Reason: reason),
            DateTimeOffset.UtcNow);
    }
}
