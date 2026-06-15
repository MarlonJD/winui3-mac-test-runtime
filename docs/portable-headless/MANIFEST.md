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
| 7-15 | planned | Implementation still needs to proceed phase by phase from `CODEX_PHASE_PLAN.md` |

## Source Integrity Snapshot

The original package files were copied into this directory as Markdown. The
archive itself does not need to be retained because the file list and content
are now represented in version control.
