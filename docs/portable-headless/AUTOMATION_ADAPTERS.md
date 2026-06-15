# Automation Adapters

## Core principle

AutomationCore is platform-independent. Adapter'lar sadece map eder.

```text
AutomationCore
    ↓
Internal driver
FlaUI/UIA3 client
macOS AX provider
Windows UIA provider optional
```

## AutomationCore model

```csharp
public sealed class AutomationNode
{
    public string RuntimeId { get; init; }
    public string? AutomationId { get; init; }
    public string? Name { get; init; }
    public AutomationControlType ControlType { get; init; }

    public Rect Bounds { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsOffscreen { get; init; }
    public bool IsKeyboardFocusable { get; init; }
    public bool HasKeyboardFocus { get; init; }

    public IReadOnlyList<AutomationNode> Children { get; init; }
    public IReadOnlySet<AutomationPattern> Patterns { get; init; }
}
```

## Pattern interfaces

```csharp
public interface IInvokePattern
{
    void Invoke();
}

public interface IValuePattern
{
    string Value { get; }
    bool IsReadOnly { get; }
    void SetValue(string value);
}

public interface ITogglePattern
{
    ToggleState ToggleState { get; }
    void Toggle();
}

public interface ISelectionItemPattern
{
    bool IsSelected { get; }
    void Select();
}

public interface IScrollPattern
{
    bool VerticallyScrollable { get; }
    bool HorizontallyScrollable { get; }
    void Scroll(double horizontalAmount, double verticalAmount);
}
```

## Internal driver

Used by:

```text
portable-headless
macos-windowed --driver internal
local debugging
```

Behavior:

```text
Find node by AutomationId
Resolve pattern
Call runtime control behavior directly
Wait for dispatcher idle
Export artifacts
```

This driver does not use OS accessibility.

## Windows native reference driver

Used by:

```text
mode: windows-reference
driver: flaui-uia3
renderer: native-winui
```

Behavior:

```text
Launch real native WinUI 3 app
Connect with FlaUI.UIA3
Find elements by AutomationId
Use native UIA patterns
Capture native screenshot
Export native automation tree
```

This is source of truth.

## macOS AX adapter

Used by:

```text
mode: macos-windowed-ax
driver: ax
```

Behavior:

```text
AutomationCore node -> NSAccessibilityElement
Button -> AXButton / press
TextBox -> text field / value
CheckBox -> checkbox / press
SelectionItem -> select
ScrollViewer -> scroll area
```

This adapter requires real macOS windowed host.

Not default PR CI.

## Windows custom-runtime UIA provider

Used by:

```text
mode: windows-custom-runtime
driver: flaui-uia3
```

Behavior:

```text
Our custom runtime runs on Windows
AutomationCore exposed as UIA provider
FlaUI.UIA3 tests our runtime
```

This is not native WinUI reference.

## Adapter separation

Do not confuse:

```text
FlaUI client against real WinUI:
    windows-reference

FlaUI client against our custom runtime:
    windows-custom-runtime
```

They must produce different artifact lanes.

## Mapping table

| AutomationCore | Windows UIA | macOS AX | Internal |
|---|---|---|---|
| Button + Invoke | ControlType.Button + InvokePattern | AXButton + press | IInvokePattern |
| TextBox + Value | Edit + ValuePattern | AXTextField + value | IValuePattern |
| CheckBox + Toggle | CheckBox + TogglePattern | AXCheckBox + press/value | ITogglePattern |
| RadioButton | RadioButton + SelectionItem | AXRadioButton | ISelectionItemPattern |
| ScrollViewer | ScrollBar/Pane + ScrollPattern | AXScrollArea | IScrollPattern |
| TextBlock | Text | AXStaticText | read-only node |
