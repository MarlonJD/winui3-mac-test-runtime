# Scenario Driver Spec

## Amaç

Aynı scenario dosyası farklı driver'larda çalışabilmelidir:

```text
portable-headless -> internal
windows-reference -> flaui-uia3
macos-windowed-ax -> ax
windows-custom-runtime -> flaui-uia3 over our provider
```

## Scenario örneği

```json
{
  "name": "login-basic",
  "window": {
    "width": 1280,
    "height": 800,
    "theme": "Light"
  },
  "steps": [
    {
      "action": "setValue",
      "automationId": "UsernameTextBox",
      "value": "demo@example.com"
    },
    {
      "action": "setValue",
      "automationId": "PasswordBox",
      "value": "secret"
    },
    {
      "action": "invoke",
      "automationId": "LoginButton"
    },
    {
      "assert": "visible",
      "automationId": "DashboardHeader"
    },
    {
      "screenshot": "after-login.png"
    }
  ]
}
```

## Actions

### `invoke`

```json
{
  "action": "invoke",
  "automationId": "LoginButton"
}
```

Maps to:

```text
Internal: IInvokePattern.Invoke
FlaUI: InvokePattern.Invoke / Button.Invoke
AX: accessibilityPerformPress
```

### `setValue`

```json
{
  "action": "setValue",
  "automationId": "UsernameTextBox",
  "value": "demo@example.com"
}
```

Maps to:

```text
Internal: IValuePattern.SetValue
FlaUI: ValuePattern.SetValue / TextBox.Enter fallback
AX: setAccessibilityValue
```

### `toggle`

```json
{
  "action": "toggle",
  "automationId": "RememberMeCheckBox"
}
```

### `select`

```json
{
  "action": "select",
  "automationId": "DashboardNavigationItem"
}
```

### `scroll`

```json
{
  "action": "scroll",
  "automationId": "MainScrollViewer",
  "vertical": 400
}
```

### `focus`

```json
{
  "action": "focus",
  "automationId": "SearchTextBox"
}
```

### `waitForIdle`

```json
{
  "action": "waitForIdle"
}
```

### `screenshot`

```json
{
  "screenshot": "after-login.png"
}
```

## Assertions

### `exists`

```json
{
  "assert": "exists",
  "automationId": "LoginButton"
}
```

### `visible`

```json
{
  "assert": "visible",
  "automationId": "DashboardHeader"
}
```

### `valueEquals`

```json
{
  "assert": "valueEquals",
  "automationId": "UsernameTextBox",
  "value": "demo@example.com"
}
```

### `selected`

```json
{
  "assert": "selected",
  "automationId": "DashboardNavigationItem"
}
```

### `pageType`

```json
{
  "assert": "pageType",
  "value": "MyApp.Pages.DashboardPage"
}
```

## Required output

Every scenario run must emit:

```text
scenario-result.json
action-log.json
automation-tree.json
layout-bounds.json
diagnostics.md
```

If screenshots are requested:

```text
screenshots/*.png
screenshots/*.metadata.json
```

## Result metadata

```json
{
  "scenario": "login-basic",
  "mode": "portable-headless",
  "driver": "internal",
  "renderer": "skia-offscreen",
  "platform": "linux-x64",
  "success": true,
  "startedAt": "ISO-8601",
  "endedAt": "ISO-8601"
}
```
