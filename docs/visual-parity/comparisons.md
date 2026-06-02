# Visual Comparison Examples

This file collects every checked-in visual comparison under
`docs/visual-parity/examples/`.

These examples are **historical visual-review fixtures**. They show the native
WinUI Windows reference, the checked-in macOS runtime render, and the pixel
diff that was copied into the repository. They are useful for reviewing older
renderer gaps, but they are not the current support or visual-quality grade
source.
They also should not be shown as proof that the current renderer is empty:
newer local artifacts under `artifacts/winui3-mac` render more component
scaffolding than these checked-in examples, but still do not reach native WinUI
visual fidelity.

Current support status is sourced from fresh
`component-evidence.json`, strict scenario results, catalog status,
interaction/accessibility evidence, and native-reference provenance. See
`docs/release/final-production-gate.md`,
`docs/release/production-evidence-view.md`,
`docs/compatibility/component-support.md`, and
`docs/compatibility/production-component-targets.md`.

As of the checked-in public component-quality dashboard, the current macOS
renderer is a usable harness scaffold rather than a high-fidelity WinUI
renderer. The public evidence set contains 58 component rows: 40 `usable` and
18 `not-rendered`. All public rows now have native/macOS/diff crop triptychs,
but many controls still have simplified chrome, missing native states, missing
templates, incomplete popup placement, or diagnostic-only rendering.

The generated dashboard at `docs/visual-parity/component-quality-dashboard.json`
is the current checked-in component-quality gate. It is blocked with 58/58
public example rows below the native-quality target because the rows lack final
native-quality grades and manual inspection metadata.

## Provenance

The checked-in comparison examples come from native WinUI public fixture
references captured by public GitHub Actions run
[`26777029415`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26777029415)
on commit `95e8d7d`. Each `windows-reference.json` records
`referenceSource: native-winui`, the workflow run ID, commit SHA, runner image,
viewport, theme, title match, capture mode, and dimensions.

The latest full native reference workflow evidence is newer:
[`26792033793`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26792033793)
on commit `3c929f4`. Those latest workflow artifacts are not all checked into
this comparison gallery.

## Summary

Historical checked-in examples:

| Scenario | Checked-in status | Changed pixels | Threshold | MAE | RMS | Component evidence |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `public-admin-workbench-light` | failed | 99.988381% | 45% | 9.678085 | 36.397928 | 9 `usable` |
| `component-basic-input-light` | failed | 30.145914% | 18% | 9.611153 | 39.380451 | 13 `usable`, zero `not-rendered` |
| `component-commands-menus-light` | failed | 28.441283% | 24% | 7.859141 | 35.183863 | 5 `usable`, 3 `not-rendered` |
| `component-layout-media-light` | failed | 43.277129% | 24% | 13.470821 | 45.268636 | 13 `usable`, 15 `not-rendered` |

Current inspected local macOS artifacts:

| Scenario | Current status | Component evidence | Interpretation |
| --- | --- | --- | --- |
| `component-basic-input-light` / `component-basic-input-checked-light` | failed against native reference for the base light scenario; checked-state smoke remains historical | Base scenario has 13 `usable` rows and zero `not-rendered`; checked-state scenario has 3 `usable` | Recognizable controls with improved static chrome; final native inspection, automation state, and interaction coverage remain incomplete. |
| `component-commands-menus-light` | passed | 5 `usable`, 3 `not-rendered` | Command and flyout scaffold exists; native menu/command fidelity remains incomplete. |
| `component-status-pickers-light` / `component-status-pickers-loading-light` / `component-status-pickers-success-light` | passed | Base scenario has 3 `usable`, 7 planned `not-rendered`; loading and success scenarios add 4 `usable` | Status/progress scaffold exists with regenerated success evidence; native animation and close/action areas remain gaps. |
| `component-layout-media-light` | passed | 13 `usable`, 15 planned/non-goal `not-rendered` | Layout/resource scaffold exists; media, web, ink, and materials remain excluded or planned. |
| `public-admin-workbench-light` | passed | 9 `usable` | Workbench scaffold is usable for smoke/E2E checks; it is not native-quality parity. |

## Public Admin Workbench

Scenario: `public-admin-workbench-light`

Checked-in status: failed. The macOS render preserves the broad page, selected
route, text content, and command-click assertion, but it does not match native
WinUI command surfaces, filter box, InfoBar, selected list row, button chrome,
focus states, shadows, or richer list/detail painters.

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI public admin reference](examples/public-admin-workbench-light/windows-reference.png) | ![macOS public admin runtime](examples/public-admin-workbench-light/mac-runtime.png) | ![Public admin pixel diff](examples/public-admin-workbench-light/pixel-diff.png) |

Artifacts:

- `examples/public-admin-workbench-light/windows-reference.json`
- `examples/public-admin-workbench-light/visual-run.json`
- `examples/public-admin-workbench-light/pixel-diff.json`

## Basic Input

Scenario: `component-basic-input-light`

Checked-in status: failed. Native WinUI shows the button, toggle button,
checkbox, radio button, combo box, repeat/hyperlink/dropdown/split/toggle-split
controls, slider, toggle switch, and rating control. The checked-in macOS
screenshot emits text-only output for those controls. Fresh component evidence
controls current production grades.

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI basic input reference](examples/component-basic-input-light/windows-reference.png) | ![macOS basic input runtime](examples/component-basic-input-light/mac-runtime.png) | ![Basic input pixel diff](examples/component-basic-input-light/pixel-diff.png) |

Artifacts:

- `examples/component-basic-input-light/windows-reference.json`
- `examples/component-basic-input-light/component-evidence.json`
- `examples/component-basic-input-light/visual-run.json`
- `examples/component-basic-input-light/pixel-diff.json`

## Commands And Menus

Scenario: `component-commands-menus-light`

Checked-in status: failed. Native WinUI shows the command bar, AppBarButton
icons, command content, flyout/menu/menu bar/context menu rows, and buttons.
The checked-in macOS screenshot shows command result text without native command
chrome. Fresh component evidence controls current production grades.

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI commands reference](examples/component-commands-menus-light/windows-reference.png) | ![macOS commands runtime](examples/component-commands-menus-light/mac-runtime.png) | ![Commands pixel diff](examples/component-commands-menus-light/pixel-diff.png) |

Artifacts:

- `examples/component-commands-menus-light/windows-reference.json`
- `examples/component-commands-menus-light/component-evidence.json`
- `examples/component-commands-menus-light/visual-run.json`
- `examples/component-commands-menus-light/pixel-diff.json`

## Layout, Media, And Resources

Scenario: `component-layout-media-light`

Checked-in status: failed. Native WinUI shows layout/resource rows, icon
glyphs, colors, brushes, corner radius, expander, annotated scroll bar, split
view, shapes, and other diagnostics. The checked-in macOS screenshot is
text-only or absent for many component visuals. Fresh component evidence
controls current production grades.

| Native WinUI Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Native WinUI layout reference](examples/component-layout-media-light/windows-reference.png) | ![macOS layout runtime](examples/component-layout-media-light/mac-runtime.png) | ![Layout pixel diff](examples/component-layout-media-light/pixel-diff.png) |

Artifacts:

- `examples/component-layout-media-light/windows-reference.json`
- `examples/component-layout-media-light/component-evidence.json`
- `examples/component-layout-media-light/visual-run.json`
- `examples/component-layout-media-light/pixel-diff.json`
