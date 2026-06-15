# Runtime Rules

## 1. Ana ürün tanımı

Bu proje “herhangi bir `.exe` veya `.msix` dosyasını macOS/Linux üzerinde çalıştıran runtime” değildir.

Ana hedef:

```text
WinUI 3 C#/XAML kaynak kodunu
source-level compatibility runtime içinde
portable headless / local macOS / Windows reference test yollarıyla
build, run, automate, screenshot ve evidence üretilebilir hale getirmek.
```

## 2. Destek sınırı

Runtime her zaman açık bir support boundary yayınlamalıdır:

```text
supported
partial
planned
windows-only
not-supported
unknown
```

`unknown` yüzeyler sessizce desteklenmiş gibi davranmamalıdır.

## 3. Headless artık macOS'a özel değildir

Zorunlu kural:

```text
Headless runtime MUST be portable.
```

Portable headless runtime şunlara bağlı olmamalıdır:

```text
NSApplication
NSWindow
NSView
NSAccessibility
AX
AppKit event loop
Metal
CAMetalLayer
Windows UIA
FlaUI
Desktop session
```

Portable headless runtime şunlara dayanmalıdır:

```text
.NET runtime
WinUI facade/runtime core
XAML loader/materializer
DependencyProperty system
Resource resolution
ControlTemplate/VSM subset
Measure/Arrange layout
AutomationCore
Internal scenario driver
Skia CPU/offscreen renderer
Portable text layout engine
```

## 4. macOS desteği hâlâ gereklidir

macOS support kaldırılmamalıdır. Ancak görevi doğru ayrılmalıdır:

```text
Local Mac developer:
    portable-headless local
    macos-windowed local
    macos-windowed-ax local/manual

Default CI:
    macOS runner kullanılmamalı.

Manual/nightly/release validation:
    macos-windowed veya macos-windowed-ax opsiyonel olabilir.
```

## 5. Linux'ta UI test yapılabilir

Linux'ta yapılacak test platform-native UI automation değildir.

Linux portable-headless şunları yapabilir:

```text
WinUI source load
XAML materialization
layout
hit-test model
internal automation
Button Invoke
TextBox SetValue
NavigationView Select
Frame.Navigate
Scroll
Skia offscreen screenshot
visual artifact
```

Linux şunları yapmaz:

```text
native Windows WinUI test
FlaUI/UIA3
macOS AX/NSAccessibility
real compositor/focus/IME validation
```

## 6. Renderer, automation'dan ayrıdır

Automation renderer'a bağlı olmamalıdır.

```text
AutomationCore:
    semantic tree
    patterns
    actions
    state
    bounds

Renderer:
    render tree
    text layout
    image output
    screenshot
```

Button click, navigation ve state değişimi Skia/Metal gerektirmemelidir. Screenshot/visual diff için renderer gerekir.

## 7. Windowed host gerekli ama default test yolu değildir

Windowed host aşağıdakiler için gereklidir:

```text
manual visual debug
real mouse/keyboard
focus debugging
IME debugging
macOS AX validation
Skia-on-Metal onscreen path
platform integration
```

Ancak büyük PR test matrisi için default lane olmamalıdır.

## 8. Windows native reference ayrı tutulur

Windows native reference:

```text
windows-latest
real native WinUI 3 app
FlaUI.UIA3
native Windows UIA tree
native screenshot
source of truth
```

Bu lane, portable headless output'u doğrulamak ve farkları belgelemek için kullanılır.

## 9. Test scenario formatı ortak kalmalıdır

Aynı scenario dosyası farklı driver'larla çalışmalıdır:

```text
portable-headless:
    internal driver

windows-reference:
    FlaUI.UIA3 client

macos-windowed-ax:
    AX adapter

windows-custom-runtime:
    optional UIA provider + FlaUI.UIA3
```

## 10. Product claim kuralı

Doğru claim:

```text
Source-level WinUI 3 compatibility runner for a documented subset,
with portable headless automation, screenshots, and Windows native reference evidence.
```

Yanlış claim:

```text
Run any WinUI 3 app on macOS/Linux.
Full Windows App SDK replacement.
Pixel-perfect WinUI renderer for all controls.
Runs arbitrary exe/msix.
```
