using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace WinUI3.MacRuntime;

public sealed record InteractionScript(IReadOnlyList<InteractionAction> Actions)
{
    public string SchemaVersion { get; init; } = ArtifactSchemas.InteractionScript;
}

public sealed record InteractionAction(
    string Type,
    string? Target,
    string? Key,
    string? Modifiers,
    string? PageType,
    string? Parameter);

public sealed record InteractionReport(
    string SchemaVersion,
    IReadOnlyList<InteractionStepResult> Steps);

public sealed record InteractionStepResult(
    int Index,
    string Type,
    string Status,
    string? Target,
    string? Message);

public sealed class InteractionScriptRunner
{
    private readonly TypeResolver typeResolver;

    public InteractionScriptRunner(TypeResolver typeResolver)
    {
        this.typeResolver = typeResolver;
    }

    public async Task<InteractionReport> RunFileAsync(Window window, string scriptPath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        var script = JsonSerializer.Deserialize<InteractionScript>(json, JsonDefaults.Options)
            ?? new InteractionScript(Array.Empty<InteractionAction>());
        return Run(window, script);
    }

    public InteractionReport Run(Window window, InteractionScript script)
    {
        var results = new List<InteractionStepResult>();
        for (var index = 0; index < script.Actions.Count; index++)
        {
            var action = script.Actions[index];
            results.Add(RunAction(window, action, index));
            BindingOperations.RefreshTree(window);
        }

        return new InteractionReport(ArtifactSchemas.InteractionReport, results);
    }

    private InteractionStepResult RunAction(Window window, InteractionAction action, int index)
    {
        try
        {
            return action.Type switch
            {
                "click" => Click(window, action, index),
                "focus" => Focus(window, action, index),
                "selectNavigation" => SelectNavigation(window, action, index),
                "navigateFrame" => NavigateFrame(window, action, index),
                "invokeAccelerator" => InvokeAccelerator(window, action, index),
                _ => Failed(index, action, $"Unsupported action type '{action.Type}'.")
            };
        }
        catch (Exception ex)
        {
            return Failed(index, action, ex.Message);
        }
    }

    private static InteractionStepResult Click(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target is not Button button)
        {
            return Failed(index, action, "Target is not a Button.");
        }

        button.PerformClick();
        return Passed(index, action);
    }

    private static InteractionStepResult Focus(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target is not FrameworkElement frameworkElement)
        {
            return Failed(index, action, "Target is not a FrameworkElement.");
        }

        frameworkElement.Focus(FocusState.Programmatic);
        return Passed(index, action);
    }

    private static InteractionStepResult SelectNavigation(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target is not NavigationViewItem item)
        {
            return Failed(index, action, "Target is not a NavigationViewItem.");
        }

        var navigationView = ElementQuery
            .Traverse(window)
            .OfType<NavigationView>()
            .FirstOrDefault(view => view.MenuItems.Contains(item));
        if (navigationView is null)
        {
            return Failed(index, action, "NavigationView parent was not found.");
        }

        navigationView.Select(item);
        return Passed(index, action);
    }

    private InteractionStepResult NavigateFrame(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target is not Frame frame)
        {
            return Failed(index, action, "Target is not a Frame.");
        }

        if (string.IsNullOrWhiteSpace(action.PageType))
        {
            return Failed(index, action, "navigateFrame requires pageType.");
        }

        var pageType = typeResolver.Resolve(action.PageType);
        if (pageType is null)
        {
            return Failed(index, action, $"Page type '{action.PageType}' was not found.");
        }

        frame.Navigate(pageType, action.Parameter);
        return Passed(index, action);
    }

    private static InteractionStepResult InvokeAccelerator(Window window, InteractionAction action, int index)
    {
        var key = Enum.Parse<VirtualKey>(action.Key ?? string.Empty, ignoreCase: true);
        var modifiers = string.IsNullOrWhiteSpace(action.Modifiers)
            ? VirtualKeyModifiers.None
            : Enum.Parse<VirtualKeyModifiers>(action.Modifiers.Replace("|", ",", StringComparison.Ordinal), ignoreCase: true);

        var accelerator = ElementQuery
            .Traverse(window)
            .OfType<Control>()
            .SelectMany(control => control.KeyboardAccelerators)
            .FirstOrDefault(candidate => candidate.Key == key && candidate.Modifiers == modifiers);
        if (accelerator is null)
        {
            return Failed(index, action, "Keyboard accelerator was not found.");
        }

        accelerator.Invoke();
        return Passed(index, action);
    }

    private static object RequireTarget(Window window, InteractionAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            throw new InvalidOperationException("Action target is required.");
        }

        return ElementQuery.FindByName(window, action.Target)
            ?? throw new InvalidOperationException($"Target '{action.Target}' was not found.");
    }

    private static InteractionStepResult Passed(int index, InteractionAction action)
    {
        return new InteractionStepResult(index, action.Type, "passed", action.Target, null);
    }

    private static InteractionStepResult Failed(int index, InteractionAction action, string message)
    {
        return new InteractionStepResult(index, action.Type, "failed", action.Target, message);
    }
}

public sealed class TypeResolver
{
    private readonly IReadOnlyList<Type> types;

    public TypeResolver(IEnumerable<Type> types)
    {
        this.types = types.ToArray();
    }

    public Type? Resolve(string typeName)
    {
        return types.FirstOrDefault(type =>
            type.FullName == typeName ||
            type.Name == typeName);
    }
}
