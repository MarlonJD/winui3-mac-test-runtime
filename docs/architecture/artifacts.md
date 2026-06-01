# Runtime Artifacts

The macOS runner writes stable JSON artifacts under the requested output
directory. The files are intended for automation first and human inspection
second.

## Files

- `run.json`: run metadata, host details, Wine optionality, and artifact paths.
- `tree.json`: logical UI tree with stable type names, element names, selected
  state, visibility, focus state, and important content properties.
- `accessibility.json`: role/name/label tree derived from the logical tree.
- `binding-failures.json`: binding paths that could not be resolved or applied.
- `resource-failures.json`: static or theme resources that could not be resolved.
- `unsupported-apis.json`: facade APIs that were touched but are not implemented.
- `diagnostics.sarif`: warning-level diagnostics derived from binding, resource,
  and unsupported API reports.
- `interactions.json`: emitted when `--script` is provided; records every
  scripted action and its result.
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

`--strict-visual` fails the process when binding failures, resource failures,
unsupported facade APIs, unsupported visual painters, failed interactions, or
pixel metrics exceed the scenario thresholds. Without `--reference`, pixel diff
status is recorded as skipped so local strict fixture smoke runs can still
validate the supported subset and renderer output.

## Compatibility Position

Artifacts describe the compatibility runtime's supported subset. They are not a
claim of full WinUI 3 compatibility or Windows binary compatibility.
Unsupported APIs are reported structurally so callers can decide whether to fail
a smoke run or track the gap as compatibility debt.
Snapshot output is deterministic smoke evidence for a supported control subset,
not a full Fluent renderer.
Windows reference screenshots are the source of truth for scenario pixel
comparison. They are captured from generic public fixture content in public
GitHub Actions runs, not from private products or private screenshots.
