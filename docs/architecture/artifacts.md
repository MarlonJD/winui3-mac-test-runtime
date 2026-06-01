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
| `accessibility.json` | `0.2` |
| `binding-failures.json` | `0.1` |
| `resource-failures.json` | `0.1` |
| `unsupported-apis.json` | `0.1` |
| `project-ingestion.json` | `0.1` |
| `interactions.json` | `0.1` |
| `snapshot.json` | `0.1`; `0.2` for `skia-v2` PNG snapshots |
| `visual/visual-run.json` | `0.1` |
| `visual/pixel-diff.json` | `0.1` |

## Files

- `run.json`: run metadata, host details, Wine optionality, and artifact paths.
- `tree.json`: logical UI tree with stable type names, element names, selected
  state, visibility, focus state, and important content properties.
- `accessibility.json`: role/name/label tree derived from the logical tree.
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
  that use compat shadow build discovery. It records the original project, the
  generated shadow project, included C# and XAML files, excluded Windows-only
  items such as `Microsoft.WindowsAppSDK`, catalog statuses for project
  features, unsupported project features, and blocking XAML diagnostics.
- `diagnostics.sarif`: warning-level diagnostics derived from binding, resource,
  and unsupported API reports.
- `interactions.json`: emitted when `--script` is provided; records every
  scripted action and its result. Supported action types are `click`, `focus`,
  `typeText`, `selectItem`, `assertProperty`, `selectNavigation`,
  `navigateFrame`, and `invokeAccelerator`.
- `snapshot.json`: renderer metadata for the deterministic snapshot.
- `screenshots/snapshot.svg`: nonblank deterministic visual representation of
  the logical tree from the default SVG renderer.
- `screenshots/snapshot.png`: nonblank deterministic PNG from the optional Skia
  renderer when `--renderer skia` is passed.
- `screenshots/mac-runtime.png`: deterministic PNG from the strict
  scenario-driven `skia-v2` renderer.
- `visual/visual-run.json`: scenario name, fixture name, runner OS, renderer,
  viewport, scale, theme, threshold configuration, unsupported visual features,
  reference/runtime/diff paths, comparison metrics, and pass/fail status.
- `visual/windows-reference.png`: copy of the real Windows reference screenshot
  captured by the public `windows-latest` workflow or supplied with
  `--reference`.
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
`visual-run.json`. When `--reference` is supplied, it also copies the reference
to `windows-reference.png` and writes pixel diff artifacts.

When `--project` points at a Windows-targeted WinUI source project, the runner
does not mutate or build the original Windows project. It writes a compat shadow
project under the output directory, retargets it to the managed macOS facade,
compiles supported XAML with the local compiler, and writes
`project-ingestion.json` before launch. Unsupported project or XAML features
fail before shadow build with catalog-backed diagnostics.

`--strict-visual` fails the process when binding failures, resource failures,
unsupported facade APIs, unsupported visual painters, failed interactions, or
pixel metrics exceed the scenario thresholds. Without `--reference`, pixel diff
status is recorded as skipped so local strict fixture smoke runs can still
validate the supported subset and renderer output.

The public Windows reference workflow currently captures and compares one light
scenario from each strict fixture category: shell, interaction/binding, and
control gallery. Each category uploads reviewable `windows-reference.png`,
`mac-runtime.png`, `pixel-diff.png`, `pixel-diff.json`, and `visual-run.json`
artifacts.

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
Windows reference screenshots are the source of truth for scenario pixel
comparison. They are captured from generic public fixture content in public
GitHub Actions runs, not from private products or private screenshots.
