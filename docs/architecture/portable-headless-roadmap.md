# Portable Headless Runtime Roadmap

The updated architecture makes `portable-headless` the default headless runtime
direction. Headless compatibility must be portable across `ubuntu-latest`,
`windows-latest`, and local macOS instead of depending on AppKit,
`NSWindow`/`NSView`, NSAccessibility/AX, Metal, or hosted macOS CI.

The current repository remains a source-level WinUI 3 compatibility harness for
a documented subset. It does not run arbitrary `.exe` or `.msix` outputs, does
not claim full WinUI 3 API coverage, and does not claim native WinUI visual
fidelity. Windows native reference evidence remains the source of truth.

## Architecture Direction

- Runtime core produces logical tree, layout, bounds, render tree, automation
  tree, diagnostics, and artifact metadata without a platform UI dependency.
- `AutomationCore` owns semantic nodes and patterns. Platform drivers and
  adapters map to it instead of defining behavior themselves.
- The internal driver calls `AutomationCore` directly for headless scenarios.
- Skia offscreen is the portable screenshot renderer for headless artifacts.
- Skia-on-Metal is reserved for a future macOS windowed host.
- Raw Metal is not the primary rendering model.
- Scenario files stay driver independent so the same scenario can run against
  portable headless, Windows native reference, and later macOS AX lanes.

## Do Not Confuse These Modes

| Mode | Purpose | Driver | Renderer | CI role |
| --- | --- | --- | --- | --- |
| `portable-headless` | Portable source-level runtime checks and screenshots | internal `AutomationCore` driver | Skia offscreen | Default fast lane on `ubuntu-latest`; also valid on Windows and local macOS |
| `windows-reference` | Real native WinUI source of truth | FlaUI.UIA3 client against real WinUI | native WinUI | Default reference lane on `windows-latest` |
| `macos-windowed` | Local/manual Mac visual and input debugging | internal driver | Skia windowed, later Skia-on-Metal | Local/manual only |
| `macos-windowed-ax` | Later macOS platform accessibility bridge validation | AX/NSAccessibility adapter mapped from `AutomationCore` | macOS windowed renderer | Manual, scheduled, release, or self-hosted Mac only |
| `windows-custom-runtime` | Optional later UIA provider over this custom runtime on Windows | FlaUI.UIA3 against this runtime's provider | custom runtime renderer | Later phase; not native WinUI truth |

`windows-reference` and `windows-custom-runtime` must remain separate artifact
lanes. FlaUI against real WinUI proves native behavior. FlaUI against the custom
runtime proves the custom provider.

## Phase Roadmap

| Phase | Theme | Outcome |
| --- | --- | --- |
| 0 | Documentation and architecture alignment | Planning package is tracked, README claims are honest, and default CI strategy is documented |
| 1 | Portable runtime core boundary | Core dependency boundary is guarded by `PortableHeadlessBoundaryTests` |
| 2 | XAML/source materialization MVP | MVP element subset, resources, unsupported diagnostics, and binding mode parsing are guarded by `Phase2XamlMaterializationTests` |
| 3 | Layout engine MVP | `ContentPresenter` materialization and deterministic single-slot bounds are guarded by Phase 3 tests |
| 4 | Portable text layout MVP | `WinUITextLayout` guards NoWrap/Wrap/WrapWholeWords, line metrics, TextBlock desired height, and wrapped Skia text rendering |
| 5 | `AutomationCore` MVP | `AutomationCore` guards semantic nodes, layout bounds, state, and Invoke/Value/Toggle/SelectionItem/Scroll pattern metadata |
| 6 | Internal scenario driver | JSON scenarios run through `AutomationCore` actions/assertions without OS automation |
| 7 | Skia offscreen renderer | Portable PNG screenshots and metadata sidecars are guarded |
| 8 | Portable-headless CI lane | `ubuntu-latest` portable checks and PNG/metadata artifact upload are guarded |
| 9 | Windows native reference lane | `windows-latest` native WinUI/FlaUI.UIA3 artifacts are guarded with explicit `windows-reference` lane metadata |
| 10 | Comparison dashboard | JSON/Markdown dashboard compares portable scenario, automation, bounds, and visual evidence with Windows native reference artifacts |
| 11 | macOS windowed host | Local/manual AppKit window scaffold displays Skia artifacts and logs internal-driver input without becoming default PR CI |
| 12 | macOS AX adapter | `AutomationCore` maps to a local/manual NSAccessibility scaffold with role/action/value metadata and no default PR CI coupling |
| 12.5 | macOS windowed live interaction loop | Windowed host maps click/key events to local runtime node state, redraws focus/press/toggle/selection/text overlays, and writes live state evidence |
| 13 | Optional Windows custom-runtime UIA provider | Windows-only custom-runtime UIA provider scaffold maps `AutomationCore` to UIA ControlTypes/patterns while staying distinct from native reference |
| 14 | Broader controls and states | `broader-control-state-coverage` publishes a guarded compatibility dashboard for ComboBox, ListView, InfoBar, Flyout, ContentDialog, Slider, ProgressRing, and ProgressBar with default/hover/pressed/disabled/focused/selected state separation and supported/partial/planned buckets |
| 15 | Release hardening | `release-hardening-manifest` guards external developer docs, no-app-source-change demo commands, Linux portable-headless plus Windows reference workflow guidance, known gaps, baseline management, artifact retention, and versioned compatibility matrix links |

## Current Command Boundary

Phase 0 does not rename the existing `winui3-mac-runner` command or introduce a
breaking `--mode` requirement. Existing commands continue to express the current
alpha behavior. Future phases can add canonical mode metadata and flags once the
portable core, drivers, and artifact lanes exist.

## Source Documents

The complete planning set is tracked under `docs/portable-headless/`, with
`docs/portable-headless/MANIFEST.md` as the repo-native inventory. The most
important source documents are:

- `docs/portable-headless/MANIFEST.md`
- `docs/portable-headless/RUNTIME_RULES.md`
- `docs/portable-headless/ARCHITECTURE_DECISIONS.md`
- `docs/portable-headless/AUTOMATION_ADAPTERS.md`
- `docs/portable-headless/CI_STRATEGY.md`
- `docs/portable-headless/CODEX_PHASE_PLAN.md`
- `docs/portable-headless/PRODUCT_POSITIONING.md`
