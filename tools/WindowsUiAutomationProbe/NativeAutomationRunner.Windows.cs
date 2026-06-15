#if NATIVE_WINDOWS_UIA
using System.Diagnostics;
using System.Reflection;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using WinUI3.MacRunner.Automation;

namespace WindowsUiAutomationProbe;

internal static class NativeAutomationRunner
{
    public static async Task<IReadOnlyList<NativeWindowsAutomationActionResult>> RunAsync(
        NativeWindowsAutomationPlan plan,
        NativeWindowsAutomationProbeOptions options)
    {
        using var automation = new UIA3Automation();
        using var application = AttachOrLaunch(options);
        var window = WaitForMainWindow(application, automation, options.Timeout, options.WindowTitle);
        var executor = new NativeFlaUIActionExecutor(window);
        var results = new List<NativeWindowsAutomationActionResult>(plan.Actions.Count);
        foreach (var action in plan.Actions)
        {
            results.Add(executor.Execute(action));
            await Task.Delay(50);
        }

        return results;
    }

    private static Application AttachOrLaunch(NativeWindowsAutomationProbeOptions options)
    {
        if (options.AttachProcessId is int pid)
        {
            return Application.Attach(pid);
        }

        if (options.AppCommand.Count == 0)
        {
            throw new ArgumentException("Missing native app command after '--'. Provide an app command or --attach-pid.");
        }

        var startInfo = new ProcessStartInfo(options.AppCommand[0])
        {
            UseShellExecute = false,
            WorkingDirectory = File.Exists(options.AppCommand[0])
                ? Path.GetDirectoryName(Path.GetFullPath(options.AppCommand[0]))
                : Environment.CurrentDirectory
        };
        foreach (var argument in options.AppCommand.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Could not start native Windows app command.");
        return Application.Attach(process.Id);
    }

    private static Window WaitForMainWindow(
        Application application,
        UIA3Automation automation,
        TimeSpan timeout,
        string expectedTitle)
    {
        var stopAt = DateTimeOffset.UtcNow + timeout;
        Window? fallback = null;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var window = application.GetMainWindow(automation, TimeSpan.FromSeconds(1));
            if (window is not null)
            {
                fallback ??= window;
                if (string.IsNullOrWhiteSpace(expectedTitle) ||
                    window.Title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
                {
                    return window;
                }
            }

            Thread.Sleep(250);
        }

        return fallback ?? throw new TimeoutException($"Could not find a native window containing title '{expectedTitle}'.");
    }
}

internal sealed class NativeFlaUIActionExecutor
{
    private readonly Window window;

    public NativeFlaUIActionExecutor(Window window)
    {
        this.window = window;
    }

    public NativeWindowsAutomationActionResult Execute(NativeWindowsAutomationActionPlan action)
    {
        if (action.CommandKind == NativeWindowsAutomationCommandKind.Unsupported)
        {
            return NativeWindowsAutomationActionResult.Skipped(action, action.UnsupportedReason ?? "Action is not supported by the native Windows probe.");
        }

        try
        {
            return action.CommandKind switch
            {
                NativeWindowsAutomationCommandKind.Invoke => Invoke(action),
                NativeWindowsAutomationCommandKind.Focus => Focus(action),
                NativeWindowsAutomationCommandKind.SetValue => SetValue(action),
                NativeWindowsAutomationCommandKind.Select => Select(action),
                NativeWindowsAutomationCommandKind.KeyboardAccelerator => NativeWindowsAutomationActionResult.Skipped(action, "Keyboard accelerator injection is not enabled in the native Windows probe yet."),
                NativeWindowsAutomationCommandKind.WaitForIdle => WaitForIdle(action),
                NativeWindowsAutomationCommandKind.AssertUiaState => AssertUiaState(action),
                _ => NativeWindowsAutomationActionResult.Skipped(action, $"Command kind '{action.CommandKind}' is not supported.")
            };
        }
        catch (Exception ex)
        {
            return NativeWindowsAutomationActionResult.Failed(
                action,
                ex.Message,
                diagnostics: new Dictionary<string, string?>
                {
                    ["exceptionType"] = ex.GetType().FullName
                });
        }
    }

    private NativeWindowsAutomationActionResult Invoke(NativeWindowsAutomationActionPlan action)
    {
        var element = RequireElement(action);
        if (TryInvokePattern(element, "Invoke", "Invoke"))
        {
            return NativeWindowsAutomationActionResult.Passed(action, "Invoked native UIA element.");
        }

        return NativeWindowsAutomationActionResult.Failed(action, "Element does not expose InvokePattern.");
    }

    private NativeWindowsAutomationActionResult Focus(NativeWindowsAutomationActionPlan action)
    {
        RequireElement(action).Focus();
        return NativeWindowsAutomationActionResult.Passed(action, "Focused native UIA element.");
    }

    private NativeWindowsAutomationActionResult SetValue(NativeWindowsAutomationActionPlan action)
    {
        var element = RequireElement(action);
        var expected = action.Parameter ?? string.Empty;
        if (TrySetValuePattern(element, expected))
        {
            return NativeWindowsAutomationActionResult.Passed(action, "Set native UIA value.", expected, expected);
        }

        return NativeWindowsAutomationActionResult.Failed(action, "Element does not expose ValuePattern.", expected: expected);
    }

    private NativeWindowsAutomationActionResult Select(NativeWindowsAutomationActionPlan action)
    {
        var element = RequireElement(action);
        if (TryInvokePattern(element, "SelectionItem", "Select") ||
            TryInvokePattern(element, "Invoke", "Invoke"))
        {
            return NativeWindowsAutomationActionResult.Passed(action, "Selected native UIA element.");
        }

        return NativeWindowsAutomationActionResult.Failed(action, "Element does not expose SelectionItemPattern or InvokePattern.");
    }

    private static NativeWindowsAutomationActionResult WaitForIdle(NativeWindowsAutomationActionPlan action)
    {
        Thread.Sleep(250);
        return NativeWindowsAutomationActionResult.Passed(action, "Waited for native UIA idle settle interval.");
    }

    private NativeWindowsAutomationActionResult AssertUiaState(NativeWindowsAutomationActionPlan action)
    {
        var element = RequireElement(action);
        var expected = action.Parameter ?? string.Empty;
        var actual = ReadState(element, action.Key);
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            ? NativeWindowsAutomationActionResult.Passed(action, "Native UIA state matched.", expected, actual)
            : NativeWindowsAutomationActionResult.Failed(action, "Native UIA state did not match.", expected, actual);
    }

    private AutomationElement RequireElement(NativeWindowsAutomationActionPlan action)
    {
        var element = FindElement(action.Selector);
        return element ?? throw new InvalidOperationException($"Could not find native UIA element for selector '{action.Target}'.");
    }

    private AutomationElement? FindElement(NativeWindowsAutomationSelector selector)
    {
        if (string.IsNullOrWhiteSpace(selector.Value))
        {
            return null;
        }

        return selector.Kind switch
        {
            NativeWindowsAutomationSelectorKind.AutomationId => window.FindFirstDescendant(cf => cf.ByAutomationId(selector.Value)),
            NativeWindowsAutomationSelectorKind.Name => window.FindFirstDescendant(cf => cf.ByName(selector.Value)),
            _ => window.FindFirstDescendant(cf => cf.ByAutomationId(selector.Value)) ??
                 window.FindFirstDescendant(cf => cf.ByName(selector.Value))
        };
    }

    private static string? ReadState(AutomationElement element, string? key)
    {
        return key switch
        {
            "enabled" => ReadFlaUIProperty(element.Properties, "IsEnabled"),
            "focused" => ReadFlaUIProperty(element.Properties, "HasKeyboardFocus"),
            "focusable" => ReadFlaUIProperty(element.Properties, "IsKeyboardFocusable"),
            "selected" => ReadPatternProperty(element, "SelectionItem", "IsSelected"),
            "checked" => ReadPatternProperty(element, "Toggle", "ToggleState"),
            "expanded" => ReadPatternProperty(element, "ExpandCollapse", "ExpandCollapseState"),
            "value" => ReadPatternProperty(element, "Value", "Value"),
            "name" => ReadFlaUIProperty(element.Properties, "Name"),
            "automationId" => ReadFlaUIProperty(element.Properties, "AutomationId"),
            "role" or "controlType" => ReadFlaUIProperty(element.Properties, "ControlType"),
            _ => null
        };
    }

    private static bool TryInvokePattern(AutomationElement element, string patternName, string methodName)
    {
        var pattern = ReadPattern(element, patternName);
        var method = pattern?.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null)
        {
            return false;
        }

        method.Invoke(pattern, Array.Empty<object>());
        return true;
    }

    private static bool TrySetValuePattern(AutomationElement element, string value)
    {
        var pattern = ReadPattern(element, "Value");
        var method = pattern?.GetType().GetMethod("SetValue", BindingFlags.Public | BindingFlags.Instance);
        if (method is null)
        {
            return false;
        }

        method.Invoke(pattern, new object[] { value });
        return true;
    }

    private static string? ReadPatternProperty(AutomationElement element, string patternName, string propertyName)
    {
        var pattern = ReadPattern(element, patternName);
        return pattern is null ? null : ReadFlaUIProperty(pattern, propertyName);
    }

    private static object? ReadPattern(AutomationElement element, string patternName)
    {
        var patterns = element.Patterns;
        var patternAccessor = patterns.GetType().GetProperty(patternName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(patterns);
        return patternAccessor?.GetType().GetProperty("PatternOrDefault", BindingFlags.Public | BindingFlags.Instance)?.GetValue(patternAccessor);
    }

    private static string? ReadFlaUIProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(source);
        if (property is null)
        {
            return null;
        }

        var valueOrDefault = property.GetType().GetProperty("ValueOrDefault", BindingFlags.Public | BindingFlags.Instance)?.GetValue(property);
        return valueOrDefault?.ToString();
    }
}
#endif
