# Rendering and Text Layout Notes

## Renderer roles

Renderer is required for:

```text
screenshot
visual diff
window painting
text drawing
component crops
```

Renderer is not required for:

```text
Button.Invoke
Frame.Navigate
TextBox value change
NavigationView selection
AutomationCore tree creation
```

## Default renderer

Portable-headless default:

```text
skia-offscreen
```

This must work on:

```text
ubuntu-latest
windows-latest
local macOS
local Linux
local Windows
```

## Metal role

Metal is not a portable-headless requirement.

Metal is only for:

```text
macOS windowed performance path
Skia-on-Metal onscreen surface
manual/debug/native host rendering
```

Do not make Metal a CI default dependency.

## Render pipeline

```text
WinUI control tree
    ↓
Measure/Arrange
    ↓
Render tree
    ↓
Skia offscreen backend
    ↓
PNG + metadata
```

Windowed macOS:

```text
WinUI control tree
    ↓
Measure/Arrange
    ↓
Render tree
    ↓
Skia canvas
    ↓
Metal-backed surface optional
    ↓
NSView/NSWindow
```

## Text layout helper

Text layout must be separated from immediate draw calls.

Proposed helper:

```csharp
public sealed class WinUITextLayout
{
    public IReadOnlyList<WinUITextLine> Lines { get; }
    public Rect LayoutBounds { get; }
    public Rect InkBounds { get; }
    public int LineCount { get; }
    public double LineHeight { get; }

    public int HitTestPoint(Point point);
    public Rect GetCaretRect(int textIndex);
    public TextRange GetLineRange(int lineIndex);
}
```

## Text wrapping rules

Support MVP:

```text
NoWrap
Wrap
WrapWholeWords
```

Layout behavior:

```text
NoWrap:
    one line
    desired width = measured text width
    desired height = line height

Wrap:
    available width limits line width
    height = lineCount * lineHeight

WrapWholeWords:
    break at whitespace when possible
    fallback to character break for long tokens
```

## Text rendering rules

Do not use one `DrawText` call for wrapped text. Render line-by-line:

```text
for each line:
    draw line at x/y baseline
```

Apply clip rect:

```text
PushClip(textRect)
DrawLines()
PopClip()
```

## Determinism rules

Portable-headless screenshot must record:

```text
font profile
scale factor
theme
renderer version
text measurement mode
OS/platform
```

Linux portable-headless output may not be pixel-identical to Windows native output. Use it as fast regression signal, not native truth.

## Native truth

Windows native screenshot remains source of truth for WinUI visual behavior.

Portable-headless screenshot is:

```text
custom runtime output
portable regression artifact
layout/text/render signal
```
