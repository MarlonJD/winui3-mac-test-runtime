# Visual Parity Evidence

This directory keeps public, reviewable visual harness evidence from the
`windows-native-screenshot.yml` workflow and local macOS strict visual runs.
The current GitHub workflow captures Windows-hosted reference PNGs from the
public WinUI fixture projects. The matching macOS runtime render, pixel diff,
and threshold failure checks are produced locally on a developer Mac with
`winui3-mac-runner` when a visual scenario needs review.

The latest full native reference artifact set was captured by public GitHub
Actions run
[`26790967052`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26790967052)
on commit `6d2fc9c`. It includes `public-admin-workbench-light`,
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

Passing strict comparison means the scenario stayed inside its documented
thresholds. It is not a claim of arbitrary WinUI 3 pixel-perfect compatibility.
It also is not a component-quality claim by itself: a whole screenshot can pass
while an individual `CommandBar`, `InfoBar`, `ListView`, icon, or resource
feature remains visibly weak or absent. Component lab scenarios must publish
`component-evidence.json` and keep visibly weak controls labeled `weak` or
`poor`; controls that only emit text or disappear in macOS screenshots must be
labeled `not-rendered` until native WinUI public reference artifacts and
reviewed macOS output justify a stronger grade.

Component visual grades:

| Grade | Meaning |
| --- | --- |
| `good` | Close to Windows with only minor text or edge differences. |
| `usable` | Recognizable and functionally correct, but native chrome differs. |
| `weak` | Structure exists, but important visual details are missing or simplified. |
| `poor` | Visibly wrong, collapsed, misplaced, or misleading. |
| `not-rendered` | No meaningful macOS component visual is present yet; this includes diagnostic-only, planned, Windows-only, unsupported, empty, or text-only output. |

## Current Public Evidence

| Scenario | Status | Changed pixels | Exact unchanged pixels | MAE | RMS | What matches | Known differences |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| `shell-light` | synthetic probe / passed | 4.00% | 96.00% | 1.57 | 14.20 | Navigation shell geometry, pane selection, text hierarchy, card surfaces. | Reference is synthetic probe output; text rasterization and edge anti-aliasing differ from Windows. |
| `interactions-light` | synthetic probe / passed | 31.69% | 68.31% | 4.06 | 17.77 | Interaction result state, list/text/image layout, binding-driven updates. | Reference is synthetic probe output; many pixels change because text and small control edges render differently on the two platforms. |
| `control-gallery-light` | synthetic probe / passed | 6.49% | 93.51% | 1.67 | 12.69 | Supported public controls, high-level spacing, checked/progress/info states. | Reference is synthetic probe output; native control chrome, exact typography, and Fluent focus/hover details are approximated. |
| `public-admin-workbench-light` | native WinUI reference / failed | 100.00% | 0.00% | 9.72 | 35.87 | Windows-targeted source ingestion, selected page, text content, and interaction state. | Native WinUI shows command surfaces, filter box, InfoBar, selected list row, and button chrome that the macOS renderer does not yet render; changed pixels exceed the `45%` threshold. |

Component lab scenario artifacts are produced for:
`component-basic-input-light`, `component-text-forms-light`,
`component-collections-light`, `component-dialogs-flyouts-light`,
`component-commands-menus-light`, `component-navigation-workbench-light`,
`component-status-pickers-light`, and `component-layout-media-light`.
Each artifact folder includes `component-evidence.json` alongside
`visual-run.json` when the native reference is compared locally against the
macOS runtime.

Native Windows reference artifacts are also produced for the public application
corpus scenarios: `public-admin-workbench-light`,
`public-admin-workbench-deferred-light`, `production-smoke-light`,
`production-e2e-workbench-light`, `single-window-continued-light`,
`settings-form-saved-light`, `resource-catalog-light`,
`resource-catalog-dark`, and `resource-catalog-high-contrast`.

## Public Admin Workbench Example

These files are copied from the public native WinUI workflow artifact for
`public-admin-workbench-light`. The `windows-reference.png` file is a native
WinUI Windows capture of
`fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj`, and
`windows-reference.json` records `referenceSource: native-winui`.

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI Windows reference](examples/public-admin-workbench-light/windows-reference.png) | ![macOS runtime](examples/public-admin-workbench-light/mac-runtime.png) | ![Pixel diff](examples/public-admin-workbench-light/pixel-diff.png) |

The native comparison currently fails: `100.00%` changed pixels against the
`45%` threshold, MAE `9.72`, and RMS `35.87`. This is an honest failure against
the corrected source of truth. The macOS render preserves the broad page and
text content, but native command surfaces, filter box, InfoBar, selected list
row, button chrome, focus states, shadows, and richer list/detail painters still
need cataloged implementation work before stronger parity claims can be made.

## Component Parity Lab Examples

These examples show native WinUI Windows fixture captures beside the current
macOS runtime rendering from this library. The visual tables are meant to make
the state of the component lab easy to inspect in the repository. Current
scenario JSON and freshly generated `component-evidence.json` artifacts are the
component-level source of truth. Component grades must not be promoted until
native WinUI Windows reference artifacts and reviewed macOS output justify the
claim.

### Basic Input

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI basic input reference](examples/component-basic-input-light/windows-reference.png) | ![macOS basic input component runtime](examples/component-basic-input-light/mac-runtime.png) | ![Basic input pixel diff](examples/component-basic-input-light/pixel-diff.png) |

`component-basic-input-light` records 13 component requirements and all 13 are
`not-rendered`. Native WinUI shows the button, toggle button, checkbox, radio
button, combo box, repeat/hyperlink/dropdown/split/toggle-split controls,
slider, toggle switch, and rating control; the macOS screenshot emits text-only
output for the controls. The native comparison fails with `42.07%` changed
pixels against the `18%` threshold, MAE `9.92`, and RMS `38.84`.

### Commands And Menus

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI commands and menus reference](examples/component-commands-menus-light/windows-reference.png) | ![macOS commands and menus component runtime](examples/component-commands-menus-light/mac-runtime.png) | ![Commands and menus pixel diff](examples/component-commands-menus-light/pixel-diff.png) |

`component-commands-menus-light` records 8 component requirements and all 8 are
`not-rendered`. Native WinUI shows the command bar, AppBarButton icons, command
content, flyout/menu/menu bar/context menu rows, and buttons; the macOS
screenshot shows command result text without native command chrome. The native
comparison fails with `40.68%` changed pixels against the `24%` threshold, MAE
`8.45`, and RMS `35.23`.

### Layout, Media, And Resources

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI layout and media reference](examples/component-layout-media-light/windows-reference.png) | ![macOS layout and media component runtime](examples/component-layout-media-light/mac-runtime.png) | ![Layout and media pixel diff](examples/component-layout-media-light/pixel-diff.png) |

`component-layout-media-light` records 28 component or source-feature
requirements: 4 resource smoke rows are `usable`, and 24 controls/features are
`not-rendered`. Native WinUI shows layout/resource rows, icon glyphs, colors,
brushes, corner radius, expander, annotated scroll bar, split view, shapes, and
other diagnostics; the macOS screenshot is text-only or absent for those
component visuals. The native comparison fails with `45.83%` changed pixels
against the `24%` threshold, MAE `10.48`, and RMS `39.27`.

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
6. Update the relevant example folder only when the artifact is public and does
   not contain private names, private screenshots, secrets, or proprietary
   fixture content. Production visual examples must come from native WinUI
   reference provenance; synthetic probe examples must remain labeled as smoke
   evidence.
