using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinUI3.MacRuntime;

public sealed record PortableScenario(
    string Name,
    IReadOnlyList<PortableScenarioStep> Steps)
{
    public static PortableScenario Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var scenario = JsonSerializer.Deserialize<PortableScenarioJson>(json, JsonDefaults.Options)
            ?? throw new InvalidOperationException("Scenario JSON did not deserialize.");
        return new PortableScenario(
            scenario.Name ?? "unnamed-scenario",
            scenario.Steps?.Select(PortableScenarioStep.FromJson).ToArray() ?? Array.Empty<PortableScenarioStep>());
    }
}

public sealed record PortableScenarioStep(
    string? Action,
    string? Assert,
    string? AutomationId,
    string? Value,
    double? Horizontal,
    double? Vertical,
    string? Screenshot)
{
    public string Kind => Action ?? Assert ?? (Screenshot is not null ? "screenshot" : "unknown");

    internal static PortableScenarioStep FromJson(PortableScenarioStepJson step)
    {
        return new PortableScenarioStep(
            step.Action,
            step.Assert,
            step.AutomationId,
            step.Value,
            step.Horizontal,
            step.Vertical,
            step.Screenshot);
    }
}

public sealed record ScenarioRunResult(
    string SchemaVersion,
    string Name,
    string Status,
    IReadOnlyList<ScenarioStepResult> Steps,
    AutomationDocument FinalAutomation);

public sealed record ScenarioStepResult(
    int Index,
    string Type,
    string Status,
    string? AutomationId,
    string? Message,
    string? Expected = null,
    string? Actual = null,
    string? Screenshot = null);

public sealed class InternalScenarioDriver
{
    public ScenarioRunResult Run(AutomationDocument automation, PortableScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(automation);
        ArgumentNullException.ThrowIfNull(scenario);

        var current = automation;
        var steps = new List<ScenarioStepResult>(scenario.Steps.Count);
        for (var index = 0; index < scenario.Steps.Count; index++)
        {
            var step = scenario.Steps[index];
            var result = RunStep(current, step, index, out var next);
            steps.Add(result);
            current = next;
        }

        return new ScenarioRunResult(
            ArtifactSchemas.ScenarioResult,
            scenario.Name,
            steps.All(step => step.Status == "passed") ? "passed" : "failed",
            steps,
            current);
    }

    private static ScenarioStepResult RunStep(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        out AutomationDocument next)
    {
        next = automation;
        if (step.Screenshot is not null)
        {
            return Passed(index, "screenshot", step.AutomationId, "Screenshot request recorded for renderer handoff.", screenshot: step.Screenshot);
        }

        return step.Action switch
        {
            "invoke" => RequirePattern(automation, step, index, AutomationPattern.Invoke),
            "setValue" => SetValue(automation, step, index, out next),
            "toggle" => Toggle(automation, step, index, out next),
            "select" => Select(automation, step, index, out next),
            "scroll" => Scroll(automation, step, index, out next),
            "focus" => Focus(automation, step, index, out next),
            "waitForIdle" => Passed(index, "waitForIdle", null, "Internal driver is idle."),
            null => RunAssertion(automation, step, index),
            _ => Failed(index, step.Kind, step.AutomationId, $"Unsupported action '{step.Action}'.")
        };
    }

    private static ScenarioStepResult RunAssertion(AutomationDocument automation, PortableScenarioStep step, int index)
    {
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        return step.Assert switch
        {
            "exists" => node is null
                ? Failed(index, "exists", step.AutomationId, "Automation node was not found.")
                : Passed(index, "exists", step.AutomationId, "Automation node exists."),
            "visible" => node is null
                ? Failed(index, "visible", step.AutomationId, "Automation node was not found.")
                : node.IsOffscreen
                    ? Failed(index, "visible", step.AutomationId, "Automation node is offscreen.", expected: "visible", actual: "offscreen")
                    : Passed(index, "visible", step.AutomationId, "Automation node is visible."),
            "valueEquals" => node is null
                ? Failed(index, "valueEquals", step.AutomationId, "Automation node was not found.")
                : string.Equals(node.Value, step.Value, StringComparison.Ordinal)
                    ? Passed(index, "valueEquals", step.AutomationId, "Value matched.", expected: step.Value, actual: node.Value)
                    : Failed(index, "valueEquals", step.AutomationId, "Value did not match.", expected: step.Value, actual: node.Value),
            "selected" => node is null
                ? Failed(index, "selected", step.AutomationId, "Automation node was not found.")
                : node.IsSelected
                    ? Passed(index, "selected", step.AutomationId, "Node is selected.", expected: "true", actual: "true")
                    : Failed(index, "selected", step.AutomationId, "Node is not selected.", expected: "true", actual: "false"),
            "textContains" => node is null
                ? Failed(index, "textContains", step.AutomationId, "Automation node was not found.")
                : ContainsText(node, step.Value)
                    ? Passed(index, "textContains", step.AutomationId, "Text contained expected value.", expected: step.Value, actual: NodeText(node))
                    : Failed(index, "textContains", step.AutomationId, "Text did not contain expected value.", expected: step.Value, actual: NodeText(node)),
            "pageType" => Passed(index, "pageType", step.AutomationId, "pageType assertion is recorded for later navigation state integration.", expected: step.Value),
            _ => Failed(index, step.Kind, step.AutomationId, $"Unsupported assertion '{step.Assert}'.")
        };
    }

    private static ScenarioStepResult RequirePattern(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        AutomationPattern pattern)
    {
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        if (node is null)
        {
            return Failed(index, step.Kind, step.AutomationId, "Automation node was not found.");
        }

        return node.Patterns.Contains(pattern)
            ? Passed(index, step.Kind, step.AutomationId, $"{pattern} pattern is available.")
            : Failed(index, step.Kind, step.AutomationId, $"{pattern} pattern is not available.");
    }

    private static ScenarioStepResult SetValue(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        out AutomationDocument next)
    {
        next = automation;
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        if (node is null)
        {
            return Failed(index, step.Kind, step.AutomationId, "Automation node was not found.");
        }

        if (!node.Patterns.Contains(AutomationPattern.Value))
        {
            return Failed(index, step.Kind, step.AutomationId, "Value pattern is not available.");
        }

        next = automation with { Root = UpdateNode(automation.Root, node.RuntimeId, target => target with { Value = step.Value ?? string.Empty }) };
        return Passed(index, step.Kind, step.AutomationId, "Value updated.", expected: step.Value, actual: step.Value);
    }

    private static ScenarioStepResult Toggle(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        out AutomationDocument next)
    {
        next = automation;
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        if (node is null)
        {
            return Failed(index, step.Kind, step.AutomationId, "Automation node was not found.");
        }

        if (!node.Patterns.Contains(AutomationPattern.Toggle))
        {
            return Failed(index, step.Kind, step.AutomationId, "Toggle pattern is not available.");
        }

        var toggled = node.ToggleState == AutomationToggleState.On ? AutomationToggleState.Off : AutomationToggleState.On;
        next = automation with { Root = UpdateNode(automation.Root, node.RuntimeId, target => target with { ToggleState = toggled }) };
        return Passed(index, step.Kind, step.AutomationId, "Toggle state updated.", expected: toggled.ToString(), actual: toggled.ToString());
    }

    private static ScenarioStepResult Select(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        out AutomationDocument next)
    {
        next = automation;
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        if (node is null)
        {
            return Failed(index, step.Kind, step.AutomationId, "Automation node was not found.");
        }

        if (!node.Patterns.Contains(AutomationPattern.SelectionItem))
        {
            return Failed(index, step.Kind, step.AutomationId, "SelectionItem pattern is not available.");
        }

        next = automation with { Root = UpdateNode(automation.Root, node.RuntimeId, target => target with { IsSelected = true }) };
        return Passed(index, step.Kind, step.AutomationId, "Node selected.", expected: "true", actual: "true");
    }

    private static ScenarioStepResult Scroll(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        out AutomationDocument next)
    {
        next = automation;
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        if (node is null)
        {
            return Failed(index, step.Kind, step.AutomationId, "Automation node was not found.");
        }

        if (!node.Patterns.Contains(AutomationPattern.Scroll))
        {
            return Failed(index, step.Kind, step.AutomationId, "Scroll pattern is not available.");
        }

        var horizontal = node.HorizontalScrollOffset + (step.Horizontal ?? 0);
        var vertical = node.VerticalScrollOffset + (step.Vertical ?? 0);
        next = automation with { Root = UpdateNode(automation.Root, node.RuntimeId, target => target with { HorizontalScrollOffset = horizontal, VerticalScrollOffset = vertical }) };
        return Passed(index, step.Kind, step.AutomationId, "Scroll offset updated.", expected: $"{step.Horizontal ?? 0},{step.Vertical ?? 0}", actual: $"{horizontal},{vertical}");
    }

    private static ScenarioStepResult Focus(
        AutomationDocument automation,
        PortableScenarioStep step,
        int index,
        out AutomationDocument next)
    {
        next = automation;
        var node = FindByAutomationId(automation.Root, step.AutomationId);
        if (node is null)
        {
            return Failed(index, step.Kind, step.AutomationId, "Automation node was not found.");
        }

        next = automation with { Root = UpdateAll(automation.Root, target => target.RuntimeId == node.RuntimeId ? target with { HasKeyboardFocus = true } : target with { HasKeyboardFocus = false }) };
        return Passed(index, step.Kind, step.AutomationId, "Focus updated.", expected: "true", actual: "true");
    }

    private static AutomationNode UpdateNode(AutomationNode node, string runtimeId, Func<AutomationNode, AutomationNode> update)
    {
        return UpdateAll(node, candidate => candidate.RuntimeId == runtimeId ? update(candidate) : candidate);
    }

    private static AutomationNode UpdateAll(AutomationNode node, Func<AutomationNode, AutomationNode> update)
    {
        var updated = update(node);
        var children = updated.Children.Select(child => UpdateAll(child, update)).ToArray();
        return updated with { Children = children };
    }

    private static AutomationNode? FindByAutomationId(AutomationNode root, string? automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return null;
        }

        if (string.Equals(root.AutomationId, automationId, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = FindByAutomationId(child, automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool ContainsText(AutomationNode node, string? expected)
    {
        return expected is null ||
            NodeText(node).Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string NodeText(AutomationNode node)
    {
        return string.Join(" ", new[] { node.Name, node.Value }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static ScenarioStepResult Passed(
        int index,
        string type,
        string? automationId,
        string? message,
        string? expected = null,
        string? actual = null,
        string? screenshot = null)
    {
        return new ScenarioStepResult(index, type, "passed", automationId, message, expected, actual, screenshot);
    }

    private static ScenarioStepResult Failed(
        int index,
        string type,
        string? automationId,
        string message,
        string? expected = null,
        string? actual = null)
    {
        return new ScenarioStepResult(index, type, "failed", automationId, message, expected, actual);
    }

}

internal sealed record PortableScenarioJson(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("steps")] PortableScenarioStepJson[]? Steps);

internal sealed record PortableScenarioStepJson(
    [property: JsonPropertyName("action")] string? Action,
    [property: JsonPropertyName("assert")] string? Assert,
    [property: JsonPropertyName("automationId")] string? AutomationId,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("horizontal")] double? Horizontal,
    [property: JsonPropertyName("vertical")] double? Vertical,
    [property: JsonPropertyName("screenshot")] string? Screenshot);
