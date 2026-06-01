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
    string? Message,
    string? Expected = null,
    string? Actual = null);

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
                "assertProperty" => AssertProperty(window, action, index),
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

    private static InteractionStepResult TypeText(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target is not TextBox textBox)
        {
            return Failed(index, action, "Target is not a TextBox.");
        }

        textBox.Text = action.Parameter ?? string.Empty;
        BindingOperations.UpdateSource(textBox, nameof(TextBox.Text));
        return Passed(index, action, expected: action.Parameter ?? string.Empty, actual: textBox.Text);
    }

    private static InteractionStepResult SelectItem(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (target is ComboBox comboBox)
        {
            var selectedIndex = FindItemIndex(comboBox.Items, action.Parameter);
            if (selectedIndex < 0)
            {
                return Failed(index, action, $"Item '{action.Parameter}' was not found.");
            }

            comboBox.SelectedIndex = selectedIndex;
            return Passed(index, action, expected: action.Parameter, actual: comboBox.SelectedItem?.ToString());
        }

        if (target is ListView listView)
        {
            var selectedIndex = FindItemIndex(listView.Items, action.Parameter);
            if (selectedIndex < 0)
            {
                return Failed(index, action, $"Item '{action.Parameter}' was not found.");
            }

            listView.SelectedIndex = selectedIndex;
            return Passed(index, action, expected: action.Parameter, actual: listView.SelectedItem?.ToString());
        }

        return Failed(index, action, "Target is not a selectable item control.");
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

    private static InteractionStepResult OpenPopup(Window window, InteractionAction action, int index)
    {
        var popup = RequirePopup(window, action);
        SetPopupOpenState(popup, isOpen: true);
        return Passed(index, action, $"Opened {SimpleType(popup)}.", expected: "True", actual: GetPopupOpenState(popup).ToString());
    }

    private static InteractionStepResult DismissPopup(Window window, InteractionAction action, int index)
    {
        var popup = RequirePopup(window, action);
        SetPopupOpenState(popup, isOpen: false);
        return Passed(index, action, $"Dismissed {SimpleType(popup)}.", expected: "False", actual: GetPopupOpenState(popup).ToString());
    }

    private static InteractionStepResult InvokeMenuItem(Window window, InteractionAction action, int index)
    {
        var popup = RequirePopup(window, action);
        var expected = action.Parameter ?? string.Empty;
        if (popup is MenuFlyout menuFlyout)
        {
            var item = menuFlyout.Items.OfType<MenuFlyoutItem>().FirstOrDefault(candidate =>
                string.Equals(candidate.Text, expected, StringComparison.Ordinal));
            if (item is null)
            {
                return Failed(index, action, $"Menu item '{expected}' was not found.");
            }

            menuFlyout.IsOpen = true;
            menuFlyout.InvokedItem = item.Text;
            item.PerformClick();
            return Passed(index, action, $"Invoked menu item '{item.Text}'.", expected: expected, actual: menuFlyout.InvokedItem);
        }

        if (popup is CommandBarFlyout commandBarFlyout)
        {
            var command = commandBarFlyout.PrimaryCommands
                .Concat(commandBarFlyout.SecondaryCommands)
                .OfType<AppBarButton>()
                .FirstOrDefault(candidate => string.Equals(candidate.Label, expected, StringComparison.Ordinal));
            if (command is null)
            {
                return Failed(index, action, $"Command '{expected}' was not found.");
            }

            commandBarFlyout.IsOpen = true;
            commandBarFlyout.InvokedCommand = command.Label;
            command.PerformClick();
            return Passed(index, action, $"Invoked command '{command.Label}'.", expected: expected, actual: commandBarFlyout.InvokedCommand);
        }

        return Failed(index, action, $"Target popup '{SimpleType(popup)}' does not expose invokable menu items.");
    }

    private static InteractionStepResult AssertProperty(Window window, InteractionAction action, int index)
    {
        var target = RequireTarget(window, action);
        if (string.IsNullOrWhiteSpace(action.Key))
        {
            return Failed(index, action, "assertProperty requires key.");
        }

        var property = target.GetType().GetProperty(action.Key);
        if (property is null || !property.CanRead)
        {
            return Failed(index, action, $"Property '{action.Key}' was not found.");
        }

        var actual = property.GetValue(target)?.ToString() ?? string.Empty;
        var expected = action.Parameter ?? string.Empty;
        return string.Equals(actual, expected, StringComparison.Ordinal)
            ? Passed(index, action, expected: expected, actual: actual)
            : Failed(index, action, $"Expected '{expected}' but found '{actual}'.", expected, actual);
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

    private static object RequireTarget(Window window, InteractionAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            throw new InvalidOperationException("Action target is required.");
        }

        return ElementQuery.FindByName(window, action.Target)
            ?? throw new InvalidOperationException($"Target '{action.Target}' was not found.");
    }

    private static object RequirePopup(Window window, InteractionAction action)
    {
        var target = RequireTarget(window, action);
        return ResolvePopup(target)
            ?? throw new InvalidOperationException($"Target '{action.Target}' does not contain a supported popup.");
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
        string? actual = null)
    {
        return new InteractionStepResult(index, action.Type, "passed", action.Target, message, expected, actual);
    }

    private static InteractionStepResult Failed(
        int index,
        InteractionAction action,
        string message,
        string? expected = null,
        string? actual = null)
    {
        return new InteractionStepResult(index, action.Type, "failed", action.Target, message, expected, actual);
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
