# Control Support Matrix Seed

This is the initial MVP support matrix.

## Legend

```text
supported:
    intended to work in portable-headless and produce automation/render output

partial:
    basic behavior available, advanced behavior incomplete

planned:
    not implemented yet

windows-only:
    native Windows reference only

not-supported:
    explicit non-goal or later large feature

unknown:
    must not be treated as supported
```

## MVP controls

| Control | Status | Automation | Render | Notes |
|---|---|---|---|---|
| Application | partial | n/a | n/a | Logical app lifecycle only |
| Window | partial | Window node | background/root | Headless logical window |
| Page | supported | Pane/Custom | content root | Frame navigation target |
| Frame | supported | n/a | content | Navigate/Content swap |
| Grid | supported | n/a | children | Auto/Star/fixed MVP |
| StackPanel | supported | n/a | children | Vertical/horizontal |
| Border | supported | n/a | rounded rect/border | padding/border |
| TextBlock | supported | Text | text lines | wrapping MVP |
| Button | supported | Invoke | border/content | Click/Command |
| TextBox | partial | Value | text box/text | text input MVP, IME later |
| PasswordBox | partial | Value-like internal | masked text | security semantics later |
| CheckBox | supported | Toggle | checkbox + label | basic |
| RadioButton | partial | SelectionItem | radio + label | group semantics later |
| NavigationView | partial | Selection/Invoke | basic shell/list | full template later |
| NavigationViewItem | partial | SelectionItem/Invoke | row selected state | basic |
| ScrollViewer | partial | Scroll | clip/offset | basic |
| Image | planned | Image | bitmap | later |
| ComboBox | planned | Expand/Selection | later | not MVP |
| ListView | planned | Selection | later | virtualization later |
| InfoBar | planned | Text/Invoke | later | good visual target |
| Flyout | planned | Window/Pane | overlay | later |
| ContentDialog | planned | Window/Dialog | overlay | later |
| WebView2 | not-supported | n/a | n/a | Windows-specific/large |
| MediaElement | not-supported | n/a | n/a | non-goal MVP |
| MapControl | not-supported | n/a | n/a | non-goal MVP |
| Acrylic/Mica | planned | n/a | approximate later | no native claim |

## MVP scenarios

### Login page

Required controls:

```text
TextBlock
TextBox
PasswordBox
Button
StackPanel/Grid
Frame.Navigate
```

Required actions:

```text
SetValue username
SetValue password
Invoke login
Assert Dashboard visible
Screenshot
```

### NavigationView page

Required controls:

```text
NavigationView
NavigationViewItem
Frame
TextBlock
Button
```

Required actions:

```text
Select nav item
Assert page type
Screenshot selected state
```

## State coverage MVP

| State | MVP |
|---|---|
| default | yes |
| disabled | yes for Button/TextBox |
| focused | partial |
| pressed | partial |
| hover | windowed only/later |
| selected | NavigationViewItem/RadioButton |
| error/validation | later |

## Matrix update rule

Every new control implementation must update:

```text
control status
automation patterns
render status
scenario coverage
known gaps
```
