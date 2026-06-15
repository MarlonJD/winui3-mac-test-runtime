---

# FILE: README_INDEX.md

# WinUI 3 Compatibility Runtime — Updated Rules and Codex Plan

Bu paket, son tartışmadaki güncellenmiş kararı merkeze alır:

> Headless runtime macOS'a özel olmamalı. Ana hedef **portable headless WinUI compatibility runtime** olmalı.
> Varsayılan CI hedefi `ubuntu-latest` veya `windows-latest` olmalı.
> macOS desteği local developer experience ve opsiyonel windowed/AX doğrulama için gerekli kalmalı, fakat default PR CI lane olmamalı.

## Dosyalar

- `RUNTIME_RULES.md`
  Runtime mimarisi ve zorunlu kurallar.

- `ARCHITECTURE_DECISIONS.md`
  ADR formatında ana kararlar.

- `CODEX_WORK_RULES.md`
  Codex'in patch üretirken uyması gereken çalışma kuralları.

- `CODEX_PHASE_PLAN.md`
  Portable headless, Windows native reference, macOS windowed/AX ve optional provider fazları.

- `CI_STRATEGY.md`
  Linux/Windows default CI, macOS optional/manual/nightly stratejisi.

- `LOCAL_DEVELOPMENT_MODES.md`
  Mac, Linux ve Windows local geliştirme modları.

- `SCENARIO_DRIVER_SPEC.md`
  Ortak scenario/action/assertion formatı.

- `AUTOMATION_ADAPTERS.md`
  Internal driver, FlaUI/UIA3, macOS AX, optional Windows UIA provider ayrımı.

- `RENDERING_TEXT_LAYOUT_NOTES.md`
  Skia, Metal, offscreen rendering ve text layout notları.

- `CONTROL_SUPPORT_MATRIX_SEED.md`
  MVP control/pattern/support matrisi.

- `PRODUCT_POSITIONING.md`
  Projenin değer önerisi, hedef kullanıcılar ve yapılmaması gereken vaatler.

- `ALL_IN_ONE_RULES_AND_PLAN.md`
  Yukarıdaki bütün içeriklerin tek dosyada birleşmiş hali.

## En kritik güncelleme

Eski düşünce:

```text
macOS headless runtime
    -> macos-latest CI
    -> skia-offscreen
```

Yeni karar:

```text
portable headless runtime
    -> ubuntu-latest / windows-latest / local macOS
    -> internal automation
    -> skia-offscreen
```

macOS:

```text
local developer experience: gerekli
windowed debug: gerekli
AX/NSAccessibility validation: opsiyonel/ileri faz
default PR CI: hayır
```



---

# FILE: RUNTIME_RULES.md

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



---

# FILE: ARCHITECTURE_DECISIONS.md

# Architecture Decisions

## ADR-001 — Headless runtime portable olacak

### Karar

Headless runtime macOS'a özel olmayacak. Ana hedef:

```text
portable-headless
```

Bu mod `ubuntu-latest`, `windows-latest`, local macOS, local Linux ve local Windows üzerinde çalışabilmelidir.

### Gerekçe

macOS hosted CI pahalıdır ve default lane olursa adoption düşer. Headless'in asıl değeri macOS runner kullanmak değil, macOS runner'a ihtiyaç duymadan source-level WinUI test/screenshot alabilmektir.

### Sonuç

Eski isimler değiştirilmelidir:

```text
macos-headless -> portable-headless
WinUI3.MacRunner headless assumptions -> platform-neutral assumptions
```

---

## ADR-002 — macOS support local developer experience için korunacak

### Karar

macOS local development desteklenecek:

```text
portable-headless local
macos-windowed local
macos-windowed-ax manual/local
```

### Gerekçe

Mac kullanan geliştirici Windows'a geçmeden WinUI source app'i doğrulamak, screenshot almak ve gerekirse pencereyle debug etmek ister.

### Sonuç

macOS windowed host yapılacaktır, fakat default PR CI lane yapılmayacaktır.

---

## ADR-003 — Linux portable UI test supported olacak

### Karar

Linux üzerinde portable-headless UI compatibility tests supported olacaktır.

### Gerekçe

Linux CI ucuz ve yaygındır. Headless mod OS-level UI automation kullanmadığı için Linux'ta çalışabilir.

### Sınır

Linux lane native Windows UIA/FlaUI veya macOS AX doğrulaması yapmaz.

---

## ADR-004 — AutomationCore platform bağımsız olacak

### Karar

Automation tree ve pattern modeli platform bağımsız `AutomationCore` içinde kalacaktır.

```text
AutomationCore
    AutomationNode
    AutomationPattern
    Invoke
    Value
    Toggle
    SelectionItem
    Scroll
    Focus
```

Platform adapter'ları sadece mapping yapacaktır.

### Adapter'lar

```text
Internal driver:
    portable-headless

FlaUI/UIA3 client:
    windows-reference

macOS AX provider:
    macos-windowed-ax

Windows UIA provider:
    optional windows-custom-runtime
```

---

## ADR-005 — Skia default offscreen renderer olacak

### Karar

Portable-headless screenshot için default renderer:

```text
skia-offscreen
```

Metal sadece macOS windowed/onscreen performance path için kullanılacaktır.

### Gerekçe

Skia CPU/offscreen Linux/Windows/macOS CI için uygundur. Metal headless default olmamalıdır.

---

## ADR-006 — Windows native remains source of truth

### Karar

Gerçek WinUI davranışı için Windows native reference lane korunacaktır:

```text
windows-latest
real native WinUI app
FlaUI.UIA3
native screenshot
native automation tree
```

Portable-headless output native Windows'un yerine geçmez; hızlı regression signal verir.

---

## ADR-007 — Same scenario, multiple drivers

### Karar

Scenario dosyaları driver bağımsız olmalıdır.

```text
same scenario
    -> internal driver
    -> FlaUI.UIA3 driver
    -> AX driver
```

### Gerekçe

Tek test tanımı ile portable runtime, Windows native reference ve macOS AX output karşılaştırılabilir.

---

## ADR-008 — Product positioning developer tool olacak

### Karar

Proje production app framework iddiası yerine developer tool / CI evidence platform olarak konumlanacaktır.

Doğru hedef:

```text
Mac/Linux/Windows CI'da WinUI source-level compatibility evidence.
Windows native reference ile karşılaştırmalı artifact.
Local Mac developer loop.
```



---

# FILE: CODEX_WORK_RULES.md

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



---

# FILE: CODEX_PHASE_PLAN.md

# Codex Phase Plan — Updated Portable Headless Strategy

Bu plan Codex/agent ile yapılacak işleri fazlara böler. Ana değişiklik: headless runtime macOS'a özel değildir; portable olmalıdır.

## Phase 0 — Rename and architecture cleanup

### Amaç

Eski “macOS headless” düşüncesini temizleyip canonical mode isimlerini yerleştirmek.

### İşler

- Mode enum/string seti oluştur:
  - `portable-headless`
  - `windows-reference`
  - `macos-windowed`
  - `macos-windowed-ax`
  - `windows-custom-runtime`
- Docs içinde `macos-headless` ifadesini `portable-headless` ile değiştir.
- Core projelerde AppKit/AX/Metal/Windows UIA bağımlılığı olmadığını doğrula.
- Runner output metadata'ya `mode`, `driver`, `renderer`, `lane` ekle.

### Done

- `portable-headless` adı CLI/docs/tests içinde canonical.
- macOS hosted CI default olarak önerilmiyor.
- Core runtime platform bağımsız dependency sınırına sahip.

---

## Phase 1 — Portable runtime core boundary

### Amaç

Platform-independent WinUI compatibility core'un sınırlarını netleştirmek.

### İşler

- Logical Window/Page/Application abstraction.
- Dispatcher/tick model.
- Basic dependency property storage.
- Visibility, IsEnabled, Name, AutomationProperties.
- Tree traversal and diagnostics.
- No AppKit/Win32 dependency in core.

### Done

- Core tests Linux/Windows/macOS üzerinde çalışabilir.
- Logical tree JSON export var.

---

## Phase 2 — XAML/source materialization MVP

### Amaç

WinUI source-level app/page/control tree oluşturmak.

### İşler

- Basic XAML load/materialize path.
- Supported subset:
  - Window
  - Page
  - Grid
  - StackPanel
  - Border
  - TextBlock
  - Button
  - TextBox
  - CheckBox
  - RadioButton
  - Frame
- StaticResource/ThemeResource minimal resolver.
- Unsupported markup diagnostics.

### Done

- Basit login page source XAML materialize edilir.
- Unsupported property/control diagnostic verir.

---

## Phase 3 — Layout engine MVP

### Amaç

Portable layout ve bounds üretmek.

### İşler

- Measure/Arrange base pipeline.
- StackPanel vertical/horizontal.
- Grid MVP:
  - Auto
  - Star
  - fixed sizes
  - row/column placement
- Border padding/border thickness.
- ContentControl/ContentPresenter MVP.
- TextBlock desired size from text layout helper.
- Layout bounds JSON export.

### Done

- Login page için stable bounds.
- Layout Linux CI'da deterministic.

---

## Phase 4 — Portable text layout MVP

### Amaç

Text overflow ve wrapping problemlerini portable şekilde çözmek.

### İşler

- `WinUITextLayout` helper.
- NoWrap, Wrap, WrapWholeWords.
- Line metrics.
- DesiredSize from line count.
- Clip rect support.
- TextBlock render lines.
- TextBox caret/hit-test hazırlığı.

### Done

- Long login text taşmaz.
- TextWrapping=Wrap width'e göre satır üretir.
- Layout height lineCount * lineHeight ile hesaplanır.

---

## Phase 5 — AutomationCore MVP

### Amaç

Renderer'dan bağımsız semantic automation tree oluşturmak.

### İşler

- AutomationNode model:
  - RuntimeId
  - AutomationId
  - Name
  - ControlType
  - Bounds
  - IsEnabled
  - IsOffscreen
  - Patterns
- Pattern interfaces:
  - Invoke
  - Value
  - Toggle
  - SelectionItem
  - Scroll
- Automation tree export.

### Done

- Button -> Invoke.
- TextBox -> Value.
- CheckBox -> Toggle.
- NavigationViewItem or RadioButton -> SelectionItem.
- Bounds layout'tan gelir.

---

## Phase 6 — Internal scenario driver

### Amaç

Portable-headless için OS automation'a ihtiyaç duymadan UI scenario çalıştırmak.

### İşler

- JSON scenario parser.
- Actions:
  - invoke
  - setValue
  - toggle
  - select
  - scroll
  - focus
  - waitForIdle
  - screenshot
- Assertions:
  - exists
  - visible
  - valueEquals
  - selected
  - pageType
  - textContains
- Action log and scenario result.

### Done

- Login scenario Linux portable-headless üzerinde çalışır.
- UI behavior renderer olmadan test edilebilir.
- Screenshot step opsiyoneldir.

---

## Phase 7 — Skia offscreen renderer

### Amaç

Portable screenshot/visual artifact üretmek.

### İşler

- Render tree abstraction.
- Skia CPU/offscreen backend.
- Shapes:
  - rect
  - rounded rect
  - border
  - line
  - text lines
  - image placeholder
  - clip
  - opacity
- Snapshot metadata.
- PNG artifact.

### Done

- `ubuntu-latest` üzerinde PNG üretebilir.
- No NSWindow/Metal dependency.
- Visual-headless login screenshot var.

---

## Phase 8 — Portable-headless CI lane

### Amaç

Ucuz default CI lane oluşturmak.

### İşler

- GitHub Action wrapper.
- Default job sample:
  - `runs-on: ubuntu-latest`
  - `mode: portable-headless`
  - `driver: internal`
  - `renderer: skia-offscreen`
- Artifact upload.
- Docs update.

### Done

- Public sample workflow Linux üzerinde çalışır.
- macOS runner default olarak önerilmez.

---

## Phase 9 — Windows native reference lane

### Amaç

Gerçek WinUI 3 davranışını source of truth olarak toplamak.

### İşler

- `windows-reference` mode.
- Build/launch native WinUI fixture.
- FlaUI.UIA3 driver.
- Automation tree capture.
- Native screenshot capture.
- Scenario run.
- Artifact metadata:
  - lane: windows-reference
  - runtime: native-winui
  - driver: flaui-uia3
  - renderer: native-winui

### Done

- Aynı scenario Windows native WinUI'de çalışır.
- Native screenshot ve automation tree üretilir.

---

## Phase 10 — Comparison/evidence dashboard

### Amaç

Portable-headless ile Windows native reference output'unu karşılaştırmak.

### İşler

- Scenario result comparison.
- Automation tree comparison:
  - AutomationId
  - ControlType
  - Name
  - Patterns
  - Bounds tolerance
- Visual comparison:
  - component crop
  - whole screenshot optional
  - tolerance config
- HTML/Markdown review page.

### Done

- `portable-headless` vs `windows-reference` farkları raporlanır.
- Failures actionable diagnostics üretir.

---

## Phase 11 — macOS windowed local host

### Amaç

Mac developer local debug için gerçek pencere host'u oluşturmak.

### İşler

- NSApplication/NSWindow host.
- Skia surface.
- Optional Skia-on-Metal path.
- Mouse/key/scroll event capture.
- Coordinate conversion.
- Hit-test -> runtime input events.
- Manual debug CLI:
  - `mode: macos-windowed`
  - `driver: internal`

### Done

- Mac'te local pencere açılır.
- Button click / TextBox input / navigation manuel denenebilir.
- CI default değildir.

---

## Phase 12 — macOS AX adapter

### Amaç

macOS platform accessibility/automation bridge'i doğrulamak.

### İşler

- AutomationCore -> NSAccessibilityElement mapping.
- Roles:
  - Button
  - StaticText
  - TextField
  - CheckBox
  - RadioButton
  - List/ListItem
  - ScrollArea
- AX actions -> AutomationCore patterns.
- AX notifications minimal.
- Local/manual tests.

### Done

- AX client Button press yapabilir.
- Text value get/set yapılabilir.
- Windowed host accessible tree expose eder.
- This remains optional/manual/nightly, not default PR CI.

---

## Phase 13 — Optional Windows custom-runtime UIA provider

### Amaç

Bizim custom runtime Windows'ta çalışırsa FlaUI.UIA3 ile test edilebilir hale getirmek.

### İşler

- AutomationCore -> Windows UIA provider mapping.
- ControlType mapping.
- Invoke/Value/Toggle/Selection/Scroll providers.
- FlaUI over custom runtime tests.
- Metadata:
  - lane: windows-custom-runtime
  - not windows-reference

### Done

- FlaUI bizim custom-rendered runtime'ı Windows'ta sürebilir.
- Native WinUI reference ile karışmaz.

---

## Phase 14 — Broader control/state coverage

### Amaç

MVP sonrası component coverage genişletmek.

### İşler

- Controls:
  - ComboBox
  - ListView
  - InfoBar
  - Flyout
  - Dialog
  - Slider
  - ProgressRing/ProgressBar
- States:
  - default
  - hover
  - pressed
  - disabled
  - focused
  - selected
- VisualStateManager expansion.
- Resource/theme expansion.
- Accessibility pattern expansion.

### Done

- Compatibility dashboard controls/states bazında güncel.
- Supported/partial/planned ayrımı açık.

---

## Phase 15 — Release hardening

### Amaç

Tooling'i kullanılabilir ürün haline getirmek.

### İşler

- CLI polish.
- GitHub Action docs.
- Sample projects.
- Known gaps.
- Baseline management.
- Artifact retention guidance.
- Versioned compatibility matrix.

### Done

- External developer docs hazır.
- “No app source change within supported subset” demo çalışır.
- Linux portable-headless + Windows reference sample workflow hazır.



---

# FILE: CI_STRATEGY.md

# CI Strategy

## Ana karar

Default PR CI macOS runner kullanmamalıdır.

Neden:

```text
macOS hosted runners pahalıdır.
Windowed/AX tests flakier olabilir.
Portable headless macOS'a bağlı olmamalıdır.
```

## Default CI lanes

### 1. Portable headless lane

```yaml
jobs:
  portable-headless:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: marlonjd/winui3-compat-runner-action@v1
        with:
          mode: portable-headless
          driver: internal
          renderer: skia-offscreen
          project: src/MyApp/MyApp.csproj
          scenario: tests/scenarios/login.json
          output: artifacts/portable-headless
```

Amaç:

```text
fast source-level WinUI compatibility check
internal automation
layout/text/render regression
Skia offscreen screenshot
cheap CI
```

### 2. Windows native reference lane

```yaml
jobs:
  windows-native-reference:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: marlonjd/winui3-compat-runner-action@v1
        with:
          mode: windows-reference
          driver: flaui-uia3
          renderer: native-winui
          project: src/MyApp/MyApp.csproj
          scenario: tests/scenarios/login.json
          output: artifacts/windows-reference
```

Amaç:

```text
real native WinUI source of truth
Windows UIA/FlaUI behavior
native screenshot
reference artifacts
```

## Optional macOS lane

macOS hosted CI sadece şu durumlarda kullanılmalıdır:

```text
workflow_dispatch
nightly/scheduled
release validation
self-hosted Mac available
explicit platform integration changes
```

Örnek:

```yaml
jobs:
  macos-windowed-ax:
    runs-on: macos-latest
    if: github.event_name == 'workflow_dispatch' || github.event_name == 'schedule'
    steps:
      - uses: actions/checkout@v4
      - uses: marlonjd/winui3-compat-runner-action@v1
        with:
          mode: macos-windowed-ax
          driver: ax
          renderer: skia-metal
          project: src/MyApp/MyApp.csproj
          scenario: tests/scenarios/login.json
          output: artifacts/macos-windowed-ax
```

## Recommended matrix

```text
Every PR:
    ubuntu-latest portable-headless
    windows-latest windows-reference smoke

Main/nightly:
    ubuntu-latest full portable-headless matrix
    windows-latest full windows-reference matrix
    optional macos-latest windowed/AX smoke

Release:
    all of the above
    compatibility dashboard
    artifacts archived
```

## What not to do

Do not make this the default:

```yaml
runs-on: macos-latest
mode: portable-headless
```

Portable-headless should run on Linux/Windows CI. macOS CI is for macOS-specific host/AX validation, not for generic headless validation.

## Artifact naming

Every artifact must include lane:

```text
portable-headless/
windows-reference/
macos-windowed-ax/
windows-custom-runtime/
```

Do not mix native reference and custom runtime output.



---

# FILE: LOCAL_DEVELOPMENT_MODES.md

# Local Development Modes

## 1. Local Mac — portable headless

Mac kullanan geliştiricinin en hızlı döngüsü:

```sh
winui3-compat-runner run \
  --mode portable-headless \
  --driver internal \
  --renderer skia-offscreen \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json \
  --output artifacts/local/login
```

Bu mod:

```text
NSWindow açmaz
AX kullanmaz
Metal gerektirmez
Windows gerektirmez
Linux/Windows CI ile aynı davranışı hedefler
```

## 2. Local Mac — windowed debug

Görsel/manual debug için:

```sh
winui3-compat-runner run \
  --mode macos-windowed \
  --driver internal \
  --renderer skia-metal \
  --project src/MyApp/MyApp.csproj
```

Bu mod:

```text
NSApplication çalıştırır
NSWindow açar
Skia/Metal yüzeyine çizer
mouse/keyboard/scroll eventlerini alır
hit-test ve input routing'i gerçek eventlerle dener
```

CI default değildir.

## 3. Local Mac — AX debug

Accessibility/automation bridge için:

```sh
winui3-compat-runner run \
  --mode macos-windowed-ax \
  --driver ax \
  --renderer skia-metal \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu mod:

```text
AutomationCore -> NSAccessibility mapping
AXPress -> Invoke
AXValue -> ValuePattern
AXSelected -> SelectionPattern
```

Doğrular.

## 4. Local Linux — portable headless

Linux geliştiricisi veya CI:

```sh
winui3-compat-runner run \
  --mode portable-headless \
  --driver internal \
  --renderer skia-offscreen \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu gerçek Linux native desktop UI test değildir. Portable WinUI compatibility runtime testidir.

## 5. Local Windows — native reference

Gerçek WinUI reference için:

```sh
winui3-compat-runner run \
  --mode windows-reference \
  --driver flaui-uia3 \
  --renderer native-winui \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu gerçek Windows WinUI app çalıştırır.

## 6. Local Windows — optional custom runtime

Phase 13 sonrası:

```sh
winui3-compat-runner run \
  --mode windows-custom-runtime \
  --driver flaui-uia3 \
  --renderer skia-offscreen \
  --project src/MyApp/MyApp.csproj \
  --scenario tests/scenarios/login.json
```

Bu native WinUI reference değildir. Bizim custom runtime'ın Windows UIA provider'ını test eder.

## Local developer promise

Mac developer için doğru vaat:

```text
Windows'a geçmeden hızlı source-level validation ve screenshot.
Gerekirse macOS windowed debug.
Gerçek Windows truth için windows-reference lane.
```

Linux developer için doğru vaat:

```text
Cheap portable headless UI compatibility tests and screenshots.
```

Windows developer için doğru vaat:

```text
Native WinUI reference and optional custom-runtime provider validation.
```



---

# FILE: SCENARIO_DRIVER_SPEC.md

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



---

# FILE: AUTOMATION_ADAPTERS.md

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



---

# FILE: RENDERING_TEXT_LAYOUT_NOTES.md

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



---

# FILE: CONTROL_SUPPORT_MATRIX_SEED.md

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



---

# FILE: PRODUCT_POSITIONING.md

# Product Positioning

## Correct value proposition

```text
Portable WinUI 3 source-level compatibility runner
for headless automation, screenshot evidence, and Windows native reference comparison.
```

## Target users

### 1. Mac-first .NET developers

Problem:

```text
Android/iOS/macOS/Linux development is available from Mac.
WinUI/Windows path requires Windows machine/runner/VM.
```

Value:

```text
Local Mac source-level WinUI validation without switching to Windows.
```

### 2. Cross-platform .NET / MAUI teams

Problem:

```text
Windows target is the odd lane in otherwise Mac/Linux-friendly development.
```

Value:

```text
Portable headless checks on Linux/Windows CI plus Windows native truth lane.
```

### 3. WinUI control/library developers

Problem:

```text
Need to validate XAML/control behavior/screenshots across many states.
```

Value:

```text
Component-level automation and screenshot artifacts with documented support matrix.
```

### 4. CI-focused teams

Problem:

```text
Windows UI automation is slower/flakier; macOS runners are expensive.
```

Value:

```text
Cheap Linux portable-headless lane catches many errors before native Windows reference.
```

## What this is not

```text
Not a full Windows App SDK replacement.
Not a way to run arbitrary .exe/.msix.
Not guaranteed full WinUI 3 API compatibility.
Not guaranteed pixel-perfect Fluent rendering for all controls.
Not a replacement for Windows native reference validation.
```

## Product language

Use:

```text
source-level
documented subset
portable headless
internal automation
Skia offscreen
Windows native reference
evidence artifacts
compatibility matrix
```

Avoid:

```text
runs any WinUI app
full WinUI on macOS
drop-in Windows replacement
100% pixel perfect
no Windows needed ever
```

## Most valuable product shape

```text
Every PR:
    ubuntu-latest portable-headless
    windows-latest native-reference smoke

Local Mac:
    portable-headless + windowed debug

Nightly/release:
    full Windows native reference
    optional macOS AX/windowed
```

## Adoption risk

If default workflow requires `macos-latest`, adoption decreases.

If default workflow runs on `ubuntu-latest` and Windows reference runs on `windows-latest`, adoption improves.

## Demo that matters

```sh
winui3-compat-runner run \
  --mode portable-headless \
  --driver internal \
  --renderer skia-offscreen \
  --project samples/LoginApp/LoginApp.csproj \
  --scenario scenarios/login.json \
  --output artifacts/login
```

Outputs:

```text
scenario-result.json
automation-tree.json
layout-bounds.json
after-login.png
diagnostics.md
```

Then:

```sh
winui3-compat-runner run \
  --mode windows-reference \
  --driver flaui-uia3 \
  --renderer native-winui \
  --project samples/LoginApp/LoginApp.csproj \
  --scenario scenarios/login.json \
  --output artifacts/windows-login
```

Outputs native truth.

## Final positioning sentence

> Test and visually validate WinUI 3 source code without making Windows the only feedback loop, while keeping real Windows WinUI as the source of truth.
