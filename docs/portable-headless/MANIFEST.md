# Portable Headless Manifest

This directory is the repo-native source for the updated portable headless
architecture plan. The external archive is no longer required to continue the
work.

## Completeness

All planning files from the supplied package are present here:

| File | Role |
| --- | --- |
| `README_INDEX.md` | Original package index and high-level intent |
| `RUNTIME_RULES.md` | Mandatory runtime boundaries and product-claim rules |
| `ARCHITECTURE_DECISIONS.md` | ADR-style decisions for portable headless, automation, rendering, CI, and product positioning |
| `CODEX_WORK_RULES.md` | Agent implementation rules and phase discipline |
| `CODEX_PHASE_PLAN.md` | Phase 0 through Phase 15 execution roadmap |
| `CI_STRATEGY.md` | Default Linux/Windows lanes and optional macOS policy |
| `LOCAL_DEVELOPMENT_MODES.md` | Local Mac, Linux, and Windows mode definitions |
| `SCENARIO_DRIVER_SPEC.md` | Shared scenario/action/assertion contract |
| `AUTOMATION_ADAPTERS.md` | AutomationCore, internal driver, FlaUI/UIA3, macOS AX, and optional Windows UIA provider split |
| `RENDERING_TEXT_LAYOUT_NOTES.md` | Skia offscreen, Metal role, render tree, and text layout notes |
| `CONTROL_SUPPORT_MATRIX_SEED.md` | MVP control and pattern support matrix |
| `PRODUCT_POSITIONING.md` | Correct product language, non-goals, and adoption framing |
| `ALL_IN_ONE_RULES_AND_PLAN.md` | Single-file copy of the full package content |

The repo adds these navigation/summary files on top:

| File | Role |
| --- | --- |
| `README.md` | Repo-local reading order and Phase 0 boundary |
| `../architecture/portable-headless-roadmap.md` | Top-level roadmap and mode separation summary |
| `../architecture/ci-strategy.md` | Repo-facing CI policy summary |

## How To Continue

Use this order for future phases:

1. Read `../architecture/portable-headless-roadmap.md` for the current phase and
   mode boundaries.
2. Read the relevant source docs in this directory.
3. Keep public commands stable unless a phase explicitly adds a compatible new
   mode flag or metadata field.
4. Preserve existing release gates and README honesty claims.
5. Run verification before marking a phase complete.

## Phase Status

| Phase | Status | Notes |
| --- | --- | --- |
| 0 | repo-docs-aligned | Planning set is tracked in the repo; README and CI docs point at portable headless as the default architecture |
| 1 | boundary-guarded | `PortableHeadlessBoundary` and `PortableHeadlessBoundaryTests` define and test the portable core dependency boundary |
| 2 | materialization-guarded | `PortableXamlMaterialization` and `Phase2XamlMaterializationTests` cover the MVP element subset, resource lookup, unsupported diagnostics, and binding mode parsing |
| 3 | layout-guarded | `ContentPresenter` is materialized and arranged as a deterministic portable single-slot surface by `Phase3XamlLayoutMaterializationTests` and `Phase3PortableLayoutTests` |
| 4 | text-layout-guarded | `WinUITextLayout` and `Phase4PortableTextLayoutTests` cover NoWrap, Wrap, WrapWholeWords, line metrics, TextBlock desired height, and wrapped Skia text rendering |
| 5 | automation-core-guarded | `AutomationCore` and `Phase5AutomationCoreTests` cover semantic nodes, layout bounds, enabled/focus/offscreen state, and Invoke/Value/Toggle/SelectionItem/Scroll pattern metadata |
| 6 | internal-scenario-driver-guarded | `InternalScenarioDriver` and `Phase6InternalScenarioDriverTests` cover JSON scenario parsing, AutomationCore-backed actions/assertions, state mutation, wait, and screenshot recording without OS automation |
| 7 | skia-offscreen-metadata-guarded | `SkiaV2SnapshotRenderer` and `Phase7SkiaOffscreenRendererTests` cover portable PNG output and `*.metadata.json` sidecars with mode, lane, driver, renderer, viewport, scale, theme, platform, font profile, text measurement, and image integrity metadata |
| 8 | portable-headless-ci-guarded | `.github/workflows/ci.yml` and `Phase8PortableHeadlessCiTests` cover the default `ubuntu-latest` portable-headless job, internal driver/skia-offscreen metadata, targeted portable tests, PNG/metadata artifact validation, and `portable-headless-artifacts` upload without hosted macOS CI |
| 9 | windows-reference-lane-guarded | `.github/workflows/windows-native-screenshot.yml` and `Phase9WindowsReferenceLaneTests` cover the `windows-latest` native WinUI reference job, `windows-reference` lane metadata, `native-winui` runtime/renderer metadata, `flaui-uia3` driver metadata, native PNG/JSON references, and separate `windows-reference-screenshots` upload |
| 10 | comparison-dashboard-guarded | `PortableHeadlessComparisonDashboard`, `portable-headless-dashboard`, and `Phase10ComparisonDashboardTests` cover JSON/Markdown reports for scenario result, automation node, bounds tolerance, and visual diff comparisons between `portable-headless` and `windows-reference` artifacts |
| 11 | macos-windowed-host-guarded | `MacOsWindowedHostScaffold`, `macos-windowed-host`, and `MacOsWindowedHostTests` cover a local/manual AppKit window scaffold over Skia runtime artifacts, coordinate conversion, hit-test event logging, `macos-windowed` metadata, and no AX/Metal/default-PR-CI coupling |
| 12 | macos-ax-adapter-guarded | `MacOsAxAdapterScaffold`, `macos-ax-adapter`, and `MacOsAxAdapterTests` cover optional/local `AutomationCore` to `NSAccessibilityElement` role/action/value mapping, `macos-ax-tree.json`, `MacOsAxAdapter.swift`, `macos-windowed-ax-adapter.json`, and no portable-headless/default-PR-CI coupling |
| 12.5 | macos-windowed-live-interaction-guarded | `macos-windowed-host` generated Swift now maps mouse/key events to local runtime node state, redraws focus/press/toggle/selection/text overlays, writes `macos-windowed-live-state.json`, and remains local/manual rather than default PR CI |
| 13 | windows-custom-runtime-uia-provider-guarded | `WindowsCustomRuntimeUiaProviderScaffold`, `windows-custom-runtime-uia`, and `WindowsCustomRuntimeUiaProviderTests` cover `AutomationCore` to Windows UIA ControlType/pattern mapping, provider source scaffolding, `windows-custom-runtime-uia-tree.json`, and lane metadata that is explicitly not `windows-reference` |
| 14 | broader-control-state-coverage-guarded | `BroaderControlStateCoverageBuilder`, `broader-control-state-coverage`, and `docs/visual-parity/broader-control-state-coverage.json` cover ComboBox, ListView, InfoBar, Flyout, ContentDialog, Slider, ProgressRing, and ProgressBar with explicit default/hover/pressed/disabled/focused/selected state coverage and supported/partial/planned separation |
| 15 | release-hardening-guarded | `ReleaseHardeningManifestBuilder`, `release-hardening-manifest`, `docs/release/release-hardening-manifest.json`, `docs/release/phase-15-release-hardening.md`, and `docs/release/sample-workflows.md` cover external developer docs, no-app-source-change demo commands, Linux portable-headless plus Windows reference workflow guidance, known gaps, baseline management, artifact retention, and versioned compatibility matrix links |

## Source Integrity Snapshot

The original package files were copied into this directory as Markdown. The
archive itself does not need to be retained because the file list and content
are now represented in version control.
