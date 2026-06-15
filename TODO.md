# TODO

This file is the short execution queue for the current repository state. The
durable architecture and phase details live in `docs/plans/index.md`,
`docs/architecture/portable-headless-roadmap.md`, and
`docs/portable-headless/MANIFEST.md`.

## Now

1. Phase 13 optional Windows custom-runtime UIA provider
   - Add a Windows-only provider lane for this custom runtime.
   - Keep it separate from `windows-reference`, which remains real native WinUI.
   - Do not treat it as the source of truth for native WinUI behavior.

## Next

2. Direct ingestion Track C
   - Add the UIA/FlaUI-compatible artifact adapter over `tree.json`,
     `accessibility.json`, and `interactions.json`.
   - Keep it separate from native Windows FlaUI/UIA3 reference evidence.

3. EMSI direct runtime UI gap closure Phase 2
   - Add fail-first direct Login `DataContext` bootstrap tests.
   - Implement the bootstrap without backend calls or credentials.

4. Phase 14 broader controls and states.

## Done / Archived

Historical plans that are no longer active live under
`docs/plans/complete/`. Keep new active plans in `docs/plans/` and move them to
`docs/plans/complete/` only after their remaining follow-up has either shipped
or been superseded by a newer plan.

- Phase 1 portable runtime core boundary: dependency contract and source scanner
  added in `PortableHeadlessBoundary` and `PortableHeadlessBoundaryTests`.
- Phase 2 portable XAML/source materialization MVP: `PortableXamlMaterialization`
  contract and tests now cover the MVP element subset, resource lookup,
  unsupported markup diagnostics, and simple binding mode parsing.
- Phase 3 portable layout and bounds MVP:
  `Phase3PortableLayoutTests` and `Phase3XamlLayoutMaterializationTests` now
  guard `ContentPresenter` materialization and deterministic single-slot child
  bounds through the portable `VisualLayoutEngine`.
- Phase 4 portable text layout MVP:
  `WinUITextLayout` and `Phase4PortableTextLayoutTests` now guard NoWrap, Wrap,
  WrapWholeWords, line metrics, TextBlock desired height from line count, and
  Skia wrapped TextBlock line-by-line rendering.
- Phase 5 AutomationCore MVP:
  `AutomationCore` and `Phase5AutomationCoreTests` now guard renderer-independent
  automation nodes, layout bounds, enabled/focus/offscreen state, and
  Invoke/Value/Toggle/SelectionItem/Scroll pattern metadata.
- Phase 6 internal scenario driver:
  `InternalScenarioDriver` and `Phase6InternalScenarioDriverTests` now guard
  JSON scenario parsing, portable action/assertion results, direct
  `AutomationCore` state mutation, wait/screenshot recording, and no OS-level UI
  automation dependency.
- Phase 7 Skia offscreen renderer hardening:
  `SkiaV2SnapshotRenderer` and `Phase7SkiaOffscreenRendererTests` now guard
  portable PNG output plus `*.metadata.json` sidecars with mode, lane, driver,
  renderer, viewport, scale, theme, platform, font profile, text measurement,
  and image integrity metadata.
- Phase 8 portable-headless CI lane:
  `.github/workflows/ci.yml` and `Phase8PortableHeadlessCiTests` now guard a
  default `ubuntu-latest` `portable-headless` job with internal driver,
  Skia-offscreen renderer metadata, targeted portable tests, strict scenario
  artifact generation, PNG/metadata contract validation, and
  `portable-headless-artifacts` upload without hosted macOS CI.
- Phase 9 Windows native reference lane:
  `.github/workflows/windows-native-screenshot.yml` and
  `Phase9WindowsReferenceLaneTests` now guard the `windows-latest`
  `windows-reference` job as the native WinUI source of truth, with
  `native-winui` runtime/renderer metadata, `flaui-uia3` driver metadata,
  native reference PNG/JSON artifacts, and separate
  `windows-reference-screenshots` upload.
- Phase 10 comparison dashboard:
  `PortableHeadlessComparisonDashboard`,
  `portable-headless-dashboard`, and `Phase10ComparisonDashboardTests` now guard
  JSON/Markdown evidence comparing `portable-headless` scenario results,
  automation nodes, bounds tolerance, and visual diff metrics against
  `windows-reference` native WinUI artifacts with actionable diagnostics.
- Phase 11 macOS windowed host:
  `MacOsWindowedHostScaffold`, `macos-windowed-host`, and
  `MacOsWindowedHostTests` now guard a local/manual AppKit window scaffold that
  displays Skia runtime artifacts, writes `macos-windowed-host.json`, logs
  mouse/key/scroll events to `macos-windowed-events.jsonl`, performs coordinate
  conversion and hit-test lookup against `tree.json`, and keeps AX/Metal out of
  this phase and out of default PR CI.
- Phase 12 macOS AX adapter:
  `MacOsAxAdapterScaffold`, `macos-ax-adapter`, and `MacOsAxAdapterTests` now
  guard an optional/local `AutomationCore` to `NSAccessibilityElement` scaffold,
  role/action/value mapping for the MVP accessibility set, `macos-ax-tree.json`,
  `MacOsAxAdapter.swift`, and `macos-windowed-ax-adapter.json` without making AX
  part of portable-headless or default PR CI.
