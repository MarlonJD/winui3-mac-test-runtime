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

## Phase 12.5 — macOS windowed live interaction loop

### Amaç

`macos-windowed` host'u sadece PNG viewer olmaktan çıkarıp local/manual canlı
debug yüzeyi haline getirmek.

### İşler

- Window mouse/key/scroll events -> runtime hit-test node.
- Click/type/toggle/select state -> local live interaction state.
- Focus, button press, checkbox/toggle, selection, and text input visual
  overlays.
- Window redraw after state mutation.
- `macos-windowed-live-state.json` evidence.
- Keep this local/manual; do not add hosted macOS PR CI.
- Keep AX adapter separate from this host loop.

### Done

- Button click produces visible press/focus feedback.
- TextBox focus/type updates visible text overlay.
- CheckBox/Toggle state changes visually.
- `macos-windowed-events.jsonl` and `macos-windowed-live-state.json` are
  produced.
- This remains optional/manual/local and not default PR CI.

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

- `windows-custom-runtime-uia` command produces provider scaffold artifacts.
- `AutomationCore` nodes map to Windows UIA ControlType and pattern names.
- Provider source exposes Invoke/Value/Toggle/Selection/Scroll provider surface.
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

- `broader-control-state-coverage` command produces the Phase 14 dashboard.
- `docs/visual-parity/broader-control-state-coverage.json` tracks ComboBox,
  ListView, InfoBar, Flyout, ContentDialog, Slider, ProgressRing, and
  ProgressBar.
- The dashboard names default, hover, pressed, disabled, focused, and selected
  coverage per control.
- Supported/partial/planned ayrımı açık.
- VisualStateManager, resource/theme, and accessibility-pattern expansion
  coverage is visible without claiming native WinUI visual fidelity.

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

- External developer docs hazır:
  `docs/release/phase-15-release-hardening.md` and
  `docs/release/sample-workflows.md`.
- `release-hardening-manifest` command produces
  `docs/release/release-hardening-manifest.json`.
- Release-candidate checks the release hardening manifest freshness.
- “No app source change within supported subset” demo commands are documented
  for `fixtures/PublicAdminWorkbench.WinUI`.
- Linux portable-headless + Windows reference sample workflow hazır.
- Known gaps, baseline management, artifact retention guidance, and versioned
  compatibility matrix links are tracked without expanding the support claim.
