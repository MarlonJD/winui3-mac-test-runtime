# TODO

This file is the short execution queue for the current repository state. The
durable architecture and phase details live in `docs/plans/index.md`,
`docs/architecture/portable-headless-roadmap.md`, and
`docs/portable-headless/MANIFEST.md`.

## Now

1. Phase 7 Skia offscreen renderer hardening
   - Produce portable PNG screenshots and renderer metadata from arranged trees.
   - Keep screenshot generation independent of AppKit, Metal, AX, and hosted
     macOS CI.
   - Connect scenario screenshot requests to renderer artifacts without making
     behavior assertions depend on pixels.

## Next

2. Direct ingestion Track C
   - Add the UIA/FlaUI-compatible artifact adapter over `tree.json`,
     `accessibility.json`, and `interactions.json`.
   - Keep it separate from native Windows FlaUI/UIA3 reference evidence.

3. EMSI direct runtime UI gap closure Phase 2
   - Add fail-first direct Login `DataContext` bootstrap tests.
   - Implement the bootstrap without backend calls or credentials.

4. Phase 8 portable-headless CI lane.

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
