# Visual Comparison Examples

This file collects every checked-in visual comparison under
`docs/visual-parity/examples/`.

These examples are **historical visual-review fixtures**. They show the native
WinUI Windows reference, the checked-in macOS runtime render, and the pixel
diff that was copied into the repository. They are useful for reviewing older
renderer gaps, but they are not the current production component grade source.

Current production component status is sourced from fresh
`component-evidence.json`, strict scenario results, catalog status,
interaction/accessibility evidence, and native-reference provenance. See
`docs/release/final-production-gate.md`,
`docs/release/production-evidence-view.md`,
`docs/compatibility/component-support.md`, and
`docs/compatibility/production-component-targets.md`.

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

| Scenario | Checked-in status | Changed pixels | Threshold | MAE | RMS | Component evidence |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `public-admin-workbench-light` | failed | 100.00% | 45% | 9.72 | 35.87 | n/a |
| `component-basic-input-light` | failed | 42.07% | 18% | 9.92 | 38.84 | 13 `not-rendered` |
| `component-commands-menus-light` | failed | 40.68% | 24% | 8.45 | 35.23 | 8 `not-rendered` |
| `component-layout-media-light` | failed | 45.83% | 24% | 10.48 | 39.27 | 4 `usable`, 24 `not-rendered` |

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
