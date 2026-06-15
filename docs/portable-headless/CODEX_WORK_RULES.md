# Codex Work Rules

Bu dosya Codex veya agent tabanlı geliştirme sırasında uyulacak kuralları tanımlar.

## 1. İsimlendirme kuralları

Kullanılacak canonical mode isimleri:

```text
portable-headless
windows-reference
macos-windowed
macos-windowed-ax
windows-custom-runtime
```

Kaçınılacak eski isimler:

```text
macos-headless
mac-headless-only
mac-ci-default
```

## 2. Platform bağımsız çekirdek

Aşağıdaki paketler/platform API'leri core runtime içine sızmamalıdır:

```text
AppKit
NSApplication
NSWindow
NSView
NSAccessibility
AX
Metal
Windows UIA
FlaUI
Win32 HWND
```

Bunlar sadece adapter/host projelerinde yer almalıdır.

Önerilen katmanlar:

```text
src/WinUI3.Compat.Core
src/WinUI3.Compat.Automation
src/WinUI3.Compat.Rendering
src/WinUI3.Compat.Renderer.Skia
src/WinUI3.Compat.Runner

src/WinUI3.Compat.Host.Mac
src/WinUI3.Compat.Automation.MacAx
src/WinUI3.Compat.Automation.WindowsUia
src/WinUI3.Compat.Reference.WindowsFlaUI
```

## 3. Headless mode kuralı

`portable-headless` mode şu şekilde çalışmalıdır:

```text
no real window
no OS accessibility
no OS automation
no required display session
internal automation only
offscreen screenshot optional
```

## 4. Automation kuralı

Her desteklenen control semantic automation node üretmelidir:

```text
AutomationId
Name
ControlType
Bounds
IsEnabled
IsVisible / IsOffscreen
SupportedPatterns
State
```

Automation action'ları renderer'a değil control behavior'a gitmelidir.

```text
Invoke -> ButtonBase.OnClick
SetValue -> TextBox.Text/PasswordBox internal value
Select -> Selector/NavigationView selection
Scroll -> ScrollViewer offset
Toggle -> CheckBox/ToggleButton state
```

## 5. Screenshot kuralı

Portable screenshot path:

```text
layout tree
render tree
Skia offscreen surface
PNG
artifact metadata
```

Screenshot metadata en az şunları içermelidir:

```json
{
  "mode": "portable-headless",
  "renderer": "skia-offscreen",
  "width": 1280,
  "height": 800,
  "scale": 1.0,
  "theme": "Light",
  "fontProfile": "portable-default"
}
```

## 6. Windows reference kuralı

`windows-reference` asla custom runtime output'u ile karıştırılmamalıdır.

Windows reference artifacts:

```text
lane: windows-reference
runtime: native-winui
driver: flaui-uia3
renderer: native-winui
```

## 7. macOS CI kuralı

Default PR CI'ya macOS runner eklenmemelidir.

macOS jobs sadece:

```text
workflow_dispatch
schedule
release
manual validation
self-hosted mac
```

koşullarında önerilir.

## 8. Linux CI kuralı

Default fast CI lane:

```text
ubuntu-latest
mode: portable-headless
driver: internal
renderer: skia-offscreen
```

olmalıdır.

## 9. Test evidence kuralı

Her scenario run şunları üretebilmelidir:

```text
scenario-result.json
automation-tree.json
action-log.json
layout-bounds.json
snapshot.png optional
snapshot-metadata.json
diagnostics.md
```

## 10. Compatibility honesty kuralı

Unsupported API/control kullanımı:

```text
silent fallback yapmamalı
diagnostic üretmeli
scenario result'ta görünmeli
compat matrix'e işlenmeli
```

## 11. No direct code copy kuralı

Uno, Avalonia, Microsoft UI Xaml, WPF gibi projelerden davranış ve mimari referans alınabilir. Doğrudan kopya yapılacaksa lisans header, attribution ve uyumluluk açıkça korunmalıdır. Tercih edilen yaklaşım bağımsız implementation'dır.

## 12. Phase discipline

Her phase sonunda:

```text
tests
docs
artifacts
compat matrix update
known gaps
```

eklenmelidir. “Çalışıyor” iddiası artifacts olmadan yapılmamalıdır.
