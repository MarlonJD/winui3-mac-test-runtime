using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
    string? Message,
    string? Expected = null,
    string? Actual = null,
    string? Selector = null,
    string? SelectorKind = null,
    string? TargetType = null,
    IReadOnlyDictionary<string, string?>? ObservedState = null,
    IReadOnlyDictionary<string, string?>? BeforeState = null,
    IReadOnlyDictionary<string, string?>? AfterState = null);

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
                "typeText" => TypeText(window, action, index),
                "selectItem" => SelectItem(window, action, index),
                "selectNavigation" => SelectNavigation(window, action, index),
                "navigateFrame" => NavigateFrame(window, action, index),
                "invokeAccelerator" => InvokeAccelerator(window, action, index),
                "openPopup" => OpenPopup(window, action, index),
                "dismissPopup" => DismissPopup(window, action, index),
                "invokeMenuItem" => InvokeMenuItem(window, action, index),
                "waitForIdle" => WaitForIdle(window, action, index),
                "assertProperty" => AssertProperty(window, action, index),
                "assertAccessibilityState" => AssertAccessibilityState(window, action, index),
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
        if (target.Element is not Button button)
        {
            return Failed(index, action, "Target is not a Button.", resolution: target);
        }

        var beforeState = BuildObservedState(button);
        button.PerformClick();
        var afterState = BuildObservedState(button);
        return Passed(index, action, resolution: target, beforeState: beforeState, afterState: afterState);
    }

    private static InteractionStepResult Focus(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target.Element is not FrameworkElement frameworkElement)
        {
            return Failed(index, action, "Target is not a FrameworkElement.", resolution: target);
        }

        var beforeState = BuildObservedState(frameworkElement);
        frameworkElement.Focus(FocusState.Programmatic);
        var afterState = BuildObservedState(frameworkElement);
        return Passed(index, action, resolution: target, beforeState: beforeState, afterState: afterState);
    }

    private static InteractionStepResult TypeText(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target.Element is not TextBox textBox)
        {
            return Failed(index, action, "Target is not a TextBox.", resolution: target);
        }

        var beforeState = BuildObservedState(textBox);
        textBox.Text = action.Parameter ?? string.Empty;
        BindingOperations.UpdateSource(textBox, nameof(TextBox.Text));
        var afterState = BuildObservedState(textBox);
        return Passed(index, action, expected: action.Parameter ?? string.Empty, actual: textBox.Text, resolution: target, beforeState: beforeState, afterState: afterState);
    }

    private static InteractionStepResult SelectItem(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target.Element is ComboBox comboBox)
        {
            var selectedIndex = FindItemIndex(comboBox.Items, action.Parameter);
            if (selectedIndex < 0)
            {
                return Failed(index, action, $"Item '{action.Parameter}' was not found.", resolution: target);
            }

            var beforeState = BuildObservedState(comboBox);
            comboBox.SelectedIndex = selectedIndex;
            var afterState = BuildObservedState(comboBox);
            return Passed(index, action, expected: action.Parameter, actual: comboBox.SelectedItem?.ToString(), resolution: target, beforeState: beforeState, afterState: afterState);
        }

        if (target.Element is ListView listView)
        {
            var selectedIndex = FindItemIndex(listView.Items, action.Parameter);
            if (selectedIndex < 0)
            {
                return Failed(index, action, $"Item '{action.Parameter}' was not found.", resolution: target);
            }

            var beforeState = BuildObservedState(listView);
            listView.SelectedIndex = selectedIndex;
            var afterState = BuildObservedState(listView);
            return Passed(index, action, expected: action.Parameter, actual: listView.SelectedItem?.ToString(), resolution: target, beforeState: beforeState, afterState: afterState);
        }

        return Failed(index, action, "Target is not a selectable item control.", resolution: target);
    }

    private static InteractionStepResult SelectNavigation(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target.Element is not NavigationViewItem item)
        {
            return Failed(index, action, "Target is not a NavigationViewItem.", resolution: target);
        }

        var navigationView = ElementQuery
            .Traverse(window)
            .OfType<NavigationView>()
            .FirstOrDefault(view => view.MenuItems.Contains(item));
        if (navigationView is null)
        {
            return Failed(index, action, "NavigationView parent was not found.", resolution: target);
        }

        var beforeState = BuildObservedState(item);
        navigationView.Select(item);
        var afterState = BuildObservedState(item);
        return Passed(index, action, resolution: target, beforeState: beforeState, afterState: afterState);
    }

    private InteractionStepResult NavigateFrame(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target.Element is not Frame frame)
        {
            return Failed(index, action, "Target is not a Frame.", resolution: target);
        }

        if (string.IsNullOrWhiteSpace(action.PageType))
        {
            return Failed(index, action, "navigateFrame requires pageType.", resolution: target);
        }

        var pageType = typeResolver.Resolve(action.PageType);
        if (pageType is null)
        {
            return Failed(index, action, $"Page type '{action.PageType}' was not found.", resolution: target);
        }

        var beforeState = BuildObservedState(frame);
        frame.Navigate(pageType, action.Parameter);
        var afterState = BuildObservedState(frame);
        return Passed(index, action, resolution: target, beforeState: beforeState, afterState: afterState);
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

    private static InteractionStepResult OpenPopup(Window window, InteractionAction action, int index)
    {
        var popup = RequirePopup(window, action);
        var beforeState = BuildObservedState(popup.Element);
        SetPopupOpenState(popup.Element!, isOpen: true);
        var afterState = BuildObservedState(popup.Element);
        return Passed(index, action, $"Opened {SimpleType(popup.Element!)}.", expected: "True", actual: GetPopupOpenState(popup.Element!).ToString(), resolution: popup, beforeState: beforeState, afterState: afterState);
    }

    private static InteractionStepResult DismissPopup(Window window, InteractionAction action, int index)
    {
        var popup = RequirePopup(window, action);
        var beforeState = BuildObservedState(popup.Element);
        SetPopupOpenState(popup.Element!, isOpen: false);
        var afterState = BuildObservedState(popup.Element);
        return Passed(index, action, $"Dismissed {SimpleType(popup.Element!)}.", expected: "False", actual: GetPopupOpenState(popup.Element!).ToString(), resolution: popup, beforeState: beforeState, afterState: afterState);
    }

    private static InteractionStepResult InvokeMenuItem(Window window, InteractionAction action, int index)
    {
        var popup = RequirePopup(window, action);
        var expected = action.Parameter ?? string.Empty;
        if (popup.Element is MenuFlyout menuFlyout)
        {
            var item = menuFlyout.Items.OfType<MenuFlyoutItem>().FirstOrDefault(candidate =>
                string.Equals(candidate.Text, expected, StringComparison.Ordinal));
            if (item is null)
            {
                return Failed(index, action, $"Menu item '{expected}' was not found.", resolution: popup);
            }

            var beforeState = BuildObservedState(menuFlyout);
            menuFlyout.IsOpen = true;
            menuFlyout.InvokedItem = item.Text;
            item.PerformClick();
            var afterState = BuildObservedState(menuFlyout);
            return Passed(index, action, $"Invoked menu item '{item.Text}'.", expected: expected, actual: menuFlyout.InvokedItem, resolution: popup, beforeState: beforeState, afterState: afterState);
        }

        if (popup.Element is CommandBarFlyout commandBarFlyout)
        {
            var command = commandBarFlyout.PrimaryCommands
                .Concat(commandBarFlyout.SecondaryCommands)
                .OfType<AppBarButton>()
                .FirstOrDefault(candidate => string.Equals(candidate.Label, expected, StringComparison.Ordinal));
            if (command is null)
            {
                return Failed(index, action, $"Command '{expected}' was not found.", resolution: popup);
            }

            var beforeState = BuildObservedState(commandBarFlyout);
            commandBarFlyout.IsOpen = true;
            commandBarFlyout.InvokedCommand = command.Label;
            command.PerformClick();
            var afterState = BuildObservedState(commandBarFlyout);
            return Passed(index, action, $"Invoked command '{command.Label}'.", expected: expected, actual: commandBarFlyout.InvokedCommand, resolution: popup, beforeState: beforeState, afterState: afterState);
        }

        return Failed(index, action, $"Target popup '{SimpleType(popup.Element!)}' does not expose invokable menu items.", resolution: popup);
    }

    private static InteractionStepResult WaitForIdle(Window window, InteractionAction action, int index)
    {
        BindingOperations.RefreshTree(window);
        return Passed(index, action, "Idle tree refreshed.");
    }

    private static InteractionStepResult AssertProperty(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (string.IsNullOrWhiteSpace(action.Key))
        {
            return Failed(index, action, "assertProperty requires key.", resolution: target);
        }

        var property = target.Element!.GetType().GetProperty(action.Key);
        if (property is null || !property.CanRead)
        {
            return Failed(index, action, $"Property '{action.Key}' was not found.", resolution: target);
        }

        var actual = property.GetValue(target.Element)?.ToString() ?? string.Empty;
        var expected = action.Parameter ?? string.Empty;
        return ValuesEqual(actual, expected)
            ? Passed(index, action, expected: expected, actual: actual, resolution: target)
            : Failed(index, action, $"Expected '{expected}' but found '{actual}'.", expected, actual, resolution: target);
    }

    private static InteractionStepResult AssertAccessibilityState(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (string.IsNullOrWhiteSpace(action.Key))
        {
            return Failed(index, action, "assertAccessibilityState requires key.", resolution: target);
        }

        var accessibility = AccessibilityTreeBuilder.Build(UiTreeBuilder.Build(window));
        var node = FindAccessibilityNode(accessibility.Root, target);
        if (node is null)
        {
            return Failed(index, action, $"Accessibility node '{action.Target}' was not found.", resolution: target);
        }

        var observedState = BuildAccessibilityState(node);
        var actual = ReadAccessibilityValue(node, action.Key) ?? string.Empty;
        var expected = action.Parameter ?? string.Empty;
        return ValuesEqual(actual, expected)
            ? Passed(index, action, expected: expected, actual: actual, resolution: target, observedState: observedState)
            : Failed(index, action, $"Expected accessibility {action.Key} '{expected}' but found '{actual}'.", expected, actual, resolution: target, observedState: observedState);
    }

    private static bool ValuesEqual(string actual, string expected)
    {
        if (bool.TryParse(actual, out var actualBool) && bool.TryParse(expected, out var expectedBool))
        {
            return actualBool == expectedBool;
        }

        return string.Equals(actual, expected, StringComparison.Ordinal);
    }

    private static int FindItemIndex(IList<object?> items, string? expected)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (string.Equals(items[index]?.ToString(), expected, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static ElementQueryResult RequireTarget(Window window, InteractionAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            throw new InvalidOperationException("Action target is required.");
        }

        var result = ElementQuery.FindBySelector(window, action.Target);
        return result.Element is null
            ? throw new InvalidOperationException($"Target '{action.Target}' was not found.")
            : result;
    }

    private static ElementQueryResult RequirePopup(Window window, InteractionAction action)
    {
        var target = RequireTarget(window, action);
        var popup = ResolvePopup(target.Element!)
            ?? throw new InvalidOperationException($"Target '{action.Target}' does not contain a supported popup.");
        return target with { Element = popup };
    }

    private static object? ResolvePopup(object target)
    {
        if (IsPopup(target))
        {
            return target;
        }

        if (target is Button button)
        {
            return button.Flyout is not null
                ? ResolvePopup(button.Flyout)
                : button.ContextFlyout is not null
                    ? ResolvePopup(button.ContextFlyout)
                    : null;
        }

        return ElementQuery.Traverse(target).FirstOrDefault(IsPopup);
    }

    private static bool IsPopup(object value)
    {
        return value is Flyout or MenuFlyout or CommandBarFlyout or ContentDialog or TeachingTip or ToolTip;
    }

    private static void SetPopupOpenState(object popup, bool isOpen)
    {
        switch (popup)
        {
            case ContentDialog dialog when isOpen:
                dialog.Show();
                break;
            case ContentDialog dialog:
                dialog.Hide("dismissed");
                break;
            case Flyout flyout:
                flyout.IsOpen = isOpen;
                break;
            case MenuFlyout menuFlyout:
                menuFlyout.IsOpen = isOpen;
                break;
            case CommandBarFlyout commandBarFlyout:
                commandBarFlyout.IsOpen = isOpen;
                break;
            case TeachingTip teachingTip:
                teachingTip.IsOpen = isOpen;
                break;
            case ToolTip toolTip:
                toolTip.IsOpen = isOpen;
                break;
        }
    }

    private static bool GetPopupOpenState(object popup)
    {
        return popup switch
        {
            ContentDialog dialog => dialog.IsOpen,
            Flyout flyout => flyout.IsOpen,
            MenuFlyout menuFlyout => menuFlyout.IsOpen,
            CommandBarFlyout commandBarFlyout => commandBarFlyout.IsOpen,
            TeachingTip teachingTip => teachingTip.IsOpen,
            ToolTip toolTip => toolTip.IsOpen,
            _ => false
        };
    }

    private static string SimpleType(object value)
    {
        var type = value.GetType().Name;
        var tick = type.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? type : type[..tick];
    }

    private static InteractionStepResult Passed(
        int index,
        InteractionAction action,
        string? message = null,
        string? expected = null,
        string? actual = null,
        ElementQueryResult? resolution = null,
        object? resolvedElement = null,
        IReadOnlyDictionary<string, string?>? observedState = null,
        IReadOnlyDictionary<string, string?>? beforeState = null,
        IReadOnlyDictionary<string, string?>? afterState = null)
    {
        var element = resolvedElement ?? resolution?.Element;
        observedState ??= afterState ?? BuildObservedState(element);
        return new InteractionStepResult(
            index,
            action.Type,
            "passed",
            action.Target,
            message,
            expected,
            actual,
            resolution?.Selector ?? action.Target,
            resolution?.SelectorKind ?? InferSelectorKind(action.Target),
            element is null ? null : SimpleType(element),
            observedState,
            beforeState,
            afterState);
    }

    private static InteractionStepResult Failed(
        int index,
        InteractionAction action,
        string message,
        string? expected = null,
        string? actual = null,
        ElementQueryResult? resolution = null,
        object? resolvedElement = null,
        IReadOnlyDictionary<string, string?>? observedState = null,
        IReadOnlyDictionary<string, string?>? beforeState = null,
        IReadOnlyDictionary<string, string?>? afterState = null)
    {
        var element = resolvedElement ?? resolution?.Element;
        observedState ??= afterState ?? BuildObservedState(element);
        return new InteractionStepResult(
            index,
            action.Type,
            "failed",
            action.Target,
            message,
            expected,
            actual,
            resolution?.Selector ?? action.Target,
            resolution?.SelectorKind ?? InferSelectorKind(action.Target),
            element is null ? null : SimpleType(element),
            observedState,
            beforeState,
            afterState);
    }

    private static string? InferSelectorKind(string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return null;
        }

        if (selector.StartsWith("automationId=", StringComparison.Ordinal))
        {
            return "automationId";
        }

        if (selector.StartsWith("name=", StringComparison.Ordinal))
        {
            return "name";
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string?>? BuildObservedState(object? element)
    {
        if (element is null)
        {
            return null;
        }

        var state = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["type"] = SimpleType(element)
        };
        if (element is FrameworkElement frameworkElement)
        {
            state["name"] = frameworkElement.Name;
            state["automationId"] = AutomationProperties.GetAutomationId(frameworkElement);
            state["isFocused"] = frameworkElement.IsFocused.ToString();
        }

        if (element is Control control)
        {
            state["isEnabled"] = control.IsEnabled.ToString();
        }

        switch (element)
        {
            case TextBlock textBlock:
                state["text"] = textBlock.Text;
                break;
            case TextBox textBox:
                state["text"] = textBox.Text;
                break;
            case Button button:
                state["content"] = button.Content?.ToString();
                break;
            case ListView listView:
                state["selectedIndex"] = listView.SelectedIndex.ToString();
                state["selectedItem"] = listView.SelectedItem?.ToString();
                break;
            case ComboBox comboBox:
                state["selectedIndex"] = comboBox.SelectedIndex.ToString();
                state["selectedItem"] = comboBox.SelectedItem?.ToString();
                break;
            case ContentDialog dialog:
                state["isOpen"] = dialog.IsOpen.ToString();
                state["result"] = dialog.Result;
                break;
            case MenuFlyout menuFlyout:
                state["isOpen"] = menuFlyout.IsOpen.ToString();
                state["invokedItem"] = menuFlyout.InvokedItem;
                break;
            case CommandBarFlyout commandBarFlyout:
                state["isOpen"] = commandBarFlyout.IsOpen.ToString();
                state["invokedCommand"] = commandBarFlyout.InvokedCommand;
                break;
            case Flyout flyout:
                state["isOpen"] = flyout.IsOpen.ToString();
                break;
            case TeachingTip teachingTip:
                state["isOpen"] = teachingTip.IsOpen.ToString();
                break;
            case ToolTip toolTip:
                state["isOpen"] = toolTip.IsOpen.ToString();
                break;
        }

        return state;
    }

    private static AccessibilityNode? FindAccessibilityNode(AccessibilityNode root, ElementQueryResult target)
    {
        if (MatchesAccessibilityNode(root, target))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var match = FindAccessibilityNode(child, target);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static bool MatchesAccessibilityNode(AccessibilityNode node, ElementQueryResult target)
    {
        if (target.Element is FrameworkElement frameworkElement)
        {
            if (!string.IsNullOrWhiteSpace(frameworkElement.Name) &&
                string.Equals(node.Name, frameworkElement.Name, StringComparison.Ordinal))
            {
                return true;
            }

            var automationId = AutomationProperties.GetAutomationId(frameworkElement);
            if (!string.IsNullOrWhiteSpace(automationId) &&
                string.Equals(node.AutomationId, automationId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return target.SelectorKind switch
        {
            "name" => string.Equals(node.Name, StripSelectorPrefix(target.Selector, "name="), StringComparison.Ordinal),
            "automationId" => string.Equals(node.AutomationId, StripSelectorPrefix(target.Selector, "automationId="), StringComparison.Ordinal),
            _ => false
        };
    }

    private static string StripSelectorPrefix(string selector, string prefix)
    {
        return selector.StartsWith(prefix, StringComparison.Ordinal)
            ? selector[prefix.Length..]
            : selector;
    }

    private static string? ReadAccessibilityValue(AccessibilityNode node, string key)
    {
        return key switch
        {
            "role" => node.Role,
            "name" => node.Name,
            "automationId" => node.AutomationId,
            "label" => node.Label,
            "helpText" => node.HelpText,
            "focused" => node.IsFocused.ToString(),
            "isFocused" => node.IsFocused.ToString(),
            "focusable" => node.IsFocusable?.ToString(),
            "isFocusable" => node.IsFocusable?.ToString(),
            "enabled" => node.IsEnabled?.ToString(),
            "isEnabled" => node.IsEnabled?.ToString(),
            "checked" => node.IsChecked?.ToString(),
            "isChecked" => node.IsChecked?.ToString(),
            "selected" => node.IsSelected?.ToString(),
            "isSelected" => node.IsSelected?.ToString(),
            "expanded" => node.IsExpanded?.ToString(),
            "isExpanded" => node.IsExpanded?.ToString(),
            "value" => node.Value,
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, string?> BuildAccessibilityState(AccessibilityNode node)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["role"] = node.Role,
            ["name"] = node.Name,
            ["automationId"] = node.AutomationId,
            ["label"] = node.Label,
            ["helpText"] = node.HelpText,
            ["isFocused"] = node.IsFocused.ToString(),
            ["isFocusable"] = node.IsFocusable?.ToString(),
            ["isEnabled"] = node.IsEnabled?.ToString(),
            ["isChecked"] = node.IsChecked?.ToString(),
            ["isSelected"] = node.IsSelected?.ToString(),
            ["isExpanded"] = node.IsExpanded?.ToString(),
            ["value"] = node.Value
        };
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
