# Visual Parity Evidence

This directory keeps public, reviewable visual harness evidence from the
`windows-native-screenshot.yml` workflow and local macOS strict visual runs.
The current GitHub workflow captures Windows-hosted reference PNGs from the
public WinUI fixture projects. The matching macOS runtime render, pixel diff,
and threshold failure checks are produced locally on a developer Mac with
`winui3-mac-runner` when a visual scenario needs review.

For the current single-page production evidence summary, including catalog
counts, Ring 0/Ring 1 status, latest recorded workflow IDs, strict scenario
results, and checked-in visual examples, see
`docs/release/production-evidence-view.md`.

The latest full native reference artifact set was captured by public GitHub
Actions run
[`26792033793`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26792033793)
on commit `3c929f4`. It includes `public-admin-workbench-light`,
`public-admin-workbench-deferred-light`, all light Ring 0 and Ring 1 component
lab scenarios, `production-smoke-light`, and
`production-e2e-workbench-light`. It also includes native WinUI corpus
references for `single-window-continued-light`, `settings-form-saved-light`,
`resource-catalog-light`, `resource-catalog-dark`, and
`resource-catalog-high-contrast`. Every downloaded `windows-reference.json`
records `referenceSource: native-winui`, `titleMatched: true`, the workflow run
ID, commit SHA, runner image, viewport, theme, title, capture mode, and image
dimensions.

The checked-in public admin workbench and component parity lab examples come
from native WinUI public fixture references captured by public GitHub Actions
run
[`26777029415`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26777029415)
on commit `95e8d7d`. Each example includes `windows-reference.json` provenance
with `referenceSource: native-winui`, the public fixture project path, scenario
path/name, commit SHA, workflow run ID, runner image, viewport, scale, theme,
window title, capture mode, and dimensions. The synthetic probe examples for
`shell-light`, `interactions-light`, and `control-gallery-light` remain
smoke-only harness evidence.

## How To Read The Metrics

Lower values are better.

| Metric | Meaning |
| --- | --- |
| `changedPixelPercentage` | Percent of pixels that are not byte-identical between reference and macOS output. Anti-aliasing and text rasterization can move this number even when the layout is close. |
| `meanAbsoluteError` | Average per-channel color error across the image. This is usually the best quick signal for broad visual similarity. |
| `rootMeanSquaredError` | Error metric that penalizes larger color differences more strongly. |
| `maxChannelDelta` | Largest single-channel color delta. A value of `255` can be caused by one high-contrast edge and does not imply the whole image is unrelated. |

Passing strict comparison means the scenario stayed inside its documented smoke
thresholds. It is not a claim of arbitrary WinUI 3 pixel-perfect compatibility
or production visual fidelity. It also is not a component-quality claim by
itself: a whole screenshot can pass while an individual `CommandBar`,
`InfoBar`, `ListView`, icon, or resource feature remains visibly weak,
simplified, or absent. Component lab scenarios must publish
`component-evidence.json` and keep visibly weak controls labeled `weak` or
`poor`; controls that only emit text or disappear in macOS screenshots must be
labeled `not-rendered` until native WinUI public reference artifacts and
reviewed macOS output justify a stronger grade. The current support subset is
limited to component rows whose current evidence meets the minimum harness
grade in `docs/compatibility/production-component-targets.md`.

Component visual grades:

| Grade | Meaning |
| --- | --- |
| `good` | Close to Windows with only minor text or edge differences. |
| `usable` | Recognizable and functionally testable, but native chrome differs; this is a harness grade, not a fidelity grade. |
| `weak` | Structure exists, but important visual details are missing or simplified. |
| `poor` | Visibly wrong, collapsed, misplaced, or misleading. |
| `not-rendered` | No meaningful macOS component visual is present yet; this includes diagnostic-only, planned, Windows-only, unsupported, empty, or text-only output. |

## Checked-In Comparisons

The checked-in PNG comparisons are collected in
`docs/visual-parity/comparisons.md`. They are historical visual-review fixtures:
each one records a native WinUI Windows reference, a local macOS runtime render,
and a pixel diff from the checked-in example set.

Do not read those checked-in comparisons as current support component
grades. Current support status is determined by fresh scenario
`component-evidence.json`, catalog status, required interactions,
accessibility export, native-reference provenance, and manual screenshot
inspection. When fresh evidence promotes a component to `usable`, the claim
applies only to that component's documented harness subset.

The latest inspected local macOS artifact set under `artifacts/winui3-mac`
contains 143 component evidence rows: 86 `usable` and 57 `not-rendered`.
Those rows show that the renderer supports narrow smoke/E2E scaffolds, but
they do not establish native WinUI visual fidelity.

`component-quality-dashboard.json` is the checked-in generated quality gate for
the public example evidence. It lists every checked-in component row, its
current grade, native-quality target, owner family, required scenario, and
remaining blocker. The current dashboard is blocked: 49/49 checked-in public
component rows still lack native-quality completion evidence, including macOS
component crops, native WinUI reference crops, component diff metrics, and
native reference provenance, plus manual screenshot inspection metadata.

Component lab scenario artifacts are produced for:
`component-basic-input-light`, `component-text-forms-light`,
`component-collections-light`, `component-dialogs-flyouts-light`,
`component-commands-menus-light`, `component-navigation-workbench-light`,
`component-status-pickers-light`, and `component-layout-media-light`.

Native Windows reference artifacts are also produced for the public application
corpus scenarios: `public-admin-workbench-light`,
`public-admin-workbench-deferred-light`, `production-smoke-light`,
`production-e2e-workbench-light`, `single-window-continued-light`,
`settings-form-saved-light`, `resource-catalog-light`,
`resource-catalog-dark`, and `resource-catalog-high-contrast`.

## Updating Evidence

When a visual scenario or renderer behavior changes:

1. Run the local strict scenario without a reference.
2. Trigger `windows-native-screenshot.yml` on the public repository.
3. Download the `windows-reference-screenshots` artifact.
4. Re-run the matching local strict scenario with `--reference` and
   `--diff-output`.
5. Inspect `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`, and
   `visual-run.json`; inspect `component-evidence.json` for component lab
   scenarios, and inspect reference provenance when the reference supplies it.
6. Run `winui3-mac-runner component-quality-dashboard` and inspect the updated
   row blockers before promoting any claim.
7. Update the relevant example folder only when the artifact is public and does
   not contain private names, private screenshots, secrets, or proprietary
   fixture content. Production visual examples must come from native WinUI
   reference provenance; synthetic probe examples must remain labeled as smoke
   evidence.
