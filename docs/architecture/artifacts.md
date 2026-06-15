# Runtime Artifacts

The macOS runner writes stable JSON artifacts under the requested output
directory. The files are intended for automation first and human inspection
second.

Every runner-owned JSON artifact includes a `schemaVersion`. Schema versions
are additive contracts for public automation; incompatible shape changes require
a new schema version and documentation update.

## Schema Versions

| Artifact | Schema version |
| --- | --- |
| `run.json` | `0.1` |
| `tree.json` | `0.1`; `0.2` when `skia-v2` layout metadata is exported |
| `accessibility.json` | `0.3` |
| `binding-failures.json` | `0.1` |
| `visual/component-evidence.json` | `0.5` |
| `component-inspection.json` | `0.2` |
| `component-inspection-template.json` | `0.2` |
| `docs/visual-parity/component-quality-dashboard.json` | `0.3` |
| `docs/visual-parity/state-coverage-matrix.json` | `0.1` |
| `docs/visual-parity/native-quality-family-tranches.json` | `0.1` |
| `docs/visual-parity/public-visual-review-index.json` | `0.2` |
| `native-reference-import.json` | `0.2` |
| `resource-failures.json` | `0.1` |
| `unsupported-apis.json` | `0.1` |
| `project-ingestion.json` | `0.1` |
| `interactions.json` | `0.3` |
| `snapshot.json` | `0.1`; `0.2` for `skia-v2` PNG snapshots |
| `visual/visual-run.json` | `0.1` |
| `visual/visual-review.json` | `0.3` |
| `visual/windows-reference.json` | `0.2` |
| `visual/pixel-diff.json` | `0.1` |

## Files

- `run.json`: run metadata, host details, Wine optionality, and artifact paths.
- `tree.json`: logical UI tree with stable type names, element names, selected
  state, visibility, focus state, and important content properties.
- `accessibility.json`: role/name/automation ID/label/help/focus/focusable/
  enabled/checked/selected/expanded/value tree derived from the logical tree.
- `binding-failures.json`: binding paths that could not be resolved or applied,
  written as `{ "schemaVersion": "0.1", "failures": [...] }`.
- `resource-failures.json`: static or theme resources that could not be
  resolved, written as `{ "schemaVersion": "0.1", "failures": [...] }`.
- `unsupported-apis.json`: facade APIs that were touched but are unavailable in
  the current alpha, written as `{ "schemaVersion": "0.1", "apis": [...] }`.
  Each entry status comes from the compatibility catalog when known
  (`planned`, `windows-only`, or `not supported`) or `unknown` when the API is
  not cataloged yet.
- `project-ingestion.json`: emitted for Windows-targeted WinUI source projects
  inspected for direct project ingestion. It records the original project, the
  generated temporary source-level host (`generatedHostPath` and
  `generatedHostProjectPath`) under
  `/private/tmp/winui3-mac-test-runtime/generated-hosts/`, the existing compat
  shadow project path when a run proceeds into build/launch, included C# and
  XAML files, excluded Windows-only items such as `Microsoft.WindowsAppSDK`,
  catalog statuses for project features, unsupported project features,
  blocking XAML diagnostics, and `windowsOnlyBoundaries`. The generated host is a
  macOS source-level harness; it is not a Windows `.exe` or `.msix` execution
  path.
  - `windowsOnlyBoundaries`: non-blocking diagnostics for Windows-only behavior
    found in the project source/XAML, package references, and project
    properties. Each entry records the boundary category (`windows-storage`,
    `windows-credentials`, `packaged-activation`, `system-backdrop`, or
    `windows-app-sdk-deployment`), the cataloged `api` and `status`, the matched
    `symbol`, the `filePath`/`line` when the boundary comes from source/XAML, a
    human-readable `reason`, and `blocksRender`. These diagnostics are honest:
    `blocksRender` is `false` so a supported page/window surface still renders
    while the runner reports what it skipped. Examples for a real WinUI app are
    `Windows.Storage.ApplicationData`, `Windows.Security.Credentials.PasswordVault`,
    `MicaBackdrop`/`SystemBackdrop`, packaged MSIX activation, and the
    `Microsoft.WindowsAppSDK`/`Microsoft.Windows.SDK.BuildTools` deployment
    references. The runner does not execute, emulate, or claim parity for any of
    these Windows-only boundaries on macOS.
- `diagnostics.sarif`: warning-level diagnostics derived from binding, resource,
  and unsupported API reports.
- `native-reference-import.json`: normalized import manifest for downloaded
  `windows-reference-screenshots` artifacts. It records every imported native
  reference, copied reference paths, provenance, missing component parity
  scenarios, and validation problems before local review runs use those
  references.
- `interactions.json`: emitted when `--script` or scenario interactions are
  provided; records every scripted action, semantic selector kind, target type,
  expected/actual values, observed target state, and before/after state for
  state-changing actions. Supported action types are `click`, `focus`,
  `typeText`, `selectItem`, `assertProperty`, `assertAccessibilityState`,
  `selectNavigation`, `navigateFrame`, `invokeAccelerator`, `openPopup`,
  `dismissPopup`, `invokeMenuItem`, and `waitForIdle`.
- UI automation evidence currently comes from `tree.json`,
  `accessibility.json`, `interactions.json`, screenshots, component crops, and
  pixel diffs. FlaUI 5.0 + FlaUI.UIA3 is the native Windows validation target,
  while macOS support requires a repo-owned FlaUI.UIA3-compatible adapter over
  these artifacts before any full FlaUI/UIA claim can be made.
- `snapshot.json`: renderer metadata for the deterministic snapshot.
- `screenshots/snapshot.svg`: nonblank deterministic visual representation of
  the logical tree from the default SVG renderer.
- `screenshots/snapshot.png`: nonblank deterministic PNG from the optional Skia
  renderer when `--renderer skia` is passed.
- `screenshots/mac-runtime.png`: deterministic PNG from the strict
  scenario-driven `skia-v2` renderer.
- `visual/visual-run.json`: scenario name, fixture name, runner OS, renderer,
  viewport, scale, theme, threshold configuration, unsupported visual features,
  reference/runtime/diff paths, component crop directory, copied reference
  provenance, visual review page path, comparison metrics, and pass/fail
  status.
- `visual/component-evidence.json`: component parity lab evidence with
  component/source-feature catalog status, presence, interaction status, visual
  grade, effective per-component thresholds, target layout region, crop paths,
  blank-crop status, native reference provenance, known gaps, and optional
  reference or crop diff metrics.
- `component-inspection.json`: reviewer-supplied manifest consumed by
  `component-inspection-apply`. Each row must name the component and target,
  honest `visualGrade`, `nativeQualityGrade`, reviewer, inspection date, native
  reference run ID, notes, optional accepted gaps, optional tolerance reason,
  and optional comparison artifact paths. The apply command validates documented
  harness grades, excludes planned or not-supported rows from promotion,
  requires promotion-valid native bounds before `good` or `production-ready`
  native-quality grades, and checks crop triptychs, component diff metrics,
  native reference provenance, run ID consistency, and artifact paths before it
  writes updated component evidence.
- `component-inspection-template.json`: generated reviewer starting point from
  `component-inspection-template`. It pre-fills component/target identity,
  native reference run ID, and comparison artifact paths, but leaves reviewer,
  date, notes, and final grades as `TODO` placeholders so
  `component-inspection-apply` rejects the template until manual inspection is
  complete.
- `visual/visual-review.html` and `visual/visual-review.json`: manual review
  artifacts generated from component evidence. Each component row places the
  native WinUI crop, macOS runtime crop, and pixel diff crop side by side when
  those crops exist, shows native reference source/run/commit provenance, and
  records missing crop or inspection metadata without promoting the row.
- `docs/visual-parity/public-visual-review-index.html` and
  `docs/visual-parity/public-visual-review-index.json`: generated public
  inspection queue for checked-in component evidence. It links each public row
  to its native, macOS, and diff crops plus the scenario visual review page and
  inspection template, embeds compact native/macOS/diff crop previews with
  component diff metrics, and carries the current component-quality dashboard
  blocker without promoting the row.
- `docs/visual-parity/state-coverage-matrix.json`: generated state,
  interaction, and accessibility matrix for `productionStateCoverage`
  requirements. It joins checked-in component evidence with required states and
  labels default-only or missing-default evidence so source-level usability is
  not confused with production-ready state coverage. Requirement rows also name
  the strict-sweep component evidence, accessibility, and visual-run artifact
  paths that the `public-product` evidence gate must validate.
- `docs/visual-parity/native-quality-family-tranches.json`: generated
  Milestone C family queue. It groups component evidence rows into selection
  controls, button/link, dropdown/menu, text/forms, navigation/list, and
  status/progress families, then blocks family promotion while native-quality
  inspection evidence, crop-threshold closure, or broader state coverage is
  missing. Family rows publish state requirement counts, missing requirement
  counts, required state names, and strict-sweep scenario names so the Milestone
  C work queue stays linked to the Milestone D state matrix. For
  `not-evaluated` rows with failed component crops, the row `remainingBlocker`
  includes the failed crop status and threshold-exceeding metric values.
- `visual/windows-reference.png`: copy of the Windows-hosted reference
  screenshot captured by the public workflow or supplied with `--reference`.
  Current checked-in examples are synthetic `WindowsNativeProbe` captures, not
  native WinUI fixture captures; production visual claims require provenance
  that identifies the reference as native WinUI.
- `visual/windows-reference.json`: reference provenance copied from the Windows
  capture artifact when available. It records `referenceSource`
  (`native-winui` or `synthetic-probe`), fixture project path, scenario path and
  name, commit SHA, workflow run, runner image, viewport, scale, theme, window
  title, capture mode, and captured dimensions.
- `visual/mac-runtime.png`: copy of the `skia-v2` runtime screenshot used for
  comparison.
- `visual/pixel-diff.png`: red-highlight PNG showing changed pixels.
- `visual/pixel-diff.json`: dimensions, changed pixel count and percentage,
  max channel delta, mean absolute error, root mean squared error, changed
  pixel bounds, thresholds, and pass/fail status.

## Visual Scenarios

Scenario files live under `fixtures/<FixtureName>/scenarios/*.json`. They are
the public contract for pixel-level comparison and include the fixture name,
scenario name, viewport, scale, theme, strict visual mode, interaction actions,
and thresholds.

`--renderer skia-v2` enables the stricter path. The runner exports deterministic
layout rectangles into `tree.json`, writes `mac-runtime.png`, records
unsupported visual features in `unsupported-apis.json`, and writes
`visual-run.json`. When component evidence is available, it also writes
`visual-review.html` and `visual-review.json` for manual crop inspection. When
`--reference` is supplied, it also copies the reference to
`windows-reference.png` and writes pixel diff artifacts. `--reference` may be a
single PNG or a normalized `native-reference-import` directory; directory
references are resolved by scenario from `native-reference-import.json` or
adjacent native WinUI provenance.

When `--project` points at a Windows-targeted WinUI source project, the runner
does not mutate or build the original Windows project. Project inspection writes
a generated temporary source-level host under `/private/tmp`, then the existing
compat shadow build path can retarget supported source/XAML to the managed
macOS facade and write `project-ingestion.json` before launch. Unsupported
project or XAML features are reported with catalog-backed diagnostics. Direct
page/window selection and semantic automation are separate runtime phases from
this host-generation contract.

`--strict-visual` fails the process when binding failures, resource failures,
unsupported facade APIs, unsupported visual painters, failed interactions, or
pixel metrics exceed the scenario thresholds. Without `--reference`, pixel diff
status is recorded as skipped so local strict fixture smoke runs can still
validate the supported subset and renderer output.

The public Windows reference workflow currently captures and compares one light
scenario from each strict fixture category: shell, interaction/binding, and
control gallery, plus the public admin/workbench fixture and component parity
lab pages. Today the Windows-side reference executable is
`WindowsNativeProbe`, which draws synthetic public reference screens. These
artifacts are harness smoke evidence, not native WinUI visual parity evidence.
Each category uploads reviewable `windows-reference.png`, `mac-runtime.png`,
`pixel-diff.png`, `pixel-diff.json`, `visual-run.json`, and component lab
`component-evidence.json` artifacts where applicable. When a
`windows-reference.json` metadata file is available, its native reference
provenance is copied into every generated component crop row.

## Diagnostics

`diagnostics.sarif` emits stable rule IDs so CI and consumers can distinguish
unsupported behavior from fixture or environment drift:

- `WINUI3MAC001`: binding path or target property failure.
- `WINUI3MAC002`: static or theme resource lookup failure.
- `WINUI3MAC003`: unavailable, planned, Windows-only, not supported, unknown,
  or unsupported facade/visual compatibility API.

## Compatibility Position

Artifacts describe the compatibility runtime's current alpha subset and its
cataloged gaps. They are not a claim of complete WinUI 3 behavior or Windows
binary compatibility. Unavailable APIs are reported structurally so callers can
decide whether to fail a smoke run or track the gap as compatibility debt.
Snapshot output is deterministic smoke evidence for a supported control subset,
not a full Fluent, material, or compositor renderer.
Native WinUI Windows reference screenshots are the intended source of truth for
scenario pixel comparison. Current synthetic probe screenshots are harness
smoke references only. Reference artifacts are captured from generic public
fixture content in public GitHub Actions runs, not from private products or
private screenshots.
